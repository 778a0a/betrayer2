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
    public string SoldierCount { get; set; }
    public string Contribution { get; set; }
    public string Prestige { get; set; }
    public string Castle { get; set; }
    public string GameDate { get; set; }
    public string ScenarioName { get; set; }
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
        GameCore core,
        Phase saveTiming,
        DateTime savedTime)
    {
        var world = core.World;
        savedTime = savedTime == default ? DateTime.Now : savedTime;
        var chara = world.Player;
        var summary = new SaveDataSummary
        {
            FaceImageId = chara?.Id ?? -1,
            Title = chara?.GetTitle() ?? "観戦モード",
            Name = chara?.Name ?? "",
            SoldierCount = chara?.Soldiers?.SoldierCount.ToString("0") ?? "--",
            Contribution = chara?.Contribution.ToString("0") ?? "--",
            Prestige = chara?.Prestige.ToString("0") ?? "--",
            Castle = (chara?.IsFree ?? true) ? "" : chara.Castle.Name,
            GameDate = world.GameDate.ToString(),
            ScenarioName = core.ScenarioName,
            SaveDataSlotNo = core.SaveDataSlotNo,
            SaveTiming = saveTiming,
            SavedTime = savedTime,
        };
        return summary;
    }
}
