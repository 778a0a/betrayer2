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
    /// 防衛のための退却
    /// </summary>
    public async ValueTask Defence(Castle castle)
    {
        // 危険軍勢がいないなら何もしない。
        if (!castle.DangerForcesExists) return;

        var dangers = castle.DangerForces(World.Forces).ToArray();
        var dangerPower = dangers.Sum(f => f.Character.Power);
        var defPower = castle.DefenceAndReinforcementPower(World.Forces);
        // 防衛兵力が少ないなら退却させる。
        if (dangerPower > defPower)
        {
            // 出撃中の軍勢について
            var castleForces = castle.Members
                .Where(m => m.IsMoving)
                .Select(m => m.Force)
                .Where(f => f.Destination.Position != castle.Position)
                .Where(f => !f.IsPlayerDirected)
                .ShuffleAsArray();
            foreach (var myForce in castleForces)
            {
                if (dangerPower < defPower)
                {
                    Debug.Log($"防衛戦力が十分なため退却しません。{myForce}");
                    continue;
                }

                Debug.LogWarning($"危険軍勢がいるため退却します。{myForce}");
                var action = StrategyActions.BackToCastle;
                var args = new ActionArgs(myForce.Character, targetCharacter: myForce.Character);
                if (!action.CanDo(args))
                {
                    Debug.LogError($"撤退できません。{myForce}");
                    continue;
                }

                await action.Do(args);
                defPower += myForce.Character.Power;
            }
        }
    }
}