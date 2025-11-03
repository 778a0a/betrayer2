using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

partial class StrategyActions
{
    /// <summary>
    /// 別の城へ物資を輸送します。
    /// </summary>
    public TranspotAction Transport { get; } = new();
    public class TranspotAction : StrategyActionBase
    {
        public override string Label => L["輸送"];
        public override string Description => L["別の城へ物資を輸送します。"];

        protected override bool VisibleCore(Character actor, GameMapTile tile) => tile.Castle?.CanOrder ?? false;

        public ActionArgs Args(Character actor, Castle c, Castle c2, float gold) =>
            new(actor, targetCastle: c, targetCastle2: c2, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 2, 0);

        protected override bool CanDoCore(ActionArgs args) => args.gold <= args.targetCastle.Gold;

        public override bool Enabled(Character actor, GameMapTile tile)
        {
            return actor.CanPay(Cost(new(actor, estimate: true))) &&
                actor.Country.Castles.Count > 1;
        }

        public bool NeedPayCost { get; set; } = true;

        public override async ValueTask Do(ActionArgs args)
        {
            var actor = args.actor;
            var sourceCastle = args.selectedTile?.Castle ?? args.targetCastle;

            if (sourceCastle == null)
            {
                Debug.LogWarning("Transport source castle is not specified.");
                return;
            }

            args.targetCastle = sourceCastle;

            async ValueTask<bool> SendTransportAsync(Castle destination, float amount)
            {
                if (destination == null) return false;
                if (amount <= 0f) return false;
                if (amount > sourceCastle.Gold)
                {
                    await MessageWindow.Show("輸送量が城の資金を超えています。");
                    return false;
                }

                var costArgs = new ActionArgs(actor, targetCastle: sourceCastle, targetCastle2: destination, gold: amount);
                if (NeedPayCost)
                {
                    var cost = Cost(costArgs);
                    if (!cost.CanPay(actor))
                    {
                        await MessageWindow.Show("采配Pが不足しています。");
                        return false;
                    }
                    PayCost(costArgs);
                }

                sourceCastle.Gold -= amount;
                destination.Gold += amount;
                args.targetCastle2 = destination;
                args.gold = amount;

                await ShowTransportMessage(actor, sourceCastle, destination, amount);
                Debug.Log($"{actor.Name} が {sourceCastle} から {destination} へ {amount}G 運びました。");
                return true;
            }

            if (actor.IsPlayer)
            {
                var targetCastles = actor.Country.Castles
                    .Where(c => c != sourceCastle)
                    .ToList();

                if (targetCastles.Count == 0)
                {
                    Debug.Log("輸送可能な城が存在しません。");
                    return;
                }

                var executedCount = 0;

                Func<bool> CanExecute = () =>
                {
                    if (!NeedPayCost) return true;
                    var cost = Cost(new ActionArgs(actor, targetCastle: sourceCastle));
                    return cost.CanPay(actor);
                };

                async ValueTask<bool> OnConfirmAsync(Castle destination, float amount)
                {
                    var success = await SendTransportAsync(destination, amount);
                    if (success)
                    {
                        executedCount++;
                    }
                    return success;
                }

                await UI.TransportScreen.Show(
                    targetCastles,
                    sourceCastle.Gold.MaxWith(10),
                    () => sourceCastle.Gold,
                    CanExecute,
                    OnConfirmAsync);

                if (executedCount == 0)
                {
                    Debug.Log("輸送がキャンセルされました。");
                }

                return;
            }

            Util.IsTrue(CanDo(args));
            await SendTransportAsync(args.targetCastle2, args.gold);
        }

        private static async ValueTask ShowTransportMessage(
            Character actor,
            Castle source,
            Castle destination,
            float amount)
        {
            if (actor.IsPlayer)
            {
                await MessageWindow.Show($"{destination.Name}へ金{amount:0}を輸送しました。");
            }
            else if (source.Boss != null && source.Boss.IsPlayer)
            {
                await MessageWindow.Show($"{destination.Name}へ金{amount:0}が輸送されました。");
            }
            else if (destination.Boss != null && destination.Boss.IsPlayer)
            {
                await MessageWindow.Show($"{source.Name}から金{amount:0}が輸送されました。");
            }
        }
    }
}
