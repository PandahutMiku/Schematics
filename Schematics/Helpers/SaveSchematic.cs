using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.API;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Pandahut.Schematics.Helpers
{
    public static class SaveSchematic
    {
        public static void SendMessageAndLog(IRocketPlayer player, string playermsg, string consolemsg)
        {
            if (player != null)
                UnturnedChat.Say(player, playermsg);
            Logger.Log(consolemsg);
        }
        public static async Task Save(string name, Vector3 position, int radius , ulong SpecificSteamid64 = 0, bool GroupOnly = false, ulong GroupOnlyID = 0, bool Rectangle = false, IRocketPlayer caller = null)
        {
            try {
            var creatorname = "Plugin";
            if (caller != null)
                creatorname = caller.DisplayName;
           
            Logger.Log($"Saving Schematic named {name} with radius of {radius} at {position.ToString()}. SpecificSteamid64 {SpecificSteamid64}, GroupOnlyID {GroupOnlyID}");
            var Barricades = GetBarricadeTransforms(position, radius, SpecificSteamid64, GroupOnly, Rectangle, GroupOnlyID);
            var Structures = GetStructureTransforms(position, radius, SpecificSteamid64, GroupOnly, Rectangle, GroupOnlyID);
            //Logger.Log($"We have found Structures: {Structures.Count}  and Barricades: {Barricades.Count}");
            var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{name}.dat", false);
            river.writeByte(Schematics.PluginVerison);
            river.writeBoolean(Schematics.Instance.Configuration.Instance.UseDatabase);
            river.writeUInt32(Provider.time);
            river.writeSingleVector3(position);
            river.writeInt32(Barricades.Count);
            river.writeInt32(Structures.Count);
            foreach (var bdata in Barricades)
            {
                river.writeUInt16(bdata.bdata.barricade.id);
                river.writeUInt16(bdata.bdata.barricade.health);
                river.writeBytes(bdata.bdata.barricade.state);
                river.writeSingleVector3(bdata.bdata.point);
                river.writeByte(bdata.bdata.angle_x);
                river.writeByte(bdata.bdata.angle_y);
                river.writeByte(bdata.bdata.angle_z);
                river.writeUInt64(bdata.bdata.owner);
                river.writeUInt64(bdata.bdata.group);
            }

            foreach (var sdata in Structures)
            {
                river.writeUInt16(sdata.sdata.structure.id);
                river.writeUInt16(sdata.sdata.structure.health);
                river.writeSingleVector3(sdata.sdata.point);
                river.writeByte(sdata.sdata.angle_x);
                river.writeByte(sdata.sdata.angle_y);
                river.writeByte(sdata.sdata.angle_z);
                river.writeUInt64(sdata.sdata.owner);
                river.writeUInt64(sdata.sdata.group);
            }

            river.closeRiver();
            if (Schematics.Instance.Configuration.Instance.UseDatabase)
                try
                {
                    var file = new FileInfo(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat");
                    var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                    var binaryReader = new BinaryReader(fileStream);
                    await Schematics.Instance.SchematicsDatabaseManager.AddSchematic(name, creatorname, Provider.serverName, binaryReader.ReadBytes((int)fileStream.Length), (int)fileStream.Length, Structures.Count + Barricades.Count);
                    binaryReader.Close();
                    fileStream.Close();
                }
                catch (Exception e)
                {
                    Logger.Log("Issue uploading file to your database, it has been saved locally instead.");
                }

            SendMessageAndLog(caller, $"Done, we have saved Structures: {Structures.Count} and Barricades: {Barricades.Count} to {(Schematics.Instance.Configuration.Instance.UseDatabase ? "Database and Files" : "Files")} called {name}.", $"Saved {Structures.Count + Barricades.Count} elements for {creatorname} to {(Schematics.Instance.Configuration.Instance.UseDatabase ? "Database and Files" : "Files")} called {name}.");
            }
            catch (Exception e)
            {
                Logger.LogException(e, $"Issue with Load Schematic {name}");
            }
        }
        public static List<StructureDataInternal> GetStructureTransforms(Vector3 position, int radius, ulong SpecificSteamid64, bool GroupOnly, bool Rectangle, ulong GroupOnlyID = 0)
        {
            radius += 92;
            float Distance;
            Vector3 Center;
            Rect rect;
            var error = 0;
            var Structures = new List<StructureDataInternal>();
            var transforms = 0;
            StructureData structure;
            var regionsfound = 0;
            var regionsused = 0;
            Vector3 pointVector3;
            StructureRegion structureRegion = null;
            Transform transform = null;
            for (var x = 0; x < StructureManager.regions.GetLength(0); x++)
                for (var y = 0; y < StructureManager.regions.GetLength(1); y++)
                {
                    regionsfound++;
                    Regions.tryGetPoint((byte)x, (byte)y, out pointVector3);
                    if (Vector3.Distance(pointVector3 += new Vector3(64, 0, 64), new Vector3(position.x, 0f, position.z)) > radius)
                        continue;

                    regionsused++;
                    structureRegion = StructureManager.regions[x, y];
                    transforms = structureRegion.drops.Count;
                    for (var i = 0; i < transforms; i++)
                    {
                        transform = structureRegion.drops[i].model;
                        var Plant = transform.parent != null && transform.parent.CompareTag("Vehicle");
                        if (structureRegion.structures[i] == null)
                        {
                            error++;
                            continue;
                        }

                        structure = structureRegion.structures[i];
                        if (GroupOnly)
                            if (structure.group != GroupOnlyID)
                                continue;
                        if (SpecificSteamid64 != 0)
                            if (structure.owner != SpecificSteamid64)
                                continue;
                        if (Rectangle == false && Vector3.Distance(position, transform.position) < radius - 92 && !Plant)
                            Structures.Add(new StructureDataInternal(structureRegion.structures[i], transform.parent != null && transform.parent.CompareTag("Vehicle") ? true : false));
                        /* Rectangle Stuff
                            else if (Rectangle && Vector3.Distance(transform.position, Center) < 0)
                            {

                            } */
                    }
                }

            //Logger.Log($"We have found {regionsfound} regions and used {regionsused} of them.");
            if (error != 0)
            {
                Logger.Log(
                    "It seems your structure regions are a bit of sync, if you have issues, gotta restart server. This issue may be caused by one of your plugins.");
                Logger.Log(
                    $"Error on executing SaveSchematic,it seems structure regions are out of sync, gotta restart if this causes issues. Sorry! This could be caused by a server plugin, or just getting unlucky.");
            }
            return Structures;
        }

        public static List<BarricadeDataInternal> GetBarricadeTransforms(Vector3 position, int radius, ulong SpecificSteamid64, bool GroupOnly, bool Rectangle, ulong GroupOnlyID = 0)
        {
            radius += 92;
            var error = 0;
            var Barricades = new List<BarricadeDataInternal>();
            var transforms = 0;
            var regionsfound = 0;
            var regionsused = 0;
            bool Plant;
            Vector3 pointVector3;
            BarricadeData barricade;
            BarricadeRegion barricadeRegion = null;
            Transform transform = null;
            for (var x = 0; x < BarricadeManager.regions.GetLength(0); x++)
                for (var y = 0; y < BarricadeManager.regions.GetLength(1); y++)
                {
                    regionsfound++;
                    Regions.tryGetPoint((byte)x, (byte)y, out pointVector3);

                    if (Vector3.Distance(pointVector3 += new Vector3(64, 0, 64), new Vector3(position.x, 0f, position.z)) > radius)
                        continue;
                    regionsused++;
                    barricadeRegion = BarricadeManager.regions[x, y];
                    transforms = barricadeRegion.drops.Count;
                    for (var i = 0; i < transforms; i++)
                    {
                        transform = barricadeRegion.drops[i].model;
                        Plant = transform.parent != null && transform.parent.CompareTag("Vehicle");
                        if (barricadeRegion.barricades[i] == null)
                        {
                            error++;
                            continue;
                        }

                        barricade = barricadeRegion.barricades[i];
                        if (GroupOnly)
                            if (barricade.group != GroupOnlyID)
                                continue;
                        if (SpecificSteamid64 != 0)
                            if (barricade.owner != SpecificSteamid64)
                                continue;

                        if (Vector3.Distance(position, transform.position) < radius - 92 && !Plant)
                            Barricades.Add(new BarricadeDataInternal(barricadeRegion.barricades[i], transform.parent != null && transform.parent.CompareTag("Vehicle") ? true : false, transform != null ? transform : null));
                    }
                }

            //Logger.Log($"We have found {regionsfound} regions and used {regionsused} of them.");
            if (error != 0)
            {
                Logger.Log(
                    "It seems your barricade regions are a bit of sync, if you have issues, gotta restart server. This issue may be caused by one of your plugins.");
                Logger.Log(
                    $"Error on executing SaveSchematic,it seems barricade regions are out of sync, gotta restart if this causes issues. Sorry! This could be caused by a server plugin, or just getting unlucky.");
            }

            return Barricades;
        }
    }
}
