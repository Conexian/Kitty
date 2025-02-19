using System.Net.NetworkInformation;
using System;
using System.Management;
using LibreHardwareMonitor.Hardware;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Diagnostics;

namespace KittyConsole
{
    internal class UtilityFunctions
    {
        public static void PlayBeepSound(string[] arguments)
        {
            if (arguments.Length >= 2 &&
                int.TryParse(arguments[0], out int frequency) &&
                int.TryParse(arguments[1], out int duration))
            {
                Task.Run(() => Console.Beep(frequency, duration));
            }
            else
            {
                Console.WriteLine("Usage: beep <frequency> <duration>");
            }
        }

        public static void DisplayMessage(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine("Usage: meow <message>");
                return;
            }

            string message = string.Join(" ", arguments);
            Console.WriteLine(message);
        }

        public static void PerformPing(string[] arguments)
        {
            if (arguments.Length == 0) { Console.WriteLine("Usage: ping <address> [count] [delay] [size]"); return; }

            int pingCount = 1;
            int delay = 1000;
            int bufferSize = 32;

            if (arguments.Length >= 2 && !int.TryParse(arguments[1], out pingCount)) { Console.WriteLine("Error: pingCount must be an integer"); return; }
            if (arguments.Length >= 3 && !int.TryParse(arguments[2], out delay)) { Console.WriteLine("Error: Delay must be an integer"); return; }
            if (arguments.Length >= 4 && !int.TryParse(arguments[3], out bufferSize)) { Console.WriteLine("Error: Buffer size must be an integer"); return; }

            string targetDomain = arguments[0];
            Ping pingSender = new Ping();

            byte[] buffer = Encoding.ASCII.GetBytes(new string('A', bufferSize));
            PingOptions options = new PingOptions();

            try
            {
                for (int i = 0; i < pingCount; i++)
                {
                    if (i > 0)
                    {
                        Thread.Sleep(delay);
                    }

                    PingReply reply = pingSender.Send(targetDomain, 10000, buffer, options);

                    if (reply.Status == IPStatus.Success)
                    {
                        Console.WriteLine($"Ping to {targetDomain} successful:");
                        Console.WriteLine($" - Roundtrip time: {reply.RoundtripTime}ms");
                        Console.WriteLine($" - Address: {reply.Address}");
                        Console.WriteLine($" - TTL: {reply.Options?.Ttl ?? -1}");
                        Console.WriteLine($" - Buffer size: {reply.Buffer.Length} bytes");
                    }
                    else
                    {
                        Console.WriteLine($"Ping failed: {reply.Status}");
                    }
                }
            }
            catch (PingException e)
            {
                Console.WriteLine("Ping operation failed. Possible causes:");
                Console.WriteLine("1. No internet connection.");
                Console.WriteLine("2. Invalid domain name.");
                Console.WriteLine("3. Firewall blocking ICMP requests.");
                Console.WriteLine($"Error: {e.Message}");
            }

            pingSender.Dispose();
        }

        public static void PortInfo(string[] arguments)
        {
            if (arguments.Length != 3)
            {
                Console.WriteLine("Usage: portscan <target> <start> <end>");
                return;
            }

            if (!int.TryParse(arguments[1], out int startPort) || !int.TryParse(arguments[2], out int endPort))
            {
                Console.WriteLine("Error: Ports must be integers.");
                return;
            }

            if (startPort > endPort)
            {
                Console.WriteLine("Error: start must not be greater than end.");
                return;
            }

            string target = arguments[0];

            Console.WriteLine($"Scanning {target} from port {startPort} to {endPort}...\n");

            Thread spinnerThread = new Thread(Spinner.ShowSpinner);
            spinnerThread.Start();

            List<Thread> scanThreads = new List<Thread>();

            for (int port = startPort; port <= endPort; port++)
            {
                Thread thread = new Thread(() => PortScanning.ScanPort(target, port));
                scanThreads.Add(thread);
                thread.Start();
            }

            foreach (var thread in scanThreads)
            {
                thread.Join();
            }

            Spinner.StopSpinner();
            spinnerThread.Join();

            Console.WriteLine("\nPort scan complete.");
        }

        public static void GetPublicIP()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    Task<string> response = client.GetStringAsync("https://api64.ipify.org");
                    response.Wait();
                    Console.WriteLine($"Public IP Address: {response.Result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving public IP: {ex.Message}");
            }
        }

        public static void ComputeFileHash(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                Console.WriteLine("Usage: hash <filePath> <algorithm>");
                Console.WriteLine("Supported algorithms: MD5, SHA1, SHA256");
                return;
            }

            string filePath = arguments[0];
            string algorithm = arguments[1].ToUpper();

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            try
            {
                using (FileStream stream = File.OpenRead(filePath))
                {
                    HashAlgorithm hasher = algorithm switch
                    {
                        "MD5" => MD5.Create(),
                        "SHA1" => SHA1.Create(),
                        "SHA256" => SHA256.Create(),
                        _ => null
                    };

                    if (hasher == null)
                    {
                        Console.WriteLine("Error: Unsupported algorithm. Use MD5, SHA1, or SHA256.");
                        return;
                    }

                    byte[] hashBytes = hasher.ComputeHash(stream);
                    string hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                    Console.WriteLine($"{algorithm} Hash: {hashString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public static void ReadTextFile(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine("Usage: cat <filePath> [lineNumbers] [maxLines]");
                return;
            }

            string filePath = arguments[0];
            bool showLineNumbers = arguments.Length > 1 && arguments[1].ToLower() == "true";
            int maxLines = arguments.Length > 2 && int.TryParse(arguments[2], out int parsedMaxLines) ? parsedMaxLines : int.MaxValue;

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Error: File not found.");
                return;
            }

            try
            {
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    int lineNumber = 1;
                    int printedLines = 0;

                    while ((line = reader.ReadLine()) != null && printedLines < maxLines)
                    {
                        if (showLineNumbers)
                        {
                            Console.WriteLine($"{lineNumber}: {line}");
                        }
                        else
                        {
                            Console.WriteLine(line);
                        }

                        lineNumber++;
                        printedLines++;
                    }

                    if (reader.ReadLine() != null)
                    {
                        Console.WriteLine("\n[Output truncated... Use a higher maxLines value to see more]");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
            }
        }

        public static void Base64Converter(string[] arguments)
        {
            if (arguments.Length < 2)
            {
                Console.WriteLine("Usage: base64 <encode|decode> <text>");
                return;
            }

            string mode = arguments[0].ToLower();
            string text = string.Join(" ", arguments.Skip(1));

            if (mode == "encode")
            {
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
                Console.WriteLine("Encoded: " + encoded);
            }
            else if (mode == "decode")
            {
                try
                {
                    byte[] decodedBytes = Convert.FromBase64String(text);
                    string decoded = Encoding.UTF8.GetString(decodedBytes);
                    Console.WriteLine("Decoded: " + decoded);
                }
                catch (FormatException)
                {
                    Console.WriteLine("Error: Provided text is not valid Base64.");
                }
            }
            else
            {
                Console.WriteLine("Error: First argument must be either 'encode' or 'decode'.");
            }
        }

        public static void OpenPath(string[] arguments)
        {
            if (arguments.Length < 1)
            {
                Console.WriteLine("Usage: open <path>");
                return;
            }

            string targetPath = arguments[0];

            try
            {
                if (Directory.Exists(targetPath))
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", targetPath) { UseShellExecute = true });
                    Console.WriteLine($"Opened directory: {targetPath}");
                }
                else if (File.Exists(targetPath))
                {
                    Process.Start(new ProcessStartInfo(targetPath) { UseShellExecute = true });
                    Console.WriteLine($"Opened file: {targetPath}");
                }
                else
                {
                    Console.WriteLine("Error: The specified path does not exist.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening the path: {ex.Message}");
            }
        }

        public static void CalculateDirectorySize(string[] arguments)
        {
            if (arguments.Length == 0) { Console.WriteLine("Usage: foldersize <path> [recursive] [listFiles]"); return; }

            string directoryPath = arguments[0];
            bool recursive = true;
            bool listFiles = false;

            if (arguments.Length >= 2 && !bool.TryParse(arguments[1], out recursive)) { Console.WriteLine("Error: Recursive must be true or false."); return; }
            if (arguments.Length >= 3 && !bool.TryParse(arguments[2], out listFiles)) { Console.WriteLine("Error: ListFiles must be true or false."); return; }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Error: Directory does not exist.");
                return;
            }

            try
            {
                string[] files = Directory.GetFiles(directoryPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
                long totalSize = 0;

                foreach (var file in files)
                {
                    try
                    {
                        FileInfo fi = new FileInfo(file);
                        totalSize += fi.Length;

                        if (listFiles)
                        {
                            Console.WriteLine($"{file} - {FormatSize(fi.Length)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error reading file '{file}': {ex.Message}");
                    }
                }

                Console.WriteLine($"\nTotal size of files in '{directoryPath}' is {FormatSize(totalSize)} ({totalSize} bytes).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error calculating directory size: " + ex.Message);
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024)
                return bytes + " B";
            double kb = bytes / 1024.0;
            if (kb < 1024)
                return kb.ToString("F2") + " KB";
            double mb = kb / 1024.0;
            if (mb < 1024)
                return mb.ToString("F2") + " MB";
            double gb = mb / 1024.0;
            return gb.ToString("F2") + " GB";
        }

        public static void BatchRenameFiles(string[] arguments)
        {
            if (arguments.Length < 3)
            {
                Console.WriteLine("Usage: batchrename <directoryPath> <searchPattern> <replacePattern> [recursive]");
                return;
            }

            string directoryPath = arguments[0];
            string searchPattern = arguments[1];
            string replacePattern = arguments[2];
            bool recursive = false;

            if (arguments.Length >= 4 && !bool.TryParse(arguments[3], out recursive))
            {
                Console.WriteLine("Error: recursive must be true or false.");
                return;
            }

            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine("Error: Directory not found.");
                return;
            }

            string[] files = Directory.GetFiles(directoryPath, "*", recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            int renamedCount = 0;

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                if (fileName.Contains(searchPattern))
                {
                    string newFileName = fileName.Replace(searchPattern, replacePattern);
                    string newFilePath = Path.Combine(Path.GetDirectoryName(file), newFileName);

                    try
                    {
                        File.Move(file, newFilePath);
                        Console.WriteLine($"Renamed: {fileName} -> {newFileName}");
                        renamedCount++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error renaming '{fileName}': {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"Total files renamed: {renamedCount}");
        }

        public static void Netstat(string[] arguments)
        {
            string protocol = arguments.Length > 0 ? arguments[0].ToLower() : "all";
            if (protocol != "tcp" && protocol != "udp" && protocol != "all")
            {
                Console.WriteLine("Usage: netstat [tcp|udp|all]");
                return;
            }

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();

            if (protocol == "tcp" || protocol == "all")
            {
                var tcpConnections = ipProperties.GetActiveTcpConnections();
                Console.WriteLine("TCP Connections:");
                Console.WriteLine("{0,-25} {1,-25} {2}", "Local Address", "Remote Address", "State");
                foreach (var conn in tcpConnections)
                {
                    Console.WriteLine("{0,-25} {1,-25} {2}",
                                      conn.LocalEndPoint,
                                      conn.RemoteEndPoint,
                                      conn.State);
                }
                Console.WriteLine();

                var tcpListeners = ipProperties.GetActiveTcpListeners();
                Console.WriteLine("TCP Listeners:");
                Console.WriteLine("{0,-25}", "Local Address");
                foreach (var listener in tcpListeners)
                {
                    Console.WriteLine("{0,-25}", listener);
                }
                Console.WriteLine();
            }

            if (protocol == "udp" || protocol == "all")
            {
                var udpListeners = ipProperties.GetActiveUdpListeners();
                Console.WriteLine("UDP Listeners:");
                Console.WriteLine("{0,-25}", "Local Address");
                foreach (var listener in udpListeners)
                {
                    Console.WriteLine("{0,-25}", listener);
                }
                Console.WriteLine();
            }
        }
    }

    internal class SystemInfo
    {
        private static bool isInitialized = false;
        private static string cpuName = "Unknown CPU";
        private static string gpuName = "Unknown GPU";
        private static float ramAmount = 0;
        private static float ramSpeed = 0;
        private static string osName = "Unknown OS";
        private static string userName = Environment.UserName;

        public static void ShowSystemInfo()
        {
            if (!isInitialized)
            {
                Thread spinnerThread = new Thread(Spinner.ShowSpinner);
                spinnerThread.Start();

                InitializeSystemInfo();
                isInitialized = true;

                Spinner.StopSpinner();
                spinnerThread.Join();
            }

            Console.WriteLine("\n=== System Information ===");
            Console.WriteLine($"OS: {osName}");
            Console.WriteLine($"User: {userName}");
            Console.WriteLine($"CPU: {cpuName}");
            Console.WriteLine($"GPU: {gpuName}");
            Console.WriteLine($"RAM: {ramAmount} GB @ {ramSpeed} MHz");
            Console.WriteLine("==========================\n");
        }

        private static void InitializeSystemInfo()
        {
            osName = GetOSName();

            Computer computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true
            };

            computer.Open();

            foreach (var hardware in computer.Hardware)
            {
                hardware.Update();

                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        cpuName = hardware.Name;
                        break;
                    case HardwareType.GpuNvidia:
                    case HardwareType.GpuAmd:
                    case HardwareType.GpuIntel:
                        gpuName = hardware.Name;
                        break;
                    case HardwareType.Memory:
                        ramAmount = GetTotalRamSize();
                        ramSpeed = GetRamSpeed();
                        break;
                }
            }

            computer.Close();
        }

        private static string GetOSName()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return obj["Caption"].ToString();
                }
            }
            return "Unknown OS";
        }

        private static float GetTotalRamSize()
        {
            float totalRam = 0;
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    totalRam += Convert.ToUInt64(obj["Capacity"]) / 1073741824;
                }
            }
            return totalRam;
        }

        private static float GetRamSpeed()
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Speed FROM Win32_PhysicalMemory"))
            {
                foreach (ManagementObject obj in searcher.Get())
                {
                    return Convert.ToUInt32(obj["Speed"]);
                }
            }
            return 0;
        }
    }


    internal class PortScanning
    {
        public static void ScanPort(string target, int port)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var result = client.BeginConnect(target, port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

                    if (success)
                    {
                        lock (Console.Out)
                        {
                            Console.Write($"\rPort {port} is OPEN.           \n");
                        }
                    }
                }
            }
            catch
            {
                // Silent failure for closed ports
            }
        }
    }

    internal static class Spinner
    {
        private static volatile bool spinnerRunning;

        public static void ShowSpinner()
        {
            spinnerRunning = true;
            char[] spinner = { '|', '/', '-', '\\' };
            int counter = 0;

            while (spinnerRunning)
            {
                Console.Write($"\r{spinner[counter % spinner.Length]}");
                Thread.Sleep(100);
                counter++;
            }

            Console.Write("\r                 \r");
        }

        public static void StopSpinner()
        {
            spinnerRunning = false;
        }
    }

    internal class Encryption
    {
        public static void EncryptText(string[] arguments)
        {
            if (arguments.Length < 1)
            {
                Console.WriteLine("Usage: encrypt <text>");
                return;
            }

            string plainText = string.Join(" ", arguments);

            using (Aes aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                byte[] key = aes.Key;
                byte[] iv = aes.IV;

                ICryptoTransform encryptor = aes.CreateEncryptor(key, iv);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
                        {
                            sw.Write(plainText);
                        }
                    }

                    byte[] encrypted = ms.ToArray();
                    string encryptedText = Convert.ToBase64String(encrypted);
                    string keyString = Convert.ToBase64String(key);
                    string ivString = Convert.ToBase64String(iv);

                    Console.WriteLine("Encrypted: " + encryptedText);
                    Console.WriteLine("Key: " + keyString);
                    Console.WriteLine("IV: " + ivString);
                }
            }
        }

        public static void DecryptText(string[] arguments)
        {
            if (arguments.Length < 3)
            {
                Console.WriteLine("Usage: decrypt <encryptedText> <key> <iv>");
                return;
            }

            string encryptedText = arguments[0];
            string keyString = arguments[1];
            string ivString = arguments[2];

            try
            {
                byte[] cipherBytes = Convert.FromBase64String(encryptedText);
                byte[] key = Convert.FromBase64String(keyString);
                byte[] iv = Convert.FromBase64String(ivString);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(cipherBytes))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                            {
                                string decryptedText = sr.ReadToEnd();
                                Console.WriteLine("Decrypted: " + decryptedText);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Decryption failed: " + ex.Message);
            }
        }
    }


    internal class Downloading
    {
        public static void DownloadUsingChoco(string[] arguments)
        {
            if (arguments.Length == 0)
            {
                Console.WriteLine("Usage: download <packageName> [silent (true/false)]");
                return;
            }

            string packageName = arguments[0];

            bool silent = true;
            if (arguments.Length > 1 && bool.TryParse(arguments[1], out bool silentArg))
            {
                silent = silentArg;
            }

            const string chocoPath = @"C:\ProgramData\chocolatey\bin\choco.exe";

            int RunProcessWithSpinner(ProcessStartInfo psi, string operationDescription)
            {
                Thread spinnerThread = new Thread(Spinner.ShowSpinner);
                spinnerThread.Start();
                try
                {
                    using (Process proc = Process.Start(psi))
                    {
                        proc.WaitForExit();
                        return proc.ExitCode;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during {operationDescription}: {ex.Message}");
                    return -1;
                }
                finally
                {
                    Spinner.StopSpinner();
                    spinnerThread.Join();
                }
            }

            if (!File.Exists(chocoPath))
            {
                Console.WriteLine("Chocolatey is not installed.");
                Console.Write("Do you want to install Chocolatey now? (Y/N): ");
                var key = Console.ReadKey(true);
                Console.WriteLine();

                if (key.Key != ConsoleKey.Y)
                {
                    Console.WriteLine("Chocolatey is required to download packages. Aborting.");
                    return;
                }

                string installCommand = "Set-ExecutionPolicy Bypass -Scope Process -Force; " +
                                        "[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12; " +
                                        "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))";

                Console.WriteLine("Installing Chocolatey...");

                ProcessStartInfo psiInstallChoco = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{installCommand}\"",
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = silent ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
                };

                int installExitCode = RunProcessWithSpinner(psiInstallChoco, "Chocolatey installation");
                if (installExitCode != 0)
                {
                    Console.WriteLine("Error installing Chocolatey. Aborting.");
                    return;
                }

                if (!File.Exists(chocoPath))
                {
                    Console.WriteLine("Chocolatey installation failed. Aborting.");
                    return;
                }
            }

            Console.WriteLine($"Downloading package: {packageName}");

            ProcessStartInfo psiDownload = new ProcessStartInfo
            {
                FileName = chocoPath,
                Arguments = $"install {packageName} -y",
                Verb = "runas",
                UseShellExecute = true,
                WindowStyle = silent ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal
            };

            int downloadExitCode = RunProcessWithSpinner(psiDownload, "package download");
            if (downloadExitCode != 0)
            {
                Console.WriteLine("Error downloading package.");
                return;
            }

            Console.WriteLine($"\nPackage '{packageName}' downloaded successfully.");
        }
    }
}


