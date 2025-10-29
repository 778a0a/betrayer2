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
    /// 出撃中のみ利用可能。進軍先を変更します。
    /// </summary>
    public ChangeDestinationAction ChangeDestination { get; } = new();
    public class ChangeDestinationAction : PersonalActionBase
    {
        public override string Label => L["変更"];
        public override string Description => L["進軍先を変更します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Moving;

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 0, 0);

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var actor = args.actor;
            bool done = await DoCore(actor, actor);
            if (!done) return;

            PayCost(args);
        }

        public static async ValueTask<bool> DoCore(Character actor, Character targetCharactor)
        {
            var force = GameCore.Instance.World.Forces.First(f => f.Character == targetCharactor);
            var prevDestination = force.Destination;
            IMapEntity target = null;

            // プレーヤーの場合は目的地選択画面を表示する。
            if (actor.IsPlayer)
            {
                // 自身の城も含めて選択可能にする。
                var cands = targetCharactor.Castle.Neighbors.Concat(new[] { targetCharactor.Castle }).ToList();
                target = await GameCore.Instance.MainUI.SelectCastleScreen.SelectDeployDestination(
                    "新しい進軍先を選択してください（必要AP: 1）",
                    "キャンセル",
                    cands,
                    selectedTile => StrategyActions.DeployAction.OnTileSelected(targetCharactor.Castle, selectedTile, true));
                if (target == null)
                {
                    Debug.Log("目的地選択がキャンセルされました。");
                    return false;
                }
                if (target is GameMapTile t && t.HasCastle)
                {
                    target = t.Castle;
                }
            }
            else
            {
                // プレーヤー以外は使わないはず。
            }

            if (target == prevDestination)
            {
                Debug.Log("目的地が変更されていません。");
                return false;
            }

            if (target != null)
            {
                // 目的地を変更する。
                force.IsPlayerDirected = actor.IsPlayer;
                force.SetDestination(target);
                Debug.Log($"{force}の目的地を{target}に変更しました。");
            }

            return true;
        }
    }
}