using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    public CastleActionBase[] Governings => new CastleActionBase[]
    {
        ImproveStability,
        ImproveCastleStrength,
        BuildNewTown,
    };

    /// <summary>
    /// 城の安定度を改善します。
    /// </summary>
    public ImproveStabilityAction ImproveStability { get; } = new();
    public class ImproveStabilityAction : CastleActionBase
    {
        public override string Label => L["治安"];
        public override string Description => L["城の安定度を改善します。"];

        public override ActionCost Cost(ActionArgs args) => 2;
        protected override bool CanDoCore(ActionArgs args) => args.targetCastle.StabilityMax > args.targetCastle.Stability;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle;

            var cap = Math.Max(chara.Attack, chara.Defense);
            var adj = 1 + (cap - 50) / 100f;
            castle.Stability = Mathf.Min(castle.StabilityMax, castle.Stability + 1 * adj);

            var contribAdj = castle.Objective == CastleObjective.Stability ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 城の強度を改善します。
    /// </summary>
    public ImproveCastleStrengthAction ImproveCastleStrength { get; } = new();
    public class ImproveCastleStrengthAction : CastleActionBase
    {
        public override string Label => L["城壁強化"];
        public override string Description => L["城の強度を改善します。"];

        public override ActionCost Cost(ActionArgs args) => 2;
        protected override bool CanDoCore(ActionArgs args) => args.targetCastle.StrengthMax > args.targetCastle.Strength;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle;

            var cap = Math.Max(Math.Max(chara.Intelligence, chara.Attack), chara.Defense);
            var adj = 1 + (cap - 50) / 100f;
            castle.Strength = Mathf.Min(castle.StrengthMax, castle.Strength + 5 * adj);

            var contribAdj = castle.Objective == CastleObjective.CastleStrength ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }

    /// <summary>
    /// 新しい町を建設します。
    /// </summary>
    public BuildNewTownAction BuildNewTown { get; } = new();
    public class BuildNewTownAction : CastleActionBase
    {
        public override string Label => L["町建設"];
        public override string Description => L["新しい町を建設します。"];

        public override ActionCost Cost(ActionArgs args) => 40;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));

            PayCost(args);

            return default;
        }
    }
}
