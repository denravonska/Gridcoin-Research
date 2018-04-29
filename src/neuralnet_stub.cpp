#include <string>

#include "neuralnet.h"
namespace NN
{
    std::string neural_contract;
    std::string neural_hash;

    bool IsEnabled()
    {
        return false;
    }

    std::string GetNeuralVersion()
    {
        return "0";
    }

    std::string GetNeuralHash()
    {
        return neural_hash;
    }

    std::string GetNeuralContract()
    {
        return std::string();
    }

    void SetNeuralContract(const std::string& contract, const std::string& hash)
    {
       neural_contract = contract;
       neural_hash = hash;
    }

    bool SetTestnetFlag(bool onTestnet)
    {
        return false;
    }

    bool SynchronizeDPOR(const std::string& data)
    {
        return false;
    }

    std::string ExecuteDotNetStringFunction(std::string function, std::string data)
    {
        return std::string();
    }

    int64_t IsNeuralNet()
    {
        return 0;
    }
}
