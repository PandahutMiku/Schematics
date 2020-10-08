using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Chat;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Pandahut.Schematics.Helpers
{
    public static class LoadSchematic
    {
        public static void SendOrLog(IRocketPlayer caller, string msg)
        {
            if (caller == null)
            {
                Logger.Log(msg);
            }
            else
            {
                Logger.Log(msg);
                UnturnedChat.Say(caller, msg);
            }
        }
        public static async Task Load(string name, bool keepLocation, bool keepState, bool keepHealth, Vector3 spawnlocation, ulong SpecificSteamid64 = 1, ulong specificgroup = 1, IRocketPlayer caller = null)
        {
            try
            {

            if (Schematics.Instance.Configuration.Instance.UseDatabase)
            {
                var Schematic = await Schematics.Instance.SchematicsDatabaseManager.GetSchematicByName(name);
                if (Schematic == null)
                {
                    SendOrLog(caller, $"Cannot find {name} in Database");
                    return;
                }

                var fs = new FileStream(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat", FileMode.Create, FileAccess.Write);
                fs.Write(Schematic.SchmeticBytes, 0, Schematic.SchmeticBytes.Length);
                fs.Close();
            }

            var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{name}.dat", true);
            var verison = river.readByte();
            if (verison != Schematics.PluginVerison)
            {
                SendOrLog(caller, $"Cannot load {name} as it was saved on a different version which is no longer compatible ({verison} Saved vs {Schematics.PluginVerison} Now)");
                return;
            }

            var useDatabase = river.readBoolean();
            var Time = river.readUInt32();
            if (DateTimeOffset.FromUnixTimeSeconds(Time).ToLocalTime() == DateTimeOffset.MinValue)
            {
                SendOrLog(caller, $"Cannot find {name}.");
                return;
            }

            var playerposition = river.readSingleVector3();
            SendOrLog(caller, $"Loading {name} saved at {DateTimeOffset.FromUnixTimeSeconds(Time).ToLocalTime().ToString()}");
            var setgroupstring = specificgroup == 1 ? "false" : specificgroup.ToString();
            var setsteamid64string = SpecificSteamid64 == 1 ? "false" : SpecificSteamid64.ToString();
            Logger.Log($"Loading {name} with parameters Keep Position: {keepLocation}, Keep Health: {keepHealth}, Keep State: {keepState}, Set Group = {setgroupstring} Set Steamid64: {setsteamid64string}.");
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
                if (!keepLocation) point = point - playerposition + spawnlocation;
                if (SpecificSteamid64 != 1)
                    owner = SpecificSteamid64;
                if (specificgroup != 1)
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
                if (!keepLocation) point = point - playerposition + spawnlocation;

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
            SendOrLog(caller, $"Done, we have loaded Structures: {structurecountInt32} and Barricades: {barricadecountInt32} from {name}");
            }
            catch (Exception e)
            {
                Logger.LogException(e, $"Issue with Load Schematic {name}");
            }
        }
    }
}
