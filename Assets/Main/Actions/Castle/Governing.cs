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
        ImproveCastleStrength,
        BuildNewTown,
    };

    /// <summary>
    /// 城の強度を改善します。
    /// </summary>
    public ImproveCastleStrengthAction ImproveCastleStrength { get; } = new();
    public class ImproveCastleStrengthAction : CastleActionBase
    {
        public override string Label => L["城壁強化"];
        public override string Description => L["城の強度を改善します。"];

        public override ActionCost Cost(ActionArgs args) => 3;

        public override ValueTask Do(ActionArgs args)
        {
            Assert.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle;

            castle.Strength += 0.5f * chara.Governing / 100f;

            chara.Contribution += 1;
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
