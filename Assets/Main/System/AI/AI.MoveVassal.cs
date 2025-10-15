using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public partial class AI
{
    /// <summary>
    /// 人員の移動
    /// </summary>
    public async ValueTask MoveVassal(Character ruler)
    {
        static float GetMinRel(Castle c)
        {
            return c.Neighbors
                .Where(n => n.Country != c.Country)
                .Select(n => n.Country.GetRelation(c.Country))
                .DefaultIfEmpty(200)
                .Min();
        }

        var castleAndMinRel = ruler.Country.Castles
            .Select(c => (castle: c, minrel: GetMinRel(c)))
            .ToList();

        // まず全部の城を安全（minrel>70?）・危険(minrel<30?)に分ける。
        var safeCastles = castleAndMinRel
            .Where(cr => cr.minrel >= 70)
            .OrderByDescending(cr => cr.minrel)
            .ToList();
        var dangerCastles = castleAndMinRel
            .Where(cr => cr.minrel <= 30)
            .OrderBy(cr => cr.minrel)
            .ToList();
        var powerAverage = ruler.Country.Members.Average(m => m.Power);

        // 安全城の移動候補キャラをリストアップする。
        // 各城について、最低1人は残るように、Powerが高めのものを候補とする。
        var moveCandsFromSafe = new List<Character>();
        foreach (var (castle, _) in safeCastles)
        {
            var defendables = castle.Members
                .Where(m => m.IsDefendable)
                .OrderByDescending(m => m.Power)
                .ToList();
            // 最低1人は残す。
            for (int i = 0; i < defendables.Count - 1; i++)
            {
                if (defendables[i].Power < powerAverage) break;
                moveCandsFromSafe.Add(defendables[i]);
            }
        }
        moveCandsFromSafe = moveCandsFromSafe
            .OrderByDescending(m => m.Power)
            .ToList();

        // 危険城の移動候補キャラをリストアップする。
        // Powerが低いキャラを選ぶ。
        var moveCandsFromDanger = new List<Character>();
        foreach (var (castle, _) in dangerCastles)
        {
            if (castle.Members.Count(m => m.IsDefendable) <= 3) continue;
            // 大規模に移動させると危険なので1城1名まで。
            var mostWeak = castle.Members
                .Where(m => m.IsDefendable)
                .OrderBy(m => m.Power)
                .FirstOrDefault();
            if (mostWeak != null && mostWeak.Power < powerAverage)
            {
                moveCandsFromDanger.Add(mostWeak);
            }
        }
        moveCandsFromDanger = moveCandsFromDanger
            .OrderBy(m => m.Power)
            .ToList();

        // 軍事能力の優れた君主が前線にいない場合は移動候補に含める。
        if (!ruler.IsMoving && ruler.Power > powerAverage && dangerCastles.All(c => c.castle != ruler.Castle))
        {
            if (ruler.Attack > 70 || ruler.Defense > 70)
            {
                moveCandsFromSafe.Add(ruler);
            }
        }

        //Debug.LogWarning($"[AI] Ruler: {ruler.Name}" +
        //    $" Safe Castles: {string.Join(", ", safeCastles.Select(cr => cr.castle.Name))}" +
        //    $" Danger Castles: {string.Join(", ", dangerCastles.Select(cr => cr.castle.Name))}" +
        //    $" Move Candidates Safe: {string.Join(", ", moveCandsFromSafe)}" +
        //    $"[AI] Move Candidates Danger: {string.Join(", ", moveCandsFromDanger)}");

        // 危険城に空きがあれば、安全城移動候補から近いやつを移動させる。
        foreach (var (castle, _) in dangerCastles)
        {
            var incomingCount = World.Forces
                .Where(f => f.Country == castle.Country)
                .Where(f => f.Destination == castle)
                .Count();
            if (castle.Members.Count + incomingCount >= castle.MaxMember) continue;

            var cand = moveCandsFromSafe
                .OrderBy(m => m.Castle.Position.DistanceTo(castle.Position))
                .ThenByDescending(m => m.Power)
                .FirstOrDefault();
            if (cand == null) continue;

            moveCandsFromSafe.Remove(cand);
            var act = core.StrategyActions.Deploy;
            var args = act.Args(ruler, cand, castle);
            if (act.CanDo(args))
            {
                await act.Do(args);
                Debug.Log($"[AI] Move: {cand} -> {castle}");
            }
            else
            {
                Debug.Log($"[AI] Move Failed: {cand} -> {castle}");
            }
        }

        // 危険城の移動候補がいればSwapを行う。
        foreach (var candDanger in moveCandsFromDanger)
        {
            var castle = candDanger.Castle;
            var candSafe = moveCandsFromSafe
                .OrderBy(m => m.Castle.Position.DistanceTo(castle.Position))
                .ThenByDescending(m => m.Power)
                .FirstOrDefault();
            if (candSafe == null) continue;

            if (0.5f.Chance())
            {
                moveCandsFromSafe.Remove(candSafe);
                var act = core.StrategyActions.Deploy;
                var args1 = act.Args(ruler, candDanger, candSafe.Castle);
                var args2 = act.Args(ruler, candSafe, castle);
                if (act.CanDo(args1) && act.CanDo(args2))
                {
                    await act.Do(args1);
                    await act.Do(args2);
                    Debug.Log($"[AI] Swap: {candDanger} <-> {candSafe}");
                }
                else
                {
                    Debug.Log($"[AI] Swap Failed: {candDanger} <-> {candSafe}");
                }
            }
        }

        // まだ移動候補がいる場合は、危険城の隣の城へ移動させる。
        if (moveCandsFromSafe.Count > 0)
        {
            var dangerNeigbors = dangerCastles
                .SelectMany(cr => cr.castle.Neighbors)
                .Where(n => n.Country == ruler.Country)
                .Distinct()
                .ToList();

            foreach (var cand in moveCandsFromSafe)
            {
                if (dangerNeigbors.Contains(cand.Castle)) continue;

                var nearDanger = dangerNeigbors
                    .OrderBy(n => n.Position.DistanceTo(cand.Castle.Position))
                    .FirstOrDefault();

                if (nearDanger == null) continue;

                var incomingCount = World.Forces
                    .Where(f => f.Country == nearDanger.Country)
                    .Where(f => f.Destination == nearDanger)
                    .Count();
                if (nearDanger.Members.Count + incomingCount >= nearDanger.MaxMember) continue;

                var act = core.StrategyActions.Deploy;
                var args = act.Args(ruler, cand, nearDanger);
                if (act.CanDo(args))
                {
                    await act.Do(args);
                    Debug.Log($"[AI] Move to Neighbor: {cand} -> {nearDanger}");
                }
                else
                {
                    Debug.Log($"[AI] Move to Neighbor Failed: {cand} -> {nearDanger}");
                }
            }
        }

        // 危険城以外で、所属が0人の城があれば、近隣の城から1人移動させる。
        var emptyCastles = ruler.Country.Castles
            .Except(dangerCastles.Select(c => c.castle))
            .Where(c => c.Members.Count == 0 || c.Members.All(m => m.IsMoving));
        foreach (var castle in emptyCastles)
        {
            // 既に向かっている軍勢があればスキップ。
            var incomingCount = World.Forces
                .Where(f => f.Country == castle.Country)
                .Where(f => f.Destination == castle)
                .Count();
            if (incomingCount > 0) continue;

            var candidates = castle.Neighbors
                .Where(n => n.Country == castle.Country)
                .Where(n => n.Members.Count(m => !m.IsMoving) >= 2)
                .SelectMany(n => n.Members)
                .Where(m => !m.IsMoving && !m.IsBoss)
                .ToList();
            var cand = candidates
                .OrderByDescending(m => m.Power)
                .FirstOrDefault();
            if (cand == null) continue;
            var act = core.StrategyActions.Deploy;
            var args = act.Args(ruler, cand, castle);
            if (act.CanDo(args))
            {
                await act.Do(args);
                Debug.Log($"[AI] Move to Empty: {cand} -> {castle}");
            }
            else
            {
                Debug.Log($"[AI] Move to Empty Failed: {cand} -> {castle}");
            }
        }
    }
}