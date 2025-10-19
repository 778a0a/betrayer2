using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ActionsBase<TActionBase> where TActionBase : ActionBase
{
    public ActionsBase(GameCore core)
    {
        foreach (var action in Actions)
        {
            action.Core = core;
        }
    }

    protected TActionBase[] Actions => GetType()
        .GetProperties()
        .Where(p => p.PropertyType.IsSubclassOf(typeof(TActionBase)))
        .Select(p => p.GetValue(this))
        .Cast<TActionBase>()
        .ToArray();
}

public class ActionBase
{
    public GameCore Core { get; set; }
    protected WorldData World => Core.World;
    protected MainUI UI => Core.MainUI;
    protected GameMapManager Map => Core.Map.Map;
    protected LocalizationManager L => Core.MainUI.L;

    public virtual string Label => GetType().Name;
    public virtual string Description => L["(説明文なし: {0})", GetType().Name];
    protected virtual ActionRequirements Requirements => ActionRequirements.None;
    /// <summary>
    /// UI上に選択肢として表示可能ならtrue
    /// </summary>
    public bool Visible(Character actor, GameMapTile tile) => Requirements.IsOK(actor) && VisibleCore(actor, tile);
    protected virtual bool VisibleCore(Character actor, GameMapTile tile) => actor.Castle.Tile == tile;
    /// <summary>
    /// UI上のボタンを有効状態として表示するならtrue
    /// </summary>
    public virtual bool Enabled(Character actor, GameMapTile tile) =>
        Visible(actor, tile) &&
        actor.CanPay(Cost(new(actor, selectedTile: tile, estimate: true))) &&
        CanDoCore(new(actor, selectedTile: tile, estimate: true));
    /// <summary>
    /// アクションの実行に必要なコスト
    /// </summary>
    public virtual ActionCost Cost(ActionArgs args) => 0;
    /// <summary>
    /// アクションを実行可能ならtrue
    /// </summary>
    public bool CanDo(ActionArgs args) =>
        args.actor.CanPay(Cost(args)) &&
        CanDoCore(args);
    /// <summary>
    /// アクションを実行可能ならtrue（子クラスでのオーバーライド用）
    /// </summary>
    /// <returns></returns>
    protected virtual bool CanDoCore(ActionArgs args) => true;
    /// <summary>
    /// アクションを実行します。
    /// </summary>
    public virtual ValueTask Do(ActionArgs args) => new();

    public void PayCost(ActionArgs args)
    {
        var cost = Cost(args);
        args.actor.Gold -= cost.actorGold;
        args.actor.ActionPoints -= cost.actionPoints;
        if (cost.castleGold > 0)
        {
            args.actor.Castle.Gold -= cost.castleGold;
        }
    }

    public override string ToString() => GetType().Name;
}

[Flags]
public enum ActionRequirements
{
    None = 0,
    Moving = 1 << 0,
    NotMoving = Moving << 1,
    Free = NotMoving << 1,
    NotFree = Free << 1,
    Vassal = NotFree << 1,
    Boss = Vassal << 1,
    Ruler = Boss << 1,
    VassalNotBoss = Ruler << 1,
    
    BossNotRuler = Boss | Vassal,
    NotMovingAndVassalNotBoss = NotMoving | VassalNotBoss,
    NotMovingAndNotFree = NotMoving | NotFree,
}
public static class ActionRequirementsExtensions
{
    public static bool IsOK(this ActionRequirements req, Character chara)
    {
        if (req == ActionRequirements.None) return true;
        if (req.HasFlag(ActionRequirements.Moving) && !chara.IsMoving) return false;
        if (req.HasFlag(ActionRequirements.NotMoving) && chara.IsMoving) return false;
        if (req.HasFlag(ActionRequirements.Free) && !chara.IsFree) return false;
        if (req.HasFlag(ActionRequirements.NotFree) && chara.IsFree) return false;
        if (req.HasFlag(ActionRequirements.Vassal) && !chara.IsVassal) return false;
        if (req.HasFlag(ActionRequirements.Boss) && !chara.IsBoss) return false;
        if (req.HasFlag(ActionRequirements.Ruler) && !chara.IsRuler) return false;
        if (req.HasFlag(ActionRequirements.VassalNotBoss) && !(chara.IsVassal && !chara.IsBoss)) return false;
        return true;
    }
}

public struct ActionCost
{
    public static readonly ActionCost Variable = Of(int.MinValue, int.MinValue, int.MinValue);
    public static ActionCost Of(int gold, int points = 0, int castleGold = 0) => new()
    {
        actorGold = gold,
        actionPoints = points,
        castleGold = castleGold,
    };

    public int actorGold;
    public int actionPoints;
    public int castleGold;

    public readonly bool CanPay(Character chara) =>
        IsVariable ||
        (actorGold == 0 || chara.Gold >= actorGold) &&
        chara.ActionPoints >= actionPoints &&
        (castleGold == 0 || chara.Castle.Gold >= castleGold);

    public readonly bool IsVariable => this == Variable;

    public static implicit operator ActionCost(int gold) => new() { actorGold = gold };
    public override readonly string ToString() => $"Cost({actorGold},{actionPoints},{castleGold})";

    public override readonly int GetHashCode() => HashCode.Combine(actorGold, actionPoints, castleGold);
    public static bool operator ==(ActionCost left, ActionCost right) => left.Equals(right);
    public static bool operator !=(ActionCost left, ActionCost right) => !(left == right);
    public override readonly bool Equals(object obj) => obj is ActionCost cost &&
               actorGold == cost.actorGold &&
               actionPoints == cost.actionPoints &&
               castleGold == cost.castleGold;
}

public struct ActionArgs
{
    public Character actor;
    public Castle targetCastle;
    public Castle targetCastle2;
    public Country targetCountry;
    public Character targetCharacter;
    public MapPosition? targetPosition;
    public float gold;
    public bool estimate;
    public GameMapTile selectedTile;

    public ActionArgs(
        Character actor,
        Castle targetCastle = null,
        Castle targetCastle2 = null,
        Country targetCountry = null,
        Character targetCharacter = null,
        MapPosition? targetPosition = null,
        float gold = 0,
        bool estimate = false,
        GameMapTile selectedTile = null)
    {
        this.actor = actor;
        this.targetCastle = targetCastle;
        this.targetCastle2 = targetCastle2;
        this.targetCountry = targetCountry;
        this.targetCharacter = targetCharacter;
        this.targetPosition = targetPosition;
        this.gold = gold;
        this.estimate = estimate;
        this.selectedTile = selectedTile;
    }

    public override readonly string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"actor: {actor.Name}");
        if (targetCastle != null) sb.Append($", targetCastle: {targetCastle.Position}");
        if (targetCastle2 != null) sb.Append($", targetCastle2: {targetCastle2.Position}");
        if (targetCountry != null) sb.Append($", targetCountry: {targetCountry.Ruler.Name}");
        if (targetCharacter != null) sb.Append($", targetCharacter: {targetCharacter.Name}");
        if (targetPosition != null) sb.Append($", targetPosition: {targetPosition}");
        if (gold != 0) sb.Append($", gold: {gold}");
        return sb.ToString();
    }
}


public partial class PersonalActions : ActionsBase<PersonalActionBase>
{
    public PersonalActions(GameCore core) : base(core)
    {
    }
}
public class PersonalActionBase : ActionBase
{
}

public partial class StrategyActions : ActionsBase<StrategyActionBase>
{
    public StrategyActions(GameCore core) : base(core)
    {
    }
}
public class StrategyActionBase : ActionBase
{
}
