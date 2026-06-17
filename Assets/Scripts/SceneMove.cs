using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneMove : MonoBehaviour
{
    // 상점 씬으로 이동
    public void OnClickGoShop()
    {
        SceneManager.LoadScene("ShopScene");
    }

    // 인벤토리 씬으로 이동
    public void OnClickGoInventory()
    {
        SceneManager.LoadScene("InventoryScene");
    }

    // 로그아웃 (로그인 씬으로 이동)
    public void OnClickLogout()
    {
        GameManager.Instance.SetUserData("", "");
        GameManager.Instance.SetInventory(new Dictionary<string, int>());
        SceneManager.LoadScene("LoginScene");
    }

    //거래소 씬으로 이동
    public void GoToAuctionScene()
    {
        SceneManager.LoadScene("AuctionScene");
    }
}