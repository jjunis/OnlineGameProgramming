using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public string UserKey { get; private set; }
    public string UserNickName { get; private set; }

    // 인벤토리 데이터 보관
    public Dictionary<string, int> Inventory { get; private set; } = new Dictionary<string, int>();

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 전환해도 파괴되지 않음
    }

    public void SetUserData(string key, string nickName)
    {
        UserKey = key;
        UserNickName = nickName;
    }

    public void SetInventory(Dictionary<string, int> inventory)
    {
        Inventory = inventory;
    }

    // 아이템 사용 시 GameManager에도 반영
    public void UseItem(string itemName)
    {
        if (Inventory.ContainsKey(itemName))
            Inventory[itemName]--;
    }
}