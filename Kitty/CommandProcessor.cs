using System;
using KittyConsole;

namespace KittyConsole
{
    class CommandProcessor
    {
        public static void ExecuteCommand(string userCommand, string[] commandArgs)
        {
            switch (userCommand.ToLower())
            {
                case "clear":
                    Console.Clear();
                    break;

                case "exit":
                    Environment.Exit(0);
                    break;

                case "help":
                case "?":
                    FileLoader.DisplayEmbeddedFile("help", "txt");
                    break;

                case "art":
                    FileLoader.DisplayEmbeddedFile("art", "txt");
                    break;

                case "ping":
                    UtilityFunctions.PerformPing(commandArgs);
                    break;

                case "beep":
                    UtilityFunctions.PlayBeepSound(commandArgs);
                    break;

                case "meow":
                    UtilityFunctions.DisplayMessage(commandArgs);
                    break;

                case "readfile":
                    FileLoader.ReadEmbeddedFileFromArguments(commandArgs);
                    break;

                case "sysinfo":
                    SystemInfo.ShowSystemInfo();
                    break;

                case "portscan":
                    UtilityFunctions.PortInfo(commandArgs);
                    break;

                default:
                    Console.WriteLine($"{userCommand} is not a recognized command.");
                    break;
            }
        }
    }
}
