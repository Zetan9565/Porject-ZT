﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager instance;
    public static DialogueManager Instance
    {
        get
        {
            if (!instance || !instance.gameObject)
                instance = FindObjectOfType<DialogueManager>();
            return instance;
        }
    }

    [SerializeField]
    private DialogueUI UI;

    public UnityEvent OnBeginDialogueEvent;
    public UnityEvent OnFinishDialogueEvent;

    public DialogueType DialogueType { get; private set; } = DialogueType.Normal;

    public Dictionary<string, DialogueData> DialogueDatas { get; private set; } = new Dictionary<string, DialogueData>();

    private Queue<DialogueWords> Words = new Queue<DialogueWords>();

    //public Dictionary<string, DialogueInfo> loadedDatas = new Dictionary<string, DialogueInfo>();

    private int page = 1;
    public int Page
    {
        get
        {
            return page;
        }
        set
        {
            if (value > 1) page = value;
            else page = 1;
        }
    }

    private int MaxPage = 1;
    public List<OptionAgent> OptionAgents { get; private set; } = new List<OptionAgent>();

    private Talker MTalker;
    private TalkObjective talkObjective;
    private Quest MQuest;
    private Dialogue dialogToSay;
    private DialogueWords lastWords;
    private BranchDialogue branchDialog;
    private BranchDialogue branchDialogInstance;
    private List<BranchDialogue> branchesComplete = new List<BranchDialogue>();

    public bool HasNotAcptQuests
    {
        get
        {
            QuestGiver questGiver = MTalker as QuestGiver;
            if (!questGiver || !questGiver.QuestInstances.Exists(x => !x.IsOngoing) || questGiver.QuestInstances.Exists(x => x.IsComplete)) return false;
            return true;
        }
    }

    public bool IsTalking { get; private set; }
    public bool TalkAble { get; private set; }

    public int IndexToGoBack { get; private set; } = -1;

    #region 开始新对话
    public void BeginNewDialogue()
    {
        if (!MTalker || !TalkAble || IsTalking) return;
        StartNormalDialogue(MTalker);
        OnBeginDialogueEvent?.Invoke();
    }

    public void StartDialogue(Dialogue dialogue, int startIndex = 0, bool sayImmediately = true)
    {
        if (!UI) return;
        if (dialogue.Words.Count < 1 || !dialogue) return;
        dialogToSay = dialogue;
        if (!DialogueDatas.ContainsKey(dialogue.ID)) DialogueDatas.Add(dialogue.ID, new DialogueData(dialogue));
        IsTalking = true;
        Words.Clear();
        if (startIndex < 0) startIndex = 0;
        else if (startIndex > dialogue.Words.Count - 1) startIndex = dialogue.Words.Count - 1;
        for (int i = startIndex; i < dialogue.Words.Count; i++)
            Words.Enqueue(dialogue.Words[i]);
        if (sayImmediately) SayNextWords();
        else MakeContinueOption(true);
        if (OptionAgents.Count < 1) MyTools.SetActive(UI.optionsParent.gameObject, false);
        MyTools.SetActive(UI.wordsText.gameObject, true);
        SetPageArea(false, false, false);
        OpenDialogueWindow();
    }
    //public void StartDialogue(DialogueWords words, bool sayImmediately = true)
    //{
    //    if (!UI) return;
    //    if (words == null) return;
    //    IsTalking = true;
    //    Words.Clear();
    //    Words.Enqueue(words);
    //    if (sayImmediately) SayNextWords();
    //    else MakeContinueOption(true);
    //    if (OptionAgents.Count < 1) MyTools.SetActive(UI.optionsParent.gameObject, false);
    //    MyTools.SetActive(UI.wordsText.gameObject, true);
    //    SetPageArea(false, false, false);
    //    OpenDialogueWindow();
    //}

    public void StartNormalDialogue(Talker talker)
    {
        if (!UI) return;
        MyTools.SetActive(UI.talkButton.gameObject, false);
        MTalker = talker;
        DialogueType = DialogueType.Normal;
        if (talker is QuestGiver && (talker as QuestGiver).QuestInstances.Count > 0)
            MyTools.SetActive(UI.questButton.gameObject, true);
        else MyTools.SetActive(UI.questButton.gameObject, false);
        CloseQuestDescriptionWindow();
        StartDialogue(talker.Info.DefaultDialogue);
        talker.OnTalkBegin();
    }

    public void StartQuestDialogue(Quest quest)
    {
        if (!quest) return;
        MQuest = quest;
        DialogueType = DialogueType.Quest;
        MyTools.SetActive(UI.questButton.gameObject, false);
        if (!MQuest.IsComplete && !MQuest.IsOngoing) StartDialogue(quest.BeginDialogue);
        else if (!MQuest.IsComplete && MQuest.IsOngoing) StartDialogue(quest.OngoingDialogue);
        else StartDialogue(quest.CompleteDialogue);
    }

    public void StartObjectiveDialogue(TalkObjective talkObjective)
    {
        if (talkObjective == null) return;
        this.talkObjective = talkObjective;
        DialogueType = DialogueType.Objective;
        StartDialogue(talkObjective.Dialogue);
    }

    public void StartBranchDialogue(BranchDialogue branch)
    {
        if (branch == null || !branch.Dialogue) return;
        if (lastWords.NeedToChusRightBranch)
        {
            branchDialogInstance = branch.Clone() as BranchDialogue;
            branchDialogInstance.runtimeParent = dialogToSay;
            if (lastWords.IndexOfRightBranch == lastWords.Branches.IndexOf(branch))
            {
                branchDialogInstance.runtimeIndexToGoBack = dialogToSay.Words.IndexOf(lastWords) + 1;
            }
            else branchDialogInstance.runtimeIndexToGoBack = dialogToSay.Words.IndexOf(lastWords);
        }
        else if (branch.GoBack)
        {
            branchDialogInstance = branch.Clone() as BranchDialogue;
            branchDialogInstance.runtimeParent = dialogToSay;
            branchDialogInstance.runtimeIndexToGoBack = branch.IndexToGo;
        }
        else branchDialogInstance = branch;
        this.branchDialog = branch;
        StartDialogue(branchDialogInstance.Dialogue, branchDialogInstance.SpecifyIndex);
    }
    #endregion

    #region 处理对话选项
    /// <summary>
    /// 生成继续按钮选项
    /// </summary>
    private void MakeContinueOption(bool force = false)
    {
        ClearOptionExceptContinue();
        OptionAgent oa = OptionAgents.Find(x => x.optionType == OptionType.Continue);
        if (Words.Count > 1 || force)
        {
            //如果还有话没说完，弹出一个“继续”按钮
            if (!oa)
            {
                oa = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent, false).GetComponent<OptionAgent>();
                oa.optionType = OptionType.Continue;
                oa.TitleText.text = "继续";
                if (!OptionAgents.Contains(oa))
                {
                    OptionAgents.Add(oa);
                }
                OpenOptionArea();
            }
        }
        else if (oa)
        {
            //如果话说完了，这是最后一句，则把“继续”按钮去掉
            OptionAgents.Remove(oa);
            ObjectPool.Instance.Put(oa.gameObject);
        }
        //当“继续”选项出现时，总没有其他选项出现，因此不必像下面一样还要处理一下，除非自己作死把行数写满让“继续”按钮没法显示
    }
    /// <summary>
    /// 生成任务列表的选项
    /// </summary>
    private void MakeTalkerQuestOption()
    {
        if (!(MTalker is QuestGiver)) return;
        ClearOptions();
        foreach (Quest quest in (MTalker as QuestGiver).QuestInstances)
        {
            if (!QuestManager.Instance.HasCompleteQuest(quest) && quest.AcceptAble)
            {
                OptionAgent oa = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent, false).GetComponent<OptionAgent>();
                oa.optionType = OptionType.Quest;
                oa.MQuest = quest;
                oa.TitleText.text = quest.Title + (quest.IsComplete ? "(完成)" : quest.IsOngoing ? "(进行中)" : string.Empty);
                if (quest.IsComplete)
                {
                    //保证完成的任务优先显示，方便玩家提交完成的任务
                    oa.transform.SetAsFirstSibling();
                    OptionAgents.Insert(0, oa);
                }
                else OptionAgents.Add(oa);
            }
        }
        //保证进行中的任务最后显示，方便玩家接取未接取的任务
        for (int i = OptionAgents.Count - 1; i >= 0; i--)
        {
            OptionAgent oa = OptionAgents[i];
            //若当前选项关联的任务在进行中
            if (oa.MQuest.IsOngoing && !oa.MQuest.IsComplete)
            {
                //则从后向前找一个新位置以放置该选项
                for (int j = OptionAgents.Count - 1; j > i; j--)
                {
                    //若找到了合适的位置
                    if (!OptionAgents[j].MQuest.IsOngoing && !OptionAgents[j].MQuest.IsComplete)
                    {
                        //则从该位置开始到选项的原位置，逐个前移一位，填补(覆盖)选项的原位置并空出新位置
                        for (int k = i; k < j; k++)
                        {
                            //在k指向目标位置之前，逐个前移
                            OptionAgents[k] = OptionAgents[k + 1];
                        }
                        //把选项放入新位置，此时选项的原位置即OptionAgents[i]已被填补(覆盖)
                        OptionAgents[j] = oa;
                        oa.transform.SetSiblingIndex(j);
                        break;
                    }
                }
            }
        }
        //把第一页以外的选项隐藏
        for (int i = UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight); i < OptionAgents.Count; i++)
        {
            MyTools.SetActive(OptionAgents[i].gameObject, false);
        }
        CheckPages();
    }

    /// <summary>
    /// 生成已完成任务选项
    /// </summary>
    private void MakeTalkerCmpltQuestOption()
    {
        if (!(MTalker is QuestGiver)) return;
        ClearOptions();
        foreach (Quest quest in (MTalker as QuestGiver).QuestInstances)
        {
            if (!QuestManager.Instance.HasCompleteQuest(quest) && quest.AcceptAble && quest.IsComplete)
            {
                OptionAgent oa = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent, false).GetComponent<OptionAgent>();
                oa.optionType = OptionType.Quest;
                oa.MQuest = quest;
                oa.TitleText.text = quest.Title + "(完成)";
                OptionAgents.Add(oa);
            }
        }
        //把第一页以外的选项隐藏
        for (int i = UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight); i < OptionAgents.Count; i++)
        {
            MyTools.SetActive(OptionAgents[i].gameObject, false);
        }
        CheckPages();
    }

    /// <summary>
    /// 生成对话目标列表的选项
    /// </summary>
    private void MakeTalkerObjectiveOption()
    {
        int index = 1;
        ClearOptionsExceptCmlptQuest();
        foreach (TalkObjective to in MTalker.objectivesTalkToThis)
        {
            if (to.AllPrevObjCmplt && !to.HasNextObjOngoing)
            {
                OptionAgent oa = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent, false).GetComponent<OptionAgent>();
                oa.optionType = OptionType.Objective;
                oa.TitleText.text = to.runtimeParent.Title;
                oa.talkObjective = to;
                OptionAgents.Add(oa);
                if (index > UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight)) MyTools.SetActive(oa.gameObject, false);//第一页以外隐藏
                index++;
            }
        }
        CheckPages();
    }

    /// <summary>
    /// 生成分支对话选项
    /// </summary>
    private void MakeBranchDialogueOption()
    {
        if (dialogToSay.Words.IndexOf(Words.Peek()) >= dialogToSay.Words.Count - 1) return;//最后一句话不支持分支
        if (Words.Peek().Branches.Count < 1) return;
        DialogueWords current = Words.Peek();
        DialogueData find = null;
        if (DialogueDatas.ContainsKey(dialogToSay.ID))
            find = DialogueDatas[dialogToSay.ID];
        ClearOptions();
        foreach (BranchDialogue branch in current.Branches)
        {
            if (find != null)
            {
                DialogueWordsData _find = find.wordsInfos.Find(x => x.wordsIndex == dialogToSay.Words.IndexOf(current));
                //这个分支是否完成了
                if (_find != null && _find.IsCmpltBranchWithIndex(current.Branches.IndexOf(branch)))
                    continue;//完成则跳过创建
            }
            if (!branchesComplete.Contains(branch) && branch.Dialogue)
            {
                OptionAgent oa = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent, false).GetComponent<OptionAgent>();
                oa.optionType = OptionType.Branch;
                oa.branchDialogue = branch;
                oa.TitleText.text = branch.Title;
                OptionAgents.Add(oa);
            }

        }
        //把第一页以外的选项隐藏
        for (int i = UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight); i < OptionAgents.Count; i++)
        {
            MyTools.SetActive(OptionAgents[i].gameObject, false);
        }
        CheckPages();
    }

    public void OptionPageUp()
    {
        if (Page <= 1) return;
        int leftLineCount = UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight);
        if (page > 0)
        {
            Page--;
            for (int i = 0; i < leftLineCount; i++)
            {
                if ((page - 1) * leftLineCount + i < OptionAgents.Count && (page - 1) * leftLineCount + i >= 0)
                    MyTools.SetActive(OptionAgents[(page - 1) * leftLineCount + i].gameObject, true);
                if (page * leftLineCount + i >= 0 && page * leftLineCount + i < OptionAgents.Count)
                    MyTools.SetActive(OptionAgents[page * leftLineCount + i].gameObject, false);
            }
        }
        if (Page == 1 && MaxPage > 1) SetPageArea(false, true, true);
        else SetPageArea(true, true, true);
        UI.pageText.text = Page.ToString() + "/" + MaxPage.ToString();
    }

    public void OptionPageDown()
    {
        if (Page >= MaxPage) return;
        int leftLineCount = UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight);
        if (page < Mathf.CeilToInt(OptionAgents.Count * 1.0f / (leftLineCount * 1.0f)))
        {
            for (int i = 0; i < leftLineCount; i++)
            {
                if ((page - 1) * leftLineCount + i < OptionAgents.Count && (page - 1) * leftLineCount + i >= 0)
                    MyTools.SetActive(OptionAgents[(page - 1) * leftLineCount + i].gameObject, false);
                if (page * leftLineCount + i >= 0 && page * leftLineCount + i < OptionAgents.Count)
                    MyTools.SetActive(OptionAgents[page * leftLineCount + i].gameObject, true);
            }
            Page++;
        }
        if (Page == MaxPage && MaxPage > 1) SetPageArea(true, false, true);
        else SetPageArea(true, true, true);
        UI.pageText.text = Page.ToString() + "/" + MaxPage.ToString();
    }
    #endregion

    #region 处理每句话
    /// <summary>
    /// 转到下一句话
    /// </summary>
    public void SayNextWords()
    {
        MakeContinueOption();
        MakeBranchDialogueOption();
        if (Words.Count == 1) HandlingLastWords();//因为Dequeue之后，话就没了，Words.Count就不是1了，而是0，所以要在此之前做这一步，意思是倒数第二句做这一步
        if (Words.Count > 0)
        {
            string talkerName = Words.Peek().TalkerName;
            if (Words.Peek().TalkerType == TalkerType.Player && PlayerInfoManager.Instance.PlayerInfo)
                talkerName = PlayerInfoManager.Instance.PlayerInfo.Name;
            UI.nameText.text = talkerName;
            lastWords = Words.Peek();
            UI.wordsText.text = Words.Dequeue().Words;
        }
        if (Words.Count <= 0)
        {
            OnFinishDialogueEvent?.Invoke();
            if (branchDialogInstance != null)
                HandlingLastBranchWords();//分支处理比较特殊，放到Dequque之后，否则分支最后一句不会讲
            //TryGoBack();
        }
    }

    public void SayTempWords(TalkerInfomation talkerInfo, string words, TalkerType talkerType, Dialogue dialogToGoBack, int indexToGoBack)
    {
        if (!UI) return;
        if (!talkerInfo || string.IsNullOrEmpty(words)) return;
        dialogToSay = dialogToGoBack;
        IndexToGoBack = indexToGoBack;
        IsTalking = true;
        Words.Clear();
        Words.Enqueue(new DialogueWords(talkerInfo, words, talkerType));
        MakeContinueOption(true);
        if (OptionAgents.Count < 1) MyTools.SetActive(UI.optionsParent.gameObject, false);
        MyTools.SetActive(UI.wordsText.gameObject, true);
        SetPageArea(false, false, false);
        StartCoroutine(WaitToGoBack());
    }

    /// <summary>
    /// 处理最后一句对话
    /// </summary>
    private void HandlingLastWords()
    {
        if (DialogueType == DialogueType.Normal && MTalker)
        {
            MTalker.OnTalkFinished();
            MakeTalkerCmpltQuestOption();
            if (MTalker.objectivesTalkToThis != null && MTalker.objectivesTalkToThis.Count > 0) MakeTalkerObjectiveOption();
            else ClearOptionsExceptCmlptQuest();
            QuestManager.Instance.UpdateUI();
        }
        else if (DialogueType == DialogueType.Objective && talkObjective != null) HandlingLastObjectiveWords();
        else if (DialogueType == DialogueType.Quest && MQuest) HandlingLastQuestWords();
    }
    /// <summary>
    /// 处理最后一句对话型目标的对话
    /// </summary>
    private void HandlingLastObjectiveWords()
    {
        talkObjective.UpdateTalkStatus();
        if (talkObjective.IsComplete)
        {
            OptionAgent oa = OptionAgents.Find(x => x.talkObjective == talkObjective);
            if (oa && oa.gameObject)
            {
                //去掉该对话目标自身的对话型目标选项
                OptionAgents.Remove(oa);
                RecycleOption(oa);
            }
            //目标已经完成，不再需要保留在对话人的目标列表里，从对话人的对话型目标里删掉相应信息
            MTalker.objectivesTalkToThis.RemoveAll(x => x == talkObjective);
        }
        talkObjective = null;//重置管理器的对话目标以防出错
        QuestManager.Instance.UpdateUI();
    }
    /// <summary>
    /// 处理最后一句任务的对话
    /// </summary>
    private void HandlingLastQuestWords()
    {
        if (!MQuest.IsOngoing || MQuest.IsComplete)
        {
            ClearOptions();
            //若是任务对话的最后一句，则根据任务情况弹出确认按钮
            OptionAgent yes = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent).GetComponent<OptionAgent>();
            OptionAgents.Add(yes);
            yes.optionType = OptionType.Confirm;
            yes.MQuest = MQuest;
            yes.TitleText.text = MQuest.IsComplete ? "完成" : "接受";
            if (!MQuest.IsComplete)
            {
                OptionAgent no = ObjectPool.Instance.Get(UI.optionPrefab, UI.optionsParent).GetComponent<OptionAgent>();
                OptionAgents.Add(no);
                no.optionType = OptionType.Back;
                no.TitleText.text = "拒绝";
            }
            OpenQuestDescriptionWindow(MQuest);
        }
        MQuest = null;
    }

    private void HandlingLastBranchWords()
    {
        DialogueWords find = branchDialogInstance.runtimeParent.Words.Find(x => x.Branches.Contains(branchDialog));
        if (find != null && find.IsRightBranch(branchDialog))
        {
            StartDialogue(branchDialogInstance.runtimeParent, branchDialogInstance.runtimeIndexToGoBack, false);
            int indexOfFind = branchDialogInstance.runtimeParent.Words.IndexOf(find);
            foreach (BranchDialogue branch in find.Branches)
            {
                branchesComplete.Add(branch);
                string parentID = branchDialogInstance.runtimeParent.ID;
                if (DialogueDatas.ContainsKey(parentID))
                {
                    DialogueWordsData _find = DialogueDatas[parentID].wordsInfos.Find(x => x.wordsIndex == indexOfFind);
                    if (find != null)
                    {
                        int indexOfBranch = find.Branches.IndexOf(branch);
                        _find.cmpltBranchIndexes.Add(indexOfBranch);//该分支已完成
                    }
                }
            }
            branchDialogInstance = null;
            branchDialog = null;
        }
        else if (find != null && find.NeedToChusRightBranch && !find.IsRightBranch(branchDialog))
        {
            //Debug.Log("Runtime" + branchInstance.runtimeIndexToGoBack);
            SayTempWords(find.TalkerInfo, find.WordsWhenChusWB, find.TalkerType, branchDialogInstance.runtimeParent, branchDialogInstance.runtimeIndexToGoBack);
            branchDialogInstance = null;
            branchDialog = null;
        }
        else if (branchDialogInstance.GoBack)//处理普通的带返回的分支
        {
            StartDialogue(branchDialogInstance.runtimeParent, branchDialogInstance.runtimeIndexToGoBack, false);
            int indexOfFind = branchDialogInstance.runtimeParent.Words.IndexOf(find);
            if (branchDialogInstance.DeleteWhenCmplt)
            {
                branchesComplete.Add(branchDialog);
                //TODO 保存分支完成信息
                string parentID = branchDialogInstance.runtimeParent.ID;
                if (DialogueDatas.ContainsKey(parentID))
                {
                    DialogueWordsData _find = DialogueDatas[parentID].wordsInfos.Find(x => x.wordsIndex == indexOfFind);
                    if (find != null)
                    {
                        int indexOfBranch = find.Branches.IndexOf(branchDialogInstance);
                        _find.cmpltBranchIndexes.Add(indexOfBranch);//该分支已完成
                    }
                }
            }
            branchDialogInstance = null;
            branchDialog = null;
        }
    }

    //private void TryGoBack()
    //{
    //    if (IndexToGoBack > -1)
    //    {
    //        //Debug.Log("GoBack");
    //        StartDialogue(dialogToSay, IndexToGoBack, false);
    //        IndexToGoBack = -1;
    //    }
    //}

    IEnumerator WaitToGoBack()
    {
        yield return new WaitUntil(() => Words.Count <= 0);
        //Debug.Log("Back");
        try
        {
            StartDialogue(dialogToSay, IndexToGoBack, false);
            IndexToGoBack = -1;
            StopCoroutine(WaitToGoBack());
        }
        catch
        {
            StopCoroutine(WaitToGoBack());
        }
    }
    #endregion

    #region UI相关
    public void OpenDialogueWindow()
    {
        if (!TalkAble) return;
        UI.dialogueWindow.alpha = 1;
        UI.dialogueWindow.blocksRaycasts = true;
        WindowsManager.Instance.PauseAllWindows(true);
    }
    public void CloseDialogueWindow()
    {
        UI.dialogueWindow.alpha = 0;
        UI.dialogueWindow.blocksRaycasts = false;
        DialogueType = DialogueType.Normal;
        MTalker = null;
        MQuest = null;
        dialogToSay = null;
        branchDialog = null;
        branchDialogInstance = null;
        ClearOptions();
        CloseQuestDescriptionWindow();
        IsTalking = false;
        WindowsManager.Instance.PauseAllWindows(false);
    }

    public void OpenOptionArea()
    {
        if (OptionAgents.Count < 1) return;
        MyTools.SetActive(UI.optionsParent.gameObject, true);
    }
    public void CloseOptionArea()
    {
        MyTools.SetActive(UI.optionsParent.gameObject, false);
        CloseQuestDescriptionWindow();
    }

    public void OpenQuestDescriptionWindow(Quest quest)
    {
        InitDescription(quest);
        UI.descriptionWindow.alpha = 1;
        UI.descriptionWindow.blocksRaycasts = true;
    }
    public void CloseQuestDescriptionWindow()
    {
        MQuest = null;
        UI.descriptionWindow.alpha = 0;
        UI.descriptionWindow.blocksRaycasts = false;
    }
    private void InitDescription(Quest quest)
    {
        if (quest == null) return;
        MQuest = quest;
        UI.descriptionText.text = string.Format("<size=16><b>{0}</b></size>\n[委托人: {1}]\n{2}",
            MQuest.Title,
            MQuest.OriginalQuestGiver.TalkerName,
            MQuest.Description);
        UI.moneyText.text = MQuest.RewardMoney > 0 ? MQuest.RewardMoney.ToString() : "无";
        UI.EXPText.text = MQuest.RewardEXP > 0 ? MQuest.RewardEXP.ToString() : "无";
        foreach (ItemAgent rwc in UI.rewardCells)
            rwc.Clear();
        foreach (ItemInfo info in quest.RewardItems)
            foreach (ItemAgent rw in UI.rewardCells)
            {
                if (rw.itemInfo == null)
                {
                    rw.Init(info);
                    break;
                }
            }
    }

    public void CanTalk(TalkTrigger2D talkTrigger)
    {
        if (IsTalking || !talkTrigger.talker) return;
        MTalker = talkTrigger.talker;
        UI.talkButton.onClick.RemoveAllListeners();
        UI.talkButton.onClick.AddListener(BeginNewDialogue);
        MyTools.SetActive(UI.talkButton.gameObject, true);
        TalkAble = true;
    }
    public void CannotTalk()
    {
        TalkAble = false;
        UI.talkButton.onClick.RemoveAllListeners();
        MyTools.SetActive(UI.talkButton.gameObject, false);
        CloseDialogueWindow();
    }

    private void OpenGiftWindow()
    {
        //TODO 把玩家背包道具读出并展示
    }
    #endregion

    #region 其它
    public void SendTalkerGifts()
    {
        if (!MTalker || !MTalker.Info.CanDEV_RLAT) return;
        OpenGiftWindow();
    }

    public void LoadTalkerQuest()
    {
        if (MTalker == null) return;
        MyTools.SetActive(UI.questButton.gameObject, false);
        Skip();
        MakeTalkerQuestOption();
        OpenOptionArea();
    }

    public void Skip()
    {
        while (Words.Count > 0)
            SayNextWords();
    }

    public void GotoDefault()
    {
        ClearOptions();
        CloseQuestDescriptionWindow();
        StartNormalDialogue(MTalker);
    }

    private void RecycleOption(OptionAgent oa)
    {
        oa.TitleText.text = string.Empty;
        oa.talkObjective = null;
        oa.MQuest = null;
        oa.optionType = OptionType.None;
        ObjectPool.Instance.Put(oa.gameObject);
    }

    private void ClearOptions()
    {
        for (int i = 0; i < OptionAgents.Count; i++)
        {
            if (OptionAgents[i])
            {
                RecycleOption(OptionAgents[i]);
            }
        }
        OptionAgents.Clear();
        Page = 1;
        SetPageArea(false, false, false);
    }

    private void ClearOptionsExceptCmlptQuest()
    {
        for (int i = 0; i < OptionAgents.Count; i++)
        {
            if (OptionAgents[i] && (OptionAgents[i].optionType != OptionType.Quest || (OptionAgents[i].optionType == OptionType.Quest && !OptionAgents[i].MQuest.IsComplete)))
            {
                RecycleOption(OptionAgents[i]);
            }
        }
        OptionAgents.RemoveAll(x => !x.gameObject.activeSelf);
        Page = 1;
        SetPageArea(false, false, false);
    }

    private void ClearOptionExceptContinue()
    {
        for (int i = 0; i < OptionAgents.Count; i++)
        {
            if (OptionAgents[i] && OptionAgents[i].optionType != OptionType.Continue)
            {
                RecycleOption(OptionAgents[i]);
            }
        }
        OptionAgents.RemoveAll(x => !x.gameObject.activeSelf);
        Page = 1;
        SetPageArea(false, false, false);
    }

    private void CheckPages()
    {
        MaxPage = Mathf.CeilToInt(OptionAgents.Count * 1.0f / ((UI.lineAmount - (int)(UI.wordsText.preferredHeight / UI.textLineHeight)) * 1.0f));
        if (MaxPage > 1)
        {
            SetPageArea(false, true, true);
            UI.pageText.text = Page.ToString() + "/" + MaxPage.ToString();
        }
        else
        {
            SetPageArea(false, false, false);
        }
    }

    private void SetPageArea(bool activeUp, bool activeDown, bool activeText)
    {
        MyTools.SetActive(UI.pageUpButton.gameObject, activeUp);
        MyTools.SetActive(UI.pageDownButton.gameObject, activeDown);
        MyTools.SetActive(UI.pageText.gameObject, activeText);
    }

    public void SetUI(DialogueUI UI)
    {
        if (!UI) return;
        this.UI = UI;
    }
    #endregion
}
public enum DialogueType
{
    Normal,
    Quest,
    Objective
}