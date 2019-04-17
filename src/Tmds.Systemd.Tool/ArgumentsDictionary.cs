using System.Collections.Generic;
using System.CommandLine;
using System.Linq;

namespace Tmds.Systemd.Tool
{
    class ArgumentsDictionary
    {
        Dictionary<string, IReadOnlyCollection<string>> _arguments;

        public ArgumentsDictionary(SymbolResultSet symbolResults)
        {
            _arguments = new Dictionary<string, IReadOnlyCollection<string>>();
            foreach (var result in symbolResults)
            {
                _arguments.Add(result.Name, result.Arguments);
            }
        }

        public bool TryGetValue(string name, out string value)
        {
            IReadOnlyCollection<string> values;
            if (_arguments.TryGetValue(name, out values) &&
                values.Count == 1)
            {
                value = values.First();
                return true;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public bool TryGetValue(string name, out IReadOnlyCollection<string> values)
        {
            return _arguments.TryGetValue(name, out values);
        }

        public void SetValue(string name, string value)
        {
            _arguments[name] = new[] { value };
        }

        public void SetIfNotContains(string name, string value)
        {
            if (!_arguments.ContainsKey(name))
            {
                _arguments.Add(name, new[] { value });
            }
        }
    }
}