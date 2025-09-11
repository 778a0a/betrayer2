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
            return actor.CanPay(Cost(new(actor, estimate: true)));
        }

        private const int ApCost = 5;
        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, ApCost, 0);

        public override async ValueTask Do(ActionArgs args)
        {
            var actor = args.actor;
            var target = (IMapEntity)args.targetCastle;
            var deployMembers = new List<Character>();

            // プレーヤーの場合
            if (actor.IsPlayer)
            {
                // 出撃元の城を取得する。
                var baseCastle = args.selectedTile.Castle;
                Assert.IsNotNull(baseCastle);

                // 出撃可能なキャラクターがいるか確認する。
                var candidateMembers = baseCastle.Members.Where(m => m.IsDefendable).ToList();
                if (candidateMembers.Count == 0)
                {
                    await MessageWindow.ShowOk("出撃可能なキャラクターが存在しません。");
                    return;
                }

                // 隣接する城を取得する。
                var neighborCastles = baseCastle.Neighbors
                    .Where(c => c != baseCastle)
                    .ToList();
                target = await UI.SelectCastleScreen.SelectDeployDestination(
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
                    target = t.Castle;
                }

                // 進軍するキャラクターを複数選択する
                deployMembers = await UI.SelectCharacterScreen.SelectMultiple(
                    "進軍するキャラクターを選択してください",
                    "決定",
                    "キャンセル",
                    candidateMembers,
                    character => character.IsDefendable,
                    selectedList =>
                    {
                        var apCost = selectedList.Count * ApCost;
                        var message = $"APコスト: {apCost} / {actor.ActionPoints}";
                        var ng = actor.ActionPoints < apCost;
                        if (ng)
                        {
                            message += " <color=red>AP(行動力)不足</color>";
                        }
                        UI.SelectCharacterScreen.labelDescription.text = message;
                        UI.SelectCharacterScreen.buttonConfirm.enabledSelf = !ng;
                    }
                );

                if (deployMembers == null || deployMembers.Count == 0)
                {
                    Debug.Log("キャラクター選択がキャンセルされました。");
                    return;
                }
            }
            else
            {
                // AIの場合は従来通り単体
                deployMembers = new List<Character> { args.targetCharacter };
            }

            //Util.IsTrue(CanDo(args));

            // 各キャラクターを個別に出撃させる
            foreach (var character in deployMembers)
            {
                var force = new Force(World, character, character.Castle.Position);
                force.SetDestination(target);
                World.Forces.Register(force);
                Debug.Log($"{force} が出撃しました。");

                if (character.IsPlayer && character != actor)
                {
                    await MessageWindow.ShowOk($"出撃命令が下りました。");
                }
            }
            
            // PayCost(args); 
            actor.ActionPoints -= deployMembers.Count * ApCost;
        }
    }
}