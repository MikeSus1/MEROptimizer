using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using UnityEngine;
using AdminToys;
using Logger = LabApi.Features.Console.Logger;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class CountPrimCommand : ICommand
{
    public string Command => "countprim";
    public string[] Aliases => new string[] { "" };
    public string Description => "Compte les countprim serveur";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        int total = 0;
        response = "";
        
        foreach (var aa in UnityEngine.Object.FindObjectsOfType<AdminToys.PrimitiveObjectToy>())
        {
            Logger.Info($"PrimitiveObjectToy trouvé: {aa.name}");
            aa.NetworkPrimitiveFlags = PrimitiveFlags.Visible;
        }

        response += $"PrimitiveObjectToy serveur trouvées: {total}";
        return true;
    }
}