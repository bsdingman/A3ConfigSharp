using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace A3ConfigSharp
{
    public static class Exporter
    {
        private static List<long> _parentIDs = new List<long>() { -1 };
        private static int _currentProgress = 0;
        private static ProgressBar _progressBar;
        public static string ModName = "";

        public static void AskUserForExportOption()
        {
            Console.Write("Export as JSON (1) or Export to MySQL (2): ");

            if (Console.ReadLine().Equals("2"))
            {
                AskUserForModName();
                ExportToMySQL(Config.ConfigFile);
            }
            else
            {
                ExportToJSON(Config.ConfigFile);
            }

            Console.WriteLine("Export has finished successfully!");
        }

        public static void AskUserForModName()
        {
            Console.Write("Please input name of mod to be associated (No Spaces): ");
            ModName = Console.ReadLine();

            while (ModName.Contains(" "))
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Name cannot contain spaces");
                Console.ResetColor();
                Console.Write("Please input name of mod to be associated (No Spaces): ");
                ModName = Console.ReadLine();
            }

            Util.SchemaName = $"a3config_{ModName.ToLower()}";
        }

        public static void ExportToMySQL(ArmaClass configFile)
        {
            Console.Write("Connecting to MySQL Database... ");
            Util.ConnectToMySQL();
            Console.WriteLine(" Finished");

            Console.WriteLine("Exporting to MySQL... ");
            ExportConfigFile(configFile.Children);
            Console.WriteLine("Finished export!");
        }

        private static void ExportConfigFile(Dictionary<string, ArmaClass> config)
        {
            Dictionary<string, long> topLevelInheritanceIDs = new Dictionary<string, long>();

            using (var progress = new ProgressBar())
            {
                var total = config.Count;
                _currentProgress = 0;

                Console.Write("Inserting Top Level Classes...");

                foreach (KeyValuePair<string, ArmaClass> armaClass in config)
                {
                    MySqlCommand command = new MySqlCommand($"INSERT INTO classes (name, parent, inheritsFrom, extractedFrom, modName) VALUES ('{armaClass.Key}',-1,-1,'{armaClass.Value.ExtractedFrom}','{ModName}')", Util.MySQLConnection);
                    command.ExecuteNonQuery();

                    topLevelInheritanceIDs.Add(armaClass.Key, command.LastInsertedId);
                    _currentProgress++;
                    progress.Report((double)_currentProgress / total);
                }
            }

            Console.WriteLine(" Finished");

            Console.Write("Inserting subclasses and attributes... ");

            _currentProgress = 0;
            _progressBar = new ProgressBar();

            // Now loop through and get everything
            foreach (KeyValuePair<string, ArmaClass> armaClass in config)
            {
                Dictionary<string, long> inheritanceIDs = new Dictionary<string, long>(topLevelInheritanceIDs);

                PrecompileInheritance(armaClass.Value.Children, ref inheritanceIDs);

                _parentIDs.Add(inheritanceIDs[armaClass.Key]);
                ExportAttributes(armaClass.Value.Attributes);
                ExportSubClass(armaClass.Value.Children, ref inheritanceIDs);
                _parentIDs.RemoveAt(_parentIDs.Count - 1);

                _currentProgress++;
                _progressBar.Report((double)_currentProgress / Config.TotalEntries);
            }

            _progressBar.Dispose();

            Console.WriteLine(" Finished");
        }

        private static void PrecompileInheritance(Dictionary<string, ArmaClass> config, ref Dictionary<string, long> inheritanceIDs)
        {
            foreach (KeyValuePair<string, ArmaClass> armaClass in config)
            {
                if (!string.IsNullOrWhiteSpace(armaClass.Value.InheritsFrom))
                {
                    if (!inheritanceIDs.ContainsKey(armaClass.Value.InheritsFrom))
                    {
                        inheritanceIDs.Add(armaClass.Value.InheritsFrom, -1);
                    } 
                }
            }
        }

        private static void ExportSubClass(Dictionary<string, ArmaClass> config, ref Dictionary<string, long> inheritanceIDs)
        {
            foreach (KeyValuePair<string, ArmaClass> armaClass in config)
            {
                string extractedFrom = armaClass.Value.ExtractedFrom;
                inheritanceIDs.TryGetValue(armaClass.Value.InheritsFrom, out long inheritsFrom);

                if (inheritsFrom == 0)
                {
                    inheritsFrom = -1;
                }

                MySqlCommand command = new MySqlCommand($"INSERT INTO classes (name, parent, inheritsFrom, extractedFrom, modName) VALUES ('{armaClass.Key}',{_parentIDs.Last()},{inheritsFrom},'{extractedFrom}','{ModName}')", Util.MySQLConnection);
                command.ExecuteNonQuery();
                _parentIDs.Add(command.LastInsertedId);

                if (inheritanceIDs.ContainsKey(armaClass.Key))
                {
                    inheritanceIDs[armaClass.Key] = command.LastInsertedId;
                }

                ExportAttributes(armaClass.Value.Attributes);
                ExportSubClass(armaClass.Value.Children, ref inheritanceIDs);

                _parentIDs.RemoveAt(_parentIDs.Count - 1);

                _currentProgress++;
                _progressBar.Report((double)_currentProgress / Config.TotalEntries);
            }
        }

        private static void ExportAttributes(Dictionary<string, ArmaAttribute> attribute)
        {
            if (attribute.Count == 0) return;

            StringBuilder insertString = new StringBuilder();
            insertString.Append("INSERT INTO attributes (name, parent, value) VALUES");
            int count = 0;
            foreach (KeyValuePair<string, ArmaAttribute> armaAttribute in attribute)
            {
                insertString.Append($"('{armaAttribute.Key}',{_parentIDs.Last()},'{armaAttribute.Value.ToJSON().Replace("'", "''")}'),");
                count++;
            }

            insertString.Length--;
            insertString.Append(";");

            MySqlCommand command = new MySqlCommand(insertString.ToString(), Util.MySQLConnection);
            command.ExecuteNonQuery();

            _currentProgress += count;
            _progressBar.Report((double)_currentProgress / Config.TotalEntries);
        }

        public static void ExportToJSON(ArmaClass configFile)
        {
            Console.Write("Creating JSON export... ");
            string configJSON = configFile.ToJSON();
            Console.WriteLine(" Finished");

            Console.Write("Beautifying... ");
            configJSON = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(configJSON), Formatting.Indented);
            Console.WriteLine(" Finished");

            Console.Write("Saving to file... ");
            File.WriteAllText("configFile.json", configJSON);
            Console.WriteLine(" Finished");
        }
    }
}
