using System;
using System.Diagnostics;
using System.IO;

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
    }
}