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
            new ConfigurationOption(Unit, "Description", null),

            new ConfigurationOption(Service, "Type", enumValues: new[] { "simple", "exec", "forking", "oneshot", "dbus", "notify", "idle" }),
            new ConfigurationOption(Service, "WorkingDirectory", "%execstartdir%"),
            new ConfigurationOption(Service, "ExecStart", required: true),
            new ConfigurationOption(Service, "Restart", enumValues: new[] { "no", "on-success", "on-failure", "on-abnormal", "on-watchdog", "on-abort", "always" }),
            new ConfigurationOption(Service, "SyslogIdentifier"),
            new ConfigurationOption(Service, "User", optionName: "uid"),
            new ConfigurationOption(Service, "Group", optionName: "gid"),
            new ConfigurationOption(Service, "Environment", aliases: new[] { "-e" }, multiple: true ),

            new ConfigurationOption(Install, "WantedBy", "multi-user.target"),
        };

        public static Command Create()
        {
            const string requiredPrefix = "(required) ";

            var createServiceCommand = new Command("create-service", "Creates a systemd unit", handler: CommandHandler.Create(new Func<bool, ParseResult, int>(CreateServiceHandler)));

            var options = new List<Option>();
            options.Add(new Option("--name", $"{requiredPrefix}Name of the unit", new Argument<string>()));
            options.Add(new Option("--user", "Create a user unit", new Argument<bool>()));
            foreach (var configOption in ServiceOptions)
            {
                string description = $"Sets {configOption.Name}";
                if (configOption.Multiple)
                {
                    description += ", may be specified multiple times";
                }
                if (configOption.Default != null)
                {
                    description += $", defaults to '{configOption.Default}'";
                }
                if (configOption.Required)
                {
                    description = requiredPrefix + description;
                }
                Argument arg = configOption.Multiple ? (Argument)new Argument<string[]>() :
                        configOption.Default != null ? new Argument<string>(configOption.Default) : new Argument<string>();
                if (configOption.EnumValues != null)
                {
                    arg.AddSuggestions(configOption.EnumValues);
                }
                options.Add(new Option(configOption.Aliases, description, arg));
            }
            // TODO: add option to add 'any' parameter

            OptionHelper.Sort(options, new[] { "name", "execstart", "user" });
            createServiceCommand.AddOptions(options);

            return createServiceCommand;
        }

        private static int CreateServiceHandler(bool user, ParseResult result)
        {
            bool systemUnit = !user;
            var commandOptions = new ArgumentsDictionary(result.CommandResult.Children);

            if (!Prerequisite.RequiredOption(commandOptions, "name", out string unitName) ||
                !Prerequisite.RequiredOption(commandOptions, "execstart", out string execStartUser) ||
                (systemUnit && !Prerequisite.RunningAsRoot()) ||
                !Prerequisite.ResolveApplication(execStartUser, systemUnit, out string execStart, out string execStartDir))
            {
                return 1;
            }

            var substitutions = new Dictionary<string, string>();
            substitutions.Add("%unitname%", unitName);
            substitutions.Add("%execstartdir%", execStartDir);

            // Replace user execstart with resolved application execstart
            commandOptions.SetValue("execstart", execStart);

            string unitFileContent = UnitFileHelper.BuildUnitFile(ServiceOptions, commandOptions, substitutions);

            string systemdServiceFilePath;
            try
            {
                systemdServiceFilePath = UnitFileHelper.CreateNewUnitFile(systemUnit, $"{unitName}.service", unitFileContent);
            }
            catch (IOException e) when (e.HResult == 17 /* EEXIST */ )
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