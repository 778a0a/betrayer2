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
        
        // 遠方での戦闘かどうかをセットする。
        var atkHome = attacker.Character.Castle.Position;
        atk.IsRemote = IsRemote(atkHome, attacker) || IsRemote(atkHome, defender);
        var defHome = defender.Character.Castle.Position;
        def.IsRemote = IsRemote(defHome, defender) || IsRemote(defHome, attacker);

        var battle = new Battle(atk, def, BattleType.Field);

        // 最寄りの城を探す。
        var tile = map.GetTile(defender);
        var nearCastle = GameCore.Instance.World.Castles.OrderBy(c => c.Position.DistanceTo(tile.Position)).First();
        battle.Title = $"{nearCastle.Name}近郊の戦い";
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

        // 遠方での戦闘かどうかをセットする。
        var atkHome = attacker.Character.Castle.Position;
        atk.IsRemote = IsRemote(atkHome, attacker) && IsRemote(atkHome, defender.Castle);

        var battle = new Battle(atk, def, BattleType.Siege);
        battle.Title = $"{defender.Castle.Name}攻防戦";
        return battle;
    }

    public static bool IsRemote(IMapEntity a, IMapEntity b)
    {
        return a.DistanceTo(b) > 10;
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
