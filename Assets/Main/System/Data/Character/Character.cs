using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// キャラクター
/// </summary>
public class Character
{
    [JsonIgnore]
    private WorldData world;
    public void AttachWorld(WorldData world) => this.world = world;

    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 攻撃
    /// </summary>
    public int Attack { get; set; }
    /// <summary>
    /// 防御
    /// </summary>
    public int Defense { get; set; }
    /// <summary>
    /// 智謀
    /// </summary>
    public int Intelligence { get; set; }
    /// <summary>
    /// 統治
    /// </summary>
    public int Governing { get; set; }

    /// <summary>
    /// 性格
    /// </summary>
    [JsonIgnore]
    public Personality Personality { get; set; }
    public string PersonalityText
    {
        get => Personality.ToString();
        set => Personality = Enum.Parse<Personality>(value);
    }

    /// <summary>
    /// 特性
    /// </summary>
    [JsonIgnore]
    public Traits Traits { get; set; }
    public string TraintsText
    {
        get => string.Join(" ", Traits.ToString().Split(", "));
        set => Traits = Enum.Parse<Traits>(string.Join(", ", value.Split(" ")));
    }

    /// <summary>
    /// 忠誠基本値
    /// </summary>
    public int LoyaltyBase { get; set; }

    /// <summary>
    /// 所持金
    /// </summary>
    public float Gold { get; set; }
    /// <summary>
    /// 功績
    /// </summary>
    public float Contribution { get; set; }
    /// <summary>
    /// 名声
    /// </summary>
    public int Prestige { get; set; }
    /// <summary>
    /// 忠誠
    /// </summary>
    [JsonIgnore] // 計算で求めるので保存不要
    public int Loyalty { get; set; }
    /// <summary>
    /// 給料配分
    /// </summary>
    public int SalaryRatio { get; set; }

    /// <summary>
    /// 軍勢
    /// </summary>
    public Soldiers Soldiers { get; set; }

    /// <summary>
    /// 連戦回数
    /// </summary>
    public int ConsecutiveBattleCount { get; set; }

    /// <summary>
    /// プレーヤーならtrue
    /// </summary>
    public bool IsPlayer { get; set; }

    /// <summary>
    /// （内部データ）強さ
    /// </summary>
    [JsonIgnore]
    public int Power => (Attack + Defense + Intelligence) / 3 * Soldiers.Power;

    //public string GetLoyaltyText(WorldData world) => world.IsRuler(this) ? "--" : Loyalty.ToString();

    /// <summary>
    /// 恨み
    /// </summary>
    public int Urami { get; set; } = 0;
    public void AddUrami(int value)
    {
        Urami = Mathf.Clamp(Urami + value, 0, 100);
    }

    /// <summary>
    /// 給料
    /// </summary>
    [JsonIgnore]
    public int Salary
    {
        get
        {
            //return 5 + (int)Math.Floor((-1 + Math.Sqrt(1 + 0.8 * Contribution)) / 2);
            //return 5 + (int)Math.Floor(Math.Sqrt(Contribution / 5));
            var sum = 0;
            var max = 100;
            var i = 0;
            for (; i < max; i++)
            {
                sum += i * (5 + i / 3);
                if (sum > Contribution) break;
            }
            return i + 4;
        }
    }
    /// <summary>
    /// 食料消費
    /// </summary>
    [JsonIgnore]
    public int FoodConsumption => Soldiers.Sum(s => s.Hp);
    [JsonIgnore]
    public int FoodConsumptionMax => Soldiers.Sum(s => s.MaxHp);

    /// <summary>
    /// 行動力
    /// </summary>
    public int ActionPoints { get; set; }

    /// <summary>
    /// 行動不能回復までの残り日数
    /// </summary>
    public int IncapacitatedDaysRemaining { get; set; }
    [JsonIgnore]
    public bool IsIncapacitated => IncapacitatedDaysRemaining > 0;
    
    [JsonIgnore]
    public bool IsDefendable => !IsIncapacitated && !IsMoving;

    /// <summary>
    /// 行動不能状態にします。
    /// </summary>
    public void SetIncapacitated()
    {
        var old = IncapacitatedDaysRemaining;
        IncapacitatedDaysRemaining = 90;
        if (old == 0)
        {
            Debug.Log($"{Name}は行動不能になりました。");
        }
        else
        {
            Debug.Log($"{Name}は行動不能状態が延長されました。(prev: {old})");
        }
    }

    public bool CanPay(ActionCost cost) => cost.CanPay(this);
    [JsonIgnore]
    public bool IsMoving => Force != null;
    [JsonIgnore]
    public Force Force { get; set; } // メモ ForceManagerと二重管理
    [JsonIgnore]
    public bool CanDefend => !IsMoving && !IsIncapacitated;
    [JsonIgnore]
    public Country Country { get; private set; } // メモ Castle.Countryと二重管理
    [JsonIgnore]
    public bool IsRuler => Country?.Ruler == this;
    [JsonIgnore]
    public bool IsVassal => !IsFree && !IsRuler;
    [JsonIgnore]
    public bool IsFree => Country == null;
    [JsonIgnore]
    public bool IsRulerOrVassal => !IsFree;
    [JsonIgnore]
    public Castle Castle { get; private set; } // メモ Castle.Memberと二重管理
    public void ChangeCastle(Castle newCastle, bool asFree)
    {
        var oldCastle = Castle;
        if (oldCastle != null)
        {
            oldCastle.FreesRaw.Remove(this);
            oldCastle.MembersRaw.Remove(this);
        }

        Castle = newCastle;
        if (asFree)
        {
            newCastle.FreesRaw.Add(this);
            Country = null;
        }
        else
        {
            newCastle.MembersRaw.Add(this);
            Country = newCastle.Country;
            // TODO 城の国が変わった場合
        }
    }

    /// <summary>
    /// 地位
    /// </summary>
    public string GetTitle(LocalizationManager L)
    {
        if (IsRuler)
        {
            return L[Country.CountryRank switch
            {
                CountryRank.Empire => "皇帝",
                CountryRank.Kingdom => "王",
                CountryRank.Duchy => "大公",
                _ => "君主",
            }];
        }
        else if (IsVassal)
        {
            var country = Country;
            var order = country.Vassals.OrderBy(c => c.Contribution).ToList().IndexOf(this);
            return L[new[]
            {
                "従士",
                "従士",
                "士長",
                "将軍",
                "元帥",
                "宰相",
                "総督",
                "副王",
            }[order]];
        }
        else
        {
            return L["浪士"];
        }
    }

    public string csvDebugData { get; set; } = "";
    public string csvDebugMemo { get; set; } = "";

    public override string ToString() => $"{Name} G:{Gold} P:{Power} (A:{Attack} D:{Defense} I:{Intelligence})";
}
