using System.Net.NetworkInformation;
using System;
using System.Management;
using LibreHardwareMonitor.Hardware;
using System.Text;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

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
            if (arguments.Length == 0) { Console.WriteLine("Usage: ping <address> [pingCount] [delay] [bufferSize]"); return; }

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
                Console.WriteLine("Usage: portscan <target> <startPort> <endPort>");
                return;
            }

            if (!int.TryParse(arguments[1], out int startPort) || !int.TryParse(arguments[2], out int endPort))
            {
                Console.WriteLine("Error: Ports must be integers.");
                return;
            }

            if (startPort > endPort)
            {
                Console.WriteLine("Error: startPort must not be greater than endPort.");
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
                Console.Write($"\rScanning... {spinner[counter % spinner.Length]}");
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
}
