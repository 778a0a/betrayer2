using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 拠点
/// </summary>
public class Castle : ICountryEntity, IMapEntity
{
    public int Id { get; set; }

    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 所有国
    /// </summary>
    [JsonIgnore]
    public Country Country { get; private set; }
    public void UpdateCountry(Country newCountry)
    {
        Country?.CastlesRaw.Remove(this);
        if (newCountry != null)
        {
            newCountry.CastlesRaw.Add(this);
            Country = newCountry;
        }

        // 所属キャラの更新などは呼び出し元で行ってもらう。
    }


    [JsonIgnore]
    public Character Boss => Members
        .OrderByDescending(m => m == Country.Ruler ? int.MaxValue : m.Contribution)
        .FirstOrDefault();

    /// <summary>
    /// 所属メンバー
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Character> Members => MembersRaw;
    [JsonIgnore]
    public List<Character> MembersRaw { get; } = new();

    [JsonIgnore]
    public float Power => Members
        .Select(m => m.Power)
        .DefaultIfEmpty(0)
        .Sum();
    [JsonIgnore]
    public float DefencePower => Members
        .Where(m => m.IsDefendable)
        .Select(m => m.Power)
        .DefaultIfEmpty(0)
        .Sum();
    public IEnumerable<Force> ReinforcementForces(ForceManager forces) => forces
        .Where(f => this.IsSelfOrAlly(f))
        .Where(f => f.Destination.Position == Position);
    public IEnumerable<Force> DangerForces(ForceManager forces) => forces
        // 友好的でない
        .Where(f => f.Country != Country && f.Country.GetRelation(Country) < 60)
        // 5マス以内にいる
        .Where(f => f.Position.DistanceTo(Position) <= 5)
        // 目的地が自城または、プレーヤーが操作する軍勢で城の周囲2マス以内に移動経路が含まれている(TODO)。
        .Where(f => f.Destination.Position == Position);

    public float DefenceAndReinforcementPower(ForceManager forces)
    {
        // 守兵の兵力
        var defPower = DefencePower;
        // 城に向かっている味方軍勢の兵力
        var forcesPower = ReinforcementForces(forces).Sum(f => f.Character.Power);
        return defPower + forcesPower;
    }

    [JsonIgnore]
    public bool DangerForcesExists { get; set; }

    /// <summary>
    /// 未所属メンバー
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Character> Frees => FreesRaw;
    [JsonIgnore]
    public List<Character> FreesRaw { get; } = new();

    /// <summary>
    /// 町
    /// </summary>
    [JsonIgnore]
    public List<Town> Towns { get; } = new();

    /// <summary>
    /// 開発レベル
    /// </summary>
    public int DevelopmentLevel { get; set; } = 1;
    /// <summary>
    /// 城塞レベル
    /// </summary>
    public int FortressLevel { get; set; } = 1;

    /// <summary>
    /// 砦強度
    /// </summary>
    public float Strength { get; set; }
    [JsonIgnore]
    public float StrengthMax => FortressLevel * 100;

    /// <summary>
    /// 安定度
    /// </summary>
    public float Stability { get; set; }

    /// <summary>
    /// 金
    /// </summary>
    public float Gold { get; set; }
    [JsonIgnore]
    public float GoldIncome => Towns.Sum(t => t.GoldIncome);
    [JsonIgnore]
    public float GoldIncomeMax => Towns.Sum(t => t.GoldIncomeMax);
    [JsonIgnore]
    public float GoldBalance => GoldIncome - Members.Sum(m => m.Salary);
    /// <summary>
    /// 食料
    /// </summary>
    public float Food { get; set; }
    [JsonIgnore]
    public float FoodIncome => Towns.Sum(t => t.FoodIncome);
    [JsonIgnore]
    public float FoodIncomeMax => Towns.Sum(t => t.FoodIncomeMax);
    [JsonIgnore]
    public float FoodBalance => FoodIncome - 3 * Members.Sum(m => m.FoodConsumption);
    /// <summary>
    /// 食料残り月数
    /// </summary>
    public float FoodRemainingMonths(GameDate current)
    {
        var remaining = Food;
        var incomePerYear = FoodIncome;
        var comsumptionPerMonth = Members.Sum(m => m.FoodConsumption);
        var months = 0;
        var date = current;
        while (true)
        {
            date = date.NextMonth();
            remaining -= comsumptionPerMonth;
            if (date.IsIncomeMonth) remaining += incomePerYear;
            if (remaining <= 0) break;
            // 無限ループにならないように適当に打ち切る。
            if (months > 30) return months;
            months++;
        }
        return months;
    }

    [JsonIgnore]
    public Dictionary<Castle, float> Distances { get; } = new();
    [JsonIgnore]
    public List<Castle> Neighbors { get; set; } = new();
    public const int NeighborDistanceMax = 5;

    /// <summary>
    /// 方針
    /// </summary>
    public CastleObjective Objective { get; set; }

    public override string ToString()
    {
        return $"城({Position} 城主: {Boss?.Name ?? "無"} - {Country.Ruler.Name}軍)";
    }
}

public enum CastleObjective
{
    None,
    Attack,
    Train,
    CastleStrength,
    Stability,
    Commerce,
    Agriculture,
}
