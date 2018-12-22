#include "neuralnet_win.h"
#include "version.h"
#include "sync.h"
#include "util.h"

#include <boost/filesystem/path.hpp>
#include <boost/algorithm/string.hpp>

#include <functional>
#include <future>
#include <fstream>
#include <map>
#include <array>
#include <cstdio>
#include <string>

using namespace NN;

// Old VB based NeuralNet.
extern std::string qtGetNeuralHash(std::string data);
extern std::string qtGetNeuralContract(std::string data);
extern double qtExecuteGenericFunction(std::string function,std::string data);
extern std::string qtExecuteDotNetStringFunction(std::string function,std::string data);
extern void qtSyncWithDPORNodes(std::string data);
int64_t IsNeural();

// While transitioning to dotnet the NeuralNet implementation has been split
// into 3 implementations; Win32 with Qt, Win32 without Qt and the rest.
// After the transition both Win32 implementations can be removed.

// Win32 with Qt enabled.
bool NeuralNetWin32::IsEnabled()
{
    return GetArgument("disableneuralnetwork", "false") == "false";
}

std::string NeuralNetWin32::GetNeuralVersion()
{
    int neural_id = static_cast<int>(IsNeural());
    return std::to_string(CLIENT_VERSION_MINOR) + "." + std::to_string(neural_id);
}

std::string NeuralNetWin32::GetNeuralHash()
{
    return qtGetNeuralHash("");
}

std::string NeuralNetWin32::GetNeuralContract()
{
    return qtGetNeuralContract("");
}

bool NeuralNetWin32::SynchronizeDPOR(const std::string& data)
{
    qtSyncWithDPORNodes(data);
    return true;
}

std::string NeuralNetWin32::ExecuteDotNetStringFunction(std::string function, std::string data)
{
    return qtExecuteDotNetStringFunction(function, data);
}

int64_t NeuralNetWin32::IsNeuralNet()
{
    return IsNeural();
}
