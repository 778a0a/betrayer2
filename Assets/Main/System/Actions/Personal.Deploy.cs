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
    /// 勝手に出撃する。隣接する他国の城を攻撃する。
    /// </summary>
    public DeployAction Deploy { get; } = new();
    public class DeployAction : PersonalActionBase
    {
        public override string Label => L["出撃"];
        public override string Description => L["勝手に出撃します。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndVassalNotBoss;

        public override ActionCost Cost(ActionArgs args) => 3;

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var actor = args.actor;
            var baseCastle = actor.Castle;

            // 隣接する城を取得する。
            var neighborCastles = baseCastle.Neighbors
                .Where(c => c != baseCastle)
                .ToList();
            IMapEntity target = await UI.SelectCastleScreen.SelectDeployDestination(
                "進軍先の城を選択してください",
                "キャンセル",
                neighborCastles,
                async selectedTile =>
                {
                    if (!selectedTile.HasCastle)
                    {
                        var ok = await MessageWindow.ShowOkCancel("城が存在しない場所に進軍します。\nよろしいですか？");
                        return ok;
                    }
                    if (!neighborCastles.Contains(selectedTile.Castle) && selectedTile.Castle.IsAttackable(actor.Country))
                    {
                        var ok = await MessageWindow.ShowOkCancel("隣接していない城のため戦闘効率が落ちます。\nよろしいですか？");
                        return ok;
                    }
                    return true;
                });

            if (target == null)
            {
                Debug.Log("城選択がキャンセルされました。");
                return;
            }
            
            if (target is GameMapTile t && t.HasCastle)
            {
                var castle = t.Castle;
                target = castle;
                var ok = await MessageWindow.ShowOkCancel($"{castle.Name}に進軍します。\nよろしいですか？");
                if (!ok) return;
            }
            else
            {
                var ok = await MessageWindow.ShowOkCancel($"指定された地点に進軍します。\nよろしいですか？");
                if (!ok) return;
            }

            // 出撃処理
            var force = new Force(World, actor, actor.Castle.Position);
            force.SetDestination(target);
            World.Forces.Register(force);
            
            Debug.Log($"{actor.Name}が{target}へ勝手に出撃しました。");

            PayCost(args);
        }
    }

}