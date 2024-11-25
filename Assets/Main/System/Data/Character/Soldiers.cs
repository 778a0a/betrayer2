using System.Collections.Generic;
using System.Linq;

/// <summary>
/// キャラの所有兵士
/// </summary>
public class Soldiers : IReadOnlyList<Soldier>
{
    /// <summary>
    /// 兵士
    /// </summary>
    public Soldier[] SoldierArray { get; set; }

    public Soldier this[int index]
    {
        get => SoldierArray[index];
        set => SoldierArray[index] = value;
    }

    public Soldiers(int count)
    {
        SoldierArray = new Soldier[count];
        for (int i = 0; i < count; i++)
        {
            SoldierArray[i] = new Soldier()
            {
                IsEmptySlot = true,
            };
        }
    }

    public Soldiers(IEnumerable<Soldier> soldiers)
    {
        SoldierArray = soldiers.ToArray();
    }


    public int Count => SoldierArray.Length;
    public IEnumerator<Soldier> GetEnumerator() => ((IEnumerable<Soldier>)SoldierArray).GetEnumerator();
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => SoldierArray.GetEnumerator();

    public bool HasEmptySlot => SoldierArray.Any(s => s.IsEmptySlot);
    public bool IsAllDead => SoldierArray.All(s => s.IsEmptySlot || s.HpFloat <= 0);

    public int Power => (int)SoldierArray.Sum(s => s.IsEmptySlot ? 0 : s.Hp / 35f * (1 + 0.2f * (s.Level - 1)));
    public int SoldierCount => SoldierArray.Where(s => !s.IsEmptySlot).Sum(s => s.Hp);
    public int SoldierCountMax => SoldierArray.Where(s => !s.IsEmptySlot).Sum(s => s.MaxHp);
    public float AttritionRate => SoldierCount == 0 ? 1f : 1f - SoldierCount / SoldierCountMax;

    public override string ToString() => $"Power:{Power} ({string.Join(",", SoldierArray.Select(s => s.ToShortString()))})";
}
