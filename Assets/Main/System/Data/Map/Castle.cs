using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 拠点
/// </summary>
public class Castle
{
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 城が存在するならtrue
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 所有国
    /// </summary>
    public Country Country { get; private set; }

    /// <summary>
    /// 所属メンバー
    /// </summary>
    public List<Character> Members { get; } = new();

    /// <summary>
    /// 町
    /// </summary>
    public List<Town> Towns { get; } = new();

    /// <summary>
    /// 砦強度
    /// </summary>
    public float Strength { get; set; }
    public float StrengthMax { get; set; }

    /// <summary>
    /// 金
    /// </summary>
    public float Gold { get; set; }
    public float GoldIncome => Towns.Sum(t => t.GoldIncome);
    public float GoldIncomeMax => Towns.Sum(t => t.GoldIncomeMax);
    public float GoldBalance => GoldIncome - Members.Sum(m => m.Salary);
    /// <summary>
    /// 食料
    /// </summary>
    public float Food { get; set; }
    public float FoodIncome => Towns.Sum(t => t.FoodIncome);
    public float FoodIncomeMax => Towns.Sum(t => t.FoodIncomeMax);
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
            if (date.IsFoodIncomeMonth) remaining += incomePerYear;
            if (remaining <= 0) break;
            // 無限ループにならないように適当に打ち切る。
            if (months > 30) return months;
            months++;
        }
        return months;
    }

    public void SetCountry(Country country)
    {
        if (Country != null)
        {
            Country.Castles.Remove(this);
        }

        Country = country;
        country.Castles.Add(this);
    }

    public void AddTown(Town town)
    {
        town.Castle = this;
        Towns.Add(town);
    }
}
