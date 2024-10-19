using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ForceManager : IReadOnlyList<Force>
{
    private WorldData world => GameCore.Instance.World;
    private readonly List<Force> forces = new();

    public ForceManager(IEnumerable<Force> initialForces)
    {
        forces.AddRange(initialForces);
    }

    public void Register(Force force)
    {
        forces.Add(force);

        force.RefreshUI();
    }

    public void Unregister(Force force)
    {
        var oldTile = world.Map.GetTile(force.Position);

        forces.Remove(force);
        // 削除対象を目的地にしている軍勢がいる場合は目的地をリセットする。
        foreach (var f in forces.Where(f => f.Destination == force))
        {
            f.SetDestination(f.Position);
            Debug.Log($"{f} 目的の軍勢が消えたため目的地をリセットしました。");
        }

        oldTile.Refresh();
    }

    /// <summary>
    /// 軍勢の移動処理を行う。
    /// </summary>
    public void OnForceMove(GameCore core)
    {
        var forcesLength = forces.Count;
        var forcesCopy = ArrayPool<Force>.Shared.Rent(forcesLength);
        forces.CopyTo(forcesCopy);
        for (var i = 0; i < forcesLength; i++)
        {
            var force = forcesCopy[i];
            if (!forces.Contains(force))
            {
                Debug.LogWarning($"軍勢更新処理 対象の軍勢が存在しません。{force}");
                continue;
            }
            OnForceMoveOne(core, force);
        }
    }

    private void OnForceMoveOne(GameCore core, Force force)
    {
        // 移動の必要がないなら何もしない。
        if (force.Destination.Position == force.Position)
        {
            Debug.Log($"軍勢更新処理 {force} 待機中...");
            return;
        }

        // タイル移動進捗を進める。
        force.TileMoveRemainingDays--;
        // 移動進捗が残っている場合は何もしない。
        if (force.TileMoveRemainingDays > 0)
        {
            Debug.Log($"軍勢更新処理 {force} 移動中...");
            return;
        }
        Debug.Log($"軍勢更新処理 {force} タイル移動処理開始");
        var world = core.World;

        // 移動値が溜まったら隣のタイルに移動する。
        var nextPos = force.Position.To(force.Direction);
        var nextTile = world.Map.GetTile(nextPos);

        // 移動先に自国以外の軍勢がいる場合は野戦を行う。
        var nextEnemies = nextTile.Forces.Where(f => f.IsEnemy(force)).ToArray();
        if (nextEnemies.Length > 0)
        {
            OnFieldBattle(world, force, nextTile, nextEnemies);
            return;
        }

        // 移動先が自国以外の城の場合は攻城戦を行う。
        if (nextTile.Castle != null && force.IsEnemy(nextTile))
        {
            OnSiege(world, force, nextTile);
            return;
        }

        // 移動先が目的地で自国の城の場合は城に入る。
        if (nextTile.Castle == force.Destination && force.IsSelf(nextTile))
        {
            var oldCastle = world.CastleOf(force.Character);
            oldCastle.Members.Remove(force.Character);
            var castle = nextTile.Castle;
            castle.Members.Add(force.Character);

            Unregister(force);
            Debug.Log($"軍勢更新処理 目的地の城に入城しました。");
            return;
        }

        // 上記以外の場合はタイルを移動する。
        force.UpdatePosition(nextPos);

        Debug.Log($"軍勢更新処理 隣のタイルに移動しました。{nextPos}");
    }

    /// <summary>
    /// 野戦処理
    /// </summary>
    private void OnFieldBattle(WorldData world, Force force, GameMapTile nextTile, Force[] enemies)
    {
        var enemy = enemies.RandomPick();
        var win = 0.5.Chance(); // TODO Battle

        // 負けた場合は本拠地へ撤退を始める。
        if (!win)
        {
            var home = world.CastleOf(force.Character);
            force.SetDestination(home);
            Debug.Log($"軍勢更新処理 野戦に敗北しました。撤退します。({force.TileMoveRemainingDays})");
            return;
        }

        // 勝った場合

        // 敵を1タイル後退させる。
        var enemyPos = enemy.Position;
        var backPos = enemyPos.To(force.Direction);
        var backTile = world.Map.GetTile(backPos);
        // 後退先に移動できないなら、軍勢を削除して行動不能にする。
        if (backTile == null ||
            backTile.Forces.Any(f => f.IsEnemy(enemy)) ||
            (backTile.Castle?.IsEnemy(enemy) ?? false))
        {
            enemy.Character.SetIncapacitated();
            Unregister(enemy);
            Debug.Log($"{enemy} 後退不可な場所で野戦に敗北したため行動不能になりました。");
        }
        // 後退先に移動できるなら、敵を後退させる。
        else
        {
            var enemyHome = world.CastleOf(enemy.Character);
            enemy.UpdatePosition(backPos);
            // 本拠地へ撤退させる。
            enemy.SetDestination(enemyHome);
            Debug.Log($"{enemy} 野戦に敗北したため後退しました。");
        }

        // まだ他に敵がいる場合は移動進捗を少しリセットする。
        if (enemies.Length > 1)
        {
            force.TileMoveRemainingDays = Math.Max(1, force.CalculateMoveCost(nextTile.Position) / 4);
            Debug.Log($"軍勢更新処理 野戦に勝利しました。({force.TileMoveRemainingDays})");
            return;
        }

        // 移動先が敵城の場合
        if (nextTile.Castle != null && force.IsEnemy(nextTile.Castle))
        {
            // 防衛可能な敵が残っている場合は、移動進捗を半分リセットする。
            if (nextTile.Castle.Members.Any(e => e.CanDefend))
            {
                force.ResetTileMoveProgress();
                force.TileMoveRemainingDays /= 2;
                Debug.Log($"軍勢更新処理 野戦(城)に勝利しました。({force.TileMoveRemainingDays})");
                return;
            }
            // 防衛可能な敵が残っていない場合は城を占領する。
            OnCastleFall(world, force, nextTile.Castle);
            return;
        }
        // 移動先が自分の城の場合は入城する（ありえる？）
        if (nextTile.Castle != null && force.IsSelf(nextTile.Castle) && force.Destination.Position == nextTile.Position)
        {
            var oldCastle = world.CastleOf(force.Character);
            oldCastle.Members.Remove(force.Character);
            nextTile.Castle.Members.Add(force.Character);
            Unregister(force);
            Debug.Log($"軍勢更新処理 野戦(城)に勝利しました。目的地の城に入城しました。");
            return;
        }

        // 敵がいなくなった場合はタイルを移動する。
        force.UpdatePosition(nextTile.Position);
        Debug.Log($"軍勢更新処理 野戦に勝利しました。隣のタイルに移動しました。({force.TileMoveRemainingDays})");
    }

    /// <summary>
    /// 攻城戦処理
    /// </summary>
    private void OnSiege(WorldData world, Force force, GameMapTile nextTile)
    {
        var castle = nextTile.Castle;
        var enemy = castle.Members.Where(e => e.CanDefend).RandomPickDefault();
        var win = enemy == null || 0.5.Chance(); // TODO Battle

        // 負けた場合は本拠地へ撤退を始める。
        if (!win)
        {
            var home = world.CastleOf(force.Character);
            force.SetDestination(home);
            Debug.Log($"軍勢更新処理 攻城戦に敗北しました。撤退します。({force.TileMoveRemainingDays})");
            return;
        }

        // 勝った場合

        // 敵を行動不能状態にする。
        enemy?.SetIncapacitated();

        // 防衛可能な敵が残っている場合は、移動進捗を半分リセットする。
        if (castle.Members.Any(e => e.CanDefend))
        {
            force.ResetTileMoveProgress();
            force.TileMoveRemainingDays /= 2;
            Debug.Log($"軍勢更新処理 攻城戦に勝利しました。({force.TileMoveRemainingDays})");
            return;
        }

        // 防衛可能な敵が残っていない場合は城を占領する。
        OnCastleFall(world, force, castle);
    }

    private void OnCastleFall(WorldData world, Force force, Castle castle)
    {
        // 駐在キャラの行動不能日数を再セットする。
        foreach (var e in castle.Members.Where(e => !e.IsMoving))
        {
            e.SetIncapacitated();
        }

        // 城の所有国を変更する。
        var oldCountry = castle.Country;
        oldCountry.Castles.Remove(castle);
        // 全ての城を失った場合は国を消滅させる。
        if (oldCountry.Castles.Count == 0)
        {
            world.Countries.Remove(oldCountry);
            var forcesToRemove = forces.Where(f => f.Country == oldCountry).ToArray();
            foreach (var f in forcesToRemove)
            {
                Unregister(f);
            }

            castle.Members.Clear();
            // TODO 他に必要な処理が色々ありそう。
        }
        // まだ他の城がある場合は、一番近くの城に所属を移動する。
        else
        {
            var nearEnemyCastle = oldCountry.Castles
                .OrderBy(c => c.Position.DirectionTo(force.Position))
                .FirstOrDefault();
            foreach (var e in castle.Members)
            {
                nearEnemyCastle.Members.Add(e);
            }
            castle.Members.Clear();
        }
        
        // 城を攻撃者の国に追加する。
        world.Map.UpdateCastleCountry(force.Country, castle);

        // 城の隣接タイルにいて、城が目的地で、進捗が半分以上のキャラは城に入る。
        var castleTile = world.Map.GetTile(castle);
        var forcesToEnterCastle = castleTile.Neighbors
            .SelectMany(n => n.Forces)
            .Where(f => f.Country == castle.Country)
            .Where(f => f.Destination.Position == castle.Position)
            .Where(f => f.TileMoveRemainingDays < f.CalculateMoveCost(castleTile.Position) / 0.5f)
            .ToArray();
        foreach (var f in forcesToEnterCastle)
        {
            var oldCastle = world.CastleOf(f.Character);
            oldCastle.Members.Remove(f.Character);
            castle.Members.Add(f.Character);
            Unregister(f);
            Debug.Log($"{f} 城に入城しました。");
        }
        castleTile.Refresh();
    }


    public Force this[int index] => forces[index];
    public int Count => forces.Count;
    IEnumerator<Force> IEnumerable<Force>.GetEnumerator() => forces.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => forces.GetEnumerator();
}
