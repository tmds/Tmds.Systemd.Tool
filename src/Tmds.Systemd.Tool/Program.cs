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
    class Program
    {
        static int Main(string[] args)
        {
            var rootCommand = new RootCommand();
            rootCommand.AddCommand(CreateServiceCommand.Create());
            rootCommand.AddCommand(CreateSocketCommand.Create());
            return rootCommand.InvokeAsync(args).Result;            
        }
    }
}
