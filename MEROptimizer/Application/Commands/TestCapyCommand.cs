using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using MEROptimizer.Application.Components;
using RemoteAdmin;
using UnityEngine;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class TestCapyCommand : ICommand
{
    public string Command => "testcapy";
    public string[] Aliases => new string[0];
    public string Description => "Spawn une testcapy client-side";

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

        var cappy = new ClientSideCapybara(pos, rot, scale, true, 0);

        cappy.SpawnClienCapybara(player);

        response = "cappy spawn (client-side).";
        return true;
    }
}