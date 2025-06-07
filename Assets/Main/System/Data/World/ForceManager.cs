using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class ForceManager : IReadOnlyList<Force>
{
    private WorldData world => GameCore.Instance.World;
    private readonly List<Force> forces = new();

    public bool ShouldCheckDefenceStatus { get; set; } = true;

    public ForceManager(IEnumerable<Force> initialForces)
    {
        foreach (var force in initialForces)
        {
            forces.Add(force);
            force.Character.Force = force;
        }
    }

    public void Register(Force force)
    {
        forces.Add(force);
        force.Character.Force = force;

        ShouldCheckDefenceStatus = true;
        force.RefreshUI();
    }

    public void Unregister(Force force)
    {
        var oldTile = world.Map.GetTile(force.Position);

        force.Character.Force = null;
        forces.Remove(force);

        // 削除対象を目的地にしている軍勢がいる場合は目的地をリセットする。
        foreach (var f in forces.Where(f => f.Destination == force))
        {
            f.SetDestination(f.Position);
            Debug.Log($"{f} 目的の軍勢が消えたため目的地をリセットしました。");
        }

        ShouldCheckDefenceStatus = true;
        oldTile.Refresh();
    }

    private GameDate prevCheck = new(0);
    /// <summary>
    /// 各城の防衛状況を確認します。
    /// </summary>
    public void UpdateDangerStatus(GameCore core)
    {
        if (!ShouldCheckDefenceStatus) return;
        ShouldCheckDefenceStatus = false;
        var prev = prevCheck;
        prevCheck = core.GameDate;
        //Debug.Log("城の防衛状況を確認します。" + (core.GameDate - prev));
        foreach (var castle in world.Castles)
        {
            var dangers = castle.DangerForces(this).ToArray();
            castle.DangerForcesExists = dangers.Length > 0;
            world.Map.GetTile(castle).UI.ShowDebugText(castle.DangerForcesExists ? "!" : "");
            if (!castle.DangerForcesExists) continue;
        }
    }

    /// <summary>
    /// 軍勢の移動処理を行う。
    /// </summary>
    public async ValueTask OnForceMove(GameCore core)
    {
        var forcesLength = forces.Count;
        var forcesCopy = ArrayPool<Force>.Shared.Rent(forcesLength);
        forces.CopyTo(forcesCopy);
        for (var i = 0; i < forcesLength; i++)
        {
            var force = forcesCopy[i];
            if (!forces.Contains(force))
            {
                // 戦闘などで軍勢が削除されている場合は何もしない。
                //Debug.LogWarning($"軍勢更新処理 対象の軍勢が存在しません。{force}");
                continue;
            }
            await OnForceMoveOne(core, force);
        }
    }

    private async ValueTask OnForceMoveOne(GameCore core, Force force)
    {
        // 移動の必要がないなら何もしない。
        if (force.Destination.Position == force.Position)
        {
            Debug.Log($"軍勢更新処理 {force} 待機中...");
            // 増援モードの待機日数を減らす。
            if (force.Mode == ForceMode.Reinforcement && force.ReinforcementWaitDays > 0)
            {
                // 対象の城が危険でなくなっていれば待機時間を減らす。
                var castle = (Castle)force.Destination;
                if (!castle.DangerForcesExists && force.ReinforcementWaitDays > 5)
                {
                    force.ReinforcementWaitDays = 5;
                    Debug.Log($"軍勢更新処理 城が危険でなくなったため待機時間を短縮しました。 {force}");
                }

                force.ReinforcementWaitDays--;
                if (force.ReinforcementWaitDays <= 0)
                {
                    // 本拠地への帰還は個人フェイズで行う。
                    Debug.Log($"軍勢更新処理 {force} 待機終了");
                }
            }
            // なぜかずっと待機になることがあったので対応する。 
            if (force.Mode == ForceMode.Normal && force.Character.Castle.Position == force.Position)
            {
                Debug.LogError($"待機エラーのため帰還します。{force}");
                Unregister(force);
            }
            return;
        }

        // タイル移動進捗を進める。
        force.TileMoveRemainingDays--;
        // 移動進捗が残っている場合は何もしない。
        if (force.TileMoveRemainingDays > 0)
        {
            //Debug.Log($"軍勢更新処理 {force} 移動中...");
            return;
        }
        //Debug.Log($"軍勢更新処理 {force} タイル移動処理開始");
        var world = core.World;

        // 移動値が溜まったら隣のタイルに移動する。
        var nextPos = force.Position.To(force.Direction);
        var nextTile = world.Map.GetTile(nextPos);

        // 移動先に通り抜け不可な軍勢がいる場合は野戦を行う。
        var nextEnemies = nextTile.Forces.Where(f => !f.CanThrough(force)).ToArray();
        if (nextEnemies.Length > 0)
        {
            // 移動先が目的地でなく、移動先に友好的な軍勢しかいない場合は迂回するように進路を変更する。
            if (force.Destination.Position != nextPos &&
                nextEnemies.All(e => e.Country.GetRelation(force.Country) >= 60))
            {
                force.ResetTileMoveProgress();
                Debug.Log($"軍勢更新処理 移動先に友好勢力がいるため迂回します。{force}");
                force.SetDestination(force.Destination, prohibiteds: nextTile.Position);
                return;
            }

            await OnFieldBattle(world, force, nextTile, nextEnemies);
            return;
        }

        // 移動先が自国以外の城の場合は攻城戦を行う。
        if (nextTile.Castle != null && force.IsAttackable(nextTile))
        {
            await OnSiege(world, force, nextTile);
            return;
        }

        // 移動先が目的地で自国の城の場合は城に入る。
        if (nextTile.Castle == force.Destination && force.IsSelf(nextTile) &&
            // 援軍の場合は入城しない。ただし自分の本拠地への帰還なら入場する。
            (force.Mode != ForceMode.Reinforcement || nextTile.Castle.Members.Contains(force.Character)))
        {
            force.Character.ChangeCastle(nextTile.Castle, false);

            Unregister(force);
            //Debug.Log($"軍勢更新処理 目的地の城に入城しました。{force}");
            return;
        }

        // 上記以外の場合はタイルを移動する。
        force.UpdatePosition(nextPos);

        //Debug.Log($"軍勢更新処理 隣のタイルに移動しました。{nextPos}");
    }

    /// <summary>
    /// 野戦処理
    /// </summary>
    private async ValueTask OnFieldBattle(WorldData world, Force force, GameMapTile nextTile, Force[] enemies)
    {
        var enemy = enemies.RandomPick();
        var battle = BattleManager.PrepareFieldBattle(force, enemy);
        var result = await battle.Do();
        var win = result == BattleResult.AttackerWin;
        force.Country.SetEnemy(enemy.Country);

        // 負けた場合は本拠地へ撤退を始める。
        if (!win)
        {
            // 全滅した場合
            if (force.Character.Soldiers.IsAllDead)
            {
                force.Character.SetIncapacitated();
                Unregister(force);
                //Debug.Log($"軍勢更新処理 野戦に敗北し、全滅しました。");
                return;
            }

            var home = force.Character.Castle;
            force.ResetTileMoveProgress();
            force.SetDestination(home);
            //Debug.Log($"軍勢更新処理 野戦に敗北しました。撤退します。({force.TileMoveRemainingDays})");
            return;
        }

        // 勝った場合

        // 敵が敵城タイルにいる場合は、敵軍勢を削除する。
        if (nextTile.Castle != null && nextTile.Castle == enemy.Character.Castle)
        {
            // やっぱり行動不能にはしない。
            //enemy.Character.SetIncapacitated();
            Unregister(enemy);
            //Debug.Log($"{enemy} 野戦(城)に敗北したため城に退却します。");
        }
        // 全滅した場合
        else if (enemy.Character.Soldiers.IsAllDead)
        {
            enemy.Character.SetIncapacitated();
            Unregister(enemy);
            //Debug.Log($"{enemy} 野戦に敗北し、全滅しました。");
        }
        else
        {
            // 敵を1タイル後退させる。
            var enemyPos = enemy.Position;
            var backPos = enemyPos.To(force.Direction);
            var backTile = world.Map.TryGetTile(backPos);
            // 後退先に移動できないなら、軍勢を削除して行動不能にする。
            if (backTile == null ||
                backTile.Forces.Any(f => !f.CanThrough(enemy)) ||
                (backTile.Castle?.IsAttackable(enemy) ?? false))
            {
                enemy.Character.SetIncapacitated();
                Unregister(enemy);
                Debug.Log($"{enemy} 後退不可な場所で野戦に敗北したため行動不能になりました。");
            }
            // 後退先に移動できるなら、敵を後退させる。
            else
            {
                var enemyHome = enemy.Character.Castle;
                enemy.UpdatePosition(backPos);
                // 本拠地へ撤退させる。
                enemy.SetDestination(enemyHome);
                //Debug.Log($"{enemy} 野戦に敗北したため後退しました。");
            }
        }

        // まだ他に敵がいる場合は移動進捗を少しリセットする。
        if (enemies.Length > 1)
        {
            force.ResetTileMoveProgress();
            force.TileMoveRemainingDays /= 4;
            //Debug.Log($"軍勢更新処理 野戦に勝利しました。({force.TileMoveRemainingDays})");
            return;
        }

        // 移動先が敵城の場合
        if (nextTile.Castle != null && force.IsAttackable(nextTile.Castle))
        {
            // 防衛可能な敵が残っている場合は、移動進捗を半分リセットする。
            if (nextTile.Castle.Members.Any(e => e.IsDefendable))
            {
                force.ResetTileMoveProgress();
                force.TileMoveRemainingDays /= 2;
                //Debug.Log($"軍勢更新処理 野戦(城)に勝利しました。({force.TileMoveRemainingDays})");
                return;
            }
            // 防衛可能な敵が残っていない場合は城を占領する。
            OnCastleFall(world, force, nextTile.Castle);
            return;
        }
        // 移動先が自分の城の場合は入城する（ありえる？）
        if (nextTile.Castle != null && force.IsSelf(nextTile.Castle) && force.Destination.Position == nextTile.Position)
        {
            var oldCastle = force.Character.Castle;
            force.Character.ChangeCastle(nextTile.Castle, false);
            Unregister(force);
            Debug.Log($"軍勢更新処理 野戦(城)に勝利しました。目的地の城に入城しました。");
            return;
        }

        // 敵がいなくなった場合はタイルを移動する。
        force.UpdatePosition(nextTile.Position);
        //Debug.Log($"軍勢更新処理 野戦に勝利しました。隣のタイルに移動しました。({force.TileMoveRemainingDays})");
    }

    /// <summary>
    /// 攻城戦処理
    /// </summary>
    private async ValueTask OnSiege(WorldData world, Force force, GameMapTile nextTile)
    {
        var castle = nextTile.Castle;
        var enemy = castle.Members.Where(e => e.IsDefendable).RandomPickDefault();
        force.Country.SetEnemy(castle.Country);
        var win = true;
        if (enemy != null)
        {
            var battle = BattleManager.PrepareSiegeBattle(force, enemy);
            var result = await battle.Do();
            win = result == BattleResult.AttackerWin;
        }
        else
        {
            Debug.Log($"軍勢更新処理 防衛可能な敵がいません。{castle}");
            force.Character.Prestige += 1;
        }

        // 負けた場合は本拠地へ撤退を始める。
        if (!win)
        {
            // 全滅した場合
            if (force.Character.Soldiers.IsAllDead)
            {
                force.Character.SetIncapacitated();
                Unregister(force);
                //Debug.Log($"軍勢更新処理 攻城戦に敗北し、全滅しました。");
                return;
            }
            var home = force.Character.Castle;
            force.SetDestination(home);
            //Debug.Log($"軍勢更新処理 攻城戦に敗北しました。撤退します。({force.TileMoveRemainingDays})");
            return;
        }

        // 勝った場合

        // 敵を行動不能状態にする。
        enemy?.SetIncapacitated();

        // 防衛可能な敵が残っている場合は、移動進捗を半分リセットする。
        if (castle.Members.Any(e => e.IsDefendable))
        {
            force.ResetTileMoveProgress();
            force.TileMoveRemainingDays /= 2;
            //Debug.Log($"軍勢更新処理 攻城戦に勝利しました。({force}, {force.TileMoveRemainingDays})");
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
        castle.UpdateCountry(force.Country);

        var nearEnemyCastle = oldCountry.Castles
            .OrderBy(c => c.Position.DistanceTo(castle.Position))
            .FirstOrDefault();
        var enemyCastleIntelligenceMax = castle.Members
            .Where(m => !m.IsMoving)
            .Select(m => m.Intelligence)
            .DefaultIfEmpty(0)
            .Max();

        // 全ての城を失った場合は国を消滅させる。
        if (oldCountry.Castles.Count == 0)
        {
            Debug.LogWarning($"滅亡処理 {oldCountry}");
            world.Countries.Remove(oldCountry);
            var forcesToRemove = forces.Where(f => f.Country == oldCountry).ToArray();
            foreach (var f in forcesToRemove)
            {
                Unregister(f);
            }

            foreach (var m in castle.Members.ToList())
            {
                // TODO ランダムに散らす。
                m.ChangeCastle(castle, true);
                m.IsImportant = false;
                m.OrderIndex = -1;
            }
            // TODO 他に必要な処理が色々ありそう。
        }
        // まだ他の城がある場合は、一番近くの城に所属を移動する。
        else
        {
            foreach (var e in castle.Members.ToList())
            {
                // キャラが軍勢を率いているなら、軍勢から一番近い城に所属を移動する。
                if (e.Force != null)
                {
                    var c = oldCountry.Castles
                        .OrderBy(c => c.Position.DistanceTo(e.Force.Position))
                        .FirstOrDefault();
                    e.ChangeCastle(c, false);
                }
                else e.ChangeCastle(nearEnemyCastle, false);
            }
        }

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
            f.Character.ChangeCastle(castle, false);
            Unregister(f);
            //Debug.Log($"{f} 城に入城しました。");
        }

        // 城を目的地にしている他の国の軍勢について
        foreach (var group in forces.Where(f => f.Destination.Position == castle.Position).GroupBy(f => f.Country))
        {
            var otherCountry = group.Key;
            if (otherCountry == force.Country) continue;
            var rel = otherCountry.GetRelation(force.Country);
            var goHome =
                // 同盟国の場合は諦めて帰る。
                rel == Country.AllyRelation ||
                // 友好国の場合も諦めて帰る。
                rel >= 60 ||
                // 敵対していない場合は友好度に応じて帰るかどうかを決める。
                (rel > 0 && Mathf.Lerp(0, 1, (60 - rel) / 60).Chance());
            if (goHome)
            {
                foreach (var otherForce in group)
                {
                    var home = otherForce.Character.Castle;
                    otherForce.SetDestination(home);
                    Debug.Log($"{otherForce} 城が他国に占領されたため帰還します。");
                    if (otherForce.Position == home.Position)
                    {
                        Unregister(otherForce);
                    }
                }
            }
            else
            {
                foreach (var otherForce in group)
                {
                    // ただし救援の場合は攻撃できないので帰還する。
                    if (otherForce.Mode == ForceMode.Reinforcement)
                    {
                        var home = otherForce.Character.Castle;
                        otherForce.SetDestination(home);
                        Debug.Log($"{otherForce} 城が他国に占領されたため帰還します。(救援)");
                        if (otherForce.Position == home.Position)
                        {
                            Unregister(otherForce);
                        }
                        //GameCore.Instance.Pause();
                    }
                    else
                    {
                        Debug.Log($"{otherForce} 城が他国に占領されましたが攻撃を続行します。");
                    }
                }
            }
        }

        // 内政値を下げる。
        var damageAdj = castle.Strength / 100f;
        var damage = 1 - Random.Range(0.1f, 0.3f) * damageAdj;
        castle.Strength *= Random.Range(0.90f, 0.99f);
        castleTile.Town.GoldIncome *= damage;
        castleTile.Town.TotalInvestment *= damage * 0.1f;

        // 残っている物資について。
        var withdrawRatio = nearEnemyCastle != null ? enemyCastleIntelligenceMax / 100 : 0;
        if (nearEnemyCastle != null)
        {
            // 守将の智謀に応じて撤退先へ退避する。
            if (castle.Gold > 0) nearEnemyCastle.Gold += castle.Gold * withdrawRatio;
        }
        castle.Gold = (castle.Gold * (1 - withdrawRatio) * Random.Range(0.5f, 0.95f)).MinWith(0);

        castleTile.Refresh();
    }

    public float ETADays(Character chara, MapPosition start, IMapEntity dest, ForceMode mode)
    {
        var force = new Force(world, chara, start, mode);
        force.SetDestination(dest);
        return force.ETADays;
    }

    public Force this[int index] => forces[index];
    public int Count => forces.Count;
    IEnumerator<Force> IEnumerable<Force>.GetEnumerator() => forces.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => forces.GetEnumerator();
}
