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
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }
    /// <summary>
    /// 地方
    /// </summary>
    public string Region { get; set; }
    /// <summary>
    /// 名称
    /// </summary>
    public string Name { get; set; }

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
        .OrderBy(m => m.OrderIndex)
        .FirstOrDefault();

    /// <summary>
    /// 所属メンバー
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<Character> Members => MembersRaw;
    [JsonIgnore]
    public List<Character> MembersRaw { get; } = new();

    [JsonIgnore]
    public int SoldierCount => Members.Select(m => m.Soldiers.SoldierCount).DefaultIfEmpty(0).Sum();
    [JsonIgnore]
    public int SoldierCountMax => Members.Select(m => m.Soldiers.SoldierCountMax).DefaultIfEmpty(0).Sum();
    [JsonIgnore]
    public float Power => Members.Select(m => m.Power).DefaultIfEmpty(0).Sum();
    [JsonIgnore]
    public float DefencePower => Members.Where(m => m.IsDefendable).Select(m => m.Power).DefaultIfEmpty(0).Sum();

    public IEnumerable<Force> ReinforcementForces(ForceManager forces) => forces
        .Where(f => this.IsSelfOrAlly(f))
        .Where(f => f.Destination.Position == Position);
    public IEnumerable<Force> DangerForces(ForceManager forces) => forces
        // 友好的でない
        .Where(f => f.Country != Country && f.Country.GetRelation(Country) < 60)
        // 5マス以内にいる
        .Where(f => f.Position.DistanceTo(Position) <= 5)
        .Where(f =>
        {
            // 目的地が自城
            if (f.Destination.Position == Position) return true;
            // プレーヤーが操作する軍勢で城の周囲2マス以内に移動経路が含まれている。
            if (f.Character.IsPlayer || f.Character.Castle.Boss.IsPlayer || f.Character.Country.Ruler.IsPlayer)
            {
                foreach (var pos in f.DestinationPath)
                {
                    if (pos.DistanceTo(Position) <= 2) return true;
                }
            }

            return false;
        });

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
    public float Strength { get; set; }
    [JsonIgnore]
    public float StrengthMax => 100;

    /// <summary>
    /// 総投資額
    /// </summary>
    public float TotalInvestment { get; set; }
    /// <summary>
    /// 発展度
    /// </summary>
    [JsonIgnore]
    public int DevLevel => (int)(TotalInvestment / 100f);
    /// <summary>
    /// 最大許容所属人数
    /// </summary>
    [JsonIgnore]
    public int MaxMember => 3 + DevLevel / 10;

    /// <summary>
    /// 金
    /// </summary>
    public float Gold { get; set; }
    [JsonIgnore]
    public float GoldIncome => Towns.Sum(t => t.GoldIncome);
    [JsonIgnore]
    public float GoldIncomeMax => TotalInvestment / 10;
    [JsonIgnore]
    public float GoldIncomeProgress => GoldIncome / GoldIncomeMax;
    [JsonIgnore]
    public float GoldBalance => GoldIncome - GoldComsumption;
    [JsonIgnore]
    public float GoldComsumption => Members.Sum(m => m.Salary * 3);
    [JsonIgnore]
    public float GoldAmari => Gold - GoldComsumption;
    /// <summary>
    /// 金の余剰。所持金をベースに、赤字の場合は今後1年分の赤字額を引いたもの。
    /// </summary>
    [JsonIgnore]
    public float GoldSurplus => Gold + GoldBalance.MaxWith(0) * 4;

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
    /// 前線ならtrue
    /// </summary>
    [JsonIgnore]
    public bool IsFrontline => Neighbors.Any(this.IsAttackable);

    /// <summary>
    /// 方針
    /// </summary>
    public CastleObjective Objective { get; set; } = new CastleObjective.None();
    /// <summary>
    /// 指定した城が攻撃目標に合致しているならtrue
    /// </summary>
    public bool IsAttackTarget(Castle target) => Objective.IsAttackTarget(target) || Country.Objective.IsAttackTarget(target);

    /// <summary>
    /// 四半期の戦略アクションを行っていればtrue
    /// </summary>
    public bool QuarterActionDone { get; set; } = false;

    [JsonIgnore]
    public GameMapTile Tile => GameCore.Instance.World.Map.GetTile(this);

    public override string ToString()
    {
        return $"城({Name} 城主: {Boss?.Name ?? "無"} - {Country.Ruler.Name}軍)";
    }
}

[JsonObject(ItemTypeNameHandling = TypeNameHandling.Auto)]
public record CastleObjective
{
    public static List<CastleObjective> Candidates(Castle castle)
    {
        var list = new List<CastleObjective>
        {
            new None(),
            new Train(),
            new Fortify(),
            new Develop(),
        };
        if (castle.IsFrontline)
        {
            // 前線なら攻撃も候補に入れる。
            foreach (var neighbor in castle.Neighbors)
            {
                if (castle.IsAttackable(neighbor))
                {
                    list.Add(new Attack { TargetCastleName = neighbor.Name });
                }
            }
        }
        else
        {
            // 後方なら輸送も候補に入れる。
            // ただし物資が余っている場合のみ。
            if (castle.GoldSurplus >= 0)
            {
                foreach (var other in castle.Country.Castles.Except(new[] { castle }))
                {
                    if (other.IsFrontline || other.Boss == castle.Country.Ruler)
                    {
                        list.Add(new Transport { TargetCastleName = other.Name });
                    }
                }
            }
        }
        return list;
    }

    public override string ToString() => GetType().Name;

    public virtual bool IsAttackTarget(Castle target) => false;

    /// <summary>
    /// 方針なし
    /// </summary>
    public record None : CastleObjective
    {
    }

    /// <summary>
    /// 城攻撃
    /// </summary>
    public record Attack : CastleObjective
    {
        public string TargetCastleName { get; set; }

        public override bool IsAttackTarget(Castle target) => target.Name == TargetCastleName;
        
        public override string ToString() => $"攻撃({TargetCastleName})";
    }

    /// <summary>
    /// 訓練
    /// </summary>
    public record Train : CastleObjective
    {
        public override string ToString() => $"訓練";
    }

    /// <summary>
    /// 防備
    /// </summary>
    public record Fortify : CastleObjective
    {
        public override string ToString() => $"防備";
    }

    /// <summary>
    /// 開発
    /// </summary>
    public record Develop : CastleObjective
    {
        public override string ToString() => $"開発";
    }

    /// <summary>
    /// 輸送
    /// </summary>
    public record Transport : CastleObjective
    {
        public string TargetCastleName { get; set; }

        public override string ToString() => $"輸送({TargetCastleName})";
    }
}
