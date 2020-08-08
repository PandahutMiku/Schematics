using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Rocket.Core.Plugins;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace Pandahut.Schematics
{
    extern alias UnityEnginePhysics;

    public class Schematics : RocketPlugin<SchematicsConfiguration>
    {
        public static Regex steamid64Regex = new Regex(@"[0-9]{17}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Schematics Instance;

        public static byte PluginVerison = 2;

        // This isn't done and is mostly unused
        public Dictionary<CSteamID, RectangleSelection> RectangleSelectionDictionary = new Dictionary<CSteamID, RectangleSelection>();
        public DatabaseManager SchematicsDatabaseManager;

        protected override void Load()
        {
            Instance = this;
            ConfigCheck();
            // This is a very simple check to delete files that are saved to the Database from Local disk, because they aren't necessary to be saved twice.
            if (Configuration.Instance.UseDatabase)
            {
                SchematicsDatabaseManager = new DatabaseManager();
                try
                {
                    var di = new DirectoryInfo(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + "/Rocket/Plugins/Schematics/Saved/");
                    foreach (var file in di.GetFiles())
                    {
                        var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{file.Name}", true);
                        try
                        {
                            // This is the version, discarded because it's useless to us, but we have to read it to get to the second variable
                            _ = river.readByte();
                            var useDatabase = river.readBoolean();
                            if (!useDatabase)
                                file.Delete();
                            river.closeRiver();
                        }
                        // Failing to get Information, maybe corrupted, not worth logging
                        catch (Exception _)
                        {
                            river.closeRiver();
                        }
                    }
                }
                // This isn't critical
                catch (Exception _)
                {
                }
            }

            Logger.Log("Welcome to Schematics! -- If you need help, feel free to email contact@pandahut.net, contact Miku#2402 or join Our Discord @ https://pandahut.net/discord");
        }

        protected override void Unload()
        {
            Logger.Log("Unloading Schematics!");
        }

        public void ConfigCheck()
        {
            // Probably not necessary?
            if (Configuration.Instance.UseDatabase == null)
            {
                Logger.Log("You're missing UseDB in Config, defaulting to N");
                Configuration.Instance.UseDatabase = false;
                Configuration.Save();
            }

            if (Configuration.Instance.SchematicsDatabaseInfo == null)
            {
                Logger.Log("You're missing Schematics Database Info in Config, defaulting it and setting UseDatabase to false");
                Configuration.Instance.SchematicsDatabaseInfo = new DatabaseInfo
                {
                    DatabaseAddress = "localhost",
                    DatabaseUsername = "unturned",
                    DatabasePassword = "password",
                    DatabaseName = "unturned",
                    DatabaseTableName = "Schematics",
                    DatabasePort = 3306
                };
                Configuration.Instance.UseDatabase = false;
                Configuration.Save();
            }
        }

        public class RectangleSelection
        {
            public Vector3 Position1;
            public Vector3 Position2;
        }

        public class Schematic
        {
            public int id;
            public int Length;
            public DateTime MadeAt;
            public string Madeby;
            public string MadeIn;
            public string SchematicName;
            public byte[] SchmeticBytes;
        }
    }
}
