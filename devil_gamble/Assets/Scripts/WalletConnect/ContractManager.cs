using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using UnityEngine;
using Utils;

public class ContractManager : MonoBehaviour
{
    public static string playerAddress;
    public static string contractAddress = "0x0585653de5a297685d3a5e175c7dbf639c1595c6da1f9f842dad98b43918f951";

    // Event to notify when a mint is successful
    public event Action<string> OnMintSuccess;
    private string _pendingMintTokenId; // Store the token ID for the pending mint

    public event Action<Dictionary<string, BigInteger>> BatchBalancesReceived;

    public static ContractManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance across scenes
        }
        else
        {
            Destroy(this.gameObject); // Ensure only one instance exists
        }
    }

    void Start()
    {

        if (PlayerPrefs.HasKey("playerAddress"))
        {
            playerAddress = PlayerPrefs.GetString("playerAddress");
            Debug.Log("Connected to wallet: " + playerAddress);
        }
        bool available = JSInteropManager.IsWalletAvailable();
        if (!available)
        {
            JSInteropManager.AskToInstallWallet();
        }
    }


    private List<string> lastBatchTokenIds;

    public void BalanceOfBatch(List<string> tokenIds)
    {
        if (string.IsNullOrEmpty(playerAddress) || tokenIds == null || tokenIds.Count == 0)
        {
            Debug.LogWarning("Invalid parameters for BalanceOfBatch.");
            return;
        }

        lastBatchTokenIds = new List<string>(tokenIds);


        List<string> calldata = new List<string>();

        // Add length of accounts array (repeat playerAddress for each tokenId)
        calldata.Add(tokenIds.Count.ToString());
        for (int i = 0; i < tokenIds.Count; i++)
        {
            calldata.Add(playerAddress);
        }

        // Add length of token_ids array
        calldata.Add(tokenIds.Count.ToString());
        foreach (var tokenId in tokenIds)
        {
            if (!TryParseU256(tokenId, out string low, out string high))
            {
                Debug.LogError("Invalid tokenId: " + tokenId);
                return;
            }

            calldata.Add(low);   // u256 low
            calldata.Add(high);  // u256 high
        }

        string calldataString = JsonUtility.ToJson(new ArrayWrapper { array = calldata.ToArray() });

        Debug.Log("Calldata for BalanceOfBatch: " + calldataString);

        JSInteropManager.CallContract(
            contractAddress,
            "balanceOfBatch",
            calldataString,
            "ContractManager",
            "Erc1155BatchCallback"
        );
    }

    public void Erc1155BatchCallback(string response)
    {
        Debug.Log("Erc1155BatchCallback response: " + response);
        JsonResponse jsonResponse = JsonUtility.FromJson<JsonResponse>(response);
        Debug.Log(jsonResponse);
        var balances = ExtractBalancesFromBatch(response);
        if (balances == null) return;

        var map = new Dictionary<string, BigInteger>();
        for (int i = 0; i < balances.Count; i++)
        {
            map[lastBatchTokenIds[i]] = balances[i];
            Debug.Log($"Token ID {lastBatchTokenIds[i]} balance: {balances[i]}");
        }

        BatchBalancesReceived?.Invoke(map);

    }

    public void MintToken(string tokenId)
    {
        _pendingMintTokenId = tokenId; // Store the token ID before sending the transaction

        string[] calldata = new string[]
        {
            playerAddress,  // felt252
            tokenId, "0",   // u256
            "1","0",        // u256 high
            "1",        // data length
            "1"         // data[0]
        };
        string calldataString = JsonUtility.ToJson(new ArrayWrapper { array = calldata });
        JSInteropManager.SendTransaction(contractAddress, "selfMint", calldataString, "ContractManager", "MintCallback");
    }

    public void MintCallback(string transactionHash)
    {
        Debug.Log("https://goerli.voyager.online/tx/" + transactionHash);
        // Invoke the success event with the stored token ID
        if (!string.IsNullOrEmpty(_pendingMintTokenId))
        {
            Debug.Log($"Mint successful for pending token ID: {_pendingMintTokenId}");
            OnMintSuccess?.Invoke(_pendingMintTokenId);
            _pendingMintTokenId = null; // Clear after use
        }
    }



    private bool TryParseU256(string input, out string lowOut, out string highOut)
    {
        lowOut = "0";
        highOut = "0";
        BigInteger value;
        try
        {
            if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                value = BigInteger.Parse(input.Substring(2), System.Globalization.NumberStyles.HexNumber);
            else
                value = BigInteger.Parse(input);
        }
        catch
        {
            return false;
        }

        if (value < 0) return false;

        BigInteger mask = BigInteger.One << 128;
        BigInteger low = value % mask;
        BigInteger high = value / mask;

        lowOut = low.ToString();   // decimal, StarkNet accepts decimal
        highOut = high.ToString();

        return true;
    }

    private static BigInteger ParseFelt(string felt)
    {
        if (felt.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            felt = felt.Substring(2);
        if (string.IsNullOrEmpty(felt))
            return BigInteger.Zero;

        // Thử parse hexa trước
        if (BigInteger.TryParse(felt, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out BigInteger hexVal))
            return hexVal;

        // Fallback: decimal
        return BigInteger.Parse(felt, CultureInfo.InvariantCulture);
    }

    public static List<BigInteger> ExtractBalancesFromBatch(string responseJson)
    {
        // Giả sử JsonResponse có field `result` là string[] hoặc List<string>
        JsonResponse jsonResponse;
        try
        {
            jsonResponse = JsonUtility.FromJson<JsonResponse>(responseJson);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Không parse được JSON: {ex.Message} | Raw: {responseJson}");
            return null;
        }

        if (jsonResponse == null || jsonResponse.result == null)
        {
            Debug.LogError("Response batch rỗng hoặc cấu trúc sai.");
            return null;
        }

        string[] resultsArray;
        if (jsonResponse.result is string[] arr)
            resultsArray = arr;
        else
        {
            Debug.LogError("Kiểu unexpected cho jsonResponse.result");
            return null;
        }

        if (resultsArray.Length == 0)
        {
            Debug.LogWarning("Batch response không có dữ liệu.");
            return new List<BigInteger>();
        }

        // Bỏ qua phần tử đầu nếu đó là count
        int offset = 1;
        // Kiểm tra độ dài có hợp lý (số phần tử còn lại phải chia hết cho 2)
        int remaining = resultsArray.Length - offset;
        if (remaining % 2 != 0)
        {
            Debug.LogError("Độ dài u256 span không chẵn, không thể parse.");
            return null;
        }

        var balances = new List<BigInteger>();
        for (int i = offset; i < resultsArray.Length; i += 2)
        {
            // Theo mẫu: [low, high]
            BigInteger low = ParseFelt(resultsArray[i]);
            BigInteger high = ParseFelt(resultsArray[i + 1]);
            BigInteger full = (high << 128) + low;
            balances.Add(full);
        }

        return balances;
    }
}
