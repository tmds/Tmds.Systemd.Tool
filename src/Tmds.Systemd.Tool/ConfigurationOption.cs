using System.Collections.Generic;

namespace Tmds.Systemd.Tool
{
    class ConfigurationOption
    {
        public ConfigurationOption(string section, string name, string @default = null, string optionName = null, bool required = false, IReadOnlyCollection<string> aliases = null)
        {
            Section = section;
            Name = name;

            Default = @default;
            OptionName = optionName ?? name.ToLowerInvariant();
            Required = required;

            if (aliases == null)
            {
                Aliases = new[] { $"--{OptionName}" };
            }
            else
            {
                List<string> list = new List<string>();
                list.Add($"--{OptionName}");
                list.AddRange(aliases);
                Aliases = list;
            }
        }

        public string Section { get; }
        public string Name { get; }

        public string Default { get; }

        public string OptionName { get; }
        public bool Required { get; }
        public IReadOnlyCollection<string> Aliases { get; }
    }
}