using System;
using System.Collections.Generic;
using System.IO;
using Rocket.API;
using Rocket.Core.Logging;
using Rocket.Unturned.Chat;
using SDG.Unturned;

namespace Pandahut.Schematics
{
    extern alias UnityEnginePhysics;

    internal class CommandViewSchematic : IRocketCommand
    {
        public string Help => "Loads Schematic Info";

        public string Name => "ViewSchematic";

        public string Syntax => "<Name>";

        public List<string> Aliases => new List<string> {"VS", "ViewS"};

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> {"schematic.view"};

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            var Console = caller is ConsolePlayer;
            if (command == null || command.Length == 0 || string.IsNullOrWhiteSpace(command[0]))
            {
                SendMessage(caller, "Invalid Syntax, use /ViewSchematic <Name>", Console);
                return;
            }

            var name = command[0].Replace(" ", "");
            if (Schematics.Instance.Configuration.Instance.UseDatabase)
            {
                var Schematic = await Schematics.Instance.SchematicsDatabaseManager.GetSchematicByName(name);
                if (Schematic == null)
                {
                    SendMessage(caller, $"Cannot find {name} in Database", Console);
                    return;
                }

                var fs = new FileStream(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat", FileMode.Create, FileAccess.ReadWrite);
                fs.Write(Schematic.SchmeticBytes, 0, Schematic.Length);
                fs.Close();
            }

            var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{name}.dat", true);
            var verison = river.readByte();
            var useDatabase = river.readBoolean();
            var Time = river.readUInt32();
            var playerposition = river.readSingleVector3();
            var barricadecountInt32 = river.readInt32();
            var structurecountInt32 = river.readInt32();
            river.closeRiver();
            SendMessage(caller, $"{name} was saved at {DateTimeOffset.FromUnixTimeSeconds(Time).ToLocalTime().ToString()} with Plugin Verison {verison}, it has {barricadecountInt32} barricades and {structurecountInt32} structures, total {barricadecountInt32 + structurecountInt32} elements.", Console);
        }

        public void SendMessage(IRocketPlayer caller, string msg, bool Console)
        {
            if (Console)
                Logger.Log(msg);
            else
                UnturnedChat.Say(caller, msg);
        }
    }
}