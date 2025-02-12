using System.Net.NetworkInformation;
using System;
using System.Management;
using LibreHardwareMonitor.Hardware;

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
            if (arguments.Length == 0)
            {
                Console.WriteLine("Usage: ping <domain> [pingCount]");
                return;
            }

            int pingCount = 1;

            if (arguments.Length == 2 && !int.TryParse(arguments[1], out pingCount))
            {
                Console.WriteLine("Error: Ping count must be an integer.");
                return;
            }

            string targetDomain = arguments[0];
            Ping pingSender = new Ping();

            try
            {
                for (int i = 0; i < pingCount; i++)
                {
                    PingReply reply = pingSender.Send(targetDomain);

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
                Thread spinnerThread = new Thread(ShowSpinner);
                spinnerThread.Start();

                InitializeSystemInfo();

                isInitialized = true;

                spinnerRunning = false;
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

        private static volatile bool spinnerRunning = true;

        private static void ShowSpinner()
        {
            char[] spinner = { '|', '/', '-', '\\' };
            int counter = 0;

            Console.Write("Fetching system info... ");

            while (spinnerRunning)
            {
                Console.Write(spinner[counter % spinner.Length]);
                Thread.Sleep(100);
                Console.Write("\b");
                counter++;
            }
        }
    }
}