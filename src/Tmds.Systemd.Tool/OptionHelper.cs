using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Tmds.Systemd.Tool
{
    static class OptionHelper
    {
        public static void Sort(List<Option> options, string[] firstOptions = null)
        {
            if (firstOptions != null)
            {
                for (int i = 0; i < firstOptions.Length; i++)
                {
                    int idx = options.FindIndex(i, options.Count - i, o => o.Name == firstOptions[i]);
                    Option original = options[i];
                    options[i] = options[idx];
                    options[idx] = original;
                }
            }
            int firstOptionsLength = firstOptions?.Length ?? 0;
            options.Sort(firstOptionsLength, options.Count - firstOptionsLength, OrderByOptionName.Instance);
        }

        public const string RequiredPrefix = "(required) ";

        public static void AddNameOption(List<Option> options)
        {
            options.Add(new Option("--name", $"{RequiredPrefix}Name of the unit", new Argument<string>()));
        }

        public static void AddUserOption(List<Option> options)
        {
            options.Add(new Option("--user", "Create a user unit", new Argument<bool>()));
        }

        public static void AddConfigurationOptions(List<Option> options, IReadOnlyCollection<ConfigurationOption> configurationOptions)
        {
            foreach (var configOption in configurationOptions)
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
                    description = RequiredPrefix + description;
                }
                Argument arg = configOption.Multiple ? (Argument)new Argument<string[]>() :
                        configOption.Default != null ? new Argument<string>(configOption.Default) : new Argument<string>();
                if (configOption.EnumValues != null)
                {
                    arg.AddSuggestions(configOption.EnumValues);
                }
                options.Add(new Option(configOption.Aliases, description, arg));
            }
        }

        public static void AddOptions(this Command command, List<Option> options)
        {
            foreach (var option in options)
            {
                command.AddOption(option);
            }
        }

        public static Dictionary<string, string> GetCommandOptions(ParseResult result)
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

        private class OrderByOptionName : IComparer<Option>
        {
            public int Compare(Option x, Option y)
                => string.CompareOrdinal(x.Name, y.Name);

            public static readonly IComparer<Option> Instance = new OrderByOptionName();
        }
    }
}