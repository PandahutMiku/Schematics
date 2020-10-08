using System;
using System.Collections.Generic;
using System.IO;
using Pandahut.Schematics.Helpers;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Pandahut.Schematics
{
    public class BarricadeDataInternal
    {
        public BarricadeData bdata;
        public bool plant;
        public Transform transform;

        public BarricadeDataInternal(BarricadeData structureData, bool Plant, Transform transform)
        {
            bdata = structureData;
            plant = Plant;
            this.transform = transform;
        }
    }

    public class StructureDataInternal
    {
        public bool plant;
        public StructureData sdata;

        public StructureDataInternal(StructureData structureData, bool Plant)
        {
            sdata = structureData;
            plant = Plant;
        }
    }

    internal class CommandSaveSchematics : IRocketCommand
    {
        public string Help => "Saves Schematic";

        public string Name => "SaveSchematic";

        public string Syntax => "<Range>";

        public List<string> Aliases => new List<string> {"SS", "SaveS"};

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> {"schematic.save"};

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            // Command: /saveSchematic
            var player = (UnturnedPlayer) caller;

            if (command == null || command.Length == 0 || command.Length == 1 || string.IsNullOrWhiteSpace(command[0]))
            {
                UnturnedChat.Say(player, "Invalid Syntax, use /SaveSchematic <name> <distance> [Optional Parameters: -Owner (Only gets structures placed by you) -Group (only gets structures placed by your current group), Input any Steamid64 to only get results from it");
                return;
            }

            // Rectangles are planned but not done rn
            var Rectangle = false;
            if (!int.TryParse(command[1], out var radius))
            {
                if (Schematics.Instance.RectangleSelectionDictionary.ContainsKey(player.CSteamID))
                    if (Schematics.Instance.RectangleSelectionDictionary[player.CSteamID].Position1 != Vector3.zero && Schematics.Instance.RectangleSelectionDictionary[player.CSteamID].Position2 != Vector3.zero)
                        Rectangle = true;
                if (!Rectangle)
                {
                    UnturnedChat.Say(player, "Invalid Syntax, use /SaveSchematic <name> <distance> [Optional Parameters: -Owner (Only gets structures placed by you) -Group (only gets structures placed by your current group), Input any Steamid64 to only get results from it");
                    //UnturnedChat.Say(player, "Invalid Syntax, use /SaveSchematic <name> <distance> or /select <1,2> [Optional Parameters: -Owner (Only gets structures placed by you) -Group (only gets structures placed by your current group), Input any Steamid64 to only get results from it");
                    return;
                }
            }

            // This is lazy, probably better way to do it then using Contains.
            var fullcommand = string.Join(" ", command).ToLower();
            ulong SpecificSteamid64 = 0;
            var GroupOnly = false;
            if (fullcommand.Contains("-owner"))
                SpecificSteamid64 = player.CSteamID.m_SteamID;
            if (fullcommand.Contains("-group"))
                GroupOnly = true;
            var match = Schematics.steamid64Regex.Match(fullcommand);
            if (match.Success && ulong.TryParse(match.Value, out var result))
                SpecificSteamid64 = result;
            var setsteamid64string = SpecificSteamid64 == 0 ? "false" : SpecificSteamid64.ToString();
            Logger.Log($"Specific Steamid64: {setsteamid64string}, Group Only: {GroupOnly}");
            var name = command[0].Replace(" ", "");
            await SaveSchematic.Save(name, player.Position, radius, SpecificSteamid64, GroupOnly,
                player.SteamGroupID.m_SteamID, Rectangle, caller).ConfigureAwait(false);
        }

    

        public void SendMessageAndLog(UnturnedPlayer player, string playermsg, string consolemsg)
        {
            UnturnedChat.Say(player, playermsg);
            Logger.Log(consolemsg);
        }

        //Not sure if this works, to be perfectly honest
        public Vector3 CenterVector3(UnturnedPlayer player)
        {
            return Vector3.Lerp(Schematics.Instance.RectangleSelectionDictionary[player.CSteamID].Position1, Schematics.Instance.RectangleSelectionDictionary[player.CSteamID].Position2, 0.5f);
        }
    }
}