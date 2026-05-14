using System;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Cli.Config;
using System.Linq;
using System.Threading;

namespace AngryWasp.PropTool.App
{

    internal class MainClass
    {
        static void Main(string[] rawArgs)
        {
            AngryWasp.Serializer.Serializer.AddSerializerAssembly(Assembly.GetExecutingAssembly());
            var parsedArgs = Arguments.Parse(rawArgs);
            CommandLine cl = new CommandLine();
            if (!ConfigMapper<CommandLine>.Process(parsedArgs, cl, null))
                return;

            Application.RegisterCommands();

            if (SerialPort.GetPortNames().Count(x => x == cl.Port) == 0)
            {
                Console.WriteLine($"Serial port {cl.Port} does not exist");
                return;
            }

            if (cl.Check)
            {
                Check(new PropTool(), cl.Port, cl.Baud);
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(cl.Program))
                {
                    if (!File.Exists(cl.Program))
                    {
                        Console.WriteLine($"Invalid program path: {0}");
                        return;
                    }
                    else
                    {
                        PropTool pt = new PropTool();

                        Console.WriteLine($"Loading program {cl.Program} to {cl.Target}");
                        pt.OpenPort(cl.Port, cl.Baud);

                        if (pt.LoadBinaryFile(cl.Program, cl.Target))
                            Console.WriteLine("OK!");
                        else
                        {
                            Console.WriteLine("ERROR!");
                            pt.ClosePort();
                            return;
                        }

                        pt.ClosePort();
                    }
                }
            }

            if (!cl.Listen)
                return;

            var pt2 = new PropTool();
            pt2.OpenPort(cl.Port, cl.Baud);
            Console.ReadKey();
            pt2.ClosePort();
        }

        private static bool Check(PropTool propTool, string port, int baud)
        {
            Console.WriteLine("Checking for propeller on port {0}", port);
            propTool.OpenPort(port, baud);
            Thread.Sleep(250);
            bool found = propTool.HwFind();
            Console.WriteLine(found ? "Propeller found" : "Propeller not found");
            propTool.ClosePort();
            return found;
        }
    }
}