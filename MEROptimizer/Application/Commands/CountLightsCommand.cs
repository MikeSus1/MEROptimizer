using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using UnityEngine;
using AdminToys;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public class CountLightsCommand : ICommand
{
    public string Command => "countlights";
    public string[] Aliases => new string[] { "clights" };
    public string Description => "Compte les lights serveur";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        int total = 0;

        // Toutes les lights dans la scène
        AdminToys.LightSourceToy[] lights = UnityEngine.Object.FindObjectsOfType<AdminToys.LightSourceToy>();

        total = lights.Count(l => l != null);

        response = $"Lights serveur trouvées: {total}";
        return true;
    }
}