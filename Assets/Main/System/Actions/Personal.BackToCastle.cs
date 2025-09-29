using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class PersonalActions
{
    /// <summary>
    /// 出撃中のみ利用可能。自身の城に撤退します。
    /// </summary>
    public BackToCastleAction BackToCastle { get; } = new();
    public class BackToCastleAction : PersonalActionBase
    {
        public override string Label => L["撤退"];
        public override string Description => L["自身の城へ撤退します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Moving;

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 0, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var done = DoCore(args.actor, args.actor);
            if (!done) return default;

            PayCost(args);

            return default;
        }

        public static bool DoCore(Character actor, Character target)
        {
            var world = GameCore.Instance.World;
            var force = world.Forces.First(f => f.Character == target);
            var prevDestination = force.Destination;
            force.IsPlayerDirected = actor.IsPlayer;
            force.SetDestination(target.Castle);

            if (force.Position == target.Castle.Position)
            {
                GameCore.Instance.World.Forces.Unregister(force);
                Debug.LogWarning($"すでに本拠地に到達しています。軍勢を削除します。");
            }

            if (force.Destination != prevDestination)
            {
                Debug.Log($"{target.Name}は{target.Castle.Name}へ撤退します。");
                return true;
            }
            else
            {
                Debug.Log("目的地が変更されていません。");
                return false;
            }
        }
    }
}