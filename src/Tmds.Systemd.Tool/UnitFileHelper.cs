using System.Collections.Generic;
using System.Text;

namespace Tmds.Systemd.Tool
{
    static class UnitFileHelper
    {
        public static string BuildUnitFile(ConfigurationOption[] options, Dictionary<string, string> userOptions, Dictionary<string, string> substitutions)
        {
            var sb = new StringBuilder();
            string currentSection = null;
            foreach (var option in options)
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