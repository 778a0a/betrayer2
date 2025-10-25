using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// セーブデータのサマリー情報（タイトル画面での表示用）
/// </summary>
public class SaveDataSummary
{
    public int FaceImageId { get; set; }
    public string Title { get; set; }
    public string Name { get; set; }
    public int SoldierCount { get; set; }
    public float Gold { get; set; }
    public string GameDate { get; set; }
    public int SaveDataSlotNo { get; set; }
    public Phase SaveTiming { get; set; } = Phase.Progress;
    public DateTime SavedTime { get; set; }

    public static SaveDataSummary Deserialize(string json)
    {
        return JsonConvert.DeserializeObject<SaveDataSummary>(json);
    }

    public static string Serialize(SaveDataSummary summary)
    {
        return JsonConvert.SerializeObject(summary);
    }

    public static SaveDataSummary Create(
        WorldData world,
        int saveDataSlotNo,
        Phase saveTiming,
        DateTime savedTime = default)
    {
        savedTime = savedTime == default ? DateTime.Now : savedTime;
        var chara = world.Player ?? world.Characters.First();
        var summary = new SaveDataSummary
        {
            FaceImageId = chara.Id,
            Title = chara.GetTitle(),
            Name = chara.Name,
            SoldierCount = chara.Soldiers.SoldierCount,
            Gold = chara.Gold,
            GameDate = world.GameDate.ToString(),
            SaveDataSlotNo = saveDataSlotNo,
            SaveTiming = saveTiming,
            SavedTime = savedTime,
        };
        return summary;
    }
}
