using System.Globalization;
using System.Numerics;
using UnityEngine;
using Utils;

public class Erc721Balance : MonoBehaviour
{
    public void BalanceOf(string userAddress, string contractAddress)
    {
        string[] calldata = new string[1];
        calldata[0] = userAddress;
        string calldataString = JsonUtility.ToJson(new ArrayWrapper { array = calldata });
        JSInteropManager.CallContract(contractAddress, "balanceOf", calldataString, "Erc721Balance", "Erc721Callback");
    }

    public void Erc721Callback(string response)
    {
        JsonResponse jsonResponse = JsonUtility.FromJson<JsonResponse>(response);
        BigInteger balance = BigInteger.Parse(jsonResponse.result[0].Substring(2), NumberStyles.HexNumber);
        Debug.Log(balance);
    }

    // Start is called before the first frame update
    void Start()
    {
        string userAddress = "0x034F62580F6e46557A2387A8175CB7D779Da1bc3F81fD40159C964c29Cb12c1a";
        string contractAddress = "0x016a03efbabdceb64d1dda2ca7e648642c5ffd6c9623976a8632950b3e953e48";
        BalanceOf(userAddress, contractAddress);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
