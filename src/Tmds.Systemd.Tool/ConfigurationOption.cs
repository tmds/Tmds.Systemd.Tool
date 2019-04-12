namespace Tmds.Systemd.Tool
{
    class ConfigurationOption
    {
        public ConfigurationOption(string section, string name, string @default)
        {
            Section = section;
            Name = name;
            Default = @default;
        }
        public string Section { get; }
        public string Name { get; }
        public string Default { get; }

        const string Unit = nameof(Unit);
        const string Service = nameof(Service);
        const string Install = nameof(Install);

        public static readonly ConfigurationOption[] ServiceOptions = new ConfigurationOption[]
        {
            new ConfigurationOption(Unit, "Description", ".NET Core %unitname%"),

            new ConfigurationOption(Service, "Type", "simple"),
            new ConfigurationOption(Service, "WorkingDirectory", "%workingdirectory%"),
            new ConfigurationOption(Service, "ExecStart", "%execstart%"),
            new ConfigurationOption(Service, "Restart", "on-failure"),
            new ConfigurationOption(Service, "SyslogIdentifier", "%unitname%"),
            new ConfigurationOption(Service, "User", null),
            new ConfigurationOption(Service, "Group", null),

            new ConfigurationOption(Install, "WantedBy", "multi-user.target"),
        };
    }
}