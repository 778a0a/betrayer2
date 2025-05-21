using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

partial class CastleActions
{
    /// <summary>
    /// 他勢力と同盟します。
    /// </summary>
    public AllyAction Ally { get; } = new();
    public class AllyAction : CastleActionBase
    {
        public override string Label => L["同盟"];
        public override string Description => L["他勢力と同盟します。"];

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var target = args.targetCountry;
            // TODO 思考処理
            args.actor.Country.SetAlly(target);
            Debug.Log($"{args.actor.Country} と {target} が同盟しました。");

            return default;
        }
    }

    /// <summary>
    /// 他勢力との関係を改善します。
    /// </summary>
    public GoodwillAction Goodwill { get; } = new();
    public class GoodwillAction : CastleActionBase
    {
        public override string Label => L["親善"];
        public override string Description => L["他勢力との関係を改善します。"];

        public ActionArgs Args(Character actor, Country target) => new(actor, targetCountry: target);

        public override ActionCost Cost(ActionArgs args) =>
            ActionCost.Of(0, 1, args.actor.Country.Castles.Count.MinWith(args.targetCountry.Castles.Count) * 20);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var target = args.targetCountry;
            target.Ruler.Castle.Gold += Cost(args).castleGold / 2;

            // TODO 思考処理
            if (args.actor.Country.IsAlly(target))
            {
                // TODO
                Debug.Log($"{args.actor.Country} と {target} が関係改善しました（同盟済み）");
            }
            else
            {
                var rel = args.actor.Country.GetRelation(target);
                var newRel = Mathf.Min(Country.AllyRelation - 1, rel + 10);
                args.actor.Country.SetRelation(target, newRel);
                Debug.Log($"{args.actor.Country} と {target} が関係改善しました（{rel} -> {newRel}）");
            }

            return default;
        }
    }

    /// <summary>
    /// 指定した場所へ進軍します。
    /// </summary>
    public MoveAction Move { get; } = new();
    public class MoveAction : CastleActionBase
    {
        public override string Label => L["進軍"];
        public override string Description => L["進軍します。"];

        public ActionArgs Args(Character actor, Character attacker, Castle target) =>
            new(actor, targetCharacter: attacker, targetCastle: target);

        protected override bool CanDoCore(ActionArgs args)
        {
            var chara = args.targetCharacter;
            if (chara.IsMoving || chara.IsIncapacitated)
            {
                return false;
            }

            return true;
        }

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position);

            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が出撃しました。");

            PayCost(args);
            return default;
        }
    }

    /// <summary>
    /// 指定した場所へ援軍として進軍します。
    /// </summary>
    public MoveAsReinforcementAction MoveAsReinforcement { get; } = new();
    public class MoveAsReinforcementAction : CastleActionBase
    {
        public override string Label => L["援軍"];
        public override string Description => L["援軍として出撃します。"];

        public ActionArgs Args(Character actor, Character attacker, Castle target) =>
            new(actor, targetCharacter: attacker, targetCastle: target);

        protected override bool CanDoCore(ActionArgs args)
        {
            var chara = args.targetCharacter;
            if (chara.IsMoving || chara.IsIncapacitated)
            {
                return false;
            }

            return true;
        }

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var force = new Force(World, args.targetCharacter, args.targetCharacter.Castle.Position, ForceMode.Reinforcement);
            force.ReinforcementOriginalTarget = args.targetCastle;
            force.SetDestination(args.targetCastle);
            World.Forces.Register(force);

            Debug.Log($"{force} が援軍として出撃しました。");

            PayCost(args);
            return default;
        }
    }

    /// <summary>
    /// 配下を雇います。
    /// </summary>
    public HireVassalAction HireVassal { get; } = new();
    public class HireVassalAction : CastleActionBase
    {
        public override string Label => L["人材募集"];
        public override string Description => L["配下を雇います。"];

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 10);

        public ActionArgs Args(Character actor, Character target) =>
            new(actor, targetCharacter: target);

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            PayCost(args);

            var chara = args.actor;

            var target = args.targetCharacter;

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

            var targetCastle = target.Castle;
            target.ChangeCastle(chara.Castle, false);

            Debug.Log($"{chara.Name}: {target.Name}を配下にしました。");

            //if (chara.IsPlayer)
            //{
            //    await MessageWindow.Show(L["{0}を配下にしました。", target.Name]);
            //}

            return default;
        }
    }

    /// <summary>
    /// 町建設
    /// </summary>
    public BuildTownAction BuildTown { get; } = new();
    public class BuildTownAction : CastleActionBase
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

    /// <summary>
    /// 発展度アップ
    /// </summary>
    public DevelopAction Develop { get; } = new();
    public class DevelopAction : CastleActionBase
    {
        public override string Label => L["発展度アップ"];
        public override string Description => L[""];

        public ActionArgs Args(Character actor, Town town) => new(actor, targetTown: town);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, (int)(100 * Mathf.Pow(1.75f, args.targetTown.DevelopmentLevel - 1)));
        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.targetTown.DevelopmentLevel++;

            PayCost(args);
            Debug.Log($"{args.targetTown} の発展度が上がりました。({args.targetTown.DevelopmentLevel})");
            return default;
        }
    }

    /// <summary>
    /// 城塞レベルアップ
    /// </summary>
    public ImproveCastleStrengthLevelAction ImproveCastleStrengthLevel { get; } = new();
    public class ImproveCastleStrengthLevelAction : CastleActionBase
    {
        public override string Label => L["城塞レベルアップ"];
        public override string Description => L[""];

        public ActionArgs Args(Character actor, Castle castle) => new(actor, targetCastle: castle);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, (int)(100 * Mathf.Pow(1.75f, args.targetCastle.FortressLevel)));
        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.targetCastle.FortressLevel++;

            PayCost(args);
            Debug.Log($"{args.targetCastle} の城塞レベルが上がりました。({args.targetCastle.FortressLevel})");
            return default;
        }
    }

    /// <summary>
    /// 引出
    /// </summary>
    public WithdrawCastleGoldAction WithdrawCastleGold { get; } = new();
    public class WithdrawCastleGoldAction : CastleActionBase
    {
        public override string Label => L["引出"];
        public override string Description => L["城の軍資金から所持金に資金を移動します。"];

        public ActionArgs Args(Character actor, int gold) => new(actor, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.actor.Castle.Gold >= args.gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.actor.Gold += args.gold;
            args.actor.Castle.Gold -= args.gold;

            PayCost(args);

            Debug.Log($"{args.actor.Name} が城から {args.gold}G 引き出しました。");
            return default;
        }
    }

    /// <summary>
    /// 預け入れ
    /// </summary>
    public DepositCastleGoldAction DepositCastleGold { get; } = new();
    public class DepositCastleGoldAction : CastleActionBase
    {
        public override string Label => L["預入"];
        public override string Description => L["所持金から城の軍資金へ資金を移動します。"];

        public ActionArgs Args(Character actor, int gold) => new(actor, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.actor.Gold >= args.gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.actor.Gold -= args.gold;
            args.actor.Castle.Gold += args.gold;

            PayCost(args);
            Debug.Log($"{args.actor.Name} が城に {args.gold}G 預け入れました。");
            return default;
        }
    }

    /// <summary>
    /// 別の城へ物資を輸送します。
    /// </summary>
    public TranspotAction Transpot { get; } = new();
    public class TranspotAction : CastleActionBase
    {
        public override string Label => L["輸送"];
        public override string Description => L["別の城へ物資を輸送します。"];

        public ActionArgs Args(Character actor, Castle c, Castle c2, float gold) =>
            new(actor, targetCastle: c, targetCastle2: c2, gold: gold);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 0);

        override protected bool CanDoCore(ActionArgs args)
        {
            return args.gold <= args.targetCastle.Gold;
        }

        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            args.targetCastle.Gold -= args.gold;
            args.targetCastle2.Gold += args.gold;
            if (!args.actor.IsRuler)
            {
                args.actor.Contribution += args.gold / 10f;
            }

            PayCost(args);
            Debug.Log($"{args.actor.Name} が {args.targetCastle} から {args.targetCastle2} へ {args.gold}G 運びました。");
            return default;
        }
    }

    /// <summary>
    /// 褒賞
    /// </summary>
    public BonusAction Bonus { get; } = new();
    public class BonusAction : CastleActionBase
    {
        public override string Label => L["褒賞"];
        public override string Description => L["臣下に褒賞を与えます。"];

        public ActionArgs Args(Character actor, Character target) => new(actor, targetCharacter: target);

        public override ActionCost Cost(ActionArgs args) => ActionCost.Of(0, 1, 20);
        public override ValueTask Do(ActionArgs args)
        {
            Util.IsTrue(CanDo(args));

            var target = args.targetCharacter;

            var oldLoyalty = target.Loyalty;
            target.Gold += 10;
            target.Loyalty = (target.Loyalty + 10).MaxWith(110);
            args.actor.Castle.Gold -= 20;

            PayCost(args);
            Debug.Log($"{args.actor.Name} が {target} に褒賞を与えました。(忠誠 {oldLoyalty} -> {target.Loyalty})");
            return default;
        }
    }
}
