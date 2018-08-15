#include "cpid.h"
#include "init.h"
#include "rpcclient.h"
#include "rpcserver.h"
#include "rpcprotocol.h"
#include "keystore.h"
#include "beacon.h"
#include "appcache.h"

double GetTotalBalance();

std::string GetBurnAddress() { return fTestNet ? "mk1e432zWKH1MW57ragKywuXaWAtHy1AHZ" : "S67nL4vELWwdDVzjgtEP4MxryarTZ9a8GB";
                             }

bool CheckMessageSignature(std::string sAction,std::string messagetype, std::string sMsg, std::string sSig, std::string strMessagePublicKey)
{
    std::string strMasterPubKey = "";
    if (messagetype=="project" || messagetype=="projectmapping")
    {
        strMasterPubKey= msMasterProjectPublicKey;
    }
    else
    {
        strMasterPubKey = msMasterMessagePublicKey;
    }

    if (!strMessagePublicKey.empty()) strMasterPubKey = strMessagePublicKey;
    if (sAction=="D" && messagetype=="beacon") strMasterPubKey = msMasterProjectPublicKey;
    if (sAction=="D" && messagetype=="poll")   strMasterPubKey = msMasterProjectPublicKey;
    if (sAction=="D" && messagetype=="vote")   strMasterPubKey = msMasterProjectPublicKey;
    if (messagetype=="protocol")  strMasterPubKey = msMasterProjectPublicKey;

    std::string db64 = DecodeBase64(sSig);
    CKey key;
    if (!key.SetPubKey(ParseHex(strMasterPubKey))) return false;
    std::vector<unsigned char> vchMsg = std::vector<unsigned char>(sMsg.begin(), sMsg.end());
    std::vector<unsigned char> vchSig = std::vector<unsigned char>(db64.begin(), db64.end());
    if (!key.Verify(Hash(vchMsg.begin(), vchMsg.end()), vchSig)) return false;
    return true;
}

bool VerifyCPIDSignature(std::string sCPID, std::string sBlockHash, std::string sSignature)
{
    std::string sBeaconPublicKey = GetBeaconPublicKey(sCPID, false);
    std::string sConcatMessage = sCPID + sBlockHash;
    bool bValid = CheckMessageSignature("R","cpid", sConcatMessage, sSignature, sBeaconPublicKey);
    if(!bValid)
        LogPrintf("VerifyCPIDSignature: invalid signature sSignature=%s, cached key=%s"
                  ,sSignature, sBeaconPublicKey);
    return bValid;
}

std::string SignMessage(std::string sMsg, std::string sPrivateKey)
{
    CKey key;
    std::vector<unsigned char> vchMsg = std::vector<unsigned char>(sMsg.begin(), sMsg.end());
    std::vector<unsigned char> vchPrivKey = ParseHex(sPrivateKey);
    std::vector<unsigned char> vchSig;
    key.SetPrivKey(CPrivKey(vchPrivKey.begin(), vchPrivKey.end())); // if key is not correct openssl may crash
    if (!key.Sign(Hash(vchMsg.begin(), vchMsg.end()), vchSig))
    {
        return "Unable to sign message, check private key.";
    }
    const std::string sig(vchSig.begin(), vchSig.end());
    std::string SignedMessage = EncodeBase64(sig);
    return SignedMessage;
}

std::string SendMessage(bool bAdd, std::string sType, std::string sPrimaryKey, std::string sValue,
                       std::string sMasterKey, int64_t MinimumBalance, double dFees, std::string strPublicKey)
{
    std::string sAddress = GetBurnAddress();
    CBitcoinAddress address(sAddress);
    if (!address.IsValid())       throw JSONRPCError(RPC_INVALID_ADDRESS_OR_KEY, "Invalid Gridcoin address");
    int64_t nAmount = AmountFromValue(dFees);
    // Wallet comments
    CWalletTx wtx;
    if (pwalletMain->IsLocked())  throw JSONRPCError(RPC_WALLET_UNLOCK_NEEDED, "Error: Please enter the wallet passphrase with walletpassphrase first.");
    std::string sMessageType      = "<MT>" + sType  + "</MT>";  //Project or Smart Contract
    std::string sMessageKey       = "<MK>" + sPrimaryKey   + "</MK>";
    std::string sMessageValue     = "<MV>" + sValue + "</MV>";
    std::string sMessagePublicKey = "<MPK>"+ strPublicKey + "</MPK>";
    std::string sMessageAction    = bAdd ? "<MA>A</MA>" : "<MA>D</MA>"; //Add or Delete
    //Sign Message
    std::string sSig = SignMessage(sType+sPrimaryKey+sValue,sMasterKey);
    std::string sMessageSignature = "<MS>" + sSig + "</MS>";
    wtx.hashBoinc = sMessageType+sMessageKey+sMessageValue+sMessageAction+sMessagePublicKey+sMessageSignature;
    std::string strError = pwalletMain->SendMoneyToDestinationWithMinimumBalance(address.Get(), nAmount, MinimumBalance, wtx);
    if (!strError.empty())        throw JSONRPCError(RPC_WALLET_ERROR, strError);
    return wtx.GetHash().GetHex().c_str();
}

std::string SendContract(std::string sType, std::string sName, std::string sContract)
{
    std::string sPass = (sType=="project" || sType=="projectmapping" || sType=="smart_contract") ? GetArgument("masterprojectkey", msMasterMessagePrivateKey) : msMasterMessagePrivateKey;
    std::string result = SendMessage(true,sType,sName,sContract,sPass,AmountFromValue(1),.00001,"");
    return result;
}

bool SignBlockWithCPID(const std::string& sCPID, const std::string& sBlockHash, std::string& sSignature, std::string& sError, bool bAdvertising=false)
{
    // Check if there is a beacon for this user
    // If not then return false as GetStoresBeaconPrivateKey grabs from the config
    if (!HasActiveBeacon(sCPID) && !bAdvertising)
    {
        sError = "No active beacon";
        return false;
    }
    // Returns the Signature of the CPID+BlockHash message.
    std::string sPrivateKey = GetStoredBeaconPrivateKey(sCPID);
    std::string sMessage = sCPID + sBlockHash;
    sSignature = SignMessage(sMessage,sPrivateKey);
    // If we failed to sign then return false
    if (sSignature == "Unable to sign message, check private key.")
    {
        sError = sSignature;
        sSignature = "";
        return false;
    }
    return true;
}

int64_t AmountFromDouble(double dAmount)
{
    if (dAmount <= 0.0 || dAmount > MAX_MONEY)        throw JSONRPCError(RPC_TYPE_ERROR, "Invalid amount");
    int64_t nAmount = roundint64(dAmount * COIN);
    if (!MoneyRange(nAmount))         throw JSONRPCError(RPC_TYPE_ERROR, "Invalid amount");
    return nAmount;
}

std::string executeRain(std::string sRecipients)
{
    CWalletTx wtx;
    wtx.mapValue["comment"] = "Rain";
    set<CBitcoinAddress> setAddress;
    vector<pair<CScript, int64_t> > vecSend;
    std::string sRainCommand = ExtractXML(sRecipients,"<RAIN>","</RAIN>");
    std::string sRainMessage = MakeSafeMessage(ExtractXML(sRecipients,"<RAINMESSAGE>","</RAINMESSAGE>"));
    std::string sRain = "<NARR>Project Rain: " + sRainMessage + "</NARR>";

    if (!sRainCommand.empty())
        sRecipients = sRainCommand;

    wtx.hashBoinc = sRain;
    int64_t totalAmount = 0;
    double dTotalToSend = 0;
    std::vector<std::string> vRecipients = split(sRecipients.c_str(),"<ROW>");
    LogPrintf("Creating Rain transaction with %" PRId64 " recipients. ", vRecipients.size());

    for (unsigned int i = 0; i < vRecipients.size(); i++)
    {
        std::string sRow = vRecipients[i];
        std::vector<std::string> vReward = split(sRow.c_str(),"<COL>");

        if (vReward.size() > 1)
        {
            std::string sAddress = vReward[0];
            std::string sAmount = vReward[1];

            if (sAddress.length() > 10 && sAmount.length() > 0)
            {
                double dAmount = RoundFromString(sAmount,4);
                if (dAmount > 0)
                {
                    CBitcoinAddress address(sAddress);
                    if (!address.IsValid())
                        throw JSONRPCError(RPC_INVALID_ADDRESS_OR_KEY, string("Invalid Gridcoin address: ")+sAddress);

                    if (setAddress.count(address))
                        throw JSONRPCError(RPC_INVALID_PARAMETER, string("Invalid parameter, duplicated address: ")+sAddress);

                    setAddress.insert(address);
                    dTotalToSend += dAmount;
                    int64_t nAmount = AmountFromDouble(dAmount);
                    CScript scriptPubKey;
                    scriptPubKey.SetDestination(address.Get());
                    totalAmount += nAmount;
                    vecSend.push_back(make_pair(scriptPubKey, nAmount));
                }
            }
        }
    }

    EnsureWalletIsUnlocked();
    // Check funds
    double dBalance = GetTotalBalance();

    if (dTotalToSend > dBalance)
        throw JSONRPCError(RPC_WALLET_INSUFFICIENT_FUNDS, "Account has insufficient funds");
    // Send
    CReserveKey keyChange(pwalletMain);
    int64_t nFeeRequired = 0;
    bool fCreated = pwalletMain->CreateTransaction(vecSend, wtx, keyChange, nFeeRequired);
    LogPrintf("Transaction Created.");

    if (!fCreated)
    {
        if (totalAmount + nFeeRequired > pwalletMain->GetBalance())
            throw JSONRPCError(RPC_WALLET_INSUFFICIENT_FUNDS, "Insufficient funds");
        throw JSONRPCError(RPC_WALLET_ERROR, "Transaction creation failed");
    }

    LogPrintf("Committing.");
    // Rain the recipients
    if (!pwalletMain->CommitTransaction(wtx, keyChange))
    {
        LogPrintf("Commit failed.");

        throw JSONRPCError(RPC_WALLET_ERROR, "Transaction commit failed");
    }
    std::string sNarr = "Rain successful:  Sent " + wtx.GetHash().GetHex() + ".";
    LogPrintf("Success %s",sNarr.c_str());
    return sNarr;
}

struct Message
{
    Message(const std::string& hashBoinc)
    {
        type = ExtractXML(hashBoinc,"<MT>","</MT>");
        key = ExtractXML(hashBoinc,"<MK>","</MK>");
        value = ExtractXML(hashBoinc,"<MV>","</MV>");
        action = ExtractXML(hashBoinc,"<MA>","</MA>");
        signature = ExtractXML(hashBoinc,"<MS>","</MS>");
        pubkey = ExtractXML(hashBoinc,"<MPK>","</MPK>");
    }

    bool IsValid() const
    {
        if (type.empty() || key.empty() || value.empty() || action.empty()  || signature.empty())
            return false;

        if (type=="beacon" && Contains(value,"INVESTOR"))
            return false;

        if (type=="superblock")
            return false;

        if(!CheckMessageSignature(action, type, type + key + value, signature, pubkey))
            return false;

        return true;
    }

    std::string type;
    std::string key;
    std::string value;
    std::string action;
    std::string signature;
    std::string pubkey;
};

typedef std::string MessageType_t;
typedef std::string MessageKey_t;
typedef std::pair<MessageType_t, MessageKey_t> MessagePair_t;
typedef std::list<uint256> TxHashCollection;
std::map<MessagePair_t, TxHashCollection> messageTable;

bool ApplyMessage(const Message& msg, int64_t nTime)
{
    if (msg.action == "A")
    {
        /* With this we allow verifying blocks with stupid beacon */
        if(msg.type == "beacon")
        {
            std::string out_cpid;
            std::string out_address;
            std::string out_publickey;
            GetBeaconElements(msg.value, out_cpid, out_address, out_publickey);
            WriteCache("beaconalt", msg.key + "." + ToString(nTime), out_publickey, nTime);
        }

        WriteCache(msg.type, msg.key, msg.value, nTime);
        if(fDebug10 && msg.type== "beacon" )
            LogPrintf("BEACON add %s %s %s", msg.key, DecodeBase64(msg.value), TimestampToHRDate(nTime));

        if (msg.type == "poll")
        {
            if (Contains(msg.key, "[Foundation"))
            {
                msPoll = "Foundation Poll: " + msg.key.substr(0,80);
            }
            else
            {
                msPoll = "Poll: " + msg.key.substr(0,80);
            }
        }
    }
    else if(msg.action == "D")
    {
        if (fDebug10) LogPrintf("Deleting key type %s Key %s Value %s", msg.type, msg.key, msg.value);
        if(fDebug10 && msg.type == "beacon" ){
            LogPrintf("BEACON DEL %s - %s", msg.key, TimestampToHRDate(nTime));
        }

        DeleteCache(msg.type, msg.key);
    }
    // If this is a boinc project, load the projects into the coin:
    else if (msg.type == "project" || msg.type == "projectmapping")
    {
        //Reserved
    }
    else
        return false;

    LogPrintf("ApplyMessage: %s %s", msg.type, msg.key);

    return true;
}

bool MemorizeMessage(const CTransaction &tx)
{
    Message msg(tx.hashBoinc);

    bool ret = ApplyMessage(msg, tx.nTime);
    if(ret)
    {
        if(fDebug)
            WriteCache("TrxID;" + msg.type, msg.key, tx.GetHash().GetHex(), tx.nTime);

        messageTable[std::make_pair(msg.type, msg.key)].push_back(tx.GetHash());
    }

    return ret;
}

bool ForgetMessage(const CTransaction& tx)
{
    Message msg(tx.hashBoinc);
    if(!msg.IsValid())
        return false;

    // Locate message section.
    const uint256& tx_hash = tx.GetHash();
    auto section_it = messageTable.find(std::make_pair(msg.type, msg.key));
    if(section_it == messageTable.end())
        return false;

    // Locate message
    TxHashCollection& hashes = section_it->second;
    auto message_it = std::find(hashes.begin(), hashes.end(), tx_hash);
    if(message_it == hashes.end())
        return error("ForgetMessage: Hash not memorized, %s", tx_hash.GetHex());

    // Invert message
    if(msg.action == "A")
        msg.action = "D";
    else if(msg.action == "D")
        msg.action == "A";
    else
        return error("ForgetMessage: Invalid message action, %s", msg.action);

    // Erase message
    if(!ApplyMessage(msg, tx.nTime))
        LogPrintf("ForgetMessage: Error unloading message");

    hashes.erase(message_it);

    LogPrintf("ForgetMessage: %s %s", msg.type, msg.key);

    // Load previous message
    while(!hashes.empty())
    {
        uint256 memorized_hash = hashes.back();

        CWalletTx wtx;
        CTransaction memorized_tx;
        uint256 hash_block;
        if(GetTransaction(memorized_hash, memorized_tx, hash_block))
        {
            Message memorized_msg(memorized_tx.hashBoinc);
            if(memorized_msg.IsValid() &&
               ApplyMessage(memorized_msg, tx.nTime))
                break;
        }

        hashes.pop_back();
    }

    return true;
}
