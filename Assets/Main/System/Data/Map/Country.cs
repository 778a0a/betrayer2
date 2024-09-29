using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 国
/// </summary>
public class Country : ICountryEntity
{
    Country ICountryEntity.Country => this;

    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 君主
    /// </summary>
    public Character Ruler { get; set; }
    /// <summary>
    /// 拠点
    /// </summary>
    public List<Castle> Castles { get; set; } = new();

    public IEnumerable<Character> Members => Castles.SelectMany(c => c.Members);
    public IEnumerable<Character> Vassals => Members.Where(c => c != Ruler);

    /// <summary>
    /// マップの国の色のインデックス
    /// </summary>
    public int ColorIndex { get; set; }
    public Sprite Sprite => MapManager.Instance.GetCountrySprite(ColorIndex);

    public string GetTerritoryName()
    {
        return Ruler.Name + (CountryRank switch
        {
            CountryRank.Empire => "帝国領",
            CountryRank.Kingdom => "王国領",
            CountryRank.Duchy => "大公領",
            _ => "領",
        });
    }

    public bool Has(GameMapTile tile)
    {
        return Castles.Any(c => c.Position == tile.Position || c.Towns.Any(t => t.Position == tile.Position));
    }

    public CountryRank CountryRank => Castles.Count switch
    {
        >= 20 => CountryRank.Empire,
        >= 10 => CountryRank.Kingdom,
        >= 5 => CountryRank.Duchy,
        _ => CountryRank.Chiefdom,
    };
}

public enum CountryRank
{
    Empire,
    Kingdom,
    Duchy,
    Chiefdom,
}

