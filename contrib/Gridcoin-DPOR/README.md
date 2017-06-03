# Gridcoin DPOR
A work-in-progress port of the VB.NET Neural Network to .NET Core. The aim of this project is to create a cross-platform version of the existing Neural Network with some added performance improvements and the ability to remove the Gridcoin Team Requirment.

Still TODO: 
- Add the NLog logging framework and logging throughout the code (with extra debug logging when debug2=true).
- Calculate mags and store in SQlite database using Dapper mini ORM.
- Implement `GetNeuralContract()`
- Implement `GetNeuralHash()`
- Lots of testing.

## Improvements
- Code has been stripped back from what is in the original .NET repo.
- The remote xml files are only downloaded if they have been changed putting less load on the BOINC project servers.
- The data is now stored in a SQlite database instead of flat files so that the Qt UI can access it.

## Commands available
Assuming you have installed .NET Core on your platform and built the source code you should be able to run the following commands at the command prompt or terminal.

### SyncDPOR
This command downloads all the BOINC project files and calculates magnitudes storing them in the **/GridcoinResearch/DPOR/db.sqlite** folder.

```bash
dotnet GridcoinDPOR.dll -d=\"C:\\Users\\3ullShark\\AppData\\Roaming\\GridcoinResearch\" -c=syncdpor2
```

TODO: add `-o=noteam` option to the `syncdpor` command to remove the team requirment.

### NeuralHash
This command gets the neural hash of the downloaded magnitude data.

TODO: Still not implemented

### NeuralContract
This command gets the neural contract of the downloaded magnitued data.

TODO: Still not implemented

## How to Build the Source
With .NET Core being cross-platform it's possible to develop on Windows, Linux and Mac and build the source on any platform. It is recommended you use the free cross-platform editor called [Visual Studio Code][1] to make changes.

Before you begin you will need to install the .NET Core SDK for your platform. Head over to the [.NET Core website][3] to find out how.

You can build the source in Visual Studio Code by pressing <kbd>CTRL</kbd>+<kbd>SHIFT</kbd>+<kbd>B</kbd>

To build a release that can be copied into the GridcoinResearch program folder navigate to the **/src/GridcoinDPOR** folder and then run the following at the command prompt:

```bash
dotnet restore
dotnet publish -c Release
```

This will copy the files to **/src/GridcounDPOR/bin/Release/publish** folder.

## How to Debug
In order to step through the code with the deubugger in Visual Studio Code you will need to do the following.

**Step 1:** Modify the **launch.json** file located in the **.vscode** directory so that the `args` field has the path to your GridcoinResearch data folder and the command you want to run. For example. 

```json
"args": ["-d=C:\Users\3ullShark\AppData\Roaming\GridcoinResearch -c=syncdpor"]
```

**Step 2:** Copy the **syncdpor.dat** file located in **src/GridcoinDPOR** into your GridcoinResrach folder.

**Step 3:** Now put a breakpoint in `Program.cs` or anyware else in the code and press <kbd>F5</kbd> to start debugging.

## How to Run the Unit Tests
Navigate to the **test/GridcoinDPOR.Tests** directory and then it the command prompt/terminal run the following:

```bash
dotnet restore
dotnet test
```

## Executing from C++
Currently the .NET Code can be executed from C++ by spawning a process and getting the contents of the output. However it is possible to host .NET Core in C++ and communicate directly with managed code but it is a lot more work to setup. Here is an example of C++ code that can be used to spawn the .NET Core process:

```c++
#include <iostream>
#include <stdexcept>
#include <stdio.h>
#include <string>

std::string exec(const char* cmd) {
    char buffer[128];
    std::string result = "";
    FILE* pipe = popen(cmd, "r");
    if (!pipe) throw std::runtime_error("popen() failed!");
    try {
        while (!feof(pipe)) {
            if (fgets(buffer, 128, pipe) != NULL)
                result += buffer;
        }
    } catch (...) {
        pclose(pipe);
        throw;
    }
    pclose(pipe);
    return result;
}

int main()
{   
    std::string result = exec("dotnet GridcoinDPOR.dll -d=\"C:\\Users\\3ullShark\\AppData\\Roaming\\GridcoinResearch\" -c=syncdpor2");
    std::cout << result;
    return 0;
}
```

## Getting Help
Leave an issue or contact BullShark in the official [#gridcoin][2] channel on IRC for any development related questions.

## License
© The Gridcoin Developers, 2017 Licensed under an [MIT License](/LICENSE).

[1]: https://code.visualstudio.com/
[2]: https://kiwiirc.com/client/irc.freenode.net:6667/#gridcoin
[3]: https://www.microsoft.com/net/core#linuxdebian

