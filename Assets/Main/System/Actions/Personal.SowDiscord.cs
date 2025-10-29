using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

partial class PersonalActions
{
    /// <summary>
    /// 敵拠点のキャラクターの忠誠を下げます。
    /// </summary>
    public SowDiscordAction SowDiscord { get; } = new();
    public class SowDiscordAction : PersonalActionBase
    {
        public override string Label => L["離間"];
        public override string Description => L["キャラクターの忠誠を下げます。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndNotFree;

        public override ActionCost Cost(ActionArgs args) => 10;

        private IEnumerable<Castle> GetTargetCastles(Character actor)
        {
            var myCastle = actor.Castle;
            var intelligence = actor.Intelligence;
            var castles = new List<Castle>();

            // 知力に応じて隣接城を追加する。
            if (intelligence > 90)
            {
                castles.AddRange(myCastle.Neighbors.SelectMany(n => n.Neighbors).Distinct());
            }
            else if (intelligence > 80)
            {
                castles.Add(myCastle);
                castles.AddRange(myCastle.Neighbors);
            }
            else
            {
                castles.Add(myCastle);
            }

            return castles.Distinct();
        }

        protected override bool VisibleCore(Character actor, GameMapTile tile)
        {
            // タイルに城がない場合は非表示
            if (tile.Castle == null) return false;

            // 対象範囲外の場合は非表示
            var targetCastles = GetTargetCastles(actor);
            if (!targetCastles.Contains(tile.Castle)) return false;

            return true;
        }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var actor = args.actor;

            // プレイヤーの場合、対象キャラを選択する。
            if (actor.IsPlayer)
            {
                var targetCastle = args.targetCastle ?? args.selectedTile.Castle;
                var targetCharacters = targetCastle.Members
                    .Where(c => c != targetCastle.Country.Ruler)
                    .Where(c => c != actor)
                    .OrderBy(c => c.OrderIndex)
                    .ToList();

                if (targetCharacters.Count == 0)
                {
                    await MessageWindow.Show("離間可能なキャラクターがいません。");
                    return;
                }

                var selectedChar = await UI.SelectCharacterScreen.Show(
                    $"離間工作を仕掛けるキャラクターを選択してください",
                    "キャンセル",
                    targetCharacters,
                    _ => true
                );

                if (selectedChar == null)
                {
                    Debug.Log("離間がキャンセルされました。");
                    return;
                }

                args.targetCharacter = selectedChar;
            }
            else
            {
                // AIの場合はtargetCharacterがセットされているはず
            }
            PayCost(args);

            // 成功判定
            var target = args.targetCharacter;

            // 基本は智謀差1で+10%成功率が上がる。
            var prob = 0.1f * (actor.Intelligence - target.Intelligence).MinWith(0);
            // 対象の城の城主が忠実な場合、城主の智謀が90以上なら1につき-10%する。
            var bossAdj = 1f;
            if (target.Castle.Boss != target && target.Castle.Boss.IsLoyal)
            {
                if (target.Castle.Boss.Intelligence > 90)
                {
                    bossAdj = 1 - 0.1f * (target.Castle.Boss.Intelligence - 90);
                }
            }
            prob *= bossAdj;

            var success = prob.Chance();
            var loyaltyDecrease = (actor.Intelligence - 85).MinWith(5) * bossAdj;
            // 忠誠95以上の場合は効果半減
            if (target.Loyalty >= 95)
            {
                loyaltyDecrease *= 0.5f;
            }
            // 忠実な場合も効果半減
            if (target.IsLoyal)
            {
                loyaltyDecrease *= 0.5f;
            }

            Debug.Log($"離間成功?: {success} ({loyaltyDecrease}:0.00) | 成功確率: {prob * 100:F1}% {actor} → {target} (城主補正: {bossAdj * 100:F1}% {target.Castle.Boss})");
            if (success)
            {
                var prevLoyalty = target.Loyalty;
                target.Loyalty = (target.Loyalty - loyaltyDecrease).MinWith(0);

                // 他勢力への実行だった場合は功績を上げる。
                if (actor.Country != target.Country)
                {
                    actor.Contribution += loyaltyDecrease;
                }

                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"{target.Name}への離間工作に成功しました。\n忠誠: {prevLoyalty:0} → {target.Loyalty:0}");
                }
            }
            // 失敗時
            else
            {
                // 智謀が高い場合は多少忠誠を減らす。
                if (actor.Intelligence >= 90)
                {
                    target.Loyalty = (target.Loyalty - loyaltyDecrease * 0.1f).MinWith(0);
                }

                // 自勢力への実行だった場合は功績を下げる。
                if (actor.Country == target.Country)
                {
                    actor.Contribution *= 0.9f;
                    if (actor.IsPlayer)
                    {
                        await MessageWindow.Show($"{target.Name}への離間工作に失敗しました...\n功績が減少しました。");
                    }
                }
                else
                {
                    await MessageWindow.Show($"{target.Name}への離間工作に失敗しました...");
                }
            }
        }
    }
}
