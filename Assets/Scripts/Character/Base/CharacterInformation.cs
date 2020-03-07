﻿using UnityEngine;

[CreateAssetMenu(fileName = "character info", menuName = "ZetanStudio/角色/角色信息")]
public class CharacterInformation : ScriptableObject
{
    [SerializeField]
    protected string _ID;
    public string ID
    {
        get
        {
            return _ID;
        }
    }

    [SerializeField]
    protected string _Name;
    public new string name
    {
        get
        {
            return _Name;
        }
    }

    [SerializeField]
#if UNITY_EDITOR
    [EnumMemberNames("未知", "男", "女")]
#endif
    protected CharacterSex sex;
    public CharacterSex Sex
    {
        get
        {
            return sex;
        }
    }
}
public enum CharacterSex
{
    Unknown,
    Male,
    Female,
}