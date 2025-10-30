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
    /// 放浪時のみ利用可能。他の勢力に仕官する。
    /// </summary>
    public GetJobAction GetJob { get; } = new();
    public class GetJobAction : PersonalActionBase
    {
        public override string Label => L["仕官"];
        public override string Description => L["既存勢力に仕官します。"];
        protected override ActionRequirements Requirements => ActionRequirements.Free;

        public override ActionCost Cost(ActionArgs args) => 5;

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var actor = args.actor;
            
            if (actor.IsPlayer)
            {
                var allCastles = GameCore.Instance.World.Castles.ToList();
                
                // プレーヤーに仕官先の城を選択させる。
                args.targetCastle = (await UI.SelectCastleScreen.SelectTile(
                    "仕官先の城を選択してください",
                    "キャンセル",
                    allCastles,
                    _ => true,
                    async tile =>
                    {
                        var country = tile.Country;
                        var castle = tile.Castle;
                        return await MessageWindow.ShowOkCancel($"{country.Ruler.Name}軍の{castle.Name}城に仕官します。\nよろしいですか？");
                    }
                ))?.Castle;

                if (args.targetCastle == null)
                {
                    Debug.Log("城選択がキャンセルされました。");
                    return;
                }
            }
            else
            {
                // AIは実行しないはず。
                throw new NotImplementedException("AIの仕官は未実装です。");
            }

            var targetCastle = args.targetCastle;
            var targetCountry = targetCastle.Country;

            // コストを支払う
            PayCost(args);

            // Powerが対象勢力のメンバーの平均以下なら50%の確率で断られる。
            var avgPower = targetCountry.Members.Average(m => m.Power);
            if (actor.Power < avgPower - 100 && 0.25f.Chance())
            {
                Debug.Log($"{actor.Name} は {targetCountry.Ruler.Name}軍 の仕官を断られました。");
                if (actor.IsPlayer)
                {
                    await MessageWindow.Show($"仕官を断られました...");
                }
                return;
            }

            // 仕官処理

            actor.IsImportant = false;
            actor.OrderIndex = targetCountry.Members.Max(m => m.OrderIndex) + 1;
            actor.Loyalty = 80 + actor.Fealty * 2;
            actor.ChangeCastle(targetCastle, false);
            
            Debug.Log($"{actor.Name} が {targetCastle.Name} ({targetCountry.Ruler.Name}軍) に仕官しました。");
            
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{targetCastle.Country.Ruler.Name}軍に仕官しました。");
            }
        }
    }

}