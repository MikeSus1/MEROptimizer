using System;
using LabApi.Events.CustomHandlers;
using LabApi.Features;
using LabApi.Loader.Features.Plugins;
using MEROptimizer.Core;
using MEROptimizer.Events;
using PlayerHandler = MEROptimizer.Events.PlayerHandler;

namespace MEROptimizer;

public class Plugin : Plugin<Config>
{
    public override string Name => "MEROptimizer";
    public override string Author { get; } = "Math";
    public override string Description { get; } = "Meant to optimize MapEditorReborn primitives by making them client sided + Providing an API to spawn & handle client side primitives.";
    public override Version Version { get; } = new(2, 0, 8, 0);
    public override Version RequiredApiVersion { get; } = new(LabApiProperties.CompiledVersion);

    public static Plugin Instance { get; private set; }

    public  OptimizationManager Manager { get; private set; }
    public PlayerHandler PlayerEvents { get; private set; }
    public ServerHandler ServerEvents { get; private set; }
    public SchematicHandler SchematicEvents { get; private set; }

    public override void Enable()
    {
        Instance = this;
        
        Manager = new OptimizationManager();
        
        Manager.Load(Config);
        
        PlayerEvents = new PlayerHandler(Manager);
        ServerEvents = new ServerHandler(Manager);
        SchematicEvents = new SchematicHandler(Manager);
        
        CustomHandlersManager.RegisterEventsHandler(PlayerEvents);
        CustomHandlersManager.RegisterEventsHandler(ServerEvents);
        
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned += SchematicEvents.OnSchematicSpawned;
        ProjectMER.Events.Handlers.Schematic.SchematicDestroyed += SchematicEvents.OnSchematicDestroyed;
    }

    public override void Disable()
    {
        Instance = null;

        CustomHandlersManager.UnregisterEventsHandler(PlayerEvents);
        CustomHandlersManager.UnregisterEventsHandler(ServerEvents);
        
        ProjectMER.Events.Handlers.Schematic.SchematicSpawned -= SchematicEvents.OnSchematicSpawned;
        ProjectMER.Events.Handlers.Schematic.SchematicDestroyed -= SchematicEvents.OnSchematicDestroyed;

        Manager?.Unload();
        Manager = null;
    }
}