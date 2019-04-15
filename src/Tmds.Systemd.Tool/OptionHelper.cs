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