using CKAN;
using log4net;
using Mono.Unix.Native;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 * CKAN AUTO-UPDATE TOOL
 * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
 *
 * This simple program is used to replace the local ckan.exe with the latest one i.e. auto-update.
 * It is a command-line tool only, meant to be invoked by the main CKAN process and not manually.
 * Argument launch must be one of: launch, nolaunch
 *
 * Invoked as:
 * AutoUpdate.exe <running CKAN PID> <running CKAN path> <updated CKAN path> <launch>
 */

namespace AutoUpdater
{
    internal class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));
        private static readonly int UpdateRetries = 8;

        public static int Main(string[] args)
        {
            Application.Initialize();

            if (args.Length != 4)
            {
                Console.WriteLine("AutoUpdate.exe <running CKAN PID> <running CKAN path> <updated CKAN path> <launch>");
                return -1;
            }

            var pid = int.Parse(args[0]);
            var currentPath = args[1];
            var updatePath = args[2];

            if (!File.Exists(updatePath))
            {
                return -1;
            }

            // wait for CKAN to close
            try
            {
                var process = Process.GetProcessById(pid);
                if (!process.HasExited)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception e)
            {
                Log.Debug("Continuing despite process check exception", e);
            }

            try
            {
                var retries = UpdateRetries;
                while (File.Exists(currentPath) && --retries > 0)
                {
                    File.Delete(currentPath);
                }

                // replace ckan.exe
                File.Move(updatePath, currentPath);

                // if we have a native chmod() call and the OS supports it
                // then make sure we set the +x bits
                if (Platform.IsUnix && Platform.IsMono)
                {
                    var executeMask = FilePermissions.S_IXUSR | FilePermissions.S_IXGRP | FilePermissions.S_IXOTH;
                    var syscall = Syscall.chmod(updatePath, executeMask);
                }
            }
            catch (Exception e)
            {
                Log.Error("Failed to update binary!", e);
                return -1;
            }

            // start the application using platform-compatible syntax
            if (args[3] == "launch")
            {
                if (Platform.IsMono)
                {
                    Process.Start("mono", String.Format("\"{0}\"", currentPath));
                }
                else
                {
                    Process.Start(currentPath);
                }
            }

            // exit code indicates success
            return 0;
        }
    }
}