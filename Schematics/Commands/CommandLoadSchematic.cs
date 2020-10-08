using System;
using System.Collections.Generic;
using System.IO;
using Pandahut.Schematics.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Pandahut.Schematics
{
    extern alias UnityEnginePhysics;

    internal class CommandLoadSchematic : IRocketCommand
    {
        public string Help => "Loads Schematic";

        public string Name => "LoadSchematic";

        public string Syntax => "<Name>";

        public List<string> Aliases => new List<string> {"LS", "LoadS", "ls"};

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> {"schematic.load"};

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            var player = (UnturnedPlayer) caller;

            if (command == null || command.Length == 0 || string.IsNullOrWhiteSpace(command[0]))
            {
                UnturnedChat.Say(caller, "Invalid Syntax, use /Loadschematic <Name> [Optional: -KeepPos -NoState -KeepHealth -SetOwner -SetGroup, Input any Steamid64 to set owner to it]");
                return;
            }

            if (!UnityEnginePhysics::UnityEngine.Physics.Raycast(player.Player.look.aim.position, player.Player.look.aim.forward, out var hit))
            {
                UnturnedChat.Say(caller, "Cannot get what you're aiming at to spawn the schematic.");
                return;
            }


            // Trying to get better spot where they're aiming, so the schematic doesn't just spawn straight in the floor
            hit.point += new Vector3(0, 1, 0);
            var fullcommand = string.Join(" ", command).ToLower();
            var keepLocation = false;
            var keepHealth = false;
            var keepState = true;
            ulong SpecificSteamid64 = 1;
            ulong specificgroup = 1;
            if (fullcommand.Contains("-keeppos"))
                keepLocation = true;
            if (keepLocation == false && Schematics.Instance.Configuration.Instance.MaxDistanceToLoadSchematic != 0 && hit.distance > Schematics.Instance.Configuration.Instance.MaxDistanceToLoadSchematic)
            {
                UnturnedChat.Say(caller, "You are aiming to somewhere past the configurable Max Distance to Load Schematic.");
                return;
            }
            if (fullcommand.Contains("-keephealth"))
                keepHealth = true;
            if (fullcommand.Contains("-nostate"))
                keepState = false;
            if (fullcommand.Contains("-setowner"))
                SpecificSteamid64 = player.CSteamID.m_SteamID;
            if (fullcommand.Contains("-setmap"))
                SpecificSteamid64 = 0;
            if (fullcommand.Contains("-setgroup"))
            {
                // Strange issues if the player has no group, no idea why or how the Group ID got equal to the player's Steamid, but this breaks /checkowner and a few other things
                // [5/15/2020 4:39:04 PM] [Info] Schematics >> Loading sillysignsandstuff for Oh Wonder with parameters Keep Position: True, Keep Health: False, Keep State: True, Set Group = 76561198138254281 Set Steamid64: 76561198138254281.
                if (player.Player.quests.groupID != CSteamID.Nil && player.Player.quests.groupID.m_SteamID != player.CSteamID.m_SteamID) 
                    specificgroup = player.Player.quests.groupID.m_SteamID;
            }

            var match = Schematics.steamid64Regex.Match(fullcommand);
            if (match.Success && ulong.TryParse(match.Value, out var result))
                SpecificSteamid64 = result;
            var name = command[0].Replace(" ", "");

            await LoadSchematic.Load(name, keepLocation, keepState, keepHealth, hit.point, SpecificSteamid64, specificgroup,
                caller).ConfigureAwait(false);
        }

        public void SendMessageAndLog(UnturnedPlayer player, string playermsg, string consolemsg)
        {
            UnturnedChat.Say(player, playermsg);
            Logger.Log(consolemsg);
        }
    }
}