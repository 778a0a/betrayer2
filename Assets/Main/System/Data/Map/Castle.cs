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
    public IEnumerable<GameMapTile> NewTownCandidates(WorldData w) => Towns
        // 既存の町に隣接している
        .SelectMany(t => w.Map.GetTile(t).Neighbors)
        // 未開拓
        .Where(t => !t.HasTown)
        .Distinct();

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
    [JsonIgnore]
    public float StabilityMax => 100;

    [JsonIgnore] public float Wealth => Gold + Food / 50;
    [JsonIgnore] public float WealthBalance => GoldBalance + FoodBalance / 50;
    [JsonIgnore] public float WealthBalanceConservative => GoldBalance + FoodBalanceConservative / 50;
    [JsonIgnore] public float WealthBalanceMax => GoldBalanceMax + FoodBalanceMax / 50;

    /// <summary>
    /// 金
    /// </summary>
    public float Gold { get; set; }
    [JsonIgnore]
    public float GoldIncome => Towns.Sum(t => t.GoldIncome) * Stability / StabilityMax;
    [JsonIgnore]
    public float GoldIncomeMax => Towns.Sum(t => t.GoldIncomeMax);
    [JsonIgnore]
    public float GoldIncomeProgress => GoldIncome / GoldIncomeMax;
    [JsonIgnore]
    public float GoldBalance => GoldIncome - GoldComsumption;
    [JsonIgnore]
    public float GoldBalanceMax => Towns.Sum(t => t.GoldIncome) - GoldComsumption;
    [JsonIgnore]
    public float GoldComsumption => Members.Sum(m => m.Salary);
    [JsonIgnore]
    public float GoldSurplus => (Gold + (GoldIncome - GoldComsumption).MaxWith(0) * 4).MinWith(0);
    /// <summary>
    /// 食料
    /// </summary>
    public float Food { get; set; }
    [JsonIgnore]
    public float FoodIncome => Towns.Sum(t => t.FoodIncome) * Stability / StabilityMax;
    [JsonIgnore]
    public float FoodIncomeMax => Towns.Sum(t => t.FoodIncomeMax);
    [JsonIgnore]
    public float FoodIncomeProgress => FoodIncome / FoodIncomeMax;
    [JsonIgnore]
    public float FoodBalance => FoodIncome - FoodComsumption;
    // 一時的に兵が減って収支が改善した場合に判断を間違えないように、最大消費量を加味した収支も計算する。
    [JsonIgnore]
    public float FoodBalanceConservative => FoodIncome - FoodComsumptionMax;
    [JsonIgnore]
    public float FoodBalanceMax => Towns.Sum(t => t.FoodIncome) - FoodComsumptionMax;
    [JsonIgnore]
    public float FoodComsumption => Members.Sum(m => m.FoodConsumption);
    [JsonIgnore]
    public float FoodComsumptionMax => Members.Sum(m => m.FoodConsumptionMax);
    [JsonIgnore]
    public float FoodSurplus => (Food + (FoodIncome - FoodComsumptionMax).MaxWith(0) * 4).MinWith(0);

    /// <summary>
    /// 食料残り月数
    /// </summary>
    public float FoodRemainingMonths(GameDate current)
    {
        var remaining = Food;
        var income = FoodIncome;
        var comsumption = FoodComsumption;
        var months = 0;
        var date = current;
        while (true)
        {
            date = date.NextMonth();
            if (date.IsIncomeMonth)
            {
                remaining -= comsumption;
                remaining += income;
                if (remaining <= 0) break;
            }
            // 無限ループにならないように適当に打ち切る。
            if (months > 36) return months;
            months++;
        }
        return months;
    }

    /// <summary>
    /// 四半期
    /// </summary>
    public int FoodRemainingQuarters()
    {
        var remaining = Food;
        var income = FoodIncome;
        var comsumption = FoodComsumption;
        var quarters = 0;
        while (true)
        {
            remaining -= comsumption;
            remaining += income;
            if (remaining <= 0) break;
            if (quarters > 40) break;
            quarters++;
        }
        return quarters;
    }

    public int GoldRemainingQuarters()
    {
        var remaining = Gold;
        var income = GoldIncome;
        var comsumption = GoldComsumption;
        var quarters = 0;
        while (true)
        {
            remaining -= comsumption;
            remaining += income;
            if (remaining <= 0) break;
            if (quarters > 40) break;
            quarters++;
        }
        return quarters;
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
