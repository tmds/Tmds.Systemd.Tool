using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Tmds.Systemd.Tool
{
    static class ProcessHelper
    {
        public static string Execute(string filename, string arguments)
        {
            using (var process = System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            }))
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    return process.StandardOutput.ReadToEnd();
                }
            }
            return null;
        }

        public static string[] GetApplicationArguments()
        {
            return File.ReadAllText($"/proc/{Process.GetCurrentProcess().Id}/cmdline").Split(new[] { '\0' });
        }

        public static bool ExecuteSuccess(string filename, string arguments)
        {
            using (var process = System.Diagnostics.Process.Start(new ProcessStartInfo
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            }))
            {
                process.WaitForExit();
                return process.ExitCode == 0;
            }
        }

        public static string FindProgramInPath(string program)
        {
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var paths = pathEnvVar.Split(':');
                foreach (var path in paths)
                {
                    string filename = Path.Combine(path, program);
                    if (File.Exists(filename))
                    {
                        return filename;
                    }
                }
            }
            return null;
        }

        public static string CreateCommandLine(string[] arguments)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < arguments.Length; i++)
            {
                string arg = arguments[i];
                if (arg.Contains(' '))
                {
                    arg = "'" + arg  + "'";
                }
                sb.Append(arg);
                if (i != arguments.Length - 1)
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }
    }
}