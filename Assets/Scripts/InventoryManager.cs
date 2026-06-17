using UnityEngine;
using Firebase.Database;
using UnityEngine.UI;
using PimDeWitte.UnityMainThreadDispatcher;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    string userKey;

    Dictionary<string, int> inventory = new Dictionary<string, int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        {
            database = FirebaseDatabase.GetInstance(
                "https://onlinegameprogramming-7f17f-default-rtdb.asia-southeast1.firebasedatabase.app/"
            );

            reference = database.RootReference;
            dispatcher = UnityMainThreadDispatcher.Instance();

            LoadInventory();
        }
    }

    void LoadInventory()
    {
        userKey = PlayerPrefs.GetString("UserKey");

        if (string.IsNullOrEmpty(userKey))
        {
            MessageText.text = "로그인 정보가 없습니다.";
            return;
        }

        reference.Child("UserInfo").Child(userKey).Child("Inventory").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 불러오기 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;

                if (snapshot.Value == null)
                {
                    dispatcher.Enqueue(() =>
                    {
                        MessageText.text = "인벤토리 데이터가 없습니다.";
                    });
                    return;
                }

                string inventoryJson = snapshot.Value.ToString();

                inventory = JsonConvert.DeserializeObject<Dictionary<string, int>>(inventoryJson);

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
        if (inventory.ContainsKey(itemName))
        {
            return inventory[itemName];
        }

        return 0;
    }

    void RefreshUI()
    {
        AxeCountText.text = "Axe : " + GetItemCount("Axe");
        SwordCountText.text = "Sword : " + GetItemCount("Sword");
        BowCountText.text = "Bow : " + GetItemCount("Bow");
    }

    void UserItem(string itemName)
    {
        if (!inventory.ContainsKey(itemName))
        {
            MessageText.text = itemName + "아이템이 없습니다.";
            return;
        }

        if (inventory[itemName] <= 0)
        {
            MessageText.text = itemName + "개수가 부족합니다. ";
            return;
        }

        inventory[itemName]--;

        SaveInventory(itemName);
    }

    public void OnClickEquipAxe()
    {
        UserItem("Axe");
    }
    public void OnClickEquipSword()
    {
        UserItem("Sword");
    }
    public void OnClickEquipBow()
    {
        UserItem("Bow");
    }

    void SaveInventory(string userItemname)
    {
        string inventoryJson = JsonConvert.SerializeObject(inventory);

        reference.Child("UserInfo").Child(userKey).Child("Inventory").SetValueAsync(inventoryJson).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                dispatcher.Enqueue(() =>
                {
                    MessageText.text = "인벤토리 저장 실패";
                });
                return;
            }

            if (task.IsCompleted)
            {
                dispatcher.Enqueue(() =>
                {
                    RefreshUI();
                    MessageText.text = userItemname + "장착 완료";
                });
            }
        });
    }
}