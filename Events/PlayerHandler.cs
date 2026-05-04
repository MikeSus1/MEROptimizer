using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.CustomHandlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;
using MEROptimizer.Components;
using MEROptimizer.Core;
using PlayerRoles;

namespace MEROptimizer.Events;

public class PlayerHandler : CustomEventsHandler
{
    private readonly OptimizationManager _manager;
    
    public PlayerHandler(OptimizationManager manager)
    {
        _manager = manager;
    }
    
    private void ForceSpawnClusters(Player player, string logReason)
    {
        foreach (OptimizedSchematic schematic in _manager.optimizedSchematics.Where(s => s?.schematic != null))
        {
            foreach (var cluster in schematic.primitiveClusters)
            {
                if (cluster.instantSpawn)
                    cluster.SpawnFor(player);
                else
                {
                    cluster.awaitingSpawnIndex[player] = 0;
                    cluster.spawning = true;
                }
            }
        }
    }

    public override void OnPlayerJoined(PlayerJoinedEventArgs ev)
    {
        Player player = ev.Player;
        if (player == null || player.IsNpc) return;

        _manager.AddPlayerTrigger(player);
        foreach (OptimizedSchematic schematic in _manager.optimizedSchematics.Where(s => s?.schematic != null))
        {
            schematic.SpawnClientPrimitives(player);
        }
    }

    public override void OnPlayerSpawned(PlayerSpawnedEventArgs ev)
    {
        Player player = ev.Player;
        if (player == null) return;

        if (player.IsNpc)
        {
            _manager.AddPlayerTrigger(player); 
        }
        else
        {
            if ((player.Role == RoleTypeId.Spectator || player.Role == RoleTypeId.Overwatch) && !_manager.shouldSpectatorsBeAffectedByPDS)
            {
                Timing.CallDelayed(.5f, () => {
                    if (player != null && (player.Role == RoleTypeId.Spectator || player.Role == RoleTypeId.Overwatch))
                        ForceSpawnClusters(player, "Spectator/Overwatch");
                });
            }
            
            if (!_manager.ShouldTutorialsBeAffectedByDistanceSpawning && player.Role == RoleTypeId.Tutorial)
            {
                Timing.CallDelayed(.5f, () => {
                    if (player != null && player.Role == RoleTypeId.Tutorial)
                        ForceSpawnClusters(player, "Tutorial");
                });
            }
            else
            {
                foreach (OptimizedSchematic schematic in _manager.optimizedSchematics)
                {
                    foreach (var cluster in schematic.primitiveClusters)
                    {
                        if (!cluster.insidePlayers.Contains(player))
                            cluster.UnspawnFor(player);
                    }
                }
                
                if (player.Role == RoleTypeId.Filmmaker || player.Role == RoleTypeId.Scp079)
                {
                    Timing.CallDelayed(0.5f, () => {
                        if (player != null && (player.Role == RoleTypeId.Filmmaker || player.Role == RoleTypeId.Scp079))
                            ForceSpawnClusters(player, "Filmmaker/079");
                    });
                }
            }
        }
    }

    public override void OnPlayerChangedSpectator(PlayerChangedSpectatorEventArgs ev)
    {
        if (!_manager.shouldSpectatorsBeAffectedByPDS || ev.Player == null || ev.NewTarget == null) return;

        foreach (OptimizedSchematic schematic in _manager.optimizedSchematics)
        {
            foreach (PrimitiveCluster cluster in schematic.primitiveClusters)
            {
                if (ev.OldTarget != null && cluster.insidePlayers.Contains(ev.OldTarget) && !cluster.insidePlayers.Contains(ev.NewTarget))
                    cluster.UnspawnFor(ev.Player);
                
                if (cluster.insidePlayers.Contains(ev.NewTarget) && (ev.OldTarget == null || !cluster.insidePlayers.Contains(ev.OldTarget)))
                    cluster.SpawnFor(ev.Player);
            }
        }
    }
}