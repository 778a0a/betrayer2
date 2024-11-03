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
}

public struct ActionCost
{
    public int actorGold;
    public int actionPoints;
    public int castleGold;

    public readonly bool CanPay(Character chara) =>
        chara.Gold >= actorGold &&
        chara.ActionPoints >= actionPoints &&
        (castleGold == 0 || chara.Castle.Gold >= castleGold);

    public static ActionCost Of(int gold, int points = 0, int countryGold = 0) => new()
    {
        actorGold = gold,
        actionPoints = points,
        castleGold = countryGold,
    };

    public static implicit operator ActionCost(int gold) => new() { actorGold = gold };
}

public struct ActionArgs
{
    public Character actor;
    public Castle targetCastle;
    public Town targetTown;
    public Country targetCountry;
    public Character targetCharacter;

    public ActionArgs(
        Character actor,
        Castle targetCastle = null,
        Town targetTown = null,
        Country targetCountry = null,
        Character targetCharacter = null)
    {
        this.actor = actor;
        this.targetCastle = targetCastle;
        this.targetTown = targetTown;
        this.targetCountry = targetCountry;
        this.targetCharacter = targetCharacter;
    }

    public override readonly string ToString()
    {
        return $"{actor.Name} -> {(object)targetCastle ?? (object)targetTown ?? targetCountry}";
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
