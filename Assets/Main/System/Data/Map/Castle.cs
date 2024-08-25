using System;
using System.Collections.Generic;
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

    /// <summary>
    /// 金
    /// </summary>
    public float Gold { get; set; }
    /// <summary>
    /// 食料
    /// </summary>
    public float Food { get; set; }

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
