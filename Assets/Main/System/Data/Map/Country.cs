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
    /// 勢力目標
    /// </summary>
    public CountryObjective Objective { get; set; }
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

    public int SoldierCount => Castles.Sum(c => c.SoldierCount);
    public float Power => Castles.Sum(c => c.Power);

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
    public Sprite Sprite => Static.GetCountrySprite(ColorIndex);

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

    /// <summary>
    /// 四半期の戦略アクションを行っていればtrue
    /// </summary>
    public bool QuarterActionDone { get; set; } = false;

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

/// <summary>
/// 勢力目標
/// </summary>
public record CountryObjective
{
    public static CountryObjective Parse(string csvColumn)
    {
        var cols = csvColumn.Split(':');
        var type = cols[0];
        switch (type)
        {
            case nameof(RegionConquest):
                return new RegionConquest
                {
                    TargetRegionName = cols[1],
                };
            case nameof(CountryAttack):
                return new CountryAttack
                {
                    TargetRulerName = cols[1],
                };
            case nameof(StatusQuo):
                return new StatusQuo();
            default:
                throw new Exception("Unknown CountryObjective type: " + type);
        }
    }
    public string ToCsvColumn()
    {
        return this switch
        {
            RegionConquest rc => $"{nameof(RegionConquest)}:{rc.TargetRegionName}",
            CountryAttack ca => $"{nameof(CountryAttack)}:{ca.TargetRulerName}",
            StatusQuo _ => nameof(StatusQuo),
            _ => throw new Exception("Unknown CountryObjective type: " + GetType().Name),
        };
    }

    public static List<IReadOnlyList<CountryObjective>> Candidates(Country country)
    {
        var list = new List<IReadOnlyList<CountryObjective>>
        {
            country.Castles
                .Concat(country.Castles.SelectMany(c => c.Neighbors))
                .Select(c => c.Region)
                .Distinct()
                .Select(r => new RegionConquest { TargetRegionName = r })
                .ToList(),
            country.Neighbors
                .Where(n => country.IsAttackable(n))
                .Select(n => new CountryAttack { TargetRulerName = n.Ruler.Name })
                .ToList(),
            new List<CountryObjective>() { new StatusQuo() }
        };

        return list;
    }

    public virtual bool IsAttackTarget(Castle target) => false;

    /// <summary>
    /// 地方統一
    /// </summary>
    public record RegionConquest : CountryObjective
    {
        public string TargetRegionName { get; set; }

        public override bool IsAttackTarget(Castle target) => target.Region == TargetRegionName;

        public override string ToString() => $"地方統一({TargetRegionName})";
    }

    /// <summary>
    /// 勢力打倒
    /// </summary>
    public record CountryAttack : CountryObjective
    {
        public string TargetRulerName { get; set; }

        public bool IsAttackTarget(Country target) => target.Ruler.Name == TargetRulerName;
        public override bool IsAttackTarget(Castle target) => IsAttackTarget(target.Country);

        public override string ToString() => $"勢力打倒({TargetRulerName})";
    }

    /// <summary>
    /// 現状維持
    /// </summary>
    public record StatusQuo : CountryObjective
    {
        public bool IsTempolary { get; set; } = false;

        public override string ToString() => IsTempolary ? "現状維持(一時的)" : "現状維持";
    }
}
