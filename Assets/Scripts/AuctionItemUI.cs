using UnityEngine;
using UnityEngine.UI;
using System;

public class AuctionItemUI : MonoBehaviour
{
    [SerializeField] Text ItemNameText;
    [SerializeField] Text SellerText;
    [SerializeField] Text PriceText;
    [SerializeField] Button BuyButton;
    [SerializeField] Text BuyButtonText;

    public void Setup(AuctionData data, bool isMine, Action onBuy)
    {
        ItemNameText.text = data.ItemName;
        SellerText.text = "판매자 : " + data.SellerNickName;
        PriceText.text = data.Price + " Coin";

        if (isMine)
        {
            BuyButtonText.text = "내 아이템";
            BuyButton.interactable = false;
        }
        else
        {
            BuyButtonText.text = "구매";
            BuyButton.interactable = true;
            BuyButton.onClick.AddListener(() => onBuy());
        }
    }
}