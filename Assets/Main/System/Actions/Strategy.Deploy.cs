using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class StrategyActions
{
    /// <summary>
    /// 指定した場所へ進軍します。
    /// </summary>
    public DeployAction Deploy { get; } = new();
    public class DeployAction : StrategyActionBase
    {
        public override string Label => L["進軍"];
        public override string Description => L["進軍します。"];

        public ActionArgs Args(Character actor, Character attacker, Castle target) =>
            new(actor, targetCharacter: attacker, targetCastle: target);

        protected override bool CanDoCore(ActionArgs args)
        {
            var chara = args.targetCharacter;
            if (chara.IsMoving || chara.IsIncapacitated)
            {
                return false;
            }

            return true;
        }

        public override bool CanUIEnable(Character actor)
        {
            
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                // 進軍可能なキャラがいる場合のみ有効
                actor.Castle.Members.Any(m => m.IsDefendable);
        }

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override async ValueTask Do(ActionArgs args)
        {
            var actor = args.actor;

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // TODO 専用画面・マップから選択可能にする

                // 隣接する城を取得する。
                var neighborCastles = actor.Castle.Neighbors
                    .Where(c => c != actor.Castle)
                    .ToList();
                args.targetCastle = (await UI.SelectCharacterScreen.Show(
                    "進軍先の城を選択してください",
                    "キャンセル",
                    neighborCastles.Select(c => c.Boss).ToList(),
                    _ => true
                ))?.Castle;

                if (args.targetCastle == null)
                {
                    Debug.Log("城選択がキャンセルされました。");
                    return;
                }

                // 進軍するキャラを選択する
                args.targetCharacter = (await UI.SelectCharacterScreen.Show(
                    "進軍するキャラクターを選択してください",
                    "キャンセル",
                    actor.Castle.Members.Where(m => m.IsDefendable).ToList(),
                    _ => true
                ));

                if (args.targetCharacter == null)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            Util.IsTrue(CanDo(args));

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position);

            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が出撃しました。");

            PayCost(args);
        }
    }
}