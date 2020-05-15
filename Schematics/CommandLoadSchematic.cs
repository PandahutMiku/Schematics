using System;
using System.Collections.Generic;
using System.IO;
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

        public void Execute(IRocketPlayer caller, string[] command)
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
            ulong SpecificSteamid64 = 0;
            ulong specificgroup = 0;
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
            if (Schematics.Instance.Configuration.Instance.UseDatabase)
            {
                var Schematic = Schematics.Instance.SchematicsDatabaseManager.GetSchematicByName(name);
                if (Schematic == null)
                {
                    UnturnedChat.Say(caller, $"Cannot find {name} in Database");
                    return;
                }

                var fs = new FileStream(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat", FileMode.Create, FileAccess.Write);
                fs.Write(Schematic.SchmeticBytes, 0, Schematic.SchmeticBytes.Length);
                fs.Close();
            }

            var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{name}.dat", true);
            var verison = river.readByte();
            if (verison == 1)
            {
                UnturnedChat.Say(caller, $"Cannot load {name} as it was saved on a different version which is no longer compatible");
                return;
            }

            var useDatabase = river.readBoolean();
            var Time = river.readUInt32();
            if (DateTimeOffset.FromUnixTimeSeconds(Time).ToLocalTime() == DateTimeOffset.MinValue)
            {
                UnturnedChat.Say(caller, $"Cannot find {name}.");
                return;
            }

            var playerposition = river.readSingleVector3();
            UnturnedChat.Say(player, $"Loading {name} saved at {DateTimeOffset.FromUnixTimeSeconds(Time).ToLocalTime().ToString()}");
            var setgroupstring = specificgroup == 0 ? "false" : specificgroup.ToString();
            var setsteamid64string = SpecificSteamid64 == 0 ? "false" : SpecificSteamid64.ToString();
            Logger.Log($"Loading {name} for {player.CharacterName} with parameters Keep Position: {keepLocation}, Keep Health: {keepHealth}, Keep State: {keepState}, Set Group = {setgroupstring} Set Steamid64: {setsteamid64string}.");
            var barricadecountInt32 = river.readInt32();
            var structurecountInt32 = river.readInt32();
            var error = 0;
            for (var i = 0; i < barricadecountInt32; i++)
            {
                var barricadeid = river.readUInt16();
                var barricadehealth = river.readUInt16();
                var barricadestate = river.readBytes();
                var point = river.readSingleVector3();
                var angleX = river.readByte();
                var angleY = river.readByte();
                var angleZ = river.readByte();
                var owner = river.readUInt64();
                var group = river.readUInt64();
                var barricade = new Barricade(barricadeid);
                if (keepHealth)
                    barricade.health = barricadehealth;
                if (keepState)
                    barricade.state = barricadestate;
                if (!keepLocation) point = point - playerposition + hit.point;
                if (SpecificSteamid64 != 0)
                    owner = SpecificSteamid64;
                if (specificgroup != 0)
                    group = specificgroup;
                var rotation = Quaternion.Euler(angleX * 2, angleY * 2, angleZ * 2);
                //rotation.eulerAngles = new Vector3(angleX, angleY, angleZ);
                var barricadetransform = BarricadeManager.dropNonPlantedBarricade(barricade, point, rotation, owner, group);
                if (barricadetransform == null)
                {
                    error++;
                    return;
                }

                var InteractableStorage = barricadetransform.GetComponent<InteractableStorage>();
                if (InteractableStorage != null) BarricadeManager.sendStorageDisplay(barricadetransform, InteractableStorage.displayItem, InteractableStorage.displaySkin, InteractableStorage.displayMythic, InteractableStorage.displayTags, InteractableStorage.displayDynamicProps);
            }

            if (error != 0)
                Logger.Log($"Unexpected Barricade Error occured {error} times");
            error = 0;
            for (var i = 0; i < structurecountInt32; i++)
            {
                var structureid = river.readUInt16();
                var structurehealth = river.readUInt16();
                var point = river.readSingleVector3();
                var angleX = river.readByte();
                var angleY = river.readByte();
                var angleZ = river.readByte();
                var owner = river.readUInt64();
                var group = river.readUInt64();
                var structure = new Structure(structureid);
                if (keepHealth)
                    structure.health = structurehealth;
                // For when nelson adds proper way to add structures
                if (!keepLocation) point = point - playerposition + hit.point;

                if (SpecificSteamid64 != 0)
                    owner = SpecificSteamid64;
                if (specificgroup != 0)
                    group = specificgroup;
                var rotation = Quaternion.Euler(angleX * 2, angleY * 2, angleZ * 2);
                //rotation.eulerAngles = new Vector3(angleX, angleY, angleZ);
                if (!StructureManager.dropReplicatedStructure(structure, point, rotation, owner, group))
                    error++;
            }

            if (error != 0)
                Logger.Log($"Unexpected Barricade Error occured {error} times");
            river.closeRiver();
            UnturnedChat.Say(player, $"Done, we have loaded Structures: {structurecountInt32} and Barricades: {barricadecountInt32} from {name}");
        }

        public void SendMessageAndLog(UnturnedPlayer player, string playermsg, string consolemsg)
        {
            UnturnedChat.Say(player, playermsg);
            Logger.Log(consolemsg);
        }
    }
}