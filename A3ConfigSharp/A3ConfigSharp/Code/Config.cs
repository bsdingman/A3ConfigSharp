using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace A3ConfigSharp
{
    public static class Config
    {
        public static List<ArmaAddon> CfgPatches = new List<ArmaAddon>();
        public static ArmaClass ConfigFile = new ArmaClass();
        public static int TotalEntries = 0;
        private static ArmaClass _parentNode = new ArmaClass();
        private static ArmaClass _currentNode = new ArmaClass();
        private static List<string> _tempNames = new List<string>();
        private static List<string> _patchNames = new List<string>();
        private static List<string> _parents = new List<string>();
        private static string _addonName = "", _currentAddon = "";

        public static void CompileCfgPatches(string path)
        {
            Console.Write("Compiling CfgPatches...");
            using (var progress = new ProgressBar())
            {
                int numberOfFiles = Util.GetFileCount(path), processedFiles = 0;
                bool inCfgPatches = false, inAddon = false;

                foreach (string file in Directory.GetFiles(path, "*.cpp", SearchOption.AllDirectories))
                {
                    foreach (string line in File.ReadAllLines(file))
                    {
                        if (!inCfgPatches)
                        {
                            Match cfgPatchesMatch = Util.CfgPatchesRegex.Match(line);
                            if (cfgPatchesMatch.Success)
                            {
                                inCfgPatches = true;
                            }
                            continue;
                        }
                        if (!inAddon)
                        {
                            Match classMatch = Util.CfgPatchesClassRegex.Match(line);
                            if (classMatch.Success)
                            {
                                inAddon = true;
                                _addonName = classMatch.Groups[1].ToString().Trim();
                            }
                            else
                            {
                                Match endMatch = Util.EndOfClassRegex.Match(line);
                                if (endMatch.Success)
                                {
                                    inCfgPatches = false;
                                }
                            }
                            continue;
                        }

                        Match addonsMatch = Util.RequiredAddonsRegex.Match(line);
                        if (addonsMatch.Success)
                        {
                            List<object> addons = Util.ParseArray(addonsMatch.Groups[1].ToString());

                            ArmaAddon addon = new ArmaAddon(_addonName, addons, SanitizeFile(File.ReadAllText(file)));
                            CfgPatches.Add(addon);
                            _tempNames.Add(_addonName.ToLower());
                        }

                        Match eocMatch = Util.EndOfClassRegex.Match(line);
                        if (eocMatch.Success)
                        {
                            inAddon = false;
                        }
                    }

                    processedFiles++;
                    progress.Report((double)processedFiles / numberOfFiles);
                }
            }

            Console.WriteLine(" Finished");

            if (CfgPatches.Count() > 0)
            {
                VerifyRequiredAddons();
                SortCfgPatches();
            }
            else
            {
                Console.Write("Unable to find class CfgPatches. This may return inaccurate configs if overwriting. Continue? (Y/N): ");

                if (Console.ReadLine().ToLower() == "n")
                {
                    Environment.Exit(0);
                }

                foreach (string file in Directory.GetFiles(path, "*.cpp", SearchOption.AllDirectories))
                {
                    ArmaAddon addon = new ArmaAddon
                    {
                        FileContents = SanitizeFile(File.ReadAllText(file)).ToList()
                    };
                }
            }
        }

        private static void VerifyRequiredAddons()
        {
            List<string> missing = new List<string>();
            foreach (ArmaAddon addon in CfgPatches)
            {
                foreach (string raddon in addon.RequiredAddons)
                {
                    string addonName = (raddon as string).ToLower();
                    if (!_tempNames.Contains(addonName))
                    {
                        missing.Add(addonName);
                    }
                }
            }

            if (missing.Count > 0)
            {
                Console.Write($"The following required addons are missing from compilation:\n\n{Util.PrintList(missing)}\n\nContinue ? (Y/N): ");

                if (Console.ReadLine().ToLower() == "n")
                {
                    Environment.Exit(0);
                }

                foreach (string patch in missing)
                {
                    _patchNames.Add(patch);
                }
            }
        }

        public static void SortCfgPatches()
        {
            List<ArmaAddon> temp = new List<ArmaAddon>();
         
            do
            {
                for (int i = 0; i < CfgPatches.Count; i++)
                {
                    ArmaAddon addon = CfgPatches[i];

                    if (_patchNames.Contains(addon.Name.ToLower())) continue;

                    if (addon.RequiredAddons.Count == 0)
                    {
                        _patchNames.Add(addon.Name.ToLower());
                        temp.Add(addon);
                        CfgPatches.Remove(addon);
                        continue;
                    }

                    bool success = false;
                    foreach (string requiredAddon in addon.RequiredAddons)
                    {
                        success = _patchNames.Contains(requiredAddon.ToLower());                        
                    }

                    if (!success) continue;

                    _patchNames.Add(addon.Name.ToLower());
                    temp.Add(addon);
                    CfgPatches.Remove(addon);
                }
            }
            while (CfgPatches.Count > 0);

            CfgPatches = new List<ArmaAddon>(temp);
        }

        private static List<string> SanitizeFile(string file)
        {
            file = Regex.Replace(file, @"enum\s*{[^;]*};", "", RegexOptions.Multiline);
            return file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
        }

        public static void ReadAllConfigs()
        {
            using (var progress = new ProgressBar())
            {
                int numberOfAddons = CfgPatches.Count;
                int processedAddons = 0;
                Console.Write("Processing addons... ");
                foreach (ArmaAddon addon in Config.CfgPatches)
                {
                    _currentNode = new ArmaClass();
                    _parentNode = new ArmaClass();
                    _parents.Clear();
                    _currentAddon = addon.Name;
                    foreach (string line in addon.FileContents)
                    {
                        ParseConfigLine(line);
                    }
                    processedAddons++;
                    progress.Report((double)processedAddons / numberOfAddons);
                }
            }

            Console.WriteLine(" Finished");
        }

        private static void ParseConfigLine(string line)
        {
            // CLASS
            Match match = Util.ClassRegex.Match(line);
            if (match.Success)
            {
                if (Util.ExternalClassRegex.IsMatch(line)) return;

                string currentClass = match.Groups[1].ToString();
                string inheritClass = match.Groups[2].ToString();

                if (string.IsNullOrWhiteSpace(inheritClass))
                {
                    inheritClass = "";
                }

                _parents.Add(currentClass);

                if (_parents.Count == 0)
                {
                    _parentNode = ConfigFile;
                }
                else
                {
                    ArmaClass temp_node = ConfigFile;

                    foreach (string parent in _parents.GetRange(0, _parents.Count - 1))
                    {
                        temp_node = temp_node.Children[parent];
                    }

                    _parentNode = temp_node;
                }

                _currentNode = new ArmaClass
                {
                    InheritsFrom = inheritClass,
                    ExtractedFrom = _currentAddon
                };

                if (!_parentNode.Children.ContainsKey(currentClass))
                {
                    _parentNode.Children.Add(currentClass, _currentNode);
                }

                if (Util.EmptyClassRegex.IsMatch(line))
                {
                    _parents.RemoveAt(_parents.Count - 1);
                }

                return;
            }

            // END OF CLASS
            if (Util.EndOfClassRegex.IsMatch(line))
            {
                _parents.RemoveAt(_parents.Count - 1);
                _currentNode = _parentNode;
                TotalEntries++;
                return;
            }

            // ATTRIBUTE
            match = Util.AttributeRegex.Match(line);
            if (match.Success)
            {
                string name = match.Groups[1].ToString();
                string value = match.Groups[2].ToString();

                // ARRAY
                match = Util.ArrayRegex.Match(value);
                if (match.Success)
                {
                    List<object> array = new List<object>();
                    string arrayString = match.Groups[1].ToString();
                    if (!string.IsNullOrWhiteSpace(arrayString))
                    {
                        array = Util.ParseArray(arrayString);
                    }

                    if (_currentNode.Attributes.ContainsKey(name))
                    {
                        _currentNode.Attributes[name] = new ArmaAttribute(2, array);
                    }
                    else
                    {
                        _currentNode.Attributes.Add(name, new ArmaAttribute(2, array));
                    }
                    TotalEntries++;
                    return;
                }

                // STRING
                if (Util.StringRegex.IsMatch(value))
                {
                    if (_currentNode.Attributes.ContainsKey(name))
                    {
                        _currentNode.Attributes[name] = new ArmaAttribute(1, value);
                    }
                    else
                    {
                        _currentNode.Attributes.Add(name, new ArmaAttribute(1, value));
                    }
                    TotalEntries++;
                    return;
                }

                // SCALAR
                if (Util.ScalarRegex.IsMatch(value))
                {
                    if (_currentNode.Attributes.ContainsKey(name))
                    {
                        _currentNode.Attributes[name] = new ArmaAttribute(0, double.Parse(value));
                    }
                    else
                    {
                        _currentNode.Attributes.Add(name, new ArmaAttribute(0, double.Parse(value)));
                    }
                    TotalEntries++;
                    return;
                }

                if (_currentNode.Attributes.ContainsKey(name))
                {
                    _currentNode.Attributes[name] = new ArmaAttribute(1, value);
                }
                else
                {
                    _currentNode.Attributes.Add(name, new ArmaAttribute(1, value));
                }

                TotalEntries++;

                return;
            }
        }
    }
}
