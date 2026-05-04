using System;
using CommandSystem;

namespace MEROptimizer.Commands
{
  [CommandHandler(typeof(RemoteAdminCommandHandler))]
  public class DisablePluginCmd : ICommand
  {
    public string Command { get; } = "mero.disable";

    public string[] Aliases { get; } = new string[] { "mero.d" };

    public string Description { get; } = "Disable or enable the optimisation of newly created schematics";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
      Plugin.Instance.Manager.isDynamiclyDisabled = !Plugin.Instance.Manager.isDynamiclyDisabled;

      response = $"New spawned schematics {(Plugin.Instance.Manager.isDynamiclyDisabled ? "<color=red>will not" : "<color=green>will")}</color> be optimized !";

      return true;
    }
  }
}
