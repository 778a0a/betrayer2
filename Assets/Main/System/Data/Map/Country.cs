using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 国
/// </summary>
public class Country : ICountryEntity
{
    public CountryManager manager;
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
    public IReadOnlyList<Castle> Castles => CastlesRaw;
    public List<Castle> CastlesRaw { get; set; } = new();

    public IEnumerable<Character> Members => Castles.SelectMany(c => c.Members);
    public IEnumerable<Character> Vassals => Members.Where(c => c != Ruler);
    public int MaxImportantMemberCount => 3 + (int)Mathf.Ceil((Castles.Count - 1) / 3f);

    public IEnumerable<Country> Neighbors => Castles
        .SelectMany(c => c.Neighbors)
        .Select(c => c.Country)
        .Distinct()
        .Where(c => c != this);

    public float GoldBalance => Castles.Sum(c => c.GoldBalance);
    public float GoldSurplus => Castles.Sum(c => c.GoldSurplus);

    public IEnumerable<Country> DiplomacyTargets => Ruler.Personality switch
    {
        Personality.Merchant or Personality.Leader =>
            Neighbors.Concat(Neighbors.SelectMany(c => c.Neighbors)).Distinct().Where(c => c != this),
        _ => Neighbors,
    };

    public const int AllyRelation = 100;
    public const int EnemyRelation = 0;
    public float GetRelation(Country other) => manager.GetRelation(this, other);
    public void SetRelation(Country other, float rel) => manager.SetRelation(this, other, rel);
    public void SetAlly(Country other) => manager.SetRelation(this, other, AllyRelation);
    public void SetEnemy(Country other) => manager.SetRelation(this, other, EnemyRelation);

    /// <summary>
    /// マップの国の色のインデックス
    /// </summary>
    public int ColorIndex { get; set; }
    public Sprite Sprite => Static.Instance.GetCountrySprite(ColorIndex);

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

    public override string ToString()
    {
        return $"国({Ruler.Name} 城数: {Castles.Count}, 将数: {Members.Count()})";
    }
}

public enum CountryRank
{
    Empire,
    Kingdom,
    Duchy,
    Chiefdom,
}

