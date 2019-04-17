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
    class CreateSocketCommand
    {

        const string Unit = nameof(Unit);
        const string Socket = nameof(Socket);
        const string Install = nameof(Install);

        private static readonly ConfigurationOption[] ConfigurationOptions = new ConfigurationOption[]
        {
            new ConfigurationOption(Unit, "Description"),

            new ConfigurationOption(Socket, "ListenStream", required: true),

            new ConfigurationOption(Install, "WantedBy", "sockets.target"),
        };

        public static Command Create()
        {

            var command = new Command("create-socket", "Creates a socket unit", handler: CommandHandler.Create(new Func<bool, ParseResult, int>(CreateServiceHandler)));

            var options = new List<Option>();
            OptionHelper.AddNameOption(options);
            OptionHelper.AddUserOption(options);
            OptionHelper.AddConfigurationOptions(options, ConfigurationOptions);
            // TODO: add option to add 'any' parameter
            OptionHelper.Sort(options, new[] { "name", "listenstream", "user" });
            command.AddOptions(options);

            return command;
        }

        private static int CreateServiceHandler(bool user, ParseResult result)
        {
            bool systemUnit = !user;
            var commandOptions = new ArgumentsDictionary(result.CommandResult.Children);

            if (!Prerequisite.RequiredOption(commandOptions, "name", out string unitName) ||
                (systemUnit && !Prerequisite.RunningAsRoot()))
            {
                return 1;
            }

            var substitutions = new Dictionary<string, string>();
            substitutions.Add("%unitname%", unitName);

            string unitFileContent = UnitFileHelper.BuildUnitFile(ConfigurationOptions, commandOptions, substitutions);

            if (!UnitFileHelper.TryCreateNewUnitFile(systemUnit, $"{unitName}.socket", unitFileContent, out string unitFilePath))
            {
                return 1;
            }

            string userOption = user ? " --user" : string.Empty;
            string sudoPrefix = user ? string.Empty : "sudo ";
            Console.WriteLine($"Created socket file at: {unitFilePath}");
            Console.WriteLine();
            Console.WriteLine("The following commands may be handy:");
            Console.WriteLine($"{sudoPrefix}systemctl{userOption} daemon-reload # Notify systemd a new service file exists");
            return 0;
        }
    }
}