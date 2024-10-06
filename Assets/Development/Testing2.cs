using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Testing2
{
    public static void SaveCsv(WorldData world)
    {
        //var castles = SavedCastles.FromWorld(world);
        //var csv = SavedCastles.ToCsv(castles) + Environment.NewLine;
        //File.WriteAllText("Assets/Resources/Scenarios/01/castle_data.csv", csv, Encoding.UTF8);
        //Debug.Log(csv);

        var charas = SavedCharacters.FromWorld(world);
        var csv = SavedCharacters.ToCsv(charas) + Environment.NewLine;
        File.WriteAllText("Assets/Resources/Scenarios/01/character_data2.csv", csv, Encoding.UTF8);
        Debug.Log(csv);
    }
}
