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
    public Character Player { get; private set; }
    public GameDate GameDate { get; set; } = new GameDate(0);

    public IEnumerable<Castle> Castles => Countries.SelectMany(c => c.Castles);

    public void SetPlayer(Character player)
    {
        var oldPlayer = Player;
        if (oldPlayer != null)
        {
            oldPlayer.IsPlayer = false;
        }

        if (player != null)
        {
            player.IsPlayer = true;
        }

        Player = player;
    }

    public override string ToString() => $"WorldData {Characters.Count} characters, {Countries.Count} countries";
}
