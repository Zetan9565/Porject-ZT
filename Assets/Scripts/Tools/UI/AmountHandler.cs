﻿using UnityEngine.UI;
using UnityEngine;

public class AmountHandler : MonoBehaviour
{
    private static AmountHandler instance;
    public static AmountHandler Instance
    {
        get
        {
            if (!instance || !instance.gameObject)
                instance = FindObjectOfType<AmountHandler>();
            return instance;
        }
    }

    public AmountUI UI;

    public long Amount { get; private set; }

    public Button Confirm { get { return UI.confirm; } }

    private long min;
    private long max;

    public void Init(UnityEngine.Events.UnityAction confirmAction, int max, int min = 0)
    {
        this.max = max;
        this.min = min;
        Amount = max >= 1 ? 1 : min;
        UI.amount.text = Amount.ToString();
        UI.confirm.onClick.RemoveAllListeners();
        UI.confirm.onClick.AddListener(confirmAction);
        UI.confirm.onClick.AddListener(CloseAmountWindow);
        UI.amount.MoveTextEnd(false);
        UI.amount.Select();
        OpenAmountWindow();
    }

    public void OpenAmountWindow()
    {
        UI.amountWindow.alpha = 1;
        UI.amountWindow.blocksRaycasts = true;
    }

    public void CloseAmountWindow()
    {
        UI.amountWindow.alpha = 0;
        UI.amountWindow.blocksRaycasts = false;
    }

    public void Number(int num)
    {
        long.TryParse(UI.amount.text, out long current);
        if (UI.amount.text.Length < UI.amount.characterLimit - 1)
        {
            current = current * 10 + num;
            if (current < min) current = min;
            else if (current > max) current = max;
        }
        else current = Amount;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Plus()
    {
        long.TryParse(UI.amount.text, out long current);
        if (current < max && UI.amount.text.Length < UI.amount.characterLimit - 1)
            current++;
        else current = max;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Minus()
    {
        long.TryParse(UI.amount.text, out long current);
        if (current > min && UI.amount.text.Length < UI.amount.characterLimit - 1)
            current--;
        else current = min;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Max()
    {
        Amount = max;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Clear()
    {
        Amount = min;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }

    public void Cancel()
    {
        UI.confirm.onClick.RemoveAllListeners();
        CloseAmountWindow();
    }

    public void SetPosition(Vector3 target)
    {
        if (UI.amountWindow.alpha > 0) UI.amountWindow.GetComponent<RectTransform>().position = target + new Vector3(-50, 55);
    }

    public void FixAmount()
    {
        UI.amount.text = System.Text.RegularExpressions.Regex.Replace(UI.amount.text, @"[^0-9]+", "");
        long.TryParse(UI.amount.text, out long current);
        if (!(current <= max && UI.amount.text.Length < UI.amount.characterLimit - 1))
            current = max;
        else if (!(current >= min && UI.amount.text.Length < UI.amount.characterLimit - 1))
            current = min;
        Amount = current;
        UI.amount.text = Amount.ToString();
        UI.amount.MoveTextEnd(false);
    }
}