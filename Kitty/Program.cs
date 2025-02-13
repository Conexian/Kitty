using System.Reflection;

namespace KittyConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;

            FileLoader.DisplayEmbeddedFile("art", "txt");

            string userName = Environment.UserName;

            while (true)
            {
                Console.Write($"{userName}-User: ");
                string userInput = Console.ReadLine() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(userInput))
                {
                    string[] inputSegments = userInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    string command = inputSegments[0];
                    string[] commandArguments = inputSegments.Length > 1 ? inputSegments[1..] : Array.Empty<string>();

                    CommandProcessor.ExecuteCommand(command, commandArguments);
                }
            }
        }
    }

    class FileLoader
    {
        public static void DisplayEmbeddedFile(string fileName, string fileExtension)
        {
            string resourcePath = $"Kitty.Resources.{fileName}.{fileExtension}";
            Assembly assembly = Assembly.GetExecutingAssembly();

            using (Stream? resourceStream = assembly.GetManifestResourceStream(resourcePath))
            {
                if (resourceStream != null)
                {
                    using (StreamReader reader = new StreamReader(resourceStream))
                    {
                        Console.WriteLine(reader.ReadToEnd() + "\n");
                    }
                }
                else
                {
                    Console.WriteLine("Error: Embedded file not found.");
                }
            }
        }

        public static void ReadEmbeddedFileFromArguments(string[] arguments)
        {
            if (arguments.Length != 2)
            {
                Console.WriteLine("Usage: readfile <filename> <extension>");
                return;
            }

            DisplayEmbeddedFile(arguments[0], arguments[1]);
        }
    }
}
