using System.Collections.Generic;
using UnityEngine;

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
