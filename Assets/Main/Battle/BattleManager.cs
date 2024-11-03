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
    public static Battle Prepare(
        Area sourceArea,
        Area targetArea,
        Character attacker,
        Character defender,
        ActionBase actionType)
    {
        var world = GameCore.Instance.World;
        var map = world.Map;

        var dir = sourceArea.GetDirectionTo(targetArea);
        var attackerTerrain = map.Helper.GetAttackerTerrain(sourceArea.Position, dir);
        var defenderTerrain = targetArea.Terrain;

        var atk = new CharacterInBattle(attacker, attackerTerrain, sourceArea, true);
        var def = new CharacterInBattle(defender, defenderTerrain, targetArea, false);
        atk.Opponent = def;
        def.Opponent = atk;

        var battle = new Battle(atk, def, actionType);
        return battle;
    }
}


