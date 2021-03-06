﻿using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ItemSelectionManager : WindowHandler<ItemSeletionUI, ItemSelectionManager>
{
    public bool IsSelecting { get; private set; }

    public GameObject PlacementArea => UI ? UI.placementArea : null;

    private readonly List<ItemAgent> itemAgents = new List<ItemAgent>();

    public ItemSelectionType SelectionType { get; private set; }

    private Action<List<ItemInfo>> onConfirm;
    private Action onCancel;

    private string dialog;

    public void StartSelection(ItemSelectionType selectionType, string title, string confirmDialog, Action<List<ItemInfo>> confirmCallback, Action cancelCallback = null)
    {
        if (selectionType == ItemSelectionType.None) return;
        SelectionType = selectionType;
        UI.windowTitle.text = title;
        onConfirm = confirmCallback;
        onCancel = cancelCallback;
        dialog = confirmDialog;
        OpenWindow();
        BackpackManager.Instance.EnableHandwork(false);
    }
    
    public void StartSelection(ItemSelectionType selectionType, string title, Action<List<ItemInfo>> confirmCallback, Action cancelCallback = null)
    {
        if (selectionType == ItemSelectionType.None) return;
        SelectionType = selectionType;
        UI.windowTitle.text = title;
        onConfirm = confirmCallback;
        onCancel = cancelCallback;
        dialog = string.Empty;
        OpenWindow();
        BackpackManager.Instance.EnableHandwork(false);
    }

    public void StartSelection(ItemSelectionType selectionType, Action<List<ItemInfo>> confirmCallback, Action cancelCallback = null)
    {
        if (selectionType == ItemSelectionType.None) return;
        SelectionType = selectionType;
        UI.windowTitle.text = "选择物品";
        onConfirm = confirmCallback;
        onCancel = cancelCallback;
        dialog = string.Empty;
        OpenWindow();
        BackpackManager.Instance.EnableHandwork(false);
    }

    public void Confirm()
    {
        if (itemAgents.Count < 1)
        {
            MessageManager.Instance.New("未选择任何道具");
            return;
        }
        List<ItemInfo> infos = new List<ItemInfo>();
        foreach (var ia in itemAgents)
        {
            infos.Add(ia.MItemInfo);
        }
        if (string.IsNullOrEmpty(dialog))
        {
            onConfirm?.Invoke(infos);
            CloseWindow();
        }
        else ConfirmManager.Instance.New(dialog, delegate
        {
            onConfirm?.Invoke(infos);
            CloseWindow();
        });
    }

    public void Clear()
    {
        foreach (var ia in itemAgents)
        {
            ia.Clear(true);
        }
        itemAgents.Clear();
        ZetanUtility.SetActive(UI.tips, true);
    }

    public bool Place(ItemInfo info)
    {
        if (info == null || info.item == null || info.Amount < 0) return false;
        if (info.item.StackAble)
        {
            if (SelectionType == ItemSelectionType.Discard)
            {
                if (info.item.DiscardAble && BackpackManager.Instance.TryLoseItem_Boolean(info))
                {
                    if(itemAgents.Exists(x => x.MItemInfo == info))
                    {
                        MessageManager.Instance.New("已选择该道具");
                        return false;
                    }
                    ItemAgent ia = ObjectPool.Get(UI.itemCellPrefab, UI.itemCellsParent).GetComponent<ItemAgent>();
                    ia.Init(ItemAgentType.Selection, -1, UI.gridScrollRect);
                    itemAgents.Add(ia);
                    ia.SetItem(info);
                    if (itemAgents.Count > 0) ZetanUtility.SetActive(UI.tips, false);
                    return true;
                }
            }
            else
            {
                if (SelectionType == ItemSelectionType.Making && info.item.MaterialType == MaterialType.None) return false;
                if (info.Amount < 2)
                {
                    if (BackpackManager.Instance.TryLoseItem_Boolean(info))
                    {
                        if(itemAgents.Exists(x => x.MItemInfo == info || x.MItemInfo.item == info.item))
                        {
                            MessageManager.Instance.New("已选择该道具");
                            return false;
                        }
                        ItemAgent ia = ObjectPool.Get(UI.itemCellPrefab, UI.itemCellsParent).GetComponent<ItemAgent>();
                        ia.Init(ItemAgentType.Selection, -1, UI.gridScrollRect);
                        itemAgents.Add(ia);
                        ia.SetItem(info);
                        if (itemAgents.Count > 0) ZetanUtility.SetActive(UI.tips, false);
                        return true;
                    }
                }
                else
                {
                    if (itemAgents.Exists(x => x.MItemInfo == info || x.MItemInfo.item == info.item))
                    {
                        MessageManager.Instance.New("已选择该道具");
                        return false;
                    }
                    AmountManager.Instance.New(delegate
                    {
                        if (BackpackManager.Instance.TryLoseItem_Boolean(info, (int)AmountManager.Instance.Amount))
                        {
                            ItemAgent ia = itemAgents.Find(x => x.MItemInfo.item == info.item);
                            if (ia) ia.MItemInfo.Amount = (int)AmountManager.Instance.Amount;
                            else
                            {
                                ia = ObjectPool.Get(UI.itemCellPrefab, UI.itemCellsParent).GetComponent<ItemAgent>();
                                ia.Init(ItemAgentType.Selection, -1, UI.gridScrollRect);
                                itemAgents.Add(ia);
                                ia.SetItem(new ItemInfo(info.item, (int)AmountManager.Instance.Amount));
                            }
                            if (itemAgents.Count > 0) ZetanUtility.SetActive(UI.tips, false);
                        }
                    }, info.Amount);
                    return true;
                }
            }
        }
        else if ((SelectionType != ItemSelectionType.Discard || SelectionType == ItemSelectionType.Discard && info.item.DiscardAble)
            && BackpackManager.Instance.TryLoseItem_Boolean(info))
        {
            if(itemAgents.Exists(x => x.MItemInfo == info))
            {
                MessageManager.Instance.New("已选择该道具");
                return false;
            }
            ItemAgent ia = ObjectPool.Get(UI.itemCellPrefab, UI.itemCellsParent).GetComponent<ItemAgent>();
            ia.Init(ItemAgentType.Selection, -1, UI.gridScrollRect);
            itemAgents.Add(ia);
            ia.SetItem(info);
            if (itemAgents.Count > 0) ZetanUtility.SetActive(UI.tips, false);
            return true;
        }
        return false;
    }

    public void TakeOut(ItemInfo info)
    {
        var itemAgent = itemAgents.Find(x => x.MItemInfo == info);
        itemAgent.Clear(true);
        itemAgents.Remove(itemAgent);
        if (itemAgents.Count < 1) ZetanUtility.SetActive(UI.tips, true);
    }

    public override void OpenWindow()
    {
        base.OpenWindow();
        if (!IsUIOpen) return;
        if (ItemWindowManager.Instance.IsUIOpen) ItemWindowManager.Instance.CloseWindow();
    }

    public override void CloseWindow()
    {
        base.CloseWindow();
        if (IsUIOpen) return;
        foreach (var ia in itemAgents)
            ia.Clear(true);
        itemAgents.Clear();
        ZetanUtility.SetActive(UI.tips, true);
        dialog = string.Empty;
        SelectionType = ItemSelectionType.None;
        onCancel?.Invoke();
        BackpackManager.Instance.EnableHandwork(true);
    }
}
public enum ItemSelectionType
{
    None,
    Discard,
    Gift,
    Making
}