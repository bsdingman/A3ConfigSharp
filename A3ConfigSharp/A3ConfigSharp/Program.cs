using System;

namespace A3ConfigSharp
{
    class Program
    {
        public static string WorkingDirectory { get; set; }

        static void Main(string[] args)
        {
            Welcome.DisplayWelcome();
            Welcome.GetWorkingDirectory();
            Config.CompileCfgPatches(WorkingDirectory);
            Config.ReadAllConfigs();
            Exporter.AskUserForExportOption();
            Console.WriteLine("Press any key to exit");
            Console.Read();
        }
    }
}
