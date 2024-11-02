using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Strategies => new CastleActionBase[]
    {
        HireVassal,
        FireVassal,
        Resign,
    };

    /// <summary>
    /// 配下を雇います。
    /// </summary>
    public HireVassalAction HireVassal { get; } = new();
    public class HireVassalAction : CastleActionBase
    {
        public override string Label => L["人材募集"];
        public override string Description => L["配下を雇います。"];

        public override int Cost(ActionArgs args) => 10;

        protected override bool CanDoCore(ActionArgs args)
        {
            if (!World.Characters.Any(c => c.IsFree)) return false;

            return true;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            // 探索は成否に拘らずコストを消費する。
            PayCost(args);

            var chara = args.Character;

            // ランダムに所属なしのキャラを選ぶ。
            var frees = World.Characters.Where(c => c.IsFree).ToList();
            var candidates = new List<Character>();
            var candCount = (int)MathF.Max(1, MathF.Ceiling(chara.Intelligence / 10) - 5);
            for (int i = 0; i < candCount; i++)
            {
                if (frees.Count == 0) break;
                var cand = frees.RandomPick();
                candidates.Add(cand);
                frees.Remove(cand);
            }

            // 対象を選ぶ。
            var target = default(Character);
            //if (chara.IsPlayer)
            //{
            //    // どのキャラを配下にするか選択する。
            //    target = await UI.ShowSearchResult(candidates.ToArray(), World);
            //    if (target == null)
            //    {
            //        Debug.Log("キャンセルされました。");
            //        return;
            //    }
            //}
            //else
            //{
            //    // 一番強いキャラを選ぶ。
            //    target = candidates.OrderByDescending(c => c.Power).First();
            //}
            target = candidates.OrderByDescending(c => c.Power).First();

            //// 対象がプレイヤーの場合は選択肢を表示する。
            //var country = World.CountryOf(chara);
            //if (target.IsPlayer)
            //{
            //    var ok = await UI.ShowRespondJobOfferScreen(country, World);
            //    //UI.HideAllUI();
            //    Util.Todo();
            //    if (!ok)
            //    {
            //        return;
            //    }
            //}

            //// プレーヤの場合は、恨みがあれば断られる場合もある。
            //if (chara.IsPlayer && target.Urami > 0)
            //{
            //    if ((target.Urami / 100f * 10).Chance())
            //    {
            //        await MessageWindow.Show(L["拒否されました。"]);
            //        target.AddUrami(-1);
            //        return;
            //    }
            //}

            target.Castle.Frees.Remove(target);
            var castle = args.Castle;
            castle.Members.Add(target);

            Debug.Log($"{chara.Name}: {target.Name}を配下にしました。");

            //if (chara.IsPlayer)
            //{
            //    await MessageWindow.Show(L["{0}を配下にしました。", target.Name]);
            //}

            return default;
        }
    }

    /// <summary>
    /// 配下を解雇します。
    /// </summary>
    public FireVassalAction FireVassal { get; } = new();
    public class FireVassalAction : CastleActionBase
    {
        public override string Label => L["追放"];
        public override string Description => L["配下を解雇します。"];

        public override int Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 勢力を捨てて放浪します。
    /// </summary>
    public ResignAction Resign { get; } = new();
    public class ResignAction : CastleActionBase
    {
        public override string Label => L["放浪"];
        public override string Description => L["勢力を捨てて放浪します。"];

        public override int Cost(ActionArgs args) => 5;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
