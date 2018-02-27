using System.Collections.Generic;

namespace A3ConfigSharp
{
    public class ArmaAddon
    {
        public string Name { get; set; } = "";
        public List<object> RequiredAddons { get; set; } = new List<object>();
        public List<string> FileContents { get; set; } = new List<string>();

        public ArmaAddon() { }
        public ArmaAddon(string name, List<object> requiredAddons, List<string> fileContents)
        {
            this.Name = name;
            this.RequiredAddons = requiredAddons;
            this.FileContents = fileContents;
        }
    }
}
