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
    class CreateServiceCommand
    {

        const string Unit = nameof(Unit);
        const string Service = nameof(Service);
        const string Install = nameof(Install);

        public static readonly ConfigurationOption[] ServiceOptions = new ConfigurationOption[]
        {
            new ConfigurationOption(Unit, "Description", "%unitname%"),

            new ConfigurationOption(Service, "Type", "simple"),
            new ConfigurationOption(Service, "WorkingDirectory", "%workingdirectory%"),
            new ConfigurationOption(Service, "ExecStart", "%execstart%", required: true),
            new ConfigurationOption(Service, "Restart", "on-failure"),
            new ConfigurationOption(Service, "SyslogIdentifier", "%unitname%"),
            new ConfigurationOption(Service, "User", optionName: "uid"),
            new ConfigurationOption(Service, "Group", optionName: "gid"),

            new ConfigurationOption(Install, "WantedBy", "multi-user.target"),
        };

        public static Command Create()
        {
            const string requiredSuffix = " (required)";

            var createServiceCommand = new Command("create-service", "Creates a systemd service", handler: CommandHandler.Create(new Func<bool, ParseResult, int>(CreateServiceHandler)));

            var options = new List<Option>();
            options.Add(new Option("--name", $"Name of the service{requiredSuffix}", new Argument<string>()));
            options.Add(new Option("--user", "Create a user service", new Argument<bool>()));
            foreach (var configOption in ServiceOptions)
            {
                options.Add(new Option(configOption.Aliases, $"Sets {configOption.Name}{(configOption.Required ? requiredSuffix : "")}", new Argument<string>()));
            }
            // TODO: add option to add environment variables
            // TODO: add option to add 'any' parameter

            OptionHelper.Sort(options, new[] { "name", "execstart", "user" });
            createServiceCommand.AddOptions(options);

            return createServiceCommand;
        }

        private static int CreateServiceHandler(bool user, ParseResult result)
        {
            bool systemUnit = !user;
            var commandOptions = OptionHelper.GetCommandOptions(result);

            if (!Prerequisite.RequiredOption(commandOptions, "name", out string unitName) ||
                !Prerequisite.RequiredOption(commandOptions, "execstart", out string execStartUser) ||
                (systemUnit && !Prerequisite.RunningAsRoot()) ||
                !Prerequisite.ResolveApplication(execStartUser, systemUnit, out string execStart, out string workingDirectory))
            {
                return 1;
            }

            // Replace user execstart with resolved application execstart
            commandOptions.Remove("execstart");
            commandOptions.Add("execstart", execStart);
            // Use workingdirectory of resolved application if not specified by the user
            if (!commandOptions.ContainsKey("workingdirectory"))
            {
                commandOptions.Add("workingdirectory", workingDirectory);
            }

            var substitutions = new Dictionary<string, string>();
            substitutions.Add("%unitname%", unitName);

            string unitFileContent = UnitFileHelper.BuildUnitFile(ServiceOptions, commandOptions, substitutions);

            string systemdServiceFilePath;
            if (user)
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
                if (systemUnit)
                {
                    // Create a new empty file.
                    using (FileStream fs = new FileStream(systemdServiceFilePath, FileMode.CreateNew))
                    {
                        using (StreamWriter sw = new StreamWriter(fs))
                        { }
                    }
                    bool deleteFile = true;
                    try
                    {
                        if (!ProcessHelper.ExecuteSuccess("chmod", $"644 {systemdServiceFilePath}") ||
                            !ProcessHelper.ExecuteSuccess("chown", $"root:root {systemdServiceFilePath}"))
                        {
                            System.Console.WriteLine($"Failed to set permissions and ownership of {systemdServiceFilePath}");
                            return 1;
                        }
                        deleteFile = false;
                    }
                    finally
                    {
                        try
                        {
                            if (deleteFile)
                            {
                                File.Delete(systemdServiceFilePath);
                            }
                        }
                        catch
                        {}
                    }
                }
                using (FileStream fs = new FileStream(systemdServiceFilePath, systemUnit ? FileMode.Truncate : FileMode.CreateNew))
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
            string userOption = user ? " --user" : string.Empty;
            string sudoPrefix = user ? string.Empty : "sudo ";
            Console.WriteLine($"{sudoPrefix}systemctl{userOption} daemon-reload # Notify systemd a new service file exists");
            Console.WriteLine($"{sudoPrefix}systemctl{userOption} start {unitName}  # Start the service");
            Console.WriteLine($"{sudoPrefix}systemctl{userOption} status {unitName} # Check the service status");
            Console.WriteLine($"{sudoPrefix}systemctl{userOption} enable {unitName} # Automatically start the service");

            return 0;
        }
    }
}