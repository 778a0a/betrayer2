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
    /// <summary>
    /// 選択肢として表示可能ならtrue
    /// </summary>
    public virtual bool CanSelect(ActionArgs args) => true;
    /// <summary>
    /// アクションの実行に必要なコスト
    /// </summary>
    public virtual ActionCost Cost(ActionArgs args) => 0;
    /// <summary>
    /// アクションを実行可能ならtrue
    /// </summary>
    public bool CanDo(ActionArgs args) =>
        CanSelect(args) &&
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

    protected void PayCost(ActionArgs args)
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

public struct ActionCost
{
    public int actorGold;
    public int actionPoints;
    public int castleGold;

    public readonly bool CanPay(Character chara) =>
        (actorGold == 0 || chara.Gold >= actorGold) &&
        chara.ActionPoints >= actionPoints &&
        (castleGold == 0 || chara.Castle.Gold >= castleGold);

    public static ActionCost Of(int gold, int points = 0, int countryGold = 0) => new()
    {
        actorGold = gold,
        actionPoints = points,
        castleGold = countryGold,
    };

    public static implicit operator ActionCost(int gold) => new() { actorGold = gold };

    public override string ToString() => $"Cost({actorGold},{actionPoints},{castleGold})";
}

public struct ActionArgs
{
    public Character actor;
    public Castle targetCastle;
    public Castle targetCastle2;
    public Town targetTown;
    public Country targetCountry;
    public Character targetCharacter;
    public MapPosition? targetPosition;
    public float gold;

    public ActionArgs(
        Character actor,
        Castle targetCastle = null,
        Castle targetCastle2 = null,
        Town targetTown = null,
        Country targetCountry = null,
        Character targetCharacter = null,
        MapPosition? targetPosition = null,
        float gold = 0)
    {
        this.actor = actor;
        this.targetCastle = targetCastle;
        this.targetCastle2 = targetCastle2;
        this.targetTown = targetTown;
        this.targetCountry = targetCountry;
        this.targetCharacter = targetCharacter;
        this.targetPosition = targetPosition;
        this.gold = gold;
    }

    public override readonly string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"actor: {actor.Name}");
        if (targetCastle != null) sb.Append($", targetCastle: {targetCastle.Position}");
        if (targetCastle2 != null) sb.Append($", targetCastle2: {targetCastle2.Position}");
        if (targetTown != null) sb.Append($", targetTown: {targetTown.Position}");
        if (targetCountry != null) sb.Append($", targetCountry: {targetCountry.Ruler.Name}");
        if (targetCharacter != null) sb.Append($", targetCharacter: {targetCharacter.Name}");
        if (targetPosition != null) sb.Append($", targetPosition: {targetPosition}");
        if (gold != 0) sb.Append($", gold: {gold}");
        return sb.ToString();
    }
}


public partial class CastleActions : ActionsBase<CastleActionBase>
{
    public CastleActions(GameCore core) : base(core)
    {
    }
}
public class CastleActionBase : ActionBase
{
}

public partial class TownActions : ActionsBase<TownActionBase>
{
    public TownActions(GameCore core) : base(core)
    {
    }
}
public class TownActionBase : ActionBase
{
}
