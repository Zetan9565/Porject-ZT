﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour, IWindow
{
    private static ShopManager instance;
    public static ShopManager Instance
    {
        get
        {
            if (!instance || !instance.gameObject)
                instance = FindObjectOfType<ShopManager>();
            return instance;
        }
    }

    [SerializeField]
    private ShopUI UI;

    public static List<Talker> Vendors { get; } = new List<Talker>();

    private List<MerchandiseAgent> merchandiseAgents = new List<MerchandiseAgent>();

    public Shop MShop { get; private set; }

    public ItemInfo MItemInfo { get; private set; }

    public bool IsUIOpen { get; private set; }

    public bool IsPausing { get; private set; }

    public Canvas SortCanvas
    {
        get
        {
            return UI.windowCanvas;
        }
    }

    private void Update()
    {
        RefreshAll(Time.deltaTime);
    }

    public void RefreshAll(float time)
    {
        /*System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();*/
        var vendorEnum = Vendors.GetEnumerator();
        while (vendorEnum.MoveNext())
        {
            vendorEnum.Current.shop.Refresh(time);
        }
        vendorEnum.Dispose();
        if (IsUIOpen)
        {
            var agentEnum = merchandiseAgents.GetEnumerator();
            while (agentEnum.MoveNext())
            {
                agentEnum.Current.UpdateInfo();
            }
            agentEnum.Dispose();
        }
        /*stopwatch.Stop();
        Debug.Log(stopwatch.Elapsed.TotalMilliseconds);*/
    }

    public void Init(Shop shop)
    {
        if (shop == null) return;
        MShop = shop;
        UI.shopName.text = MShop.ShopName;
        foreach (MerchandiseAgent ma in merchandiseAgents)
            ma.Clear(true);
        merchandiseAgents.RemoveAll(x => !x.gameObject.activeSelf);
        UI.commodityTab.isOn = true;
        SetPage(0);
    }

    #region 货物处理相关
    public void SellItem(MerchandiseInfo info)
    {
        if (MShop == null || info == null || info.IsInvalid) return;
        if (!MShop.Commodities.Contains(info)) return;
        long maxAmount = info.SOorENAble ? info.LeftAmount : info.SellPrice > 0 ? BackpackManager.Instance.MBackpack.Money / info.SellPrice : 999;
        if (info.LeftAmount == 1 && info.SOorENAble)
        {
            ConfirmHandler.Instance.NewConfirm(string.Format("确定购买1个 [{0}] 吗？", info.Item.name), delegate
            {
                if (OnSell(info))
                    MessageManager.Instance.NewMessage(string.Format("购买了1个 [{0}]", info.Item.name));
            });
        }
        else if (info.IsSoldOut)
        {
            ConfirmHandler.Instance.NewConfirm("该商品暂时缺货");
        }
        else
        {
            AmountHandler.Instance.SetPosition(MyTools.ScreenCenter, Vector2.zero);
            AmountHandler.Instance.Init(delegate
            {
                ConfirmHandler.Instance.NewConfirm(string.Format("确定购买{0}个 [{1}] 吗？", (int)AmountHandler.Instance.Amount, info.Item.name), delegate
                {
                    if (OnSell(info, (int)AmountHandler.Instance.Amount))
                        MessageManager.Instance.NewMessage(string.Format("购买了{0}个 [{1}]", (int)AmountHandler.Instance.Amount, info.Item.name));
                });
            }, maxAmount);
        }
    }

    bool OnSell(MerchandiseInfo info, int amount = 1)
    {
        if (MShop == null || info == null || info.IsInvalid || amount < 1) return false;
        if (!MShop.Commodities.Contains(info)) return false;
        if (!BackpackManager.Instance.TryGetItem_Boolean(info.Item, amount)) return false;
        if (info.SOorENAble && amount > info.LeftAmount)
        {
            if (!info.IsSoldOut) MessageManager.Instance.NewMessage("该商品数量不足");
            else MessageManager.Instance.NewMessage("该商品暂时缺货");
            return false;
        }
        if (!BackpackManager.Instance.TryLoseMoney(amount * info.SellPrice))
            return false;
        BackpackManager.Instance.LoseMoney(amount * info.SellPrice);
        BackpackManager.Instance.GetItem(info.Item, amount);
        if (info.SOorENAble) info.LeftAmount -= amount;
        MerchandiseAgent ma = GetMerchandiseAgentByInfo(info);
        if (ma) ma.UpdateInfo();
        return true;
    }

    public void PurchaseItem(MerchandiseInfo info)
    {
        if (MShop == null || info == null || info.IsInvalid) return;
        if (!MShop.Acquisitions.Contains(info)) return;
        int backpackAmount = BackpackManager.Instance.GetItemAmount(info.Item);
        int maxAmount = info.SOorENAble ? (info.LeftAmount > backpackAmount ? backpackAmount : info.LeftAmount) : backpackAmount;
        if (info.LeftAmount == 1)
        {
            ConfirmHandler.Instance.NewConfirm(string.Format("确定出售1个 [{0}] 吗？", info.Item.name), delegate
            {
                if (OnPurchase(info, (int)AmountHandler.Instance.Amount))
                    MessageManager.Instance.NewMessage(string.Format("出售了1个 [{1}]", (int)AmountHandler.Instance.Amount, info.Item.name));
            });
        }
        else if (info.IsEnough)
        {
            ConfirmHandler.Instance.NewConfirm("这种物品暂无特价收购需求，确定按原价出售吗？", delegate
            {
                PurchaseItem(MItemInfo, true);
            });
        }
        else
        {
            AmountHandler.Instance.SetPosition(MyTools.ScreenCenter, Vector2.zero);
            AmountHandler.Instance.Init(delegate
            {
                ConfirmHandler.Instance.NewConfirm(string.Format("确定出售{0}个 [{1}] 吗？", (int)AmountHandler.Instance.Amount, info.Item.name), delegate
                {
                    if (OnPurchase(info, (int)AmountHandler.Instance.Amount))
                        MessageManager.Instance.NewMessage(string.Format("出售了{0}个 [{1}]", (int)AmountHandler.Instance.Amount, info.Item.name));
                });
            }, maxAmount);
        }
    }

    public void PurchaseItem(ItemInfo info, bool force = false)
    {
        if (MShop == null || info == null || !info.Item) return;
        if (!info.Item.SellAble)
        {
            MessageManager.Instance.NewMessage("这种物品不可出售");
            return;
        }
        if (info.gemstone1 != null || info.gemstone2 != null)
        {
            MessageManager.Instance.NewMessage("镶嵌宝石的物品不可出售");
            return;
        }
        MItemInfo = info;
        MerchandiseInfo find = MShop.Acquisitions.Find(x => x.Item == info.Item);
        if (find != null && !force)
        {
            PurchaseItem(find);
            return;
        }
        if (info.Amount == 1)
        {
            ConfirmHandler.Instance.NewConfirm(string.Format("确定出售1个 [{0}] 吗？", info.Item.name), delegate
            {
                if (OnPurchase(info))
                    MessageManager.Instance.NewMessage(string.Format("出售了1个 [{1}]", (int)AmountHandler.Instance.Amount, info.Item.name));
            });
        }
        else
        {
            AmountHandler.Instance.SetPosition(MyTools.ScreenCenter, Vector2.zero);
            AmountHandler.Instance.Init(delegate
            {
                ConfirmHandler.Instance.NewConfirm(string.Format("确定出售{0}个 [{1}] 吗？", (int)AmountHandler.Instance.Amount, info.Item.name), delegate
                {
                    if (OnPurchase(info, (int)AmountHandler.Instance.Amount))
                        MessageManager.Instance.NewMessage(string.Format("出售了{0}个 [{1}]", (int)AmountHandler.Instance.Amount, info.Item.name));
                });
            }, BackpackManager.Instance.GetItemAmount(info.Item));
        }
    }

    bool OnPurchase(MerchandiseInfo info, int amount = 1)
    {
        if (MShop == null || info == null || info.IsInvalid || amount < 1) return false;
        if (!MShop.Acquisitions.Contains(info)) return false;
        var itemAgents = BackpackManager.Instance.GetItemAgentsByItem(info.Item).ToArray();
        if (itemAgents.Length < 1)
        {
            MessageManager.Instance.NewMessage("行囊中没有这种物品");
            return false;
        }
        if (info.SOorENAble && amount > info.LeftAmount)
        {
            if (!info.IsEnough) MessageManager.Instance.NewMessage("不收够这么多的这种物品");
            else MessageManager.Instance.NewMessage("这种物品暂无收购需求");
            return false;
        }
        ItemBase item = itemAgents[0].MItemInfo.Item;
        if (!BackpackManager.Instance.TryLoseItem_Boolean(item, amount)) return false;
        BackpackManager.Instance.GetMoney(amount * info.PurchasePrice);
        BackpackManager.Instance.LoseItem(item, amount);
        if (info.SOorENAble) info.LeftAmount -= amount;
        MerchandiseAgent ma = GetMerchandiseAgentByInfo(info);
        if (ma) ma.UpdateInfo();
        return true;
    }

    bool OnPurchase(ItemInfo info, int amount = 1)
    {
        if (MShop == null || info == null || !info.Item || amount < 1) return false;
        MItemInfo = null;
        if (!info.Item.SellAble)
        {
            MessageManager.Instance.NewMessage("这种物品不可出售");
            return false;
        }
        if (BackpackManager.Instance.GetItemAmount(info.Item) < 1)
        {
            MessageManager.Instance.NewMessage("行囊中没有这种物品");
            return false;
        }
        if (amount > info.Amount)
        {
            MessageManager.Instance.NewMessage("行囊中没有这么多的这种物品");
            return false;
        }
        if (!BackpackManager.Instance.TryLoseItem_Boolean(info, amount)) return false;
        BackpackManager.Instance.GetMoney(amount * info.Item.SellPrice);
        BackpackManager.Instance.LoseItem(info, amount);
        return true;
    }
    #endregion

    #region UI相关
    public void OpenWindow()
    {
        if (!UI || !UI.gameObject) return;
        if (IsUIOpen) return;
        if (IsPausing) return;
        UI.shopWindow.alpha = 1;
        UI.shopWindow.blocksRaycasts = true;
        WindowsManager.Instance.Push(this);
        IsUIOpen = true;
        UIManager.Instance.EnableJoyStick(false);
    }

    public void CloseWindow()
    {
        if (!UI || !UI.gameObject) return;
        if (!IsUIOpen) return;
        if (IsPausing) return;
        UI.shopWindow.alpha = 0;
        UI.shopWindow.blocksRaycasts = false;
        IsUIOpen = false;
        IsPausing = false;
        MShop = null;
        MItemInfo = null;
        WindowsManager.Instance.Remove(this);
        if (BackpackManager.Instance.IsUIOpen) BackpackManager.Instance.CloseWindow();
        ItemWindowHandler.Instance.CloseItemWindow();
        if (DialogueManager.Instance.IsUIOpen) DialogueManager.Instance.PauseDisplay(false);
        AmountHandler.Instance.Cancel();
    }

    public void OpenCloseWindow()
    {
        throw new System.NotImplementedException();
    }

    public void PauseDisplay(bool pause)
    {
        if (!UI || !UI.gameObject) return;
        if (!IsUIOpen) return;
        if (IsPausing && !pause)
        {
            UI.shopWindow.alpha = 1;
            UI.shopWindow.blocksRaycasts = true;
        }
        else if (!IsPausing && pause)
        {
            UI.shopWindow.alpha = 0;
            UI.shopWindow.blocksRaycasts = false;
        }
        IsPausing = pause;
    }

    public void SetPage(int page)
    {
        if (!UI || !UI.gameObject || !MShop) return;
        switch (page)
        {
            case 0:
                int originalSize = merchandiseAgents.Count;
                if (MShop.Commodities.Count >= originalSize)
                    for (int i = 0; i < MShop.Commodities.Count - originalSize; i++)
                    {
                        MerchandiseAgent ma = ObjectPool.Instance.Get(UI.merchandiseCellPrefab, UI.merchandiseCellsParent).GetComponent<MerchandiseAgent>();
                        ma.Clear();
                        merchandiseAgents.Add(ma);
                    }
                else
                {
                    for (int i = 0; i < originalSize - MShop.Commodities.Count; i++)
                        merchandiseAgents[i].Clear(true);
                    merchandiseAgents.RemoveAll(x => !x.gameObject.activeSelf);
                }
                foreach (MerchandiseAgent ma in merchandiseAgents)
                    ma.Clear();
                foreach (MerchandiseInfo mi in MShop.Commodities)
                    foreach (MerchandiseAgent ma in merchandiseAgents)
                        if (ma.IsEmpty && !mi.IsInvalid)
                        {
                            ma.Init(mi, MerchandiseType.SellToPlayer);
                            break;
                        }
                break;
            case 1:
                originalSize = merchandiseAgents.Count;
                int acqCount = MShop.Acquisitions.FindAll(x => !x.IsInvalid && x.Item.SellAble).Count;
                if (acqCount >= originalSize)
                    for (int i = 0; i < acqCount - originalSize; i++)
                    {
                        MerchandiseAgent ma = ObjectPool.Instance.Get(UI.merchandiseCellPrefab, UI.merchandiseCellsParent).GetComponent<MerchandiseAgent>();
                        ma.Clear();
                        merchandiseAgents.Add(ma);
                    }
                else
                {
                    for (int i = 0; i < originalSize - acqCount; i++)
                        merchandiseAgents[i].Clear(true);
                    merchandiseAgents.RemoveAll(x => !x.gameObject.activeSelf);
                }
                foreach (MerchandiseAgent ma in merchandiseAgents)
                    ma.Clear();
                foreach (MerchandiseInfo mi in MShop.Acquisitions)
                    foreach (MerchandiseAgent ma in merchandiseAgents)
                        if (ma.IsEmpty && !mi.IsInvalid && mi.Item.SellAble)
                        {
                            ma.Init(mi, MerchandiseType.BuyFromPlayer);
                            break;
                        }
                break;
            default: break;
        }
        ItemWindowHandler.Instance.CloseItemWindow();
    }

    public void SetUI(ShopUI UI)
    {
        this.UI = UI;
    }

    public void ResetUI()
    {
        merchandiseAgents.Clear();
        IsUIOpen = false;
        IsPausing = false;
    }
    #endregion

    public MerchandiseAgent GetMerchandiseAgentByInfo(MerchandiseInfo info)
    {
        return merchandiseAgents.Find(x => x.merchandiseInfo == info);
    }

    public MerchandiseAgent GetMerchandiseAgentByItem(ItemInfo info)
    {
        return merchandiseAgents.Find(x => x.IsMacth(info));
    }
}