using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using MEROptimizer.Application.Components;
using RemoteAdmin;
using UnityEngine;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class TestLightCommand : ICommand
{
    public string Command => "testlight";
    public string[] Aliases => new string[0];
    public string Description => "Spawn une light client-side";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!(sender is PlayerCommandSender playerSender))
        {
            response = "Commande uniquement en jeu.";
            return false;
        }

        Player player = Player.Get(playerSender);

        Vector3 pos = player.Position + player.Camera.transform.forward * 2f + Vector3.up;
        Quaternion rot = Quaternion.identity;
        Vector3 scale = Vector3.one;

        var light = new ClientSideLight(
            pos,
            rot,
            scale,
            intensity: 5f,
            range: 10f,
            color: Color.white,
            shadows: LightShadows.None,
            shadowStrength: 1f,
            type: LightType.Point,
            shape: LightShape.Cone,
            spotAngle: 30f,
            innerSpotAngle: 20f,
            parentNetId: 0
        );

        light.SpawnClientLight(player);

        response = "Light spawn (client-side).";
        return true;
    }
}