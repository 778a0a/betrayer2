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
    public List<Castle> Castles { get; set; }
    public ForceManager Forces { get; set; }
    public GameMap Map { get; set; }
    public Character Player => Characters.FirstOrDefault(c => c.IsPlayer);

    public Country CountryOf(Castle castle) => Countries.FirstOrDefault(c => c.Castles.Contains(castle));
    public Castle CastleOf(Character chara) => Castles.FirstOrDefault(c => c.Members.Contains(chara));

    public override string ToString() => $"WorldData {Characters.Length} characters, {Countries.Count} countries";
}
