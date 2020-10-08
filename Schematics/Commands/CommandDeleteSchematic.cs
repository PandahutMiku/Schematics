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

    internal class CommandDeleteSchematic : IRocketCommand
    {
        public string Help => "Deletes Schematic Info";

        public string Name => "DeleteSchematic";

        public string Syntax => "<Name>";

        public List<string> Aliases => new List<string>();

        public AllowedCaller AllowedCaller => AllowedCaller.Both;

        public List<string> Permissions => new List<string> {"schematic.delete"};

        public async void Execute(IRocketPlayer caller, string[] command)
        {
            var Console = caller is ConsolePlayer;
            if (command == null || command.Length == 0 || string.IsNullOrWhiteSpace(command[0]))
            {
                SendMessage(caller, "Invalid Syntax, use /DeleteSchematic <Name>", Console);
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

                var success = await Schematics.Instance.SchematicsDatabaseManager.DeleteSchematic(Schematic.id);
                if (success)
                    SendMessage(caller, $"Successfully deleted {Schematic.SchematicName} from Database, it'll be automatically deleted from your files on next restart.", Console);
                else
                    SendMessage(caller, $"Failed deleting {Schematic.SchematicName} from Database, most likely not enough database permissions.", Console);
            }
            else
            {
                SendMessage(caller, "You do not have database enabled, if you want to delete a file schematic, you can just delete it manually.", Console);
                return;
            }

            try
            {
                var file = new FileInfo(ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + $"/Rocket/Plugins/Schematics/Saved/{name}.dat");
                if (!file.Exists && !Schematics.Instance.Configuration.Instance.UseDatabase)
                {
                    SendMessage(caller, $"Can't find file with name of {name} in files.", Console);
                    return;
                }

                file.Delete();
                SendMessage(caller, $"Successfully deleted {file.Name} from Files.", Console);
            }
            catch (Exception e)
            {
                SendMessage(caller, "File is in use, printing error to your logs. File will most likely go away on next restart if it was saved with database enabled, or you can delete yourself.", Console);
                Logger.LogError(e.Message);
            }
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