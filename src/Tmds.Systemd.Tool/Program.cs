using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Tmds.Systemd.Tool
{
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddCommand(CreateServiceCommand());
            return rootCommand.InvokeAsync(args).Result;            
        }

        private static Command CreateServiceCommand()
        {
            var createServiceCommand = new Command("create-service", "Creates a systemd service", handler: CommandHandler.Create(new Func<bool, ParseResult, int>(CreateServiceHandler)));
            createServiceCommand.AddOption(new Option("--name", "Name of the service", new Argument<string>()));
            createServiceCommand.AddOption(new Option("--user-unit", "Create a user service", new Argument<bool>()));
            foreach (var configOption in ConfigurationOption.ServiceOptions)
            {
                createServiceCommand.AddOption(new Option($"--{configOption.Name.ToLowerInvariant()}", $"Sets {configOption.Name}", new Argument<string>()));
            }
            // TODO: place this in a specific order and mark required arguments...
            return createServiceCommand;
        }

        private static Dictionary<string, string> GetCommandOptions(ParseResult result)
        {
            var userOptions = new Dictionary<string, string>();
            foreach (var childResult in result.CommandResult.Children)
            {
                if (childResult.Arguments.Count > 0)
                {
                    userOptions.Add(childResult.Name.ToLowerInvariant(), childResult.Arguments.First());
                }
            }
            return userOptions;
        }

        private static bool GetRequired(Dictionary<string, string> commandOptions, string name, out string value)
        {
            if (!commandOptions.TryGetValue(name.ToLowerInvariant(), out value))
            {
                Console.WriteLine($"Missing required option: --{name}");
                return false;
            }
            return true;
        }

        private static bool ResolveApplication(string execStartUser, out string execStart, out string workingDirectory)
        {
            workingDirectory = null;
            if (File.Exists(execStartUser))
            {
                execStart = Path.GetFullPath(execStartUser);
            }
            else
            {
                execStart = FindProgramInPath(execStartUser);
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
                execStart = $"'{dotnetPath}' '{execStart}'";
            }
            string scls = GetSoftwareCollections();
            if (scls != null)
            {
                string sclPath = FindProgramInPath("scl");
                if (sclPath != null)
                {
                    execStart = $"{sclPath} enable {scls} -- {execStart}";
                }
            }

            return true;
        }

        private static bool FindProgramInPath(string program, out string programPath)
        {
            programPath = FindProgramInPath(program);

            // Deal with SELinux issue on Fedora:
            if (program == "dotnet" && programPath == "/usr/lib64/dotnet/dotnet")
            {
                using (var process = System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = "stat",
                    Arguments = $"-c %C {programPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true
                }))
                {
                    process.WaitForExit();
                    if (process.ExitCode == 0)
                    {
                        var context = process.StandardOutput.ReadToEnd();
                        if (context.Contains("lib_t"))
                        {
                            Console.WriteLine($"The dotnet executable at {programPath} doesn't have the proper SELinux context.");
                            System.Console.WriteLine("Please update the context by running the following commands:");
                            System.Console.WriteLine($"sudo semanage fcontext -a -t bin_t {programPath}");
                            System.Console.WriteLine($"sudo restorecon -R -v {programPath}");
                            return false;
                        }
                    }
                }
            }

            if (programPath == null)
            {
                Console.WriteLine($"Cannot find {program} on PATH");
                return false;
            }

            return true;
        }

        private static string BuildUnitFile(ConfigurationOption[] options, Dictionary<string, string> userOptions, Dictionary<string, string> substitutions)
        {
            var sb = new StringBuilder();
            string currentSection = null;
            foreach (var option in ConfigurationOption.ServiceOptions)
            {
                string optionValue = Evaluate(option.Name, userOptions, option.Default, substitutions);
                if (optionValue != null)
                {
                    if (currentSection != option.Section)
                    {
                        if (currentSection != null)
                        {
                            sb.AppendLine();
                        }
                        sb.AppendLine($"[{option.Section}]");
                        currentSection = option.Section;
                    }
                    sb.AppendLine($"{option.Name}={optionValue}");
                }
            }
            return sb.ToString();
        }

        [DllImport("libc")]
        public static extern int geteuid();

        private static bool VerifyRunningAsRoot()
        {
            int euid = geteuid();
            if (euid != 0)
            {
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

        private static string GetSoftwareCollections() => Environment.GetEnvironmentVariable("X_SCLS");

        private static int CreateServiceHandler(bool userUnit, ParseResult result)
        {
            var commandOptions = GetCommandOptions(result);

            if (!GetRequired(commandOptions, "name", out string unitName) ||
                !GetRequired(commandOptions, "execstart", out string execStartUser) ||
                (!userUnit && !VerifyRunningAsRoot()) ||
                !ResolveApplication(execStartUser, out string execStart, out string workingDirectory))
            {
                return 1;
            }

            commandOptions.Remove("execstart");
            commandOptions.Add("execstart", execStart);
            if (!commandOptions.ContainsKey("workingdirectory"))
            {
                commandOptions.Add("workingdirectory", workingDirectory);
            }

            var substitutions = new Dictionary<string, string>();
            substitutions.Add("%unitname%", unitName);

            string unitFileContent = BuildUnitFile(ConfigurationOption.ServiceOptions, commandOptions, substitutions);

            string systemdServiceFilePath;
            if (userUnit)
            {
                string userUnitFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/.config/systemd/user";
                Directory.CreateDirectory(userUnitFolder);
                systemdServiceFilePath = Path.Combine(userUnitFolder, $"{unitName}.service");
            }
            else
            {
                systemdServiceFilePath = $"/etc/systemd/system/{unitName}.service";
            }

            try
            {
                // TODO: permissions
                using (FileStream fs = new FileStream(systemdServiceFilePath, FileMode.CreateNew))
                {
                    using (StreamWriter sw = new StreamWriter(fs))
                    {
                        sw.Write(unitFileContent);
                    }
                }
            }
            catch (IOException) when (File.Exists(systemdServiceFilePath))
            {
                Console.WriteLine("A service with that name already exists.");
                return 1;
            }

            Console.WriteLine($"Created service file at: {systemdServiceFilePath}");

            Console.WriteLine();
            Console.WriteLine("The following commands may be handy:");
            string userOption = userUnit ? "--user" : string.Empty;
            string sudoPrefix = userUnit ? string.Empty : "sudo ";
            Console.WriteLine($"{sudoPrefix}systemctl {userOption} daemon-reload # Notify systemd a new service file exists");
            Console.WriteLine($"{sudoPrefix}systemctl {userOption} start {unitName}  # Start the service");
            Console.WriteLine($"{sudoPrefix}systemctl {userOption} status {unitName} # Check the service status");
            Console.WriteLine($"{sudoPrefix}systemctl {userOption} enable {unitName} # Automatically start the service");

            return 0;
        }

        private static string FindProgramInPath(string program)
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

        private static string Evaluate(string name, Dictionary<string, string> userOptions, string @default, Dictionary<string, string> substitutions)
        {
            string userValue;
            if (!userOptions.TryGetValue(name.ToLowerInvariant(), out userValue))
            {
                userValue = @default;
            }
            if (userValue != null)
            {
                foreach (var substitution in substitutions)
                {
                    userValue = userValue.Replace(substitution.Key, substitution.Value);
                }
            }
            return userValue;
        }
    }
}
