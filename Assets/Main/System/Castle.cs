using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

/// <summary>
/// 拠点
/// </summary>
public class Castle
{
    /// <summary>
    /// 所有国
    /// </summary>
    public Country Owner { get; set; }

    /// <summary>
    /// 所属メンバー
    /// </summary>
    public List<Character> Member { get; set; }

    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 町
    /// </summary>
    public List<Town> Towns { get; set; }

    /// <summary>
    /// 砦強度
    /// </summary>
    public float Strength { get; set; }
}

/// <summary>
/// 町
/// </summary>
public class Town
{
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 商業
    /// </summary>
    public float Commerce { get; set; }

    /// <summary>
    /// 食料生産
    /// </summary>
    public float Food { get; set; }
}
