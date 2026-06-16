using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ShopManager : MonoBehaviour
{

    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text CoinText;
    [SerializeField] Text MessagerText;

    [SerializeField] Text AxeCountText;
    [SerializeField] Text SwordCountText;
    [SerializeField] Text BowCountText;

    string userKey;

    int currentCoin;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        database = FirebaseDatabase.GetInstance(
            "https://onlinegameprogramming-7f17f-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );

        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        LoadUserData();
    }

    public void LoadUserData()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessagerText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessagerText.text = "유저 정보 불러오기 실패";
                });

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
                    MessagerText.text = "유저 정보 불러오기 완료";
                });
            }
        });
    }

    void RefreshUI()
    {
        CoinText.text = " Coin: " + currentCoin;

        AxeCountText.text = "Axe : " + (inventory.ContainsKey("Axe") ? inventory["Axe"] : 0);
        SwordCountText.text = "Sword : " + (inventory.ContainsKey("Sword") ? inventory["Sword"] : 0);
        BowCountText.text = "Bow : " + (inventory.ContainsKey("Bow") ? inventory["Bow"] : 0);
    }

    public void OnClickBuyAxe()
    {
        BuyItem("Axe", 150);
    }
    public void OnClickBuySword()
    {
        BuyItem("Sword", 250);
    }
    public void OnClickBuyBow()
    {
        BuyItem("Bow", 300);
    }

    void BuyItem(string itemName, int price)
    {
        if (currentCoin < price)
        {
            MessagerText.text = "코인이 부족합니다.";
            return;
        }

        currentCoin -= price;

        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName]++;
        }
        else
        {
            inventory.Add(itemName, 1);
        }

        SaveUserData(itemName);
    }

    void SaveUserData(string boughtItemName)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        Dictionary<string, object> updateData = new Dictionary<string, object>();

        updateData["Coin"] = currentCoin;
        updateData["Inventory"] = inventoryJson;

        reference.Child("UserInfo").Child(userKey).UpdateChildrenAsync(updateData).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessagerText.text = "구매 저장 실패";
                });

                return;
            }

            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessagerText.text = boughtItemName + " 구매 완료";
                });
            }
        });
    }

    // Update is called once per frame
    void Update()
    {

    }

}