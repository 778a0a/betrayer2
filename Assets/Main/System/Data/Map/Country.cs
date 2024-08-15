using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 国
/// </summary>
public class Country
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 君主
    /// </summary>
    public Character Ruler { get; set; }
    /// <summary>
    /// 拠点
    /// </summary>
    public List<Castle> Catsles { get; set; }

    public IEnumerable<Character> Members => Catsles.SelectMany(c => c.Member);
    public IEnumerable<Character> Vassals => Members.Where(c => c != Ruler);

    /// <summary>
    /// マップの国の色のインデックス
    /// </summary>
    public int ColorIndex { get; set; }
}
