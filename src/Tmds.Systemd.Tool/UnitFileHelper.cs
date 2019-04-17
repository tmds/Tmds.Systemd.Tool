using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tmds.Systemd.Tool
{
    static class UnitFileHelper
    {
        public static string BuildUnitFile(ConfigurationOption[] options, ArgumentsDictionary optionValues, Dictionary<string, string> substitutions)
        {
            var sb = new StringBuilder();
            string currentSection = null;
            foreach (var option in options)
            {
                IReadOnlyCollection<string> value;
                if (!optionValues.TryGetValue(option.Name.ToLowerInvariant(), out value))
                {
                    string defaultValue = option.Default;
                    if (defaultValue != null)
                    {
                        value = new[] { defaultValue };
                    }
                }
                if (value != null && value.Count > 0 && !string.IsNullOrEmpty(value.First()))
                {
                    // Start a new section
                    if (currentSection != option.Section)
                    {
                        if (currentSection != null)
                        {
                            sb.AppendLine();
                        }
                        sb.AppendLine($"[{option.Section}]");
                        currentSection = option.Section;
                    }
                    foreach (var val in value)
                    {
                        string substitutedValue = val;
                        foreach (var subsitution in substitutions)
                        {
                            substitutedValue = substitutedValue.Replace(subsitution.Key, subsitution.Value);
                        }
                        sb.AppendLine($"{option.Name}={substitutedValue}");
                    }
                }
            }
            return sb.ToString();
        }

        public static string CreateNewUnitFile(bool systemUnit, string filename, string content)
        {
            string systemdServiceFilePath;
            if (systemUnit)
            {
                systemdServiceFilePath = $"/etc/systemd/system/{filename}";
            }
            else
            {
                string userUnitFolder = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}/.config/systemd/user";
                Directory.CreateDirectory(userUnitFolder);
                systemdServiceFilePath = Path.Combine(userUnitFolder, $"{filename}");
            }

            if (systemUnit)
            {
                FileHelper.CreateFileForRoot(systemdServiceFilePath);
            }

            using (FileStream fs = new FileStream(systemdServiceFilePath, systemUnit ? FileMode.Truncate : FileMode.CreateNew))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(content);
                }
            }

            return systemdServiceFilePath;
        }
    }
}