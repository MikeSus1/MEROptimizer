using LabApi.Events.CustomHandlers;
using MEROptimizer.Core;
using MEROptimizer.Helper;

namespace MEROptimizer.Events;

public class ServerHandler : CustomEventsHandler
{
    private readonly OptimizationManager _manager;
    
    public ServerHandler(OptimizationManager manager)
    {
        _manager = manager;
    }
    
    public override void OnServerWaitingForPlayers()
    {
        _manager.Clear();
        PrefabHelper.RegisterPrefabs();
    }
}