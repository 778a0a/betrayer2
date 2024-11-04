using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class BattleManager
{
    public static Battle PrepareFieldBattle(
        Force attacker,
        Force defender)
    {
        var world = GameCore.Instance.World;
        var map = world.Map;

        var attackerTerrain = map.GetTile(attacker).Terrain;
        var defenderTerrain = map.GetTile(defender).Terrain;

        var atk = new CharacterInBattle(attacker.Character, attackerTerrain, true);
        var def = new CharacterInBattle(defender.Character, defenderTerrain, false);
        atk.Opponent = def;
        def.Opponent = atk;

        var battle = new Battle(atk, def, BattleType.Field);
        return battle;
    }

    public static Battle PrepareSiegeBattle(
        Force attacker,
        Character defender)
    {
        var world = GameCore.Instance.World;
        var map = world.Map;

        var attackerTerrain = map.GetTile(attacker).Terrain;
        var defenderTerrain = map.GetTile(defender.Castle).Terrain;

        var atk = new CharacterInBattle(attacker.Character, attackerTerrain, true);
        var def = new CharacterInBattle(defender, defenderTerrain, false);
        atk.Opponent = def;
        def.Opponent = atk;

        var battle = new Battle(atk, def, BattleType.Siege);
        return battle;
    }
}

public enum BattleType
{
    Field,
    Siege,
}

public enum BattleResult
{
    None = 0,
    AttackerWin,
    DefenderWin,
    Draw,
}
