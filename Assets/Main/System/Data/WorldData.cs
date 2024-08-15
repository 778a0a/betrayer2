using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldData
{
    public Character[] Characters { get; set; }
    // なぜかList<T>だと、HotReload後にforeachしたときにエラーが起きるのでIList<T>を使います。
    public IList<Country> Countries { get; set; }
    public GameMap Map { get; set; }

    public bool IsRuler(Character chara) => Countries.Any(c => c.Ruler == chara);
    public bool IsVassal(Character chara) => Countries.Any(c => c.Vassals.Contains(chara));
    public bool IsFree(Character chara) => !IsRuler(chara) && !IsVassal(chara);
    public bool IsRulerOrVassal(Character chara) => IsRuler(chara) || IsVassal(chara);

    public Country CountryOf(Character chara) => Countries.FirstOrDefault(c => c.Ruler == chara || c.Vassals.Contains(chara));
    public Country CountryOf(Castle castle) => Countries.FirstOrDefault(c => c.Catsles.Contains(castle));

    public override string ToString() => $"WorldData {Characters.Length} characters, {Countries.Count} countries";
}
