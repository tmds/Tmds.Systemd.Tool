using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Tmds.Systemd.Tool
{
    static class Prerequisite
    {
        public static bool ResolveApplication(string execStartUser, bool systemUnit, out string execStart, out string workingDirectory)
        {
            workingDirectory = null;
            if (File.Exists(execStartUser))
            {
                execStart = Path.GetFullPath(execStartUser);
            }
            else
            {
                execStart = ProcessHelper.FindProgramInPath(execStartUser);
            }
            if (execStart == null)
            {
                Console.WriteLine($"Cannot find '{execStartUser}'");
                return false;
            }
            workingDirectory = Path.GetDirectoryName(execStart);
            if (execStart.EndsWith(".dll"))
            {
                if (!FindProgramInPath("dotnet", out string dotnetPath))
                {
                    return false;
                }

                // Fedora: ensure dotnet exe has proper SELinux context.
                if (systemUnit &&
                    dotnetPath == "/usr/bin/dotnet" &&
                    ProcessHelper.Execute("readlink", dotnetPath).Trim() == "/usr/lib64/dotnet/dotnet")
                {
                    string dotnetExePath = "/usr/lib64/dotnet/dotnet";

                    if (ProcessHelper.Execute("stat", $"-c %C {dotnetExePath}")?.Contains("lib_t") == true)
                    {
                        Console.WriteLine($"The dotnet executable at {dotnetExePath} doesn't have the proper SELinux context.");
                        System.Console.WriteLine("Please update the context by running the following commands:");
                        Console.WriteLine("sudo yum install -y policycoreutils-python-utils");
                        Console.WriteLine($"sudo semanage fcontext -a -t bin_t {dotnetExePath.Replace("/lib64/", "/lib/")}");
                        Console.WriteLine($"sudo restorecon -v {dotnetExePath}");
                        return false;
                    }
                }
                execStart = $"'{dotnetPath}' '{execStart}'";
            }
            string scls = GetSoftwareCollections();
            if (scls != null)
            {
                string sclPath = ProcessHelper.FindProgramInPath("scl");
                if (sclPath != null)
                {
                    execStart = $"{sclPath} enable {scls} -- {execStart}";
                }
            }

            return true;
        }

        [DllImport("libc")]
        public static extern int geteuid();

        public static bool RunningAsRoot()
        {
            int euid = geteuid();
            if (euid != 0)
            {
                // TODO: print a full command line that includes the executable.
                string sudoCommand = "sudo";
                string scls = GetSoftwareCollections();
                if (scls != null)
                {
                    sudoCommand = $"{sudoCommand} scl enable {scls} --";
                }
                Console.WriteLine($"This command needs root. Please run it with '{sudoCommand}'.");
                return false;
            }
            return true;
        }

        private static bool FindProgramInPath(string program, out string programPath)
        {
            programPath = ProcessHelper.FindProgramInPath(program);

            if (programPath == null)
            {
                Console.WriteLine($"Cannot find {program} on PATH");
                return false;
            }

            return true;
        }

        private static string GetSoftwareCollections() => Environment.GetEnvironmentVariable("X_SCLS");

        public static bool RequiredOption(Dictionary<string, string> commandOptions, string name, out string value)
        {
            if (!commandOptions.TryGetValue(name.ToLowerInvariant(), out value))
            {
                Console.WriteLine($"Missing required option: --{name}");
                return false;
            }
            return true;
        }
    }
}