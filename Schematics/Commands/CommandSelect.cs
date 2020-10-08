using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;
// These aren't done and are set to not Compile
namespace Pandahut.Schematics
{
    extern alias UnityEnginePhysics;

    internal class CommandSelect : IRocketCommand
    {
        public string Help => "Select Rectangle Info";

        public string Name => "select";

        public string Syntax => "<1, 2>";

        public List<string> Aliases => new List<string> { "SL" };

        public AllowedCaller AllowedCaller => AllowedCaller.Player;

        public List<string> Permissions => new List<string> { "schematic.select" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            var uPlayer = (UnturnedPlayer) caller;
            if (command == null || command.Length == 0 || string.IsNullOrWhiteSpace(command[0]))
            {
                UnturnedChat.Say(caller, $"Invalid Syntax, use /Select <1,2>");
                return;
            }

            if (command[0].ToLower().StartsWith("2"))
            {
                CheckOrAddToDictionary(uPlayer);
                Schematics.Instance.RectangleSelectionDictionary[uPlayer.CSteamID].Position2 = uPlayer.Position;
                UnturnedChat.Say(caller, $"You have set Position 2 to {uPlayer.Position.ToString()}");
            }
            else
            {
                CheckOrAddToDictionary(uPlayer);
                Schematics.Instance.RectangleSelectionDictionary[uPlayer.CSteamID].Position1 = uPlayer.Position;
                UnturnedChat.Say(caller, $"You have set Position 1 to {uPlayer.Position.ToString()}");
            }
        }

        public void CheckOrAddToDictionary(UnturnedPlayer player)
        {
            if (!Schematics.Instance.RectangleSelectionDictionary.ContainsKey(player.CSteamID))
                Schematics.Instance.RectangleSelectionDictionary.Add(player.CSteamID, new Schematics.RectangleSelection() { Position1 = Vector3.zero, Position2 = Vector3.zero });
        }
    }
}