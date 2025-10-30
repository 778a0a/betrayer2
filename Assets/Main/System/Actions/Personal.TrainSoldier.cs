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
    /// 兵士を訓練します。
    /// </summary>
    public TrainSoldiersAction TrainSoldiers { get; } = new();
    public class TrainSoldiersAction : PersonalActionBase
    {
        public override string Label => L["訓練"];
        public override string Description => L["兵士を訓練します。(右クリックで10回実行)"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMoving;
        protected override bool VisibleCore(Character actor, GameMapTile tile)
        {
            if (actor.IsFree && tile.HasCastle) return true;
            return base.VisibleCore(actor, tile);
        }

        public override ActionCost Cost(ActionArgs args) => 2;

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            var chara = args.actor;

            var drillMasterExists = chara.IsFree ?
                chara.Traits.HasFlag(Traits.Drillmaster) :
                chara.Castle.Members.Any(m => m.Traits.HasFlag(Traits.Drillmaster));
            var isKnight = chara.Traits.HasFlag(Traits.Knight);
            foreach (var soldier in chara.Soldiers)
            {
                if (soldier.IsEmptySlot) continue;
                soldier.AddExperience(chara, true, drillMasterExists, isKnight);
            }

            PayCost(args);

            return default;
        }
    }
}