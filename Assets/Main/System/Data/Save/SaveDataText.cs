using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// セーブデータのテキスト形式
/// 複数のセクションを区切り文字で結合したもの
/// </summary>
public class SaveDataText
{
    private readonly static string SaveDataSectionDivider = ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>";

    private string saveDataText;

    private SaveDataText(string saveDataText)
    {
        this.saveDataText = saveDataText;
    }

    public static SaveDataText FromCompressed(string compressed)
    {
        var saveDataText = Util.DecompressGzipBase64(compressed);
        return new SaveDataText(saveDataText);
    }

    public static SaveDataText FromPlainText(string saveDataText)
    {
        return new SaveDataText(saveDataText);
    }

    public string Compress() => Util.CompressGzipBase64(saveDataText);
    public string PlainText() => saveDataText;
    public int Length => saveDataText.Length;

    /// <summary>
    /// ゲームデータからセーブデータテキストを作成します。
    /// </summary>
    public static SaveDataText Serialize(
        WorldData world,
        int saveDataSlotNo,
        Phase saveTiming,
        DateTime savedTime = default)
    {
        var charas = SavedCharacters.FromWorld(world, retainFreeCharaCastleRandom: false);
        var countries = SavedCountries.FromWorld(world);
        var castles = SavedCastles.FromWorld(world);
        var forces = SavedForces.FromWorld(world);
        var countryRelations = SavedCountryRelations.FromWorld(world);
        var terrains = SavedTerrains.FromWorld(world);
        var misc = SavedWorldMiscData.FromWorld(world);
        var summary = SaveDataSummary.Create(world, saveDataSlotNo, saveTiming, savedTime);

        var saveData = new SaveData
        {
            Characters = charas,
            Countries = countries,
            Castles = castles,
            Forces = forces,
            CountryRelations = countryRelations,
            Terrains = terrains,
            Misc = misc,
            Summary = summary,
        };
        var text = Serialize(saveData);
        return text;
    }

    /// <summary>
    /// SaveDataオブジェクトからセーブデータテキストを作成します。
    /// </summary>
    public static SaveDataText Serialize(SaveData data)
    {
        var sb = new StringBuilder();

        // キャラデータ（CSV）
        var charasCsv = SavedCharacters.ToCsv(data.Characters);
        sb.AppendLine(charasCsv);

        // 国データ（CSV）
        sb.AppendLine(SaveDataSectionDivider);
        var countriesCsv = SavedCountries.ToCsv(data.Countries);
        sb.AppendLine(countriesCsv);

        // 城データ（CSV）
        sb.AppendLine(SaveDataSectionDivider);
        var castlesCsv = SavedCastles.ToCsv(data.Castles);
        sb.AppendLine(castlesCsv);

        // 軍勢データ（CSV）
        sb.AppendLine(SaveDataSectionDivider);
        var forcesCsv = SavedForces.ToCsv(data.Forces);
        sb.AppendLine(forcesCsv);

        // 国家間関係データ（CSV）
        sb.AppendLine(SaveDataSectionDivider);
        var countryRelationsCsv = SavedCountryRelations.ToCsv(data.CountryRelations);
        sb.AppendLine(countryRelationsCsv);

        // 地形データ（CSV）
        sb.AppendLine(SaveDataSectionDivider);
        var terrainsCsv = SavedTerrains.ToCsv(data.Terrains);
        sb.AppendLine(terrainsCsv);

        // その他データ（JSON）
        sb.AppendLine(SaveDataSectionDivider);
        var miscJson = SavedWorldMiscData.Serialize(data.Misc);
        sb.AppendLine(miscJson);

        // セーブ画面用情報（JSON）
        sb.AppendLine(SaveDataSectionDivider);
        var summaryJson = SaveDataSummary.Serialize(data.Summary);
        sb.AppendLine(summaryJson);

        return FromPlainText(sb.ToString());
    }

    /// <summary>
    /// テキストからSaveDataオブジェクトをデシリアライズします。
    /// </summary>
    public SaveData Deserialize()
    {
        var sections = saveDataText.Split(
            new[] { SaveDataSectionDivider },
            StringSplitOptions.RemoveEmptyEntries);

        var charasCsv = sections[0].Trim();
        var charas = SavedCharacters.FromCsv(charasCsv);

        var countriesCsv = sections[1].Trim();
        var countries = SavedCountries.FromCsv(countriesCsv);

        var castlesCsv = sections[2].Trim();
        var castles = SavedCastles.FromCsv(castlesCsv);

        var forcesCsv = sections[3].Trim();
        var forces = SavedForces.FromCsv(forcesCsv);

        var countryRelationsCsv = sections[4].Trim();
        var countryRelations = SavedCountryRelations.FromCsv(countryRelationsCsv);

        var terrainsCsv = sections[5].Trim();
        var terrains = SavedTerrains.FromCsv(terrainsCsv);

        var miscJson = sections[6].Trim();
        var misc = SavedWorldMiscData.Deserialize(miscJson);

        var summaryJson = sections[7].Trim();
        var summary = SaveDataSummary.Deserialize(summaryJson);

        return new SaveData
        {
            Characters = charas,
            Countries = countries,
            Castles = castles,
            Forces = forces,
            CountryRelations = countryRelations,
            Terrains = terrains,
            Misc = misc,
            Summary = summary,
        };
    }

    /// <summary>
    /// サマリー情報だけをデシリアライズします。
    /// </summary>
    public SaveDataSummary DeserializeSummary()
    {
        var sections = saveDataText.Split(
            new[] { SaveDataSectionDivider },
            StringSplitOptions.RemoveEmptyEntries);

        var summaryJson = sections[7].Trim();
        var summary = SaveDataSummary.Deserialize(summaryJson);

        return summary;
    }

    public override string ToString()
    {
        return saveDataText;
    }
}
