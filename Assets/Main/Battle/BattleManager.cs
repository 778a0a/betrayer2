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
        var map = GameCore.Instance.World.Map;
        var atk = new CharacterInBattle(attacker.Character, map.GetTile(attacker), true, false);
        var def = new CharacterInBattle(defender.Character, map.GetTile(defender), false, false);
        atk.Opponent = def;
        def.Opponent = atk;

        var battle = new Battle(atk, def, BattleType.Field);
        return battle;
    }

    public static Battle PrepareSiegeBattle(
        Force attacker,
        Character defender)
    {
        var map = GameCore.Instance.World.Map;
        var atk = new CharacterInBattle(attacker.Character, map.GetTile(attacker), true, false);
        var def = new CharacterInBattle(defender, map.GetTile(defender.Castle), false, true);
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
