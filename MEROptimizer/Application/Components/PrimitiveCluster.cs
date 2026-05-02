using AdminToys;
using LabApi.Features.Wrappers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MEROptimizer.Application.Components
{
    public class PrimitiveCluster : MonoBehaviour
    {
        public int id { get; set; }
        public List<ClientSidePrimitive> primitives { get; set; }
        public List<ClientSideLight> lights = new();
        public ClientSidePrimitive displayClusterPrimitive { get; set; }

        // OPTIMISATION GC : Au lieu de stocker une copie de la liste complète (qui explose la RAM), 
        // on stocke juste l'INDEX (int) de la dernière primitive qu'on a fait spawn pour ce joueur.
        public Dictionary<Player, int> awaitingSpawnIndex = new Dictionary<Player, int>();
        
        // Pour éviter d'allouer une nouvelle liste à chaque Update, on cache les clés ici.
        private List<Player> playersToProcessCache = new List<Player>();

        public List<Player> insidePlayers = new List<Player>();

        public bool instantSpawn;
        private int numberOfPrimitivePerSpawn; // Converti en int pour l'indexation
        private int updatePassed = 0;
        private bool multiFrameSpawn = false;
        public bool spawning = false;

        public void Start()
        {
            instantSpawn = MEROptimizer.numberOfPrimitivePerSpawn == 0;

            if (MEROptimizer.numberOfPrimitivePerSpawn > 0 && MEROptimizer.numberOfPrimitivePerSpawn < 1)
            {
                numberOfPrimitivePerSpawn = Mathf.CeilToInt(MEROptimizer.numberOfPrimitivePerSpawn * 10);
                multiFrameSpawn = true;
            }
            else
            {
                numberOfPrimitivePerSpawn = Mathf.CeilToInt(MEROptimizer.numberOfPrimitivePerSpawn);
            }

            float radius = this.GetComponent<SphereCollider>().radius;
            displayClusterPrimitive = new ClientSidePrimitive(this.transform.position - new Vector3(0, 2000, 0),
              this.transform.rotation, Vector3.one * radius, PrimitiveType.Sphere, new Color(1, 0, 1, .4f), PrimitiveFlags.Visible);
        }

        public void OnDestroy()
        {
            foreach (ClientSidePrimitive primitive in primitives)
                primitive.DestroyForEveryone();

            foreach (ClientSideLight light in lights)
                light.DestroyForEveryone();

            displayClusterPrimitive?.DestroyForEveryone();
        }

        public void OnTriggerEnter(Collider collider)
        {
            if (collider == null || collider.transform.parent != null) return;
            if (!collider.CompareTag("Player") || !collider.gameObject.TryGetComponent(out PlayerTrigger playerTrigger)) return;

            Player player = playerTrigger.player;
            if (player == null || player.Role == PlayerRoles.RoleTypeId.Filmmaker) return;
            if (!Application.MEROptimizer.ShouldTutorialsBeAffectedByDistanceSpawning && player.Role == PlayerRoles.RoleTypeId.Tutorial) return;

            if (!player.IsNpc)
            {
                if (instantSpawn)
                {
                    SpawnFor(player);
                }
                else
                {
                    // OPTIMISATION : On démarre l'index de spawn à 0
                    awaitingSpawnIndex[player] = 0;
                    spawning = true;
                }
            }

            if (!insidePlayers.Contains(player))
            {
                insidePlayers.Add(player);
            }
        }

        public void Update()
        {
            if (!spawning) return;

            if (multiFrameSpawn)
            {
                updatePassed++;
                if (updatePassed < numberOfPrimitivePerSpawn) return;
                updatePassed = 0;
            }

            if (awaitingSpawnIndex.Count == 0)
            {
                spawning = false;
                return;
            }

            playersToProcessCache.Clear();
            playersToProcessCache.AddRange(awaitingSpawnIndex.Keys);

            bool allSpawnsCompleted = true;

            foreach (Player player in playersToProcessCache)
            {
                int startIndex = awaitingSpawnIndex[player];
                int batchSize = multiFrameSpawn ? 1 : numberOfPrimitivePerSpawn;
                int endIndex = Mathf.Min(startIndex + batchSize, primitives.Count);

                List<Player> spectators = player.CurrentSpectators.ToList();

                for (int i = startIndex; i < endIndex; i++)
                    primitives[i].SpawnClientPrimitive(player);

                foreach (Player spec in spectators)
                {
                    for (int i = startIndex; i < endIndex; i++)
                        primitives[i].SpawnClientPrimitive(spec);
                }

                if (endIndex >= primitives.Count)
                {
                    foreach (ClientSideLight light in lights)
                        light.SpawnClientLight(player);

                    foreach (Player spec in spectators)
                    {
                        foreach (ClientSideLight light in lights)
                            light.SpawnClientLight(spec);
                    }

                    awaitingSpawnIndex.Remove(player);
                }
                else
                {
                    awaitingSpawnIndex[player] = endIndex;
                    allSpawnsCompleted = false;
                }
            }

            if (allSpawnsCompleted)
                spawning = false;
        }

        public void OnTriggerExit(Collider collider)
        {
            if (collider == null || collider.transform.parent != null) return;
            if (!collider.CompareTag("Player") || !collider.gameObject.TryGetComponent(out PlayerTrigger playerTrigger)) return;

            Player player = playerTrigger.player;
            if (player == null || player.Role == PlayerRoles.RoleTypeId.Filmmaker) return;
            if (!Application.MEROptimizer.ShouldTutorialsBeAffectedByDistanceSpawning && player.Role == PlayerRoles.RoleTypeId.Tutorial) return;

            awaitingSpawnIndex.Remove(player);
            UnspawnFor(player);
            insidePlayers.Remove(player);
        }

        public void SpawnFor(Player player)
        {
            if (player == null || player.IsNpc) return;
            
            // Le batching est respecté ici (envoi à la chaîne sur la même connexion)
            foreach (ClientSidePrimitive primitive in primitives)
            {
                primitive.SpawnClientPrimitive(player);
            }
            
            foreach (ClientSideLight light in lights)
            {
                light.SpawnClientLight(player);
            }
        }

        public void UnspawnFor(Player player)
        {
            if (player == null || player.IsNpc) return;

            foreach (ClientSidePrimitive primitive in primitives)
                primitive.DestroyClientPrimitive(player);

            foreach (ClientSideLight light in lights)
                light.DestroyClientLight(player);

            foreach (Player p in player.CurrentSpectators)
            {
                foreach (ClientSidePrimitive primitive in primitives)
                    primitive.DestroyClientPrimitive(p);

                foreach (ClientSideLight light in lights)
                    light.DestroyClientLight(p);
            }
        }

        public void DisplayRadius(Player player) => displayClusterPrimitive?.SpawnClientPrimitive(player);
        public void HideRadius(Player player) => displayClusterPrimitive?.DestroyClientPrimitive(player);
    }
}