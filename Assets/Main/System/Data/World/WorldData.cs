using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Random = UnityEngine.Random;

public class WorldData
{
    public List<Character> Characters { get; set; }
    public CountryManager Countries { get; set; }
    public ForceManager Forces { get; set; }
    public GameMapManager Map { get; set; }
    public Character Player => Characters.FirstOrDefault(c => c.IsPlayer);

    public IEnumerable<Castle> Castles => Countries.SelectMany(c => c.Castles);

    public Country CountryOf(Castle castle) => Countries.FirstOrDefault(c => c.Castles.Contains(castle));

    public override string ToString() => $"WorldData {Characters.Count} characters, {Countries.Count} countries";
}
