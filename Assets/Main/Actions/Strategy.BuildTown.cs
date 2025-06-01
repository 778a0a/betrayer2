using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class StrategyActions
{
    /// <summary>
    /// 町建設
    /// </summary>
    public BuildTownAction BuildTown { get; } = new();
    public class BuildTownAction : StrategyActionBase
    {
        public override string Label => L["町建設"];
        public override string Description => L[""];

        public ActionArgs Args(Character actor, Castle castle, MapPosition pos) => new(actor, targetCastle: castle, targetPosition: pos);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, (int)(100 * Mathf.Pow(2, args.targetCastle.Towns.Count - 1)));

        override protected bool CanDoCore(ActionArgs args)
        {
            var pos = args.targetPosition.Value;
            var tile = World.Map.GetTile(pos);

            // すでに町がある場合は不可
            if (tile.Town != null)
            {
                return false;
            }

            // 既存の町に隣接していない場合は不可
            var cands = args.targetCastle.NewTownCandidates(World);
            if (!cands.Contains(tile))
            {
                return false;
            }

            return true;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            Map.RegisterTown(args.targetCastle, new Town()
            {
                Position = args.targetPosition.Value,
            });
            World.Map.GetTile(args.targetPosition.Value).Refresh();

            PayCost(args);
            Debug.Log($"{args.targetCastle} に新しい町が建設されました。（{args.targetPosition}）");
            return default;
        }
    }
}