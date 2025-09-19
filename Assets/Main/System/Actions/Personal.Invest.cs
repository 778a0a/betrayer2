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
    /// 町に投資を行い、ゴールド収入上限を増やします。
    /// </summary>
    public InvestAction Invest { get; } = new();
    public class InvestAction : PersonalActionBase
    {
        public override string Label => L["投資"];
        public override string Description => L["町に投資を行い、城のゴールド収入上限を増やします。"];
        protected override ActionRequirements Requirements => ActionRequirements.NotMovingAndNotFree;

        private const int GoldCost = 5;
        public override ActionCost Cost(ActionArgs args) => GoldCost;

        public ActionArgs Args(Character actor) => new(actor);

        protected override bool CanDoCore(ActionArgs args) => true;

        public override ValueTask Do(ActionArgs args)
        {
            return DoCore(this, args, GoldCost);
        }

        public static ValueTask DoCore(ActionBase action, ActionArgs args, int cost)
        {
            Util.IsTrue(action.CanDo(args));
            var chara = args.actor;
            
            // 投資金額を補正する。
            // 能力値
            var adj = 1 + (chara.Governing - 75) / 100f;
            // 特性
            if (chara.Traits.HasFlag(Traits.Merchant)) adj += 0.25f;
            // 地形
            var adjTerrain = TerrainAdjustment(chara.Castle);
            // 発展度
            var adjLevel = chara.Castle.TotalInvestment switch
            {
                < 1000 => 6.0f,
                < 2000 => 5.0f,
                < 3000 => 4.0f,
                < 4000 => 2.0f,
                < 5000 => 1.0f,
                < 6000 => 0.75f,
                < 7000 => 0.5f,
                _ => 0.3f,
            };
            // 総投資額に加算する。
            chara.Castle.TotalInvestment += cost * adj * adjTerrain * adjLevel;

            // 功績を加算する。
            chara.Contribution += cost / GoldCost * adj * 2;

            action.PayCost(args);
            return default;
        }

        public static float TerrainAdjustment(Castle castle)
        {
            return GameCore.Instance.World.Map.GetTile(castle).Terrain switch
            {
                Terrain.River or Terrain.LargeRiver => 0.2f,
                Terrain.Mountain => 0.5f,
                Terrain.Hill => 0.8f,
                Terrain.Forest => 0.75f,
                _ => 1
            };
        }
    }
}