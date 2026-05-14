# AngryWasp.PropTool
A basic tool for flashing the firmware of a P8X32A propeller microcontroller

build from source or install as a dotnet tool with  
dotnet tool install -g AngryWasp.PropTool  
Installing the package will give you a global tool 'proptool'  

Usage:  
Get a summary of all command line options by running 'proptool --help'

Examples:
proptool --check COM1  
proptool --port COM1 --target EEPROM --program ./Program.binary --listen  
