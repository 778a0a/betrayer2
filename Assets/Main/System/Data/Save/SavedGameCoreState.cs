using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// ゲーム進行状態
/// </summary>
public class SavedGameCoreState
{
    public int GameDateTicks { get; set; }

    public static SavedGameCoreState Create(GameCore core)
    {
        return new SavedGameCoreState
        {
            GameDateTicks = core.GameDate.Ticks,
        };
    }

    public static string Serialize(SavedGameCoreState state)
    {
        return JsonConvert.SerializeObject(state);
    }

    public static SavedGameCoreState Deserialize(string json)
    {
        return JsonConvert.DeserializeObject<SavedGameCoreState>(json);
    }
}
