using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace A3ConfigSharp
{
    public static class Welcome
    {
        public static void DisplayWelcome()
        {
            Console.WriteLine("///////////////////////////////////////////////////////\n\t\tArma 3 Config Parser\n\t\t  By Bryan Dingman\n///////////////////////////////////////////////////////\n");
        }

        public static void GetWorkingDirectory()
        {
            string path = "";
            Console.Write("Please input a directory to process: ");
            path = Console.ReadLine();

            while (!Directory.Exists(path))
            {
                if (path.ToLower().Equals("q"))
                {
                    Environment.Exit(0);
                }

                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Directory does not exist. ");
                Console.ResetColor();
                Console.Write("Please input a directory to process: ");
                path = Console.ReadLine();                
            }

            Program.WorkingDirectory = path;
        }
    }
}
