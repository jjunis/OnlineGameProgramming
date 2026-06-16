using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class AuctionListManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessageText;
    [SerializeField] Transform ListingContainer;
    [SerializeField] GameObject AuctionItemPrefab;

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
                dispatcher.Enqueue(() => MessageText.text = "유저 정보 로드 실패");
                return;
            }

            DataSnapshot snapshot = task.Result;
            currentCoin = int.Parse(snapshot.Child("Coin").Value.ToString());
            string invJson = snapshot.Child("Inventory").Value.ToString();
            inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(invJson);

            dispatcher.Enqueue(() =>
            {
                CoinText.text = "Coin : " + currentCoin;
                LoadAuctionList();
            });
        });
    }
    public void LoadAuctionList()
    {
        reference.Child("AuctionList").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() => MessageText.text = "경매 목록 로드 실패");
                return;
            }

            DataSnapshot snapshot = task.Result;

            dispatcher.Enqueue(() =>
            {
                foreach (Transform child in ListingContainer)
                    Destroy(child.gameObject);

                if (!snapshot.HasChildren)
                {
                    MessageText.text = "등록된 아이템이 없습니다.";
                    return;
                }

                foreach (DataSnapshot child in snapshot.Children)
                {
                    bool isSold = bool.Parse(child.Child("IsSold").Value.ToString());
                    if (isSold) continue;

                    AuctionData data = new AuctionData();
                    data.AuctionKey = child.Child("AuctionKey").Value.ToString();
                    data.SellerKey = child.Child("SellerKey").Value.ToString();
                    data.SellerNickName = child.Child("SellerNickName").Value.ToString();
                    data.ItemName = child.Child("ItemName").Value.ToString();
                    data.Count = int.Parse(child.Child("Count").Value.ToString());
                    data.Price = int.Parse(child.Child("Price").Value.ToString());
                    data.IsSold = isSold;

                    bool isMine = data.SellerKey == userKey;
                    CreateAuctionItemUI(data, isMine);
                }

                MessageText.text = "경매 목록 로드 완료";
            });
        });
    }

    void CreateAuctionItemUI(AuctionData data, bool isMine)
    {
        GameObject obj = Instantiate(AuctionItemPrefab, ListingContainer);
        AuctionItemUI ui = obj.GetComponent<AuctionItemUI>();
        ui.Setup(data, isMine, () => OnClickBuy(data));
    }

    void OnClickBuy(AuctionData data)
    {
        if (data.SellerKey == userKey)
        {
            MessageText.text = "내가 등록한 아이템은 구매할 수 없습니다.";
            return;
        }

        if (currentCoin < data.Price)
        {
            MessageText.text = "코인이 부족합니다.";
            return;
        }

        reference.Child("AuctionList").Child(data.AuctionKey).Child("IsSold")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() => MessageText.text = "구매 확인 실패");
                    return;
                }

                bool isSold = bool.Parse(task.Result.Value.ToString());
                if (isSold)
                {
                    dispatcher.Enqueue(() => MessageText.text = "이미 판매된 아이템입니다.");
                    return;
                }

                ProcessBuy(data);
            });
    }

    void ProcessBuy(AuctionData data)
    {
        reference.Child("UserInfo").Child(data.SellerKey).Child("Coin")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() => MessageText.text = "판매자 정보 조회 실패");
                    return;
                }

                int sellerCoin = int.Parse(task.Result.Value.ToString());
                sellerCoin += data.Price;

                currentCoin -= data.Price;

                if (inventory.ContainsKey(data.ItemName))
                    inventory[data.ItemName] += data.Count;
                else
                    inventory[data.ItemName] = data.Count;

                string buyerInvJson = JsonConvert.SerializeObject(inventory);

                Dictionary<string, object> updateData = new Dictionary<string, object>();
                updateData["AuctionList/" + data.AuctionKey + "/IsSold"] = true;
                updateData["UserInfo/" + userKey + "/Coin"] = currentCoin;
                updateData["UserInfo/" + userKey + "/Inventory"] = buyerInvJson;
                updateData["UserInfo/" + data.SellerKey + "/Coin"] = sellerCoin;

                reference.UpdateChildrenAsync(updateData).ContinueWith(updateTask =>
                {
                    if (updateTask.IsFaulted)
                    {
                        currentCoin += data.Price;
                        inventory[data.ItemName] -= data.Count;
                        dispatcher.Enqueue(() => MessageText.text = "구매 저장 실패");
                        return;
                    }

                    dispatcher.Enqueue(() =>
                    {
                        CoinText.text = "Coin : " + currentCoin;
                        MessageText.text = data.ItemName + " 구매 완료!";
                        LoadAuctionList();
                    });
                });
            });
    }

    public void OnClickRefresh()
    {
        LoadAuctionList();
    }
}