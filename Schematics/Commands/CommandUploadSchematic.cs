using System;
using System.Collections.Generic;
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

    internal class CommandUploadSchematic : IRocketCommand
    {
        public string Help => "Upload Schematic Info";

        public string Name => "UploadSchematic";

        public string Syntax => "<Name>";

        public List<string> Aliases => new List<string> {};

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> { "schematic.upload" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            bool Console = caller is ConsolePlayer;
            if (command == null || command.Length == 0 || string.IsNullOrWhiteSpace(command[0]))
            {
                SendMessage(caller, $"Invalid Syntax, use /uploadschematic <Name>", Console);
                return;
            }
            var name = command[0].Replace(" ", "");
            if (Schematics.Instance.Configuration.Instance.UseDatabase)
                try
                {
                    var file = new FileInfo(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat");
                    if (file == null || !file.Exists)
                    {
                        SendMessage(caller, $"Can't find file with name of {name} in files. May of never been on this server in first place.", Console);
                        return;
                    }
                    var river = ServerSavedata.openRiver($"/Rocket/Plugins/Schematics/Saved/{name}.dat", isReading: true);
                    try
                    {
                        var verison = river.readByte();
                    var useDatabase = river.readBoolean();
                    var Time = river.readUInt32();
                    var playerposition = river.readSingleVector3();
                    var barricadecountInt32 = river.readInt32();
                    var structurecountInt32 = river.readInt32();
                    river.closeRiver();
                    var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);
                    var binaryReader = new BinaryReader(fileStream);
                    Schematics.Instance.SchematicsDatabaseManager.AddSchematic(name, GetName(caller, Console), Provider.serverName, binaryReader.ReadBytes((int)fileStream.Length), (int)fileStream.Length, barricadecountInt32 + structurecountInt32);
                    binaryReader.Close();
                    fileStream.Close();
                    SendMessage(caller, $"Successfully uploaded {name} to database.", Console);
                    file.Delete();
                    }
                    catch (Exception e)
                    {
                        river.closeRiver();
                        SendMessage(caller, "Issue uploading file to your database, bad name or file has wrong format.", Console);
                        return;
                    }
                }
                catch (Exception e)
                {
                    SendMessage(caller, "Issue uploading file to your database, wrong name or file has wrong format.", Console);
                    return;
                }
        }
        public void SendMessage(IRocketPlayer caller, string msg, bool Console)
        {
            if (Console)
                Logger.Log(msg);
            else
                UnturnedChat.Say(caller, msg);
        }

        public string GetName(IRocketPlayer caller, bool Console)
        {
            if (Console)
                return "Console";
            else
                return caller.DisplayName;
        }
    }
}