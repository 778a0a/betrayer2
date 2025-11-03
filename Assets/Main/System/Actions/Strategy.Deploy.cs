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

        protected override bool VisibleCore(Character actor, GameMapTile tile) => tile.Castle?.CanOrder ?? false;

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

        public override bool Enabled(Character actor, GameMapTile tile)
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
                var candidateMembers = baseCastle.Members.Where(m => m.IsDefendable).OrderByDescending(m => m.Soldiers.SoldierCount).ToList();
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
                    "進軍先の城を選択してください\n<size=30><color=#aaa>※マップクリック可、遠方の城も選択できます</color></size>",
                    "キャンセル",
                    neighborCastles,
                    selectedTile => OnTileSelected(baseCastle, selectedTile, false));

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
                        var message = $"采配Pコスト: {apCost} / {actor.ActionPoints}";
                        var ng = actor.ActionPoints < apCost;
                        if (ng)
                        {
                            message += " <color=red>采配P不足</color>";
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
                // 忠誠が低いなら一定確率で拒否する。
                // 忠誠90なら10%、忠誠80なら50%
                var baseThreshold = 90 - character.Fealty;
                var denyProb = character.Loyalty > baseThreshold ?
                    0 :
                    0.10f + (baseThreshold - character.Loyalty) * 0.05f;
                var denied = denyProb.Chance();
                if (!character.IsPlayer && denied)
                {
                    if (actor.IsPlayer)
                    {
                        character.Loyalty = (character.Loyalty - 10).MinWith(0);
                        await MessageWindow.Show($"{character.Name}は出撃を拒否しました！");
                    }
                    else
                    {
                        Debug.Log($"{character.Name}は出撃を拒否しました。");
                    }
                    continue;
                }

                var force = new Force(World, character, character.Castle.Position);
                force.IsPlayerDirected = actor.IsPlayer;
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

        public static async ValueTask<bool> OnTileSelected(Castle baseCastle, GameMapTile selectedTile, bool isChangeDestination)
        {
            // 進軍コマンドの場合（変更コマンドでない場合）、自城は選択不可にする。
            if (!isChangeDestination && baseCastle == selectedTile.Castle)
            {
                return false;
            }


            var isRemote = BattleManager.IsRemote(baseCastle, selectedTile);
            // 城が存在しない場合
            if (!selectedTile.HasCastle)
            {
                // 遠方なら進軍不可にする。
                if (isRemote)
                {
                    await MessageWindow.ShowOk("遠方のため進軍できません。");
                    return false;
                }
                return await MessageWindow.ShowOkCancel("城が存在しない場所に進軍します。\nよろしいですか？");
            }

            // 城が存在する場合
            var castle = selectedTile.Castle;

            // 自国の城なら確認不要
            if (castle.IsSelf(baseCastle.Country))
            {
                return true;
            }
            // 同盟国の場合、遠方の場合のみ確認する。
            if (castle.IsAlly(baseCastle.Country))
            {
                if (isRemote)
                {
                    var ok = await MessageWindow.ShowOkCancel("遠方の城のため戦闘効率が落ちます。\nよろしいですか？");
                    return ok;
                }
                return true;
            }
            // 他国の場合
            // 自国か同盟国の城と隣接していない城は攻撃不可
            if (!castle.Neighbors.Any(n => n.Country.IsSelfOrAlly(baseCastle.Country)))
            {
                await MessageWindow.ShowOk("自国か同盟国に隣接していない城は攻撃できません。");
                return false;
            }
            // 遠方の場合は確認する。
            if (isRemote)
            {
                var ok = await MessageWindow.ShowOkCancel("遠方の城のため戦闘効率が落ちます。\nよろしいですか？");
                return ok;
            }
            return true;
        }
    }
}