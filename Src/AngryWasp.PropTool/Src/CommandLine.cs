using System;
using AngryWasp.Cli.Config;
using AngryWasp.Serializer;

namespace AngryWasp.PropTool.App
{
    public class CommandLine
    {
        [CommandLineArgument("port", "Serial port to use.")]
        public string Port { get; set; } = "COM1";

        [CommandLineArgument("target", "Flash target. RAM or EEPROM.")]
        public Load_Type Target { get; set; } = Load_Type.RAM;

        [CommandLineArgument("check", "Check for Propeller on port. No flashing will occur if true. Defaults false")]
        public bool Check { get; set; } = false;

        [CommandLineArgument("program", "Path to .binary file to load.")]
        public string Program { get; set; }

        [CommandLineArgument("listen", "Open the serial port or keep open after programming.")]
        public bool Listen { get; set; } = false;

        [CommandLineArgument("baud", "Serial port baud rate. Default 115200")]
        public int Baud { get; set; } = 115200;
    }

    public class LoadTypeSerializer : ISerializer<Load_Type>
    {
        public Load_Type Deserialize(string value) => Enum.Parse<Load_Type>(value);

        public string Serialize(Load_Type value) => value.ToString();
    }
}