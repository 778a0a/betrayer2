using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

/// <summary>
/// その他のデータ
/// </summary>
public class SavedWorldMiscData
{
    public int GameDateTicks { get; set; }

    public static SavedWorldMiscData FromWorld(WorldData world)
    {
        return new SavedWorldMiscData
        {
            GameDateTicks = world.GameDate.Ticks,
        };
    }

    public static string Serialize(SavedWorldMiscData misc) => JsonConvert.SerializeObject(misc);
    public static SavedWorldMiscData Deserialize(string json) => JsonConvert.DeserializeObject<SavedWorldMiscData>(json);
}
