using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace A3ConfigSharp
{
    public static class Util
    {
        public static Regex EndOfClassRegex = new Regex(@"^\s*};\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ClassRegex = new Regex(@"class\s+([\w\d]+):?\s*(?:([\w\d]+)\s*(?:{\s*};)?$)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ExternalClassRegex = new Regex(@"class\s*[\w\d]*;", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex AttributeRegex = new Regex(@"\s*([^\t\[\]\s]*)\[?\]?\s*=\s*(.*)\s*;", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex EmptyClassRegex = new Regex(@"class\s+([\w\d]+):?\s*(?:([\w\d]+)\s*)?{};", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex StringRegex = new Regex("^\"(.*)\"$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ScalarRegex = new Regex(@"^-?[0-9.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex ArrayRegex = new Regex(@"^\s*{(.*)}\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static Regex CfgPatchesRegex = new Regex(@"^class\s*cfgpatches\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex CfgPatchesClassRegex = new Regex(@"class\s*(.*)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        public static Regex RequiredAddonsRegex = new Regex(@"requiredaddons\[\]\s*=\s*{(.*)};", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        public static string SchemaName = "";

        public static MySqlConnection MySQLConnection = new MySqlConnection($"server=127.0.0.1;Convert Zero Datetime=True;charset=utf8;user={Properties.Settings.Default.MySQLUsername};password={Properties.Settings.Default.MySQLPassword};database={SchemaName};");

        public static void ConnectToMySQL()
        {
            try
            {
                MySQLConnection.Open();
                CreateDatabaseAndTables();
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"Connection to MySQL Database failed. Reason: {ex.Message}");
                Console.WriteLine("Press any button to exit");
                Console.Read();
                Environment.Exit(0);
            }
        }

        private static void CreateDatabaseAndTables()
        {
            MySqlCommand command = new MySqlCommand($"CREATE DATABASE IF NOT EXISTS `{SchemaName}`; USE `{SchemaName}`;", MySQLConnection);
            command.ExecuteNonQuery();

            command = new MySqlCommand("DROP TABLE IF EXISTS `attributes`; CREATE TABLE `attributes` (`id` bigint(20) unsigned NOT NULL AUTO_INCREMENT, `name` varchar(256) NOT NULL, `parent` bigint(20) NOT NULL, `value` longtext NOT NULL, PRIMARY KEY (`id`,`parent`), UNIQUE KEY `id_UNIQUE` (`id`,`parent`), KEY `index` (`parent`)) ENGINE=InnoDB DEFAULT CHARSET=utf8;", MySQLConnection);
            command.ExecuteNonQuery();

            command = new MySqlCommand("DROP TABLE IF EXISTS `classes`; CREATE TABLE `classes` (`id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,`name` varchar(256) NOT NULL,`parent` bigint(20) NOT NULL,`inheritsFrom` bigint(20) NOT NULL,`extractedFrom` varchar(128) NOT NULL,`modName` text NOT NULL,PRIMARY KEY (`id`),UNIQUE KEY `id_UNIQUE` (`id`)) ENGINE=InnoDB DEFAULT CHARSET=utf8;", MySQLConnection);
            command.ExecuteNonQuery();
        }

        public static void DisconnectFromMySQL()
        {
            if (MySQLConnection != null && MySQLConnection.State == System.Data.ConnectionState.Open)
            {
                MySQLConnection.Close();
            }
        }

        public static string PrintList<T>(List<T> list)
        {
            if (list.Count == 0)
            {
                return "[]";
            }
            string output = "[";
            foreach (var obj in list)
            {
                if (obj.GetType().Equals(typeof(List<object>)))
                {
                    output += $"{PrintList(obj as List<object>)},";
                    continue;
                }
                if (obj.GetType().Equals(typeof(string)))
                {
                    string objAsString = obj as string;
                    output += $"\"{objAsString.Replace(@"\", @"\\").Replace(@"""", "\\\"")}\",";
                    continue;
                }
                if (obj.GetType().Equals(typeof(bool)))
                {
                    bool objAsBool = (obj as bool?) ?? false;
                    output += objAsBool ? "true" : "false";
                    continue;
                }
                output += $"{obj},";
            }

            output = output.Substring(0, output.Count() - 1);
            output += "]";

            return output;
        }

        public static List<object> ParseArray(string item)
        {
            List<object> array = new List<object>();
            List<object> currentArray = array;
            char[] characters = item.ToCharArray();
            string builder = "";
            bool inString = false, inNumber = false, inBool = false;
            List<int> indices = new List<int>();
            int index = 0;

            for (int i = 0; i < characters.Count(); i++)
            {
                char character = characters[i];
                string stringChar = character.ToString();

                // End of Array
                if (Regex.IsMatch(stringChar, @"}") && !inString)
                {
                    currentArray = array;
                    foreach (int x in indices)
                    {
                        currentArray = currentArray[x] as List<object>;
                    }
                    indices.RemoveAt(indices.Count - 1);
                    index = currentArray.Count() - 1;
                }

                // End scalar and bool
                if ((Regex.IsMatch(stringChar, @",") || Regex.IsMatch(stringChar, @"}")) && (inNumber || inBool))
                {
                    if (Util.ScalarRegex.IsMatch(builder))
                    {
                        currentArray.Add(double.Parse(builder));
                    }
                    else if (Regex.IsMatch(builder, "true"))
                    {
                        currentArray.Add(true);
                    }
                    else if (Regex.IsMatch(builder, "false"))
                    {
                        currentArray.Add(false);
                    }
                    builder = "";
                    inNumber = false;
                    inBool = false;
                    index++;
                    continue;
                }

                // End string
                if (Regex.IsMatch(stringChar, "\"") && inString)
                {
                    if (characters.Count() > (i + 1))
                    {
                        char c1 = characters[i + 1];

                        if (!c1.Equals(',') && !c1.Equals('}') || c1.Equals('"'))
                        {
                            builder += character;
                            continue;
                        }
                        if (c1.Equals(','))
                        {
                            if ((i - 2) >= 0)
                            {
                                char c_0 = characters[i - 1];
                                char c_1 = characters[i - 2];

                                if (c_0.Equals('"') && !c_1.Equals(','))
                                {
                                    builder += character;
                                    continue;
                                }
                            }
                        }
                    }
                    currentArray.Add(builder);
                    builder = "";
                    inString = false;
                    index++;
                    continue;
                }

                // Match start of array, ignore string
                if (Regex.IsMatch(stringChar, @"{") && !inString && !inNumber && !inBool)
                {
                    List<object> temp_list = array;
                    foreach (int x in indices)
                    {
                        temp_list = temp_list[x] as List<object>;
                    }
                    temp_list.Add(new List<object>());
                    currentArray = temp_list.Last() as List<object>;
                    indices.Add(temp_list.Count() - 1);
                    index = 0;
                    continue;
                }

                // Build the string
                if (inString)
                {
                    builder += character;
                    continue;
                }

                if (Regex.IsMatch(stringChar, "\"") && !inString)
                {
                    inString = true;
                    continue;
                }

                // Match scalar, ignore string
                if (Util.ScalarRegex.IsMatch(stringChar) && !inString)
                {
                    inNumber = true;
                    builder += character;
                    continue;
                }

                // Match bool, ignore string
                if ((Regex.IsMatch(stringChar, @"t|f") || inBool) && !inString)
                {
                    inBool = true;
                    builder += character;
                    continue;
                }
            }

            if (inBool || inNumber)
            {
                if (Util.ScalarRegex.IsMatch(builder))
                {
                    array.Add(double.Parse(builder));
                }
                else if (Regex.IsMatch(builder, "true"))
                {
                    array.Add(true);
                }
                else if (Regex.IsMatch(builder, "false"))
                {
                    array.Add(false);
                }
            }

            return array;
        }

        public static int GetFileCount(string directory)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            return dirInfo.EnumerateDirectories().AsParallel().SelectMany(di => di.EnumerateFiles("*.cpp", SearchOption.AllDirectories)).Count();
        }
    }
}
