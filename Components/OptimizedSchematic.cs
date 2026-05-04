using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MEC;
using MEROptimizer.API.ClientSideObjects;
using MEROptimizer.Components.ClientSideObjects;
using PlayerRoles;
using ProjectMER.Features.Objects;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace MEROptimizer.Components
{
  public class OptimizedSchematic
  {
    public SchematicObject schematic { get; set; }

    private string schematicName;

    public List<Collider> colliders { get; set; }

    public List<ClientSidePrimitive> nonClusteredPrimitives { get; set; }
    public List<ClientSideLight> nonClusteredLights { get; set; }
    public List<ClientSideCapybara> nonClusteredCapybaras { get; set; }

    public List<PrimitiveCluster> primitiveClusters { get; set; }

    public DateTime spawnTime { get; set; }

    public int schematicServerSidePrimitiveEmptiesCount = -1;

    public int schematicServerSidePrimitiveCount { get; set; } = -1;

    public int GetTotalPrimitiveCount()
    {
      int count = nonClusteredPrimitives.Count;

      foreach (PrimitiveCluster cluster in primitiveClusters)
      {
        count += cluster.primitives.Count;
      }

      return count;
    }

    public OptimizedSchematic(
      SchematicObject schematic,
      List<Collider> colliders,
      Dictionary<ClientSidePrimitive, bool> primitives,
      Dictionary<ClientSideLight, bool> lights,
      Dictionary<ClientSideCapybara, bool> capybaras,
      bool doClusters = false,
      float distance = 50,
      List<string> excludedUnspawnObjects = null,
      float maxDistanceForPrimitiveCluster = 2.5f,
      int maxPrimitivesPerCluster = 100)
    {
      this.schematic = schematic;
      this.colliders = colliders;
      spawnTime = DateTime.Now;

      schematicName = schematic.name;

      nonClusteredPrimitives = new List<ClientSidePrimitive>();
      nonClusteredLights = new List<ClientSideLight>();
      primitiveClusters = new List<PrimitiveCluster>();

      GenerateClustersAndSpawn(
        doClusters,
        primitives,
        lights,
        capybaras,
        distance,
        excludedUnspawnObjects,
        maxDistanceForPrimitiveCluster,
        maxPrimitivesPerCluster);
    }

    private void GenerateClustersAndSpawn(
      bool doClusters,
      Dictionary<ClientSidePrimitive, bool> primitives,
      Dictionary<ClientSideLight, bool> lights,
      Dictionary<ClientSideCapybara, bool> capybaras,
      float distance,
      List<string> excludedUnspawnObjects,
      float maxDistanceForPrimitiveCluster,
      int maxPrimitivesPerCluster)
    {
      if (!doClusters)
      {
        foreach (ClientSidePrimitive primitive in primitives.Keys)
        {
          nonClusteredPrimitives.Add(primitive);
        }
        foreach (ClientSideLight light in lights.Keys)
        {
          nonClusteredLights.Add(light);
        }
        foreach (ClientSideCapybara capybara in capybaras.Keys)
        {
          nonClusteredCapybaras.Add(capybara);
        }
      }
      else
      {
        List<ClientSideLight> clusteredLights = new();

        foreach (ClientSideLight light in lights.Keys.ToList())
        {
          if (!lights[light])
          {
            nonClusteredLights.Add(light);
            lights.Remove(light);
          }
          else
          {
            clusteredLights.Add(light);
          }
        }
        
        List<ClientSideCapybara> clusteredCapybaras = new();

        foreach (ClientSideCapybara capybara in capybaras.Keys.ToList())
        {
          if (!capybaras[capybara])
          {
            nonClusteredCapybaras.Add(capybara);
            capybaras.Remove(capybara);
          }
          else
          {
            clusteredCapybaras.Add(capybara);
          }
        }
        
        // Remove non clustered primitives and big objects
        foreach (ClientSidePrimitive primitive in primitives.Keys.ToList())
        {
          if (!primitives[primitive])
          {
            nonClusteredPrimitives.Add(primitive);
            primitives.Remove(primitive);
          }
          else
          {
            if (Plugin.Instance.Manager.MinimumSizeBeforeBeingBigPrimitive > 0)
            {
              Vector3 size = primitive.Scale;

              if (Math.Abs(size.x) + Math.Abs(size.y) + Math.Abs(size.z) > Plugin.Instance.Manager.MinimumSizeBeforeBeingBigPrimitive)
              {
                nonClusteredPrimitives.Add(primitive);
                primitives.Remove(primitive);
              }
            }
          }
        }

        if (!primitives.IsEmpty())
        {

          // Calculate the center of the schematic, where the first cluster will spawn
          Vector3 center3D = Vector3.zero;
          foreach (ClientSidePrimitive p in primitives.Keys)
          {
            center3D += p.Position;
          }

          center3D /= primitives.Count;

          // Sort the primitives by their distance with the center
          List<ClientSidePrimitive> sortedPrimitives = primitives.Keys.ToList();
          sortedPrimitives = sortedPrimitives.OrderBy(s => Vector3.Distance(s.Position, center3D)).ToList();

          Dictionary<int, List<ClientSidePrimitive>> clusters = new Dictionary<int, List<ClientSidePrimitive>>();

          int clusterNumber = 1;

          // Creates clusters, add the primitives to the clusters until all clusters are generated
          while (sortedPrimitives.Count > 0)
          {

            ClientSidePrimitive closestFromCenterPrimitive = sortedPrimitives.First();

            List<ClientSidePrimitive> clusterPrimitives = new List<ClientSidePrimitive>() { closestFromCenterPrimitive };

            List<ClientSidePrimitive> sortedPrimitiveByCluster = sortedPrimitives.ToList();

            Vector3 centerPos = closestFromCenterPrimitive.Position;

            // Keep all of the primitives where their distance correspond
            sortedPrimitiveByCluster.RemoveAll(p =>
            Vector3.Distance(p.Position, centerPos) > maxDistanceForPrimitiveCluster);

            // Remove excess primitives based on config
            if (sortedPrimitiveByCluster.Count > maxPrimitivesPerCluster)
            {
              sortedPrimitiveByCluster = sortedPrimitiveByCluster.OrderBy(s => Vector3.Distance(s.Position, centerPos)).ToList();
              sortedPrimitiveByCluster.RemoveRange(maxPrimitivesPerCluster, sortedPrimitiveByCluster.Count - maxPrimitivesPerCluster);
            }


            clusterPrimitives.AddRange(sortedPrimitiveByCluster);

            sortedPrimitives.RemoveAll(p => clusterPrimitives.Contains(p));

            // sort the primitives on their y value, so that the first to spawn will be the bottom ones

            clusterPrimitives = clusterPrimitives.OrderBy(p => p.Position.y).ToList();

            clusters.Add(clusterNumber++, clusterPrimitives);
          }

          //Creates the Gameobjects for the clusters
          foreach (KeyValuePair<int, List<ClientSidePrimitive>> cluster in clusters)
          {
            // Get the center of the cluster

            Vector3 center = Vector3.zero;
            foreach (ClientSidePrimitive primitive in cluster.Value)
            {
              center += primitive.Position;
            }

            center /= cluster.Value.Count;

            // Creates the GameObject

            GameObject gameObject = new GameObject($"[MERO] PrimitiveCluster_{schematic.name}_{cluster.Key}");

            gameObject.transform.position = center + new Vector3(0, 2000, 0);
            gameObject.transform.rotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;

            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = distance;
            collider.isTrigger = true;

            PrimitiveCluster primitiveCluster = gameObject.AddComponent<PrimitiveCluster>();
            primitiveCluster.id = cluster.Key;
            primitiveCluster.primitives = cluster.Value;

            primitiveClusters.Add(primitiveCluster);
          }
          
          foreach (ClientSideLight light in clusteredLights)
          {
            PrimitiveCluster nearestCluster = null;
            float nearestDistance = float.MaxValue;

            foreach (PrimitiveCluster cluster in primitiveClusters)
            {
              if (cluster == null)
                continue;

              Vector3 clusterCenter = cluster.transform.position - new Vector3(0, 2000, 0);
              float distanceToCluster = Vector3.Distance(light.Position, clusterCenter);

              if (distanceToCluster < nearestDistance)
              {
                nearestDistance = distanceToCluster;
                nearestCluster = cluster;
              }
            }

            if (nearestCluster != null && nearestDistance <= maxDistanceForPrimitiveCluster * 2f)
            {
              nearestCluster.lights.Add(light);
            }
            else
            {
              GameObject gameObject = new GameObject($"[MERO] LightCluster_{schematic.name}_{primitiveClusters.Count + 1}");

              gameObject.transform.position = light.Position + new Vector3(0, 2000, 0);
              gameObject.transform.rotation = Quaternion.identity;
              gameObject.transform.localScale = Vector3.one;

              SphereCollider collider = gameObject.AddComponent<SphereCollider>();
              collider.radius = distance;
              collider.isTrigger = true;

              PrimitiveCluster lightCluster = gameObject.AddComponent<PrimitiveCluster>();
              lightCluster.id = primitiveClusters.Count + 1;
              lightCluster.primitives = new List<ClientSidePrimitive>();
              lightCluster.lights = new List<ClientSideLight> { light };

              primitiveClusters.Add(lightCluster);
            }
          }
        }
      }

      // Spawn of primitives

      foreach (ClientSidePrimitive primitive in nonClusteredPrimitives)
      {
        primitive.SpawnForEveryone();
      }
      
      foreach (ClientSideLight light in nonClusteredLights)
      {
        light.SpawnForEveryone();
      }


      // Spawn clusters for custom chiantos roles

      Timing.CallDelayed(.5f, () =>
      {
        if (this == null) return;

        foreach (Player player in Player.List.Where(p => p != null && !p.IsNpc))
        {
          bool shouldSpawn = false;

          if (!Plugin.Instance.Manager.ShouldTutorialsBeAffectedByDistanceSpawning && player.Role == RoleTypeId.Tutorial)
            shouldSpawn = true;

          if (!Plugin.Instance.Manager.shouldSpectatorsBeAffectedByPDS && (player.Role == RoleTypeId.Spectator || player.Role == RoleTypeId.Overwatch))
            shouldSpawn = true;

          if (player.Role == RoleTypeId.Filmmaker || player.Role == RoleTypeId.Scp079)
            shouldSpawn = true;

          if (shouldSpawn)
          {
            foreach (PrimitiveCluster cluster in primitiveClusters)
            {
              if (cluster.instantSpawn)
              {
                cluster.SpawnFor(player);
              }
              else
              {
                // CORRECTION : On initialise l'index à 0 pour lancer le process d'Update
                cluster.awaitingSpawnIndex[player] = 0;
                cluster.spawning = true;
              }
            }
          }
        }
      });
    }

    public void RefreshFor(Player player)
    {
      HideFor(player, false);

      foreach (ClientSidePrimitive primitive in nonClusteredPrimitives)
      {
        primitive.SpawnForPlayer(player);
      }
      
      foreach (ClientSideLight light in nonClusteredLights)
      {
        light.SpawnForPlayer(player);
      }
    }

    public void HideFor(Player player, bool showDebug = true)
    {
      if (player == null) return;
      if (showDebug)
      {
        Logger.Debug($"Hiding client side primitives of {this.schematicName} to {player.DisplayName}");
      }

      foreach (ClientSidePrimitive primitive in nonClusteredPrimitives)
      {
        primitive.DestroyForPlayer(player);
      }
      
      foreach (ClientSideLight light in nonClusteredLights)
      {
        light.DestroyForPlayer(player);
      }
    }



    public void SpawnClientPrimitivesToAll()
    {
      foreach (Player player in Player.List.Where(p => p != null && !p.IsNpc))
      {
        SpawnClientPrimitives(player);
      }
    }

    public void SpawnClientPrimitives(Player player)
    {
      if (player == null) return;
      
      foreach (ClientSidePrimitive primitive in nonClusteredPrimitives)
      {
        primitive.SpawnForPlayer(player);
      }
      foreach (ClientSideLight light in nonClusteredLights)
      {
        light.SpawnForPlayer(player);
      }
    }

    public void Destroy()
    {
      foreach (Collider collider in colliders.Where(c => c != null && c.gameObject != null))
      {
        UnityEngine.Object.Destroy(collider);
      }

      foreach (ClientSidePrimitive primitive in nonClusteredPrimitives)
      {
        primitive.DestroyForEveryone();
      }
      foreach (ClientSideLight light in nonClusteredLights)
      {
        light.DestroyForEveryone();
      }

      foreach (PrimitiveCluster cluster in primitiveClusters.Where(c => c != null && c.gameObject != null))
      {
        UnityEngine.Object.Destroy(cluster);
      }
    }
  }
}
