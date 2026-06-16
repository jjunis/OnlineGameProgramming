using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class AuctionManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text AxeCountText;
    [SerializeField] Text SwordCountText;
    [SerializeField] Text BowCountText;
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;

    [Header("Sell Price Input")]
    [SerializeField] InputField PriceInput;

    string userKey;
    string nickName;
    int currentCoin;

    Dictionary<string, int> inventory = new Dictionary<string, int>();

    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://onlinegameprogramming-7f17f-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();
        LoadMyData();
    }

    void LoadMyData()
    {
        userKey = PlayerPrefs.GetString("UserKey");
        nickName = PlayerPrefs.GetString("UserNickName");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() => MessageText.text = "내 정보 불러오기 실패");
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());

                string inventoryJson = snapshot.Child("Inventory").Value.ToString();
                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = "정보 불러오기 완료";
                });
            }
        });
    }

    int GetItemCount(string itemName)
    {
        return inventory.ContainsKey(itemName) ? inventory[itemName] : 0;
    }

    void RefreshUI()
    {
        AxeCountText.text = "Axe : " + GetItemCount("Axe");
        SwordCountText.text = "Sword : " + GetItemCount("Sword");
        BowCountText.text = "Bow : " + GetItemCount("Bow");
        CoinText.text = "Coin : " + currentCoin;
    }

    void SellItem(string itemName)
    {
        if (string.IsNullOrEmpty(PriceInput.text))
        {
            MessageText.text = "판매 가격을 입력해주세요.";
            return;
        }

        if (!int.TryParse(PriceInput.text, out int price) || price <= 0)
        {
            MessageText.text = "가격은 1 이상이어야 합니다.";
            return;
        }

        if (!inventory.ContainsKey(itemName) || inventory[itemName] <= 0)
        {
            MessageText.text = itemName + " 아이템이 없습니다.";
            return;
        }

        // 인벤토리 차감
        inventory[itemName]--;
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        // AuctionList에 새 키 생성
        string auctionKey = reference.Child("AuctionList").Push().Key;

        AuctionData data = new AuctionData(auctionKey, userKey, nickName, itemName, 1, price);

        Dictionary<string, object> updateData = new Dictionary<string, object>();
        updateData["UserInfo/" + userKey + "/Inventory"] = inventoryJson;
        updateData["AuctionList/" + auctionKey + "/AuctionKey"] = data.AuctionKey;
        updateData["AuctionList/" + auctionKey + "/SellerKey"] = data.SellerKey;
        updateData["AuctionList/" + auctionKey + "/SellerNickName"] = data.SellerNickName;
        updateData["AuctionList/" + auctionKey + "/ItemName"] = data.ItemName;
        updateData["AuctionList/" + auctionKey + "/Count"] = data.Count;
        updateData["AuctionList/" + auctionKey + "/Price"] = data.Price;
        updateData["AuctionList/" + auctionKey + "/IsSold"] = data.IsSold;
        updateData["AuctionList/" + auctionKey + "/ListedAt"] = ServerValue.Timestamp;

        reference.UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // 실패 시 인벤토리 롤백
                inventory[itemName]++;
                dispatcher.Enqueue(() => MessageText.text = "판매 등록 실패");
                return;
            }

            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = itemName + " 판매 등록 완료! (" + price + " Coin)";
                    PriceInput.text = "";
                });
            }
        });
    }

    public void ReloadAll()
    {
        userKey = PlayerPrefs.GetString("UserKey");
        nickName = PlayerPrefs.GetString("UserNickName");
        LoadMyData();
    }

    public void OnSellAxeButton() { SellItem("Axe"); }
    public void OnSellSwordButton() { SellItem("Sword"); }
    public void OnSellBowButton() { SellItem("Bow"); }
}