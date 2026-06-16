using UnityEngine;

[System.Serializable]
public class AuctionData
{
    public string AuctionKey;
    public string SellerKey;
    public string SellerNickName;
    public string ItemName;
    public int Count;
    public int Price;
    public bool IsSold;
    public long ListedAt;

    public AuctionData() { }

    public AuctionData(string auctionKey, string sellerKey, string sellerNickName,
        string itemName, int count, int price)
    {
        AuctionKey = auctionKey;
        SellerKey = sellerKey;
        SellerNickName = sellerNickName;
        ItemName = itemName;
        Count = count;
        Price = price;
        IsSold = false;
    }
}