using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Timer = System.Windows.Forms.Timer;







namespace XAPI
{
    public static class XAPI
    {

        /*
         I DO NOT RECOMMEND CHANGING ANYTHING HERE, BECAUSE ITS FOR YOUR AUTOUPDATE & REQUIREMENTS DOWNLOAD!
        */
        private static readonly string[] dllFiles =
        {
            "Xeno.dll", // The injector dll
            "xxhash.dll", // references
            "zstd.dll", // references
            "libcrypto-3-x64.dll", // references
            "libssl-3-x64.dll" // references
        };

        private static readonly string[] dllUrls =
        {
            "https://github.com/cloudyExecutor/webb/releases/download/dlls/Xeno.dll",
            "https://github.com/cloudyExecutor/webb/releases/download/dlls/xxhash.dll",
            "https://github.com/cloudyExecutor/webb/releases/download/dlls/zstd.dll",
            "https://github.com/cloudyExecutor/webb/releases/download/dlls/libcrypto-3-x64.dll",
            "https://github.com/cloudyExecutor/webb/releases/download/dlls/libssl-3-x64.dll"
        };

        private static Timer time1 = new Timer();
        private static API XAPIInstance; 
        private static readonly string versionFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cver.txt");
        private static readonly string remoteVersionUrl = "https://raw.githubusercontent.com/cloudyExecutor/webb/refs/heads/main/cxapirefrences.real";
        private static string currentVersion = "69.0.0"; // Default version

        static XAPI()
        {
            EnsureVersionFile();
            EnsureDllsExist();
            CreateXAPIInstance();

            time1.Tick += ticktimer32433;
            time1.Start();
        }

        private static void EnsureVersionFile()
        {
            if (!File.Exists(versionFilePath))
            {
                File.WriteAllText(versionFilePath, currentVersion);
            }
            else
            {
                currentVersion = File.ReadAllText(versionFilePath);
            }

            if (!IsVersionUpToDate())
            {
                // reget Xeno.dll
                string xenoDllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Xeno.dll");
                if (File.Exists(xenoDllPath))
                {
                    File.Delete(xenoDllPath);
                }
                DownloadDll(dllUrls[0], xenoDllPath);
                File.WriteAllText(versionFilePath, GetRemoteVersion());
            }
        }

        private static bool IsVersionUpToDate()
        {
            string remoteVersion = GetRemoteVersion();
            return currentVersion == remoteVersion;
        }

        private static string GetRemoteVersion()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    return client.GetStringAsync(remoteVersionUrl).Result.Trim();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to fetch remote version: {ex.Message}");
                    return currentVersion; // womp womp
                }
            }
        }

        private static void EnsureDllsExist()
        {
            string binFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");

            if (!Directory.Exists(binFolder))
            {
                Directory.CreateDirectory(binFolder);
            }

            for (int i = 0; i < dllFiles.Length; i++)
            {
                string dllPath = Path.Combine(binFolder, dllFiles[i]);
                if (!File.Exists(dllPath))
                {
                    DownloadDll(dllUrls[i], dllPath);
                }
            }
        }

        private static void DownloadDll(string url, string outputPath)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    byte[] dllData = client.GetByteArrayAsync(url).Result;
                    File.WriteAllBytes(outputPath, dllData);  // Replaced async with sync version
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to download DLL from {url}: {ex.Message}");
                }
            }
        }

        private static void CreateXAPIInstance()
        {
            XAPIInstance = new API();
        }

        public static void Inject()
        {
            XAPIInstance?.InjectAPI();
        }

        public static void KillRoblox()
        {
            XAPIInstance?.KillRoblox();
        }

        public static bool IsInjected()
        {
            return XAPIInstance?.IsInjected() ?? false;
        }

        public static bool IsRobloxOpen()
        {
            return Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;
        }

        public static string[] GetActiveClientNames()
        {
            return XAPIInstance?.GetActiveClientNames();
        }

        public static void Execute(string script)
        {
            XAPIInstance?.Execute(script);
        }

        private static void ticktimer32433(object sender, EventArgs e)
        {
            if (!IsRobloxOpen())
            {
                if (XAPIInstance != null)
                {
                    XAPIInstance.Deject();
                    XAPIInstance = null;
                }
            }
            else if (XAPIInstance == null)
            {
                CreateXAPIInstance();
            }
        }

        public static void SetAutoInject(bool value)
        {
            XAPIInstance?.AutoInject(value);
        }
    }


    public class API
    {
        public static string XAPIVersion = "1.1.0";

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct ClientInfo
        {
            public string version;
            public string name;
            public int id;
        }

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Inject();

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr GetClients();

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern void Execute(byte[] scriptSource, string[] clientUsers, int numUsers);

        [DllImport("bin\\Xeno.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr Compilable(byte[] scriptSource);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private bool isInjected;
        private System.Timers.Timer time;
        private bool autoinject;

        public API()
        {
            time = new System.Timers.Timer();
            time.Elapsed += timertick;
            time.AutoReset = true;

            Task.Run(async () =>
            {
                while (true)
                {
                    if (IsRobloxOpen() && autoinject && !isInjected)
                    {
                        InjectAPI();
                    }
                    await Task.Delay(1000);
                }
            });
        }

        public void KillRoblox()
        {
            if (IsRobloxOpen())
            {
                var processes = Process.GetProcessesByName("RobloxPlayerBeta");
                foreach (var process in processes)
                {
                    process.Kill();
                }
            }
        }

        public void AutoInject(bool value)
        {
            autoinject = value;
        }

        public bool IsInjected() => isInjected;

        public bool IsRobloxOpen() => Process.GetProcessesByName("RobloxPlayerBeta").Length != 0;

        public string[] GetActiveClientNames() => GetClientsFromDll().Select(c => c.name).ToArray();

        public void InjectAPI()
        {
            if (IsRobloxOpen())
            {
                try
                {
                    Inject();
                    isInjected = true;
                    if (!time.Enabled)
                    {
                        time.Start();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed To Inject XAPI: " + ex.Message, "Injecting Error");
                    isInjected = false;
                }
            }
        }

        public void Deject()
        {
            isInjected = false;

            IntPtr hModule = GetModuleHandle("bin\\Xeno.dll");

            if (hModule != IntPtr.Zero)
            {
                FreeLibrary(hModule);
            }

            Reload();
        }

        public void Reload()
        {
            if (!isInjected)
            {
                LoadLibrary("bin\\Xeno.dll");
                isInjected = true;
            }
        }

        private void timertick(object sender, EventArgs e)
        {
            if (!IsRobloxOpen())
            {
                isInjected = false;
                if (time.Enabled)
                {
                    time.Stop();
                }
            }
        }

        public void Execute(string script)
        {
            try
            {
                if (!IsInjected() || !IsRobloxOpen())
                {
                    return;
                }

                var clients = GetClientsFromDll();
                if (clients == null || clients.Count == 0)
                {
                    return;
                }

                var clientUsers = clients.GroupBy(c => c.id)
                                         .Select(g => g.First().name)
                                         .ToArray();

                if (clientUsers.Length > 0)
                {
                    Execute(Encoding.UTF8.GetBytes(script), clientUsers, clientUsers.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing script: {ex.Message}");
            }
        }

        public string GetCompilableStatus(string script)
        {
            IntPtr resultPtr = Compilable(Encoding.ASCII.GetBytes(script));
            string result = Marshal.PtrToStringAnsi(resultPtr);
            Marshal.FreeCoTaskMem(resultPtr);
            return result;
        }

        private List<ClientInfo> GetClientsFromDll()
        {
            var clients = new List<ClientInfo>();
            IntPtr currentPtr = GetClients();
            while (true)
            {
                var client = Marshal.PtrToStructure<ClientInfo>(currentPtr);
                if (client.name == null) break;
                clients.Add(client);
                currentPtr += Marshal.SizeOf<ClientInfo>();
            }
            return clients;
        }
    }
}