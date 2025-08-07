using UnityEngine;
using Utils;

public class Mint : MonoBehaviour
{
    public void MintToken(string recipient, string contractAddress)
    {
        string[] calldata = new string[]
        {
            recipient,  // felt252
            "1", "0",   // u256
            "1",        // data length
            "1"         // data[0]
        };
        string calldataString = JsonUtility.ToJson(new ArrayWrapper { array = calldata });
        JSInteropManager.SendTransaction(contractAddress, "safeMint", calldataString, "Erc721Mint", "MintCallback");
    }

    public void MintCallback(string transactionHash)
    {
        Debug.Log("https://goerli.voyager.online/tx/" + transactionHash);
    }

    // Start is called before the first frame update
    void Start()
    {
        string recipientAddress = "0x034F62580F6e46557A2387A8175CB7D779Da1bc3F81fD40159C964c29Cb12c1a";
        string contractAddress = "0x016a03efbabdceb64d1dda2ca7e648642c5ffd6c9623976a8632950b3e953e48";
        MintToken(recipientAddress, contractAddress);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
