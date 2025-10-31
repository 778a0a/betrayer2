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
    /// 放浪時のみ利用可能。城を攻撃して成功すれば勢力を旗揚げする。
    /// </summary>
    public SeizeAction Seize { get; } = new();
    public class SeizeAction : PersonalActionBase
    {
        public override string Label => L["奪取"];
        public override string Description => L["城を攻撃して勢力を旗揚げします。"];
        protected override ActionRequirements Requirements => ActionRequirements.Free;
        protected override bool VisibleCore(Character actor, GameMapTile tile)
        {
            return tile.HasCastle;
        }

        public override ActionCost Cost(ActionArgs args) => 5;

        public bool IsSucceeded { get; set; }

        public override async ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));
            IsSucceeded = false;

            var actor = args.actor;
            if (actor.IsPlayer)
            {
                var tile = args.selectedTile;
                var country = tile.Country;
                var castle = tile.Castle;
                if (country == null || castle == null) return;

                var ok = await MessageWindow.ShowOkCancel($"{country.Ruler.Name}軍の{castle.Name}城を攻撃します。\nよろしいですか？");
                if (!ok)
                {
                    Debug.Log("城選択がキャンセルされました。");
                    return;
                }
                args.targetCastle = castle;
            }
            PayCost(args);

            var targetCastle = args.targetCastle;

            var newCountry = RebelAction.CreateNewCountry(actor, World);
            var targetCountry = targetCastle.Country;

            // 一番有利な隣接タイルを選ぶ。
            var forceTile = targetCastle.Tile.Neighbors.Shuffle().OrderByDescending(tile =>
                Battle.TraitsAdjustment(tile, actor.Traits) +
                Battle.TerrainAdjustment(tile.Terrain)).First();
            var force = new Force(World, actor, forceTile.Position);
            force.SetDestination(targetCastle);
            force.TileMoveRemainingDays = 0;
            World.Forces.Register(force);


            // 攻城戦を行う。
            var result = await World.Forces.OnSiege(World, force, targetCastle.Tile);
            
            // 落城した場合
            if (result == SeigeResult.CastleFall)
            {
                IsSucceeded = true;
                await MessageWindow.Show($"奪取成功！新しい君主になりました。");
                
                World.Countries.UpdateRanking(newCountry);

                // 外交関係を設定する。
                foreach (var c in World.Countries.Where(c => c != newCountry))
                {
                    // 旧国とは敵対関係にする。
                    if (c == targetCountry)
                    {
                        newCountry.SetEnemy(c);
                        continue;
                    }

                    // 他は旧国の関係値をベースとする。
                    var rel = targetCountry.GetRelation(c);
                    // 50以上なら反転させる。
                    if (rel >= 50)
                    {
                        rel = 100 - rel;
                    }
                    // 50未満なら10だけ改善する。
                    else
                    {
                        rel = (rel + 10).MaxWith(50);
                    }
                    newCountry.SetRelation(c, rel);
                }
            }
            // 落城しなかった場合
            else
            {
                // 勢力を解散する。
                World.Forces.Unregister(force);
                World.Countries.Remove(newCountry);
                actor.Country = null;

                switch (result)
                {
                    case SeigeResult.AttackerWinButWithdraw:
                        await MessageWindow.Show($"攻撃に成功しましたが、撤退しました。");
                        break;
                    case SeigeResult.AttackerWinButCastleNotFall:
                        await MessageWindow.Show($"攻撃に成功しましたが、まだ守将が残っています。");
                        break;
                    case SeigeResult.DefenderWin:
                        await MessageWindow.Show($"攻撃に失敗しました。");
                        break;
                    default:
                        break;
                }
            }
        }
    }

}