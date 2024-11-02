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
    public static int SalaryRatioMin = 0;
    public static int SalaryRatioMax = 100;

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
    /// 忠誠基本値
    /// </summary>
    public int LoyaltyBase { get; set; }

    /// <summary>
    /// 所持金
    /// </summary>
    public int Gold { get; set; }
    /// <summary>
    /// 功績
    /// </summary>
    public int Contribution { get; set; }
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
    /// プレーヤーならtrue
    /// </summary>
    public bool IsPlayer { get; set; }

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
    public int Salary
    {
        get
        {
            //return 5 + (int)Math.Floor((-1 + Math.Sqrt(1 + 0.8 * Contribution)) / 2);
            //return 5 + (int)Math.Floor(Math.Sqrt(Contribution / 5));
            var sum = 0;
            var max = 100;
            for (int i = 0; i < max; i++)
            {
                sum += i * 10;
                if (sum > Contribution) return i + 4;
            }
            return max + 4;
        }
    }
    /// <summary>
    /// 食料消費
    /// </summary>
    public int FoodConsumption => Soldiers.Sum(s => s.MaxHp) / 12;

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

    [JsonIgnore]
    public bool IsMoving => world.Forces.Any(f => f.Character == this);
    [JsonIgnore]
    public bool CanDefend => !IsMoving && !IsIncapacitated;
    [JsonIgnore]
    public Country Country => world.Countries.FirstOrDefault(c => c.Ruler == this || c.Vassals.Contains(this));
    [JsonIgnore]
    public bool IsRuler => Country?.Ruler == this;
    [JsonIgnore]
    public bool IsVassal => Country?.Vassals.Contains(this) ?? false;
    [JsonIgnore]
    public bool IsFree => Country == null;
    [JsonIgnore]
    public bool IsRulerOrVassal => !IsFree;
    [JsonIgnore]
    public Castle Castle =>
        Country?.Castles.FirstOrDefault(c => c.Members.Contains(this)) ??
        world.Castles.FirstOrDefault(c => c.Frees.Contains(this));

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
