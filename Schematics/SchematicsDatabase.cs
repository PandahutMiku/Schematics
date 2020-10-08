using System;
using System.Data;
using System.Threading.Tasks;
using I18N.West;
using MySql.Data.MySqlClient;
using Rocket.Core.Logging;

namespace Pandahut.Schematics
{
    public class DatabaseManager
    {
        internal DatabaseManager()
        {
            new CP1250();
            var connection = CreateConnection();
            try
            {
                connection.Open();
                connection.Close();

                CreateSchematicsDatabaseScheme();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1042)
                    Logger.LogError("Schematics failed to connect to MySQL database host.");
                else
                    Logger.LogException(ex);
                Logger.Log("Cannot connect to Database! Check your MySQL Information defined in Configuration.");
                Schematics.Instance.UnloadPlugin();
            }
        }

        private MySqlConnection CreateConnection()
        {
            MySqlConnection connection = null;
            try
            {
                if (Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabasePort == 0) Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabasePort = 3306;
                connection = new MySqlConnection(string.Format("SERVER={0};DATABASE={1};UID={2};PASSWORD={3};PORT={4};", Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseAddress, Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseName, Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseUsername, Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabasePassword, Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabasePort.ToString()));
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            return connection;
        }

        internal async Task CreateSchematicsDatabaseScheme()
        {
            try
            {
                var connection = CreateConnection();
                var command = connection.CreateCommand();
                connection.Open();
                command.CommandText = "SHOW TABLES LIKE '" + Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseTableName + "';";
                var checkDB = command.ExecuteScalar();
                if (checkDB == null)
                {
                    //CREATE TABLE `unturned_rptest`.`SchematicsTableName` ( `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT , `Name` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , `Schematic` MEDIUMBLOB NOT NULL , `Madeby` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , `MadeAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP , `MadeIn` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB CHARSET=latin1 COLLATE latin1_swedish_ci;
                    command.CommandText = "CREATE TABLE `" + Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseTableName + "` ( `id` INT(11) UNSIGNED NOT NULL AUTO_INCREMENT , `Name` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , `Schematic` MEDIUMBLOB NOT NULL ,`Length` INT(11)  NOT NULL ,`TotalElements` INT(11)  NOT NULL , `Madeby` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , `MadeAt` TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP , `MadeIn` VARCHAR(32) CHARACTER SET latin1 COLLATE latin1_swedish_ci NOT NULL , PRIMARY KEY (`id`)) ENGINE = InnoDB CHARSET=latin1 COLLATE latin1_swedish_ci;";
                    await command.ExecuteNonQueryAsync();
                }

                connection.Close();
            }
            catch (Exception ex)
            {
                Logger.Log("Commands Table Creation Error!");
                Logger.LogException(ex);
            }
        }

        public async Task<Schematics.Schematic> GetSchematicByName(string Name)
        {
            // I don't think wrapping this stuff in Tasks is necessary anymore since MySQL v8 supports Async Methods for everything, but even then MySQL.Data's Async is "fake" and just Task Wrappers themselves, will leave this for now though.
            Task<Schematics.Schematic> task = Task.Run(async () =>
            {
                MySqlDataReader dataReader = null;
                MySqlConnection connection = null;
                MySqlCommand command = null;
                try
                {
                     connection = CreateConnection();
                     command =
                        new MySqlCommand(
                            "SELECT * FROM `" +
                            Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseTableName +
                            "` WHERE `Name` = @Name", connection);
                    command.Parameters.AddWithValue("@Name", Name);
                    await connection.OpenAsync();
                    dataReader = (MySqlDataReader) await command.ExecuteReaderAsync(CommandBehavior.SingleRow);
                    var schematic = new Schematics.Schematic();
                    while (dataReader.Read())
                    {
                        schematic.id = Convert.ToInt32(dataReader["id"]);
                        schematic.SchematicName = Convert.ToString(dataReader["Name"]);
                        schematic.SchmeticBytes = (byte[]) dataReader["Schematic"];
                        schematic.Length = Convert.ToInt32(dataReader["Length"]);
                        schematic.Madeby = Convert.ToString(dataReader["Madeby"]);
                        schematic.MadeAt = Convert.ToDateTime(dataReader["MadeAt"]);
                        schematic.MadeIn = Convert.ToString(dataReader["MadeIn"]);
                    }

                    dataReader.Close();
                    await connection.CloseAsync().ConfigureAwait(false);

                    if (schematic.SchmeticBytes != null)
                        return schematic;
                    return null;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return null;
                }
                finally
                {
                    if (dataReader != null)
                    {
                        dataReader.Dispose();
                    }

                    if (command != null)
                    {
                        command.Dispose();
                    }

                    if (connection != null)
                    {
                        connection.Dispose();
                    }
                }
            });
            await task;
            return task.Result;
        }

        public async Task AddSchematic(string Name, string Madeby, string MadeIn, byte[] blob, int length, int TotalElementCount)
        {
            // I don't think wrapping this stuff in Tasks is necessary anymore since MySQL v8 supports Async Methods for everything, but even then MySQL.Data's Async is "fake" and just Task Wrappers themselves, will leave this for now though.
            _ = Task.Run(async () =>
             {
                 MySqlConnection connection = null;
                 MySqlCommand command = null;
                 try
                 {
                     connection = CreateConnection();
                     command = connection.CreateCommand();
                     command.CommandText = "REPLACE INTO `" +
                                           Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo
                                               .DatabaseTableName +
                                           "` (`Name`, `Madeby`, `MadeAt`, `MadeIn`, `Schematic`, `Length`, `TotalElements`) VALUES (@Name,@MadeBy,@MadeAt,@MadeIn,@Schematic,@Length, @TotalElements);";
                     command.Parameters.AddWithValue("@Name", Name);
                     command.Parameters.AddWithValue("@Madeby", Madeby);
                     command.Parameters.AddWithValue("@MadeAt", DateTime.Now);
                     command.Parameters.AddWithValue("@MadeIn", MadeIn);
                     command.Parameters.AddWithValue("@Schematic", blob);
                     command.Parameters.AddWithValue("@Length", length);
                     command.Parameters.AddWithValue("@TotalElements", TotalElementCount);
                     await connection.OpenAsync().ConfigureAwait(false);
                     await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                     await connection.CloseAsync().ConfigureAwait(false);
                 }
                 catch (Exception ex)
                 {
                     Logger.LogException(ex);
                 }
                 finally
                 {


                     if (command != null)
                     {
                         command.Dispose();
                     }

                     if (connection != null)
                     {
                         connection.Dispose();
                     }
                 }
             });
           
        }

        public async Task<bool> DeleteSchematic(int id)
        {
            // I don't think wrapping this stuff in Tasks is necessary anymore since MySQL v8 supports Async Methods for everything, but even then MySQL.Data's Async is "fake" and just Task Wrappers themselves, will leave this for now though.
            Task<bool> task = Task.Run(async () =>
            {
                try
                {
                    var connection = CreateConnection();
                    var command = connection.CreateCommand();
                    command.CommandText =
                        $"DELETE FROM `{Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseTableName}` WHERE `{Schematics.Instance.Configuration.Instance.SchematicsDatabaseInfo.DatabaseTableName}`.`id` = {id}; ";
                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();
                    await connection.CloseAsync().ConfigureAwait(false);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                    return false;
                }
            });
            await task;
            return task.Result;
        }
    }
}