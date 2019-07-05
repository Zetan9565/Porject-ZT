﻿using LeoLuz.PlugAndPlayJoystick;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : SingletonMonoBehaviour<UIManager>
{
    [SerializeField]
    private CanvasGroup canvasGroup;

    [SerializeField]
    private Button questButton;

    [SerializeField]
    private Button backpackButton;

    [SerializeField]
    private Button buildingButton;

    [SerializeField]
    private Button settingButton;

    [SerializeField]
    private AnalogicKnob joyStick;
    public AnalogicKnob JoyStick
    {
        get
        {
            return joyStick;
        }
    }

    [SerializeField]
    private UIButtonToButton interactiveButton;
    public UIButtonToButton InteractiveButton
    {
        get
        {
            return interactiveButton;
        }
    }

    [SerializeField]
    private Text interactiveName;

    private void Awake()
    {
#if UNITY_STANDALONE
        EnableJoyStick(false);
        MyUtilities.SetActive(JoyStick.gameObject, false);
        MyUtilities.SetActive(JoyStick.KnobBackground.gameObject, false);
#elif UNITY_ANDROID
        MyUtilities.SetActive(JoyStick.gameObject, true);
        MyUtilities.SetActive(JoyStick.KnobBackground.gameObject, true);
#endif
        MyUtilities.SetActive(InteractiveButton.gameObject, false);
        questButton.onClick.AddListener(QuestManager.Instance.OpenCloseWindow);
        backpackButton.onClick.AddListener(BackpackManager.Instance.OpenCloseWindow);
        buildingButton.onClick.AddListener(BuildingManager.Instance.OpenCloseWindow);
        settingButton.onClick.AddListener(EscapeMenuManager.Instance.OpenCloseWindow);
    }

    public void EnableJoyStick(bool value)
    {
        JoyStick.enabled = value && !(DialogueManager.Instance.IsUIOpen || ShopManager.Instance.IsUIOpen ||
            WarehouseManager.Instance.IsUIOpen || QuestManager.Instance.IsUIOpen || BuildingManager.Instance.IsPreviewing);
    }

    public void EnableInteractive(bool value, string name = null)
    {
#if UNITY_ANDROID
        if (!value)
            MyUtilities.SetActive(InteractiveButton.gameObject, value);
        else
        {
            MyUtilities.SetActive(InteractiveButton.gameObject, value &&
                (DialogueManager.Instance.TalkAble && !WarehouseManager.Instance.IsUIOpen && !LootManager.Instance.IsUIOpen ||
                WarehouseManager.Instance.StoreAble && !DialogueManager.Instance.IsUIOpen && !LootManager.Instance.IsUIOpen ||
                LootManager.Instance.PickAble && !DialogueManager.Instance.IsUIOpen && !WarehouseManager.Instance.IsUIOpen
                ));
        }
#endif
        if (!string.IsNullOrEmpty(name) && value)
        {
            MyUtilities.SetActive(interactiveName.transform.parent.gameObject, true);
            interactiveName.text = name;
        }
        else
        {
            MyUtilities.SetActive(interactiveName.transform.parent.gameObject, false);
            interactiveName.text = string.Empty;
        }
    }

    public void ShowAll()
    {
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        EnableJoyStick(true);
    }

    public void HideAll()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        JoyStick.enabled = false;
    }
}
