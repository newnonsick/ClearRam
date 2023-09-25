using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Collections.Generic;
using System.Management;
using System.Media;
using clear;
using Microsoft.VisualBasic.Devices;

namespace FreeMemory
{
    //Declaration of structures
    //SYSTEM_CACHE_INFORMATION
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SYSTEM_CACHE_INFORMATION
    {
        public uint CurrentSize;
        public uint PeakSize;
        public uint PageFaultCount;
        public uint MinimumWorkingSet;
        public uint MaximumWorkingSet;
        public uint Unused1;
        public uint Unused2;
        public uint Unused3;
        public uint Unused4;
    }

    //SYSTEM_CACHE_INFORMATION_64_BIT
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SYSTEM_CACHE_INFORMATION_64_BIT
    {
        public long CurrentSize;
        public long PeakSize;
        public long PageFaultCount;
        public long MinimumWorkingSet;
        public long MaximumWorkingSet;
        public long Unused1;
        public long Unused2;
        public long Unused3;
        public long Unused4;
    }

    //TokPriv1Luid
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TokPriv1Luid
    {
        public int Count;
        public long Luid;
        public int Attr;
    }
    public class Program
    {
        //Declaration of constants
        const int SE_PRIVILEGE_ENABLED = 2;
        const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        const string SE_PROFILE_SINGLE_PROCESS_NAME = "SeProfileSingleProcessPrivilege";
        const int SystemFileCacheInformation = 0x0015;
        const int SystemMemoryListInformation = 0x0050;
        const int MemoryPurgeStandbyList = 4;
        const int MemoryEmptyWorkingSets = 2;

        //Import of DLL's (API) and the necessary functions 
        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool LookupPrivilegeValue(string host, string name, ref long pluid);

        [DllImport("advapi32.dll", SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr htok, bool disall, ref TokPriv1Luid newst, int len, IntPtr prev, IntPtr relen);

        [DllImport("ntdll.dll")]
        public static extern UInt32 NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);

        [DllImport("psapi.dll")]
        static extern int EmptyWorkingSet(IntPtr hwProc);

        //Function to clear working set of all processes
        public static void EmptyWorkingSetFunction()
        {
            //Declaration of variables
            string ProcessName = string.Empty;
            Process[] allProcesses = Process.GetProcesses();
            List<string> successProcesses = new List<string>();
            List<string> failProcesses = new List<string>();

            //Cycle through all processes
            for (int i = 0; i < allProcesses.Length; i++)
            {
                System.Threading.Thread.Sleep(70);
                Process p = new Process();
                p = allProcesses[i];
                //Try to empty the working set of the process, if succesfull add to successProcesses, if failed add to failProcesses with error message
                try
                {
                    ProcessName = p.ProcessName;
                    Misc.Cleartext(ProcessName);
                    EmptyWorkingSet(p.Handle);
                    successProcesses.Add(ProcessName);
                }
                catch (Exception ex)
                {
                    failProcesses.Add(ProcessName + ": " + ex.Message);
                }
            }
            Console.Clear();
            System.Threading.Thread.Sleep(500);
            //Print the lists with successful and failed processes
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("   SUCCESSFULLY CLEARED PROCESSES: " + successProcesses.Count);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   -------------------------------");
            for (int i = 0; i < successProcesses.Count; i++)
            {
                Misc.Outtext(successProcesses[i], ConsoleColor.Green);
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("   FAILED CLEARED PROCESSES: " + failProcesses.Count);
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   -------------------------------");
            for (int i = 0; i < failProcesses.Count; i++)
            {
                Misc.Outtext(failProcesses[i], ConsoleColor.Red);
            }
            Console.WriteLine();
        }

        //Function to check if OS is 64-bit or not, returns boolean
        public static bool Is64BitMode()
        {
            return Marshal.SizeOf(typeof(IntPtr)) == 8;
        }

        //Function used to clear file system cache, returns boolean
        public static void ClearFileSystemCache(bool ClearStandbyCache)
        {
            try
            {
                //Check if privilege can be increased
                if (SetIncreasePrivilege(SE_INCREASE_QUOTA_NAME))
                {
                    uint num1;
                    int SystemInfoLength;
                    GCHandle gcHandle;
                    //First check which version is running, then fill structure with cache information. Throw error is cache information cannot be read.
                    if (!Is64BitMode())
                    {
                        SYSTEM_CACHE_INFORMATION cacheInformation = new SYSTEM_CACHE_INFORMATION();
                        cacheInformation.MinimumWorkingSet = uint.MaxValue;
                        cacheInformation.MaximumWorkingSet = uint.MaxValue;
                        SystemInfoLength = Marshal.SizeOf(cacheInformation);
                        gcHandle = GCHandle.Alloc(cacheInformation, GCHandleType.Pinned);
                        num1 = NtSetSystemInformation(SystemFileCacheInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                        gcHandle.Free();
                    }
                    else
                    {
                        SYSTEM_CACHE_INFORMATION_64_BIT information64Bit = new SYSTEM_CACHE_INFORMATION_64_BIT();
                        information64Bit.MinimumWorkingSet = -1L;
                        information64Bit.MaximumWorkingSet = -1L;
                        SystemInfoLength = Marshal.SizeOf(information64Bit);
                        gcHandle = GCHandle.Alloc(information64Bit, GCHandleType.Pinned);
                        num1 = NtSetSystemInformation(SystemFileCacheInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                        gcHandle.Free();
                    }
                    if (num1 != 0)
                        throw new Exception("NtSetSystemInformation(SYSTEMCACHEINFORMATION) error: ", new Win32Exception(Marshal.GetLastWin32Error()));
                }

                //If passes paramater is 'true' and the privilege can be increased, then clear standby lists through MemoryPurgeStandbyList
                if (ClearStandbyCache && SetIncreasePrivilege(SE_PROFILE_SINGLE_PROCESS_NAME))
                {
                    int SystemInfoLength = Marshal.SizeOf(MemoryPurgeStandbyList);
                    GCHandle gcHandle = GCHandle.Alloc(MemoryPurgeStandbyList, GCHandleType.Pinned);
                    uint num2 = NtSetSystemInformation(SystemMemoryListInformation, gcHandle.AddrOfPinnedObject(), SystemInfoLength);
                    gcHandle.Free();
                    if (num2 != 0)
                        throw new Exception("NtSetSystemInformation(SYSTEMMEMORYLISTINFORMATION) error: ", new Win32Exception(Marshal.GetLastWin32Error()));
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
            }
        }

        //Function to increase Privilege, returns boolean
        private static bool SetIncreasePrivilege(string privilegeName)
        {
            using (WindowsIdentity current = WindowsIdentity.GetCurrent(TokenAccessLevels.Query | TokenAccessLevels.AdjustPrivileges))
            {
                TokPriv1Luid newst;
                newst.Count = 1;
                newst.Luid = 0L;
                newst.Attr = SE_PRIVILEGE_ENABLED;

                //Retrieves the LUID used on a specified system to locally represent the specified privilege name
                if (!LookupPrivilegeValue(null, privilegeName, ref newst.Luid))
                    throw new Exception("Error in LookupPrivilegeValue: ", new Win32Exception(Marshal.GetLastWin32Error()));

                //Enables or disables privileges in a specified access token
                int num = AdjustTokenPrivileges(current.Token, false, ref newst, 0, IntPtr.Zero, IntPtr.Zero) ? 1 : 0;
                if (num == 0)
                    throw new Exception("Error in AdjustTokenPrivileges: ", new Win32Exception(Marshal.GetLastWin32Error()));
                return num != 0;
            }
        }

        //Function to delete temp file

        public static void DeleteTempFile()
        {
            Console.Write(".");
            string tempPath = Path.GetTempPath();

            DirectoryInfo tempDir = new DirectoryInfo(tempPath);

            foreach (FileInfo file in tempDir.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    ;
                }
            }

            foreach (DirectoryInfo dir in tempDir.GetDirectories())
            {
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                    ;
                }
            }
            Thread.Sleep(250);
            Console.Write(".");
        }


        //Function to delete Prefetch file
        static void ClearPrefetchFiles()
        {
            Console.Write(".");
            string prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

            DirectoryInfo prefetchDir = new DirectoryInfo(prefetchPath);

            foreach (FileInfo file in prefetchDir.GetFiles())
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    ;
                }
            }
            Thread.Sleep(250);
            Console.Write(".");
        }



        //MAIN Program
        static void Main(string[] args)
        {
            Console.Title = "Clear Ram | Made with ❤";
            var oldRamCounter = new PerformanceCounter("Memory", "Available Bytes");
            var oldTotalRam = new ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
            var oldRamUsage = oldTotalRam - oldRamCounter.NextValue() / (1024 * 1024);
            Console.ForegroundColor = ConsoleColor.White;

            //delete temp files
            Console.Write("\n   Deleting temp files.");
            Thread.Sleep(250);
            DeleteTempFile();
            Thread.Sleep(250);
            Console.Clear();

            //delete prefetch files
            Console.Write("\n   Deleting prefetch files.");
            Thread.Sleep(250);
            ClearPrefetchFiles();
            Thread.Sleep(250);
            Console.Clear();


            //Clear working set of all processes
            EmptyWorkingSetFunction();
            //Clear file system cache
            ClearFileSystemCache(true);
            Console.ForegroundColor = ConsoleColor.Green;

            Console.WriteLine();
            var newRamCounter = new PerformanceCounter("Memory", "Available Bytes");
            var newTotalRam = new ComputerInfo().TotalPhysicalMemory / (1024 * 1024);
            var newRamUsage = newTotalRam - newRamCounter.NextValue() / (1024 * 1024);

            Misc.Totalramtext(oldRamUsage, "Before", ConsoleColor.Red);
            Misc.Totalramtext(newRamUsage, "After", ConsoleColor.Green);

            //Waiting for input of user to close program
            Misc.Infotext("Press Win + Ctrl + Shift + B to restart your graphics drivers.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("   Press any key to exit.");
            Console.ReadKey();
        }
    }

}