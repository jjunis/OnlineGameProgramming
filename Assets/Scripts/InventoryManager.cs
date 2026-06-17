using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    FirebaseDatabase database;
    DatabaseReference reference;
    UnityMainThreadDispatcher dispatcher;

    [Header("UI")]
    [SerializeField] Text AxeCountText;
    [SerializeField] Text SwordCountText;
    [SerializeField] Text BowCountText;
    [SerializeField] Text MessageText;
    [SerializeField] Text UserNickNameText;

    string userKey;
    Dictionary<string, int> inventory = new Dictionary<string, int>();

    bool isProcessing = false;

    void Start()
    {

        database = FirebaseDatabase.GetInstance(
            "https://onlinegameprogramming-7f17f-default-rtdb.asia-southeast1.firebasedatabase.app/"
        );
        reference = database.RootReference;
        dispatcher = UnityMainThreadDispatcher.Instance();

        UserNickNameText.text = GameManager.Instance.UserNickName + "님의 인벤토리";

        if (GameManager.Instance != null && GameManager.Instance.Inventory.Count > 0)
        {
            userKey = GameManager.Instance.UserKey;
            inventory = GameManager.Instance.Inventory;
            RefreshUI();
            MessageText.text = "인벤토리 불러오기 완료";
        }
        else
        {
            LoadInventory();
        }
    }

    void LoadInventory()
    {
        userKey = GameManager.Instance != null
            ? GameManager.Instance.UserKey
            : PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).Child("Inventory")
            .GetValueAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() => MessageText.text = "인벤토리 불러오기 실패");
                    return;
                }

                if (task.IsCompleted)
                {
                    DataSnapshot snapshot = task.Result;

                    if (snapshot.Value == null)
                    {
                        dispatcher.Enqueue(() => MessageText.text = "인벤토리 데이터가 없습니다.");
                        return;
                    }

                    string inventoryJson = snapshot.Value.ToString();
                    inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

                    GameManager.Instance?.SetInventory(inventory);

                    dispatcher.Enqueue(() =>
                    {
                        RefreshUI();
                        MessageText.text = "인벤토리 불러오기 완료";
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
    }

    void UserItem(string itemName)
    {
        if (isProcessing)
        {
            MessageText.text = "처리 중입니다...";
            return;
        }

        if (!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + " 아이템이 없습니다.";
            return;
        }

        if (inventory[itemName] <= 0)
        {
            MessageText.text = itemName + " 개수가 부족합니다.";
            return;
        }

        isProcessing = true;

        inventory[itemName]--;
        GameManager.Instance?.UseItem(itemName);

        SaveInventory(itemName);
    }

    public void OnClickEquipAxe() => UserItem("Axe");
    public void OnClickEquipSword() => UserItem("Sword");
    public void OnClickEquipBow() => UserItem("Bow");

    void SaveInventory(string userItemname)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory")
            .SetValueAsync(inventoryJson).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 저장 실패";
                        isProcessing = false;
                    });
                    return;
                }

                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = userItemname + " 장착 완료";
                    isProcessing = false;
                });
            });
    }
}