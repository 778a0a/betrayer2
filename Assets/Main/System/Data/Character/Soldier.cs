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

    public int MaxHp => Level * 5 + 30;

    public bool IsEmptySlot { get; set; }
    public bool IsAlive => !IsEmptySlot && HpFloat > 0;

    public void AddExperience(Character owner)
    {
        if (IsEmptySlot) return;
        
        Experience += 10 + Random.Range(0, 4);
        // 十分経験値が貯まればレベルアップする。
        if (Experience >= Level * 100 && Level < 13)
        {
            Level += 1;
            Experience = 0;
            owner.Contribution += 1;
        }
    }

    [JsonIgnore]
    public Texture2D Image => Static.Instance.GetSoldierImage(Level);

    public override string ToString() => IsEmptySlot ? "Empty" : $"Lv{Level} HP{Hp}/{MaxHp} Exp:{Experience}";
    public string ToShortString() => IsEmptySlot ? "E" : $"{Level}";
}
