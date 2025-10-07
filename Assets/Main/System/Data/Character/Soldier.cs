using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 兵士
/// </summary>
public class Soldier
{
    /// <summary>
    /// レベル
    /// </summary>
    public int Level { get; set; }
    /// <summary>
    /// 経験値
    /// </summary>
    public int Experience { get; set; }
    /// <summary>
    /// HP
    /// </summary>
    public int Hp
    {
        get => (int)Mathf.Ceil(HpFloat);
        set => HpFloat = value;
    }

    public float HpFloat { get; set; }

    public int MaxHp => IsEmptySlot ? 0 : (Level * 5 + 30);

    public bool IsEmptySlot { get; set; }
    public bool IsAlive => !IsEmptySlot && HpFloat > 0;
    public bool IsDeadInBattle { get; set; }

    public void AddExperience(Character owner, bool isTraining = false, bool drillMasterExists = false)
    {
        if (IsEmptySlot) return;
        
        var exp = 20 + Random.Range(-5, 5) + (drillMasterExists ? Random.Range(2, 5) : 0);
        if (isTraining)
        {
            exp = (int)(exp * owner.Attack.MinWith(owner.Defense, owner.Intelligence) / 100f);
        }
        else
        {
            exp /= 5;
        }
        Experience += exp;

        // 十分経験値が貯まればレベルアップする。
        if (Experience >= GetNextLevelExperience(Level))
        {
            Level += 1;
            Experience = 0;
            owner.Contribution += 0.3f;
        }
    }

    public static int GetNextLevelExperience(int currentLevel)
    {
        if (s_LevelUpExperienceTable.TryGetValue(currentLevel, out var nextExp))
        {
            return nextExp;
        }
        return 6000;
    }

    private static readonly Dictionary<int, int> s_LevelUpExperienceTable = new()
    {
        {  1,  150 },
        {  2,  200 },
        {  3,  250 },
        {  4,  300 },
        {  5,  400 },
        {  6,  600 },
        {  7, 1000 },
        {  8, 1500 },
        {  9, 3000 },
        { 10, 4000 },
        { 11, 5000 },
        { 12, 6000 },
        { 13, 6000 },
        { 14, 6000 },
        { 15, 6000 },
        { 16, 6000 },
    };

    [JsonIgnore]
    public Texture2D Image => Static.GetSoldierImage(Level);

    public override string ToString() => IsEmptySlot ? "Empty" : $"Lv{Level} HP{Hp}/{MaxHp} Exp:{Experience}";
    public string ToShortString() => IsEmptySlot ? "E" : $"{Level}";
}
