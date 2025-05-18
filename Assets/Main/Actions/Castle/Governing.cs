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
        protected override bool CanDoCore(ActionArgs args) => args.targetCastle.Stability < args.targetCastle.StabilityMax;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle;

            var cap = chara.Attack.MinWith(chara.Defense);
            var adj = 1 + (cap - 50) / 100f;
            var adjDev = 4 / (float)castle.Towns.Sum(t => t.DevelopmentLevel);
            castle.Stability = (castle.Stability + 1 * adj * adjDev).MaxWith(castle.StabilityMax);

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

        protected override bool CanDoCore(ActionArgs args) => args.targetCastle.Strength < args.targetCastle.StrengthMax;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle;

            var cap = chara.Intelligence.MinWith(chara.Attack).MinWith(chara.Defense);
            var adj = 1 + (cap - 50) / 100f;
            var adjDim = Town.Diminish(castle.Strength, castle.StrengthMax, 100);
            var adjImp = chara.IsImportant ? 1 : 0.5f;
            var adjCount = Mathf.Pow(0.9f, (chara.Castle.Members.Count - 3).MinWith(0));
            castle.Strength = (castle.Strength + 5 * adj * adjDim * adjImp * adjCount).MaxWith(castle.StrengthMax);

            var contribAdj = castle.Objective == CastleObjective.CastleStrength ? 1.5f : 1;
            chara.Contribution += adj * contribAdj;
            PayCost(args);

            return default;
        }
    }
}
