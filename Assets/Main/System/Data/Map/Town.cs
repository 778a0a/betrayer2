
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 町
/// </summary>
public class Town : ICountryEntity, IMapEntity
{
    /// <summary>
    /// 位置
    /// </summary>
    [JsonProperty("P")]
    public MapPosition Position { get; set; }

    /// <summary>
    /// 町が所属する城
    /// </summary>
    [JsonIgnore]
    public Castle Castle { get; set; }

    [JsonIgnore]
    public Country Country => Castle.Country;

    ///// <summary>
    ///// 開発レベル
    ///// </summary>
    //[JsonProperty("D")]
    //public int DevelopmentLevel { get; set; } = 0;

    ///// <summary>
    ///// 累計投資額
    ///// </summary>
    //[JsonProperty("I")]
    //public float TotalInvestment { get; set; } = 0;

    /// <summary>
    /// 商業
    /// </summary>
    [JsonProperty("G")]
    public float GoldIncome { get; set; }
    
    //[JsonIgnore]
    //public float GoldIncomeMax => TotalInvestment / 10f;
    //[JsonIgnore]
    //public float GoldImproveAdj => 1 + (GoldIncomeMax - GoldIncome) / GoldIncomeMax;

    //public static float Diminish(float current, float max, float maxBase)
    //{
    //    var progress = current / max;
    //    return progress switch
    //    {
    //        < 0.25f => 2.0f,
    //        < 0.5f => 1.5f,
    //        _ => (1.5f - progress) * (3f / (1 + current / maxBase)).MaxWith(1), // lv4なら0.6、lv5なら0.5、lv6なら0.43
    //    };
    //}
}
