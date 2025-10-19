using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering.Universal;

partial class PersonalActions
{
    /// <summary>
    /// 城の強度を改善します。
    /// </summary>
    public FortifyAction Fortify { get; } = new();
    public class FortifyAction : PersonalActionBase
    {
        public override string Label => L["築城"];
        public override string Description => L["城の強度を改善します。(右クリックで10回実行)"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndNotFree;

        public override ActionCost Cost(ActionArgs args) => 2;

        protected override bool CanDoCore(ActionArgs args)
        {
            var castle = args.targetCastle ?? args.actor.Castle;
            return castle.Strength < castle.StrengthMax;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;
            var castle = args.targetCastle ?? chara.Castle;

            var cap = chara.Intelligence.MinWith(chara.Attack).MinWith(chara.Defense);
            var adj = 1 + (cap - 50) / 100f;
            var adjDim = (castle.StrengthMax - castle.Strength) / castle.StrengthMax;
            var adjImp = chara.IsImportant || chara.IsPlayer ? 1 : 0.8f;
            var adjCount = chara.IsPlayer ? 1 : Mathf.Pow(0.9f, (chara.Castle.Members.Count - 3).MinWith(0));
            // 発展度までは上がりやすくする
            var adjDev = castle.Strength < castle.DevLevel ? 1.5f : 1;
            castle.Strength = (castle.Strength + 0.1f * adj * adjDim * adjImp * adjCount * adjDev).MaxWith(castle.StrengthMax);

            chara.Contribution += adjImp * adj * 1;
            PayCost(args);

            return default;
        }
    }
}