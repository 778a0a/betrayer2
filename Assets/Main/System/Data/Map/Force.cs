using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Force : ICountryEntity, IMapEntity
{
    public Force(WorldData world, Character character, MapPosition position, ForceMode mode = ForceMode.Normal)
    {
        this.world = world;
        Character = character;
        Country = character?.Country;
        Position = position;
        Destination = position;
        Direction = Direction.Right;
        Mode = mode;
    }

    private WorldData world;

    public ForceMode Mode { get; set; }

    /// <summary>
    /// 軍勢の所属国
    /// </summary>
    [JsonIgnore]
    public Country Country { get; set; }
    /// <summary>
    /// 軍勢の統率者
    /// </summary>
    [JsonIgnore]
    public Character Character { get; set; }

    /// <summary>
    /// 軍勢の位置
    /// </summary>
    public MapPosition Position { get; private set; }

    /// <summary>
    /// 軍勢の目的地
    /// </summary>
    [JsonIgnore]
    public IMapEntity Destination { get; private set; }

    [JsonIgnore]
    public LinkedList<MapPosition> DestinationPath { get; set; }

    /// <summary>
    /// 軍勢の向き
    /// </summary>
    public Direction Direction { get; private set; }

    /// <summary>
    /// 隣のタイルに移動するのにかかる残り日数
    /// </summary>
    public float TileMoveRemainingDays { get; set; }

    /// <summary>
    /// 増援モード時の残り待機日数
    /// </summary>
    public int ReinforcementWaitDays { get; set; }

    /// <summary>
    /// 到着予想日数
    /// </summary>
    [JsonIgnore]
    public float ETADays
    {
        get
        {
            var days = TileMoveRemainingDays;
            var pos = Position;
            foreach (var next in DestinationPath)
            {
                days += CalculateMoveCost(pos, next);
                pos = next;
            }
            return days;
        }
    }

    public Castle ReinforcementOriginalTarget { get; set; }

    /// <summary>
    /// タイル移動時に、戦闘せずに同じタイルに移動できるならtrue
    /// </summary>
    public bool CanThrough(Force other)
    {
        // 自国・同盟国なら通過可能
        if (!this.IsAttackable(other)) return true;
        // 同じ救援先の救援軍なら通過可能
        return Mode == ForceMode.Reinforcement &&
            other.Mode == ForceMode.Reinforcement &&
            ReinforcementOriginalTarget == other.ReinforcementOriginalTarget;
    }

    public void UpdatePosition(MapPosition pos)
    {
        var oldTile = world.Map.GetTile(Position);
        Position = pos;
        oldTile.Refresh();

        if (DestinationPath.Count > 0)
        {
            var expectedPos = DestinationPath.First();
            if (expectedPos == pos)
            {
                DestinationPath.RemoveFirst();
                if (DestinationPath.Count > 0)
                {
                    Direction = pos.DirectionTo(DestinationPath.First());
                }
            }
            else if (Destination.Position != pos)
            {
                Debug.Log($"軍勢の位置がPathと一致しません。経路を再計算します。 {this}");
                DestinationPath = FindPath(Destination);
                Direction = Position.DirectionTo(DestinationPath.First());
            }
        }

        ResetTileMoveProgress();

        // 増援モードで目的地に到着した場合
        if (Mode == ForceMode.Reinforcement && Destination.Position == Position)
        {
            // しばらくの間待機する。
            ReinforcementWaitDays = 90;
            Debug.Log($"増援モード 待機を開始します。{this}");
            //GameCore.Instance.Pause();
        }

        world.Forces.ShouldCheckDefenceStatus = true;
        RefreshUI();
    }

    public void RefreshUI()
    {
        var tile = world.Map.GetTile(Position);
        tile.Refresh();
    }

    /// <summary>
    /// 目的地を設定します。
    /// </summary>
    /// <param name="destination"></param>
    public void SetDestination(IMapEntity destination, bool updateUI = true, bool isRestoring = false, params MapPosition[] prohibiteds)
    {
        var prevDestination = Destination;
        var prevDirection = Direction;
        Destination = destination;
        if (Destination.Position != Position)
        {
            DestinationPath = FindPath(destination, prohibiteds);
            Direction = Position.DirectionTo(DestinationPath.First());
        }
        else
        {
            Debug.LogWarning($"{this} DestinationとPositionが一致しています。");
            DestinationPath = new LinkedList<MapPosition>();
        }
        // 目的地が変わった場合は移動日数をリセットする
        if (!isRestoring && (prevDestination.Position == Position || prevDirection != Direction))
        {
            ResetTileMoveProgress();
        }

        world.Forces.ShouldCheckDefenceStatus = true;
        if (updateUI)
        {
            RefreshUI();
        }
    }

    private LinkedList<MapPosition> FindPath(IMapEntity dest, params MapPosition[] prohibiteds)
    {
        var path = FindPathCore(dest.Position, prohibiteds: prohibiteds);
        
        // NPCの場合
        //if (!Character.IsPlayer && !Character.Country.Ruler.IsPlayer)
        if (dest is Castle castle)
        {
            // 敵対国の城が目的地の場合
            if (this.IsAttackable(castle))
            {
                if (path.Count >= 2)
                {
                    Debug.Assert(dest.Position == path.Last.Value);
                    var castleTile = world.Map.GetTile(dest);
                    // 城の目前のタイルの両隣が攻撃に有利な地形ならそちらに移動する。
                    var castlePrevTile = world.Map.GetTile(path.Last.Previous.Value);
                    var cands = castleTile.Neighbors.Intersect(new[] { castlePrevTile }.Concat(castlePrevTile.Neighbors));
                    // 水軍または海賊なら、城に隣接する川・大河マスも候補にする
                    if (Character.Traits.HasFlag(Traits.Admiral) || Character.Traits.HasFlag(Traits.Pirate))
                    {
                        cands = cands.Concat(castleTile.Neighbors.Where(tile => tile.Terrain == Terrain.River || tile.Terrain == Terrain.LargeRiver));
                    }
                    // 普通のキャラの場合、候補が全て川・大河なら、他の陸地タイルも含める。
                    else if (cands.All(tile => tile.Terrain == Terrain.River || tile.Terrain == Terrain.LargeRiver))
                    {
                        cands = castleTile.Neighbors;
                    }
                    var target = cands
                        // 戦闘補正が+なものを優先する。
                        .OrderByDescending(tile =>
                            Battle.TraitsAdjustment(tile, Character.Traits) +
                            Battle.TerrainAdjustment(tile.Terrain))
                        // 疑似距離が近いものを優先する。
                        .ThenBy(tile => tile.DistanceTo(Position))
                        // 元の選択を優先する。
                        .ThenByDescending(tile => tile.Position == castlePrevTile.Position ? 0.001f : 0)
                        .First();
                    if (target != castlePrevTile)
                    {
                        var path2 = FindPathCore(target.Position, prohibiteds: prohibiteds);
                        if (path2 != null)
                        {
                            path = path2;
                            path.AddLast(dest.Position);
                        }
                    }
                }
            }
        }
        if (path == null)
        {
            Debug.Log($"経路が見つかりませんでした。 {this} -> {dest}");
            path = new LinkedList<MapPosition>();
        }
        return path;
    }

    private LinkedList<MapPosition> FindPathCore(MapPosition dest, bool recursive = false, bool useRiver = false, params MapPosition[] prohibiteds)
    {
        if (Position == dest) return new LinkedList<MapPosition>();
        // 水軍か海賊の場合、川・大河を使う経路を優先する。
        if (!recursive && (Character.Traits.HasFlag(Traits.Admiral) || Character.Traits.HasFlag(Traits.Pirate)))
        {
            var res = FindPathCore(dest, true, true, prohibiteds);
            // 経路が見つかって、極端に長くなければ採用する。
            if (res != null && res.Count < 7)
            {
                return res;
            }
        }

        var start = world.Map.GetTile(Position);
        var open = new List<GameMapTile> { start };
        var close = new HashSet<GameMapTile>();
        var cameFrom = new Dictionary<GameMapTile, GameMapTile>();
        var g = new Dictionary<GameMapTile, float> { { start, 0 } };
        var f = new Dictionary<GameMapTile, float> { { start, H(start, dest) } };

        while (open.Count > 0)
        {
            var current = open.OrderBy(p => f.GetValueOrDefault(p, float.MaxValue)).First();
            if (current.Position == dest)
            {
                return ReconstructPath(cameFrom, current);
            }

            open.Remove(current);
            close.Add(current);

            var cands = current.Neighbors
                // 目的地以外の城は移動禁止にする。
                .Where(tile => tile.Castle == null || tile.Position == dest) 
                .Where(tile => !prohibiteds.Contains(tile.Position));
            if (useRiver)
            {
                cands = cands.Where(tile => Util.IsMarine(tile.Terrain) || tile.Position == dest);
            }
            foreach (var neighborTile in cands)
            {
                var neighbor = neighborTile;
                if (close.Contains(neighbor))
                {
                    continue;
                }

                var gscore = g[current] + CalculateMoveCost(current, neighbor);
                if (!open.Contains(neighbor))
                {
                    open.Add(neighbor);
                }
                else if (gscore >= g.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                g[neighbor] = gscore;
                f[neighbor] = gscore + H(neighbor, dest);
            }
        }
        return null;

        static float H(GameMapTile pos, MapPosition dest)
        {
            return pos.DistanceTo(dest) * 30 * 2; // 1マスの最大移動コスト
        }

        static LinkedList<MapPosition> ReconstructPath(Dictionary<GameMapTile, GameMapTile> cameFrom, GameMapTile current)
        {
            var path = new LinkedList<MapPosition>();
            path.AddFirst(current.Position);
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.AddFirst(current.Position);
            }
            path.RemoveFirst();
            return path;
        }
    }

    public void ResetTileMoveProgress()
    {
        if (Destination.Position == Position)
        {
            TileMoveRemainingDays = 0;
            return;
        }
        var tile = world.Map.GetTile(Position);
        var nextPos = Position.To(Direction);
        var nextTile = world.Map.TryGetTile(nextPos);
        if (nextTile == null)
        {
            // 軍勢がマップの端の手前で野戦に負けた場合に起きることがある。
            // すぐ後にSetDestinationを呼んで更新する場合もあるのでエラーにはしない。
            Debug.LogWarning($"軍勢の方向が不正です。{this}: {nextPos}");
            TileMoveRemainingDays = 1;
            return;
        }
        TileMoveRemainingDays = CalculateMoveCost(tile, nextTile);
    }

    public float CalculateMoveCost(MapPosition nextPos) => CalculateMoveCost(Position, nextPos);
    public float CalculateMoveCost(MapPosition fromPos, MapPosition nextPos)
    {
        var t1 = world.Map.TryGetTile(fromPos);
        var t2 = world.Map.TryGetTile(nextPos);
        if (t1 == null || t2 == null)
        {
            Debug.Break();
            throw new InvalidOperationException($"Invalid Position (from: {fromPos}, next: {nextPos})");
        }
        return CalculateMoveCost(t1, t2);
    }
    public float CalculateMoveCost(GameMapTile current, GameMapTile next)
    {
        // キャラの攻撃能力に応じて移動コストを補正する。
        var martialAdj = Character.Attack;

        // 援軍の場合は、攻撃と防衛の高い方を採用する。
        if (Mode == ForceMode.Reinforcement)
        {
            martialAdj = Mathf.Max(Character.Attack, Character.Defense);
        }
        // 自国領の場合は防衛能力で補正する。
        else if (Country.Has(current) || Country.Has(next))
        {
            martialAdj = Character.Defense;
        }
        // 能力が70の場合は補正1.0、能力が80の場合は0.9、能力が60の場合は1.1
        var martialAdjRate = 1.0f - (martialAdj - 70) * 0.01f;

        var currentCost = tileMoveCost[current.Terrain];
        currentCost *= TerrainTraitMoveAdjustment(current.Terrain, Character.Traits);
        var nextCost = tileMoveCost[next.Terrain];
        nextCost *= TerrainTraitMoveAdjustment(next.Terrain, Character.Traits);
        return (currentCost + nextCost) * martialAdjRate;
    }

    public override string ToString()
    {
        return $"軍勢({Character.Name} at {Position} -> {Destination} ({TileMoveRemainingDays}))";
    }

    public void AttachWorld(WorldData world)
    {
        this.world = world;
    }

    // タイルの移動にかかる日数
    // (現在のタイルのコスト + 移動先のタイルのコスト) が実際にかかる日数
    private static readonly Dictionary<Terrain, float> tileMoveCost = new()
    {
        { Terrain.LargeRiver, 20 },
        { Terrain.River,      15 },
        { Terrain.Plain,      5 },
        { Terrain.Hill,       8 },
        { Terrain.Forest,     10 },
        { Terrain.Mountain,   15 },
    };
    private static float TerrainTraitMoveAdjustment(Terrain terrain, Traits traits)
    {
        var adj = 1f;
        if (traits.HasFlag(Traits.DivineSpeed)) adj *= 0.5f;
        switch (terrain)
        {
            case Terrain.LargeRiver:
                if (traits.HasFlag(Traits.Pirate)) adj *= 0.25f;
                if (traits.HasFlag(Traits.Admiral)) adj *= 0.5f;
                break;
            case Terrain.River:
                if (traits.HasFlag(Traits.Pirate)) adj *= 0.33f;
                if (traits.HasFlag(Traits.Admiral)) adj *= 0.5f;
                break;
            case Terrain.Plain:
                if (traits.HasFlag(Traits.Pirate)) adj *= 2;
                if (traits.HasFlag(Traits.Admiral)) adj *= 1.6f;
                break;
            case Terrain.Hill:
                if (traits.HasFlag(Traits.Pirate)) adj *= 2;
                if (traits.HasFlag(Traits.Admiral)) adj *= 1.25f;
                if (traits.HasFlag(Traits.Mountaineer)) adj *= 0.6f;
                if (traits.HasFlag(Traits.Hunter)) adj *= 0.8f;
                break;
            case Terrain.Forest:
                if (traits.HasFlag(Traits.Pirate)) adj *= 2;
                if (traits.HasFlag(Traits.Admiral)) adj *= 1.25f;
                if (traits.HasFlag(Traits.Mountaineer)) adj *= 0.8f;
                if (traits.HasFlag(Traits.Hunter)) adj *= 0.499f;
                break;
            case Terrain.Mountain:
                if (traits.HasFlag(Traits.Pirate)) adj *= 2;
                if (traits.HasFlag(Traits.Admiral)) adj *= 1.25f;
                if (traits.HasFlag(Traits.Mountaineer)) adj *= 0.33f;
                if (traits.HasFlag(Traits.Hunter)) adj *= 0.8f;
                break;
        }
        return adj;
    }

    private struct TerrainDevAdjustmentData
    {
        public float BaseFood;
        public float NeighborFood;
        public float BaseGold;
        public float NeighborGold;
        public TerrainDevAdjustmentData(float baseFood, float neighborFood, float baseGold, float neighborGold)
        {
            BaseFood = baseFood;
            NeighborFood = neighborFood;
            BaseGold = baseGold;
            NeighborGold = neighborGold;
        }
    }
}

public enum ForceMode
{
    /// <summary>
    /// 通常
    /// </summary>
    Normal,
    /// <summary>
    /// 援軍
    /// </summary>
    Reinforcement,
}