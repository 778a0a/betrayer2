using System;
using System.Collections.Generic;
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
    public Force Force { get; set; }

    /// <summary>
    /// プレーヤーならtrue
    /// </summary>
    public bool IsPlayer { get; set; }

    /// <summary>
    /// 侵攻済みならtrue
    /// </summary>
    public bool IsAttacked { get; set; }

    /// <summary>
    /// 行動済みならtrue
    /// </summary>
    public bool IsExhausted { get; set; }

    /// <summary>
    /// （内部データ）強さ
    /// </summary>
    [JsonIgnore]
    public int Power => (Attack + Defense + Intelligence) / 3 * Force.Power;

    //public string GetLoyaltyText(WorldData world) => world.IsRuler(this) ? "--" : Loyalty.ToString();

    public string debugImagePath { get; set; }
    public string debugMemo { get; set; }

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
    /// 行動力
    /// </summary>
    public int ActionPoint { get; set; }

    ///// <summary>
    ///// 地位
    ///// </summary>
    //public string GetTitle(WorldData world, LocalizationManager L)
    //{
    //    if (world.IsRuler(this))
    //    {
    //        var country = world.CountryOf(this);
    //        return L[country.CountryRank switch
    //        {
    //            CountryRank.Empire => "皇帝",
    //            CountryRank.Kingdom => "王",
    //            CountryRank.Duchy => "大公",
    //            _ => "君主",
    //        }];
    //    }
    //    else if (world.IsVassal(this))
    //    {
    //        var country = world.CountryOf(this);
    //        var order = Mathf.Max(country.Vassals.Count, country.VassalCountMax) - country.Vassals.IndexOf(this) - 1;
    //        return L[new[]
    //        {
    //            "従士",
    //            "従士",
    //            "士長",
    //            "将軍",
    //            "元帥",
    //            "宰相",
    //            "総督",
    //            "副王",
    //        }[order]];
    //    }
    //    else
    //    {
    //        return L["浪士"];
    //    }
    //}

    public override string ToString() => $"{Name} G:{Gold} P:{Power} (A:{Attack} D:{Defense} I:{Intelligence})";
}
