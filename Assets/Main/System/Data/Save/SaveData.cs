using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

/// <summary>
/// セーブデータ全体を表すクラス
/// </summary>
public class SaveData
{
    public List<SavedCharacter> Characters { get; set; }
    public List<SavedCountry> Countries { get; set; }
    public List<SavedCastle> Castles { get; set; }
    public List<SavedForce> Forces { get; set; }
    public List<SavedCountryRelation> CountryRelations { get; set; }
    public List<SavedTerrain> Terrains { get; set; }
    public SavedGameCoreState State { get; set; }
    public SaveDataSummary Summary { get; set; }
}
