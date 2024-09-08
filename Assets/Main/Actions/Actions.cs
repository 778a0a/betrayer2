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
    protected GameMap Map => Core.Map.Map;
    protected LocalizationManager L => Core.MainUI.L;

    public virtual string Label => GetType().Name;
    public virtual string Description => L["(説明文なし: {0})", GetType().Name];
    /// <summary>
    /// 選択肢として表示可能ならtrue
    /// </summary>
    public virtual bool CanSelect(ActionArgs args) => true;
    /// <summary>
    /// アクションの実行に必要なGold
    /// </summary>
    public virtual int Cost(ActionArgs args) => 0;
    /// <summary>
    /// アクションを実行可能ならtrue
    /// </summary>
    public bool CanDo(ActionArgs args) =>
        CanSelect(args) &&
        args.Character.Gold >= Cost(args) &&
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
        args.Character.Gold -= Cost(args);
    }
}

public class ActionArgs
{
    public Character Character { get; set; }
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
