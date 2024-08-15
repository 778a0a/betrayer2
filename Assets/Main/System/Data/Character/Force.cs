using System.Linq;

/// <summary>
/// 軍勢
/// </summary>
public class Force
{
    /// <summary>
    /// 兵士
    /// </summary>
    public Soldier[] Soldiers { get; set; }

    public bool HasEmptySlot => Soldiers.Any(s => s.IsEmptySlot);

    public int Power => (int)Soldiers.Sum(s => s.IsEmptySlot ? 0 : s.Hp / 35f * (1 + 0.2f * (s.Level - 1)));
    public int SoldierCount => Soldiers.Where(s => !s.IsEmptySlot).Sum(s => s.Hp);

    public override string ToString() => $"Power:{Power} ({string.Join(",", Soldiers.Select(s => s.ToShortString()))})";
}