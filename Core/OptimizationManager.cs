using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MEROptimizer.Components;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace MEROptimizer.Core
{
    public class OptimizationManager
    {
        public bool excludeCollidables;
        public List<string> excludedNames = new();
        public bool hideDistantPrimitives;
        public bool shouldSpectatorsBeAffectedByPDS;
        public bool ShouldTutorialsBeAffectedByDistanceSpawning;
        public float distanceRequiredForUnspawning;
        public Dictionary<string, float> CustomSchematicSpawnDistance = new();
        public float maxDistanceForPrimitiveCluster;
        public int maxPrimitivesPerCluster;
        public List<string> excludedNamesForUnspawningDistantObjects = new();
        public float numberOfPrimitivePerSpawn;
        public float MinimumSizeBeforeBeingBigPrimitive;
        
        public bool isDynamiclyDisabled = false;
        public static bool IsDebug = false;
        
        public List<OptimizedSchematic> optimizedSchematics = new();
        
        public void Load(Config config)
        {
            IsDebug = config.Debug;
            excludeCollidables = config.OptimizeOnlyNonCollidable;
            excludedNames = config.excludeObjects.Select(n => n.ToLower()).ToList();
            hideDistantPrimitives = config.ClusterizeSchematic;
            distanceRequiredForUnspawning = config.SpawnDistance;
            excludedNamesForUnspawningDistantObjects = config.excludeUnspawningDistantObjects;
            maxDistanceForPrimitiveCluster = config.MaxDistanceForPrimitiveCluster;
            maxPrimitivesPerCluster = config.MaxPrimitivesPerCluster;
            shouldSpectatorsBeAffectedByPDS = config.ShouldSpectatorBeAffectedByDistanceSpawning;
            numberOfPrimitivePerSpawn = config.numberOfPrimitivePerSpawn;
            MinimumSizeBeforeBeingBigPrimitive = config.MinimumSizeBeforeBeingBigPrimitive;
            ShouldTutorialsBeAffectedByDistanceSpawning = config.ShouldTutorialsBeAffectedByDistanceSpawning;
            CustomSchematicSpawnDistance = config.CustomSchematicSpawnDistance;
        }

        public void Unload() => Clear();

        public void Clear()
        {
            foreach (var s in optimizedSchematics) s?.Destroy();
            optimizedSchematics.Clear();
        }
        
        public void AddPlayerTrigger(Player player)
        {
            if (player == null) return;
            
            GameObject playerTrigger = new GameObject($"{player.PlayerId}_MERO_TRIGGER");
            playerTrigger.tag = "Player";
            
            Rigidbody rb = playerTrigger.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            
            playerTrigger.AddComponent<BoxCollider>().size = new Vector3(1, 2, 1);
            playerTrigger.AddComponent<PlayerTrigger>().player = player;
        }
    }
}