using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using MEC;
using MEROptimizer.API.ClientSideObjects;
using MEROptimizer.Components;
using MEROptimizer.Components.ClientSideObjects;
using MEROptimizer.Core;
using ProjectMER.Events.Arguments;
using ProjectMER.Features.Objects;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace MEROptimizer.Events;

public class SchematicHandler
{
    private readonly OptimizationManager _manager;

    public SchematicHandler(OptimizationManager manager)
    {
        _manager = manager;
    }

    public void OnSchematicSpawned(SchematicSpawnedEventArgs ev)
    {
        if (_manager.isDynamiclyDisabled)
        {
            Logger.Warn($"Skipping optimization of {ev.Schematic.name} (Plugin disabled)");
            return;
        }

        if (ev.Schematic == null || _manager.excludedNames.Any(n => ev.Schematic.Name.ToLower().Contains(n)))
            return;
        
        List<Transform> parentsToExclude = ev.Schematic.GetComponentsInChildren<Animator>().Select(anim => anim.transform).ToList();
        
        var primitivesToOptimize = new Dictionary<PrimitiveObjectToy, bool>();
        var lightsToOptimize = new Dictionary<LightSourceToy, bool>();
        var capybaraToOptimize = new Dictionary<CapybaraToy, bool>();
        
        CollectToOptimize(ev.Schematic.transform, parentsToExclude, primitivesToOptimize, true, p => !(_manager.excludeCollidables && p.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable)) && p.PrimitiveFlags != PrimitiveFlags.None);
        CollectToOptimize(ev.Schematic.transform, parentsToExclude, lightsToOptimize, true);
        CollectToOptimize(ev.Schematic.transform, parentsToExclude, capybaraToOptimize, true);

        if (primitivesToOptimize.Count == 0 && lightsToOptimize.Count == 0 && capybaraToOptimize.Count == 0) return;

        Logger.Info($"[MERO] {ev.Schematic.Name}: Prims={primitivesToOptimize.Count}, Lights={lightsToOptimize.Count}, Capys={capybaraToOptimize.Count}");
        
        var clientPrims = ProcessPrimitives(primitivesToOptimize, out List<Collider> colliders);
        var clientLights = ProcessLights(lightsToOptimize);
        var clientCapys = ProcessCapybaras(capybaraToOptimize);
        
        float spawnDist = _manager.CustomSchematicSpawnDistance.TryGetValue(ev.Schematic.Name, out float custom) 
            ? custom : _manager.distanceRequiredForUnspawning;

        OptimizedSchematic optimized = new OptimizedSchematic(
            ev.Schematic,
            colliders,
            clientPrims,
            clientLights,
            clientCapys,
            _manager.hideDistantPrimitives,
            spawnDist,
            _manager.excludedNamesForUnspawningDistantObjects,
            _manager.maxDistanceForPrimitiveCluster,
            _manager.maxPrimitivesPerCluster);

        _manager.optimizedSchematics.Add(optimized);
        
        DestroyOriginals(primitivesToOptimize.Keys, lightsToOptimize.Keys, capybaraToOptimize.Keys);
        
        Timing.CallDelayed(1f, () =>
        {

            if (ev.Schematic == null || ev.Schematic == null) return;
            optimized.schematicServerSidePrimitiveCount = ev.Schematic.GetComponentsInChildren<PrimitiveObjectToy>().Where(p => p != null).Count();
            optimized.schematicServerSidePrimitiveEmptiesCount = ev.Schematic.GetComponentsInChildren<PrimitiveObjectToy>().Where(p => p != null && p.PrimitiveFlags == PrimitiveFlags.None).Count();

        });
        
        Timing.RunCoroutine(RunCleanupPass(ev.Schematic, optimized));
    }

    public void OnSchematicDestroyed(SchematicDestroyedEventArgs ev)
    {
        _manager.optimizedSchematics.RemoveAll(s =>
        {
            if (s.schematic != null && s.schematic != ev.Schematic) return false;
            s.Destroy();
            return true;
        });
    }
    
    private void CollectToOptimize<T>(Transform parent, List<Transform> excluded, Dictionary<T, bool> results, bool cluster, Func<T, bool> extraFilter = null) where T : Component
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null || excluded.Contains(child)) continue;

            bool currentCluster = cluster;
            
            if (currentCluster && _manager.excludedNamesForUnspawningDistantObjects.Any(n => child.name.Contains(n)))
                currentCluster = false;
            
            if (_manager.excludedNames.Any(n => child.name.ToLower().Contains(n.ToLower()))) continue;

            if (child.TryGetComponent<T>(out var comp))
            {
                if (extraFilter == null || extraFilter(comp))
                {
                    results.Add(comp, currentCluster);
                }
            }
            
            CollectToOptimize(child, excluded, results, currentCluster, extraFilter);
        }
    }

    private Dictionary<ClientSidePrimitive, bool> ProcessPrimitives(Dictionary<PrimitiveObjectToy, bool> raw, out List<Collider> colliders)
    {
        var dict = new Dictionary<ClientSidePrimitive, bool>();
        colliders = new List<Collider>();

        foreach (var kvp in raw)
        {
            var p = kvp.Key;
            var cp = new ClientSidePrimitive(p.transform.position, p.transform.rotation, p.transform.lossyScale, p.PrimitiveType, p.NetworkMaterialColor, p.PrimitiveFlags);
            dict.Add(cp, kvp.Value);

            if (p.PrimitiveFlags.HasFlag(PrimitiveFlags.Collidable))
            {
                GameObject colObj = new GameObject($"[MEROCOLLIDER] {p.name}");
                colObj.transform.position = p.transform.position;
                colObj.transform.rotation = p.transform.rotation;
                colObj.transform.localScale = new Vector3(Mathf.Abs(p.transform.lossyScale.x), Mathf.Abs(p.transform.lossyScale.y), Mathf.Abs(p.transform.lossyScale.z));
                colObj.layer = p.NetworkMaterialColor.a < 1 ? LayerMask.NameToLayer("Glass") : 0;
                
                var mc = colObj.AddComponent<MeshCollider>();
                mc.sharedMesh = PrimitiveObjectToy.PrimitiveTypeToMesh[p.PrimitiveType];
                colliders.Add(mc);
            }
        }
        return dict;
    }

    private Dictionary<ClientSideLight, bool> ProcessLights(Dictionary<LightSourceToy, bool> raw) =>
        raw.ToDictionary(kvp => new ClientSideLight(kvp.Key.transform.position, kvp.Key.transform.rotation, kvp.Key.transform.lossyScale, 
            kvp.Key.NetworkLightIntensity, kvp.Key.NetworkLightRange, kvp.Key.NetworkLightColor, kvp.Key.NetworkShadowType, 
            kvp.Key.NetworkShadowStrength, kvp.Key.NetworkLightType, kvp.Key.NetworkSpotAngle, kvp.Key.NetworkInnerSpotAngle, 0), kvp => kvp.Value);

    private Dictionary<ClientSideCapybara, bool> ProcessCapybaras(Dictionary<CapybaraToy, bool> raw) =>
        raw.ToDictionary(kvp => new ClientSideCapybara(kvp.Key.transform.position, kvp.Key.transform.rotation, kvp.Key.transform.lossyScale, kvp.Key.CollisionsEnabled, 0), kvp => kvp.Value);

    private void DestroyOriginals(IEnumerable<PrimitiveObjectToy> primitive, IEnumerable<LightSourceToy> light, IEnumerable<CapybaraToy> capybara)
    {
        foreach (PrimitiveObjectToy item in primitive) GameObject.Destroy(item.gameObject);
        foreach (LightSourceToy item in light) GameObject.Destroy(item.gameObject);
        foreach (CapybaraToy item in capybara) GameObject.Destroy(item.gameObject);
    }

    private IEnumerator<float> RunCleanupPass(SchematicObject schematicObj, OptimizedSchematic opt)
    {
        yield return Timing.WaitForSeconds(0.1f);
        int total = 0;
        for (int pass = 0; pass < 30; pass++)
        {
            if (schematicObj == null) break;
            var targets = schematicObj.GetComponentsInChildren<PrimitiveObjectToy>(true)
                .Where(p => p != null && p.PrimitiveFlags == PrimitiveFlags.None && p.transform.childCount == 0).ToList();
            
            if (targets.Count == 0) break;
            foreach (var t in targets) { GameObject.Destroy(t.gameObject); total++; }
            yield return Timing.WaitForSeconds(0.1f);
        }
        Logger.Info($"[MERO] Cleanup finished: {total} empty pivots removed.");
    }
}