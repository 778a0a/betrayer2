using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// セーブデータの保存・読込を管理します。
/// </summary>
public class SaveDataManager
{
    public static SaveDataManager Instance { get; private set; } = new();

    private const string SaveDataKeyPrefix = "SaveData";
    private static string SaveDataKey(int slotNo) => $"{SaveDataKeyPrefix}{slotNo}";
    public const int AutoSaveDataSlotNo = -1;

    public bool HasSaveData(int slotNo) => PlayerPrefs.HasKey(SaveDataKey(slotNo));
    public bool HasAutoSaveData() => PlayerPrefs.HasKey(SaveDataKey(AutoSaveDataSlotNo));

    public void Save(int slotNo, GameCore core, Phase timing)
    {
        var text = CreateSaveDataText(core, timing);

        Save(slotNo, text);
    }
    public void Save(int slotNo, SaveDataText saveDataText)
    {
        var compressed = saveDataText.Compress();
        Debug.Log($"セーブデータ圧縮: {saveDataText.Length} -> {compressed.Length} ({compressed.Length / (float)saveDataText.Length * 100:F1}%)");
        PlayerPrefs.SetString(SaveDataKey(slotNo), compressed);
        PlayerPrefs.Save();
        //Debug.Log(saveDataText);
    }

    private SaveDataText CreateSaveDataText(GameCore core, Phase timing)
    {
        var saveData = SaveDataText.Serialize(core, timing, DateTime.Now);
        return saveData;
    }

    public SaveData Load(int slotNo)
    {
        var text = LoadSaveDataText(slotNo);
        var saveData = text.Deserialize();
        return saveData;
    }

    public SaveDataSummary LoadSummary(int slotNo)
    {
        var text = LoadSaveDataText(slotNo);
        var summary = text.DeserializeSummary();
        return summary;
    }

    public SaveDataText LoadFromClipboard()
    {
        var text = SaveDataText.FromPlainText(GUIUtility.systemCopyBuffer);
        return text;
    }

    public void Delete(int slotNo)
    {
        PlayerPrefs.DeleteKey(SaveDataKey(slotNo));
        PlayerPrefs.Save();
    }

    public void Copy(int srcSlotNo, int dstSlotNo)
    {
        var textOriginal = LoadSaveDataText(srcSlotNo);
        var saveData = textOriginal.Deserialize();

        // スロット番号を書き換える。
        saveData.Summary.SaveDataSlotNo = dstSlotNo;

        var text = SaveDataText.Serialize(saveData);
        var compressed = text.Compress();
        PlayerPrefs.SetString(SaveDataKey(dstSlotNo), compressed);
        PlayerPrefs.Save();
    }

    public SaveDataText LoadSaveDataText(int slotNo)
    {
        var compressed = PlayerPrefs.GetString(SaveDataKey(slotNo));
        var saveData = SaveDataText.FromCompressed(compressed);
        return saveData;
    }
}
