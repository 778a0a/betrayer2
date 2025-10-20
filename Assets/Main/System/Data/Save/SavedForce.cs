using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SavedForce
{
    public int ContryId { get; set; }
    public int CharacterId { get; set; }
    public ForceDestinationType DestinationType { get; set; }
    public MapPosition DestinationPosition { get; set; }
    public int DestinationForceCharacterId { get; set; }
    public int ReinforcementOriginalTargetCastleId { get; set; }
    public Force Data { get; set; }
    public string Memo { get; set; }

    public static SavedForce ParseCsvRow(string[] header, string line)
    {
        var values = line.Split('\t');
        var force = new SavedForce
        {
            ContryId = int.Parse(values[0]),
            CharacterId = int.Parse(values[1]),
            DestinationType = Enum.Parse<ForceDestinationType>(values[2]),
            DestinationPosition = JsonConvert.DeserializeObject<MapPosition>(values[3]),
            DestinationForceCharacterId = int.Parse(values[4]),
            ReinforcementOriginalTargetCastleId = int.Parse(values[5]),
            Data = JsonConvert.DeserializeObject<Force>(values[6]),
            Memo = values[7],
        };
        return force;
    }
}

public enum ForceDestinationType
{
    None,
    Position,
    Force,
}


public static class SavedForces
{
    public static List<SavedForce> FromWorld(WorldData world)
    {
        var forces = new List<SavedForce>();
        for (int i = 0; i < world.Forces.Count; i++)
        {
            var original = world.Forces[i];
            var force = new SavedForce
            {
                ContryId = original.Country.Id,
                CharacterId = original.Character.Id,
                DestinationType = original.Destination switch
                {
                    Force _ => ForceDestinationType.Force,
                    _ => ForceDestinationType.Position,
                },
                DestinationPosition = original.Destination.Position,
                DestinationForceCharacterId = original.Destination is Force f ? f.Character.Id : 0,
                ReinforcementOriginalTargetCastleId = original.ReinforcementOriginalTarget?.Id ?? -1,
                Data = original,
                Memo = original.Character.Name,
            };
            forces.Add(force);
        }
        return forces;
    }

    public static string ToCsv(List<SavedForce> forces)
    {
        var sb = new StringBuilder();

        var delimiter = "\t";
        // ヘッダー
        sb.Append(nameof(SavedForce.ContryId)).Append(delimiter);
        sb.Append(nameof(SavedForce.CharacterId)).Append(delimiter);
        sb.Append(nameof(SavedForce.DestinationType)).Append(delimiter);
        sb.Append(nameof(SavedForce.DestinationPosition)).Append(delimiter);
        sb.Append(nameof(SavedForce.DestinationForceCharacterId)).Append(delimiter);
        sb.Append(nameof(SavedForce.ReinforcementOriginalTargetCastleId)).Append(delimiter);
        sb.Append(nameof(SavedForce.Data)).Append(delimiter);
        sb.Append(nameof(SavedForce.Memo)).Append(delimiter);
        sb.AppendLine();

        // 中身
        for (var i = 0; i < forces.Count; i++)
        {
            var force = forces[i];
            sb.Append(force.ContryId).Append(delimiter);
            sb.Append(force.CharacterId).Append(delimiter);
            sb.Append(force.DestinationType).Append(delimiter);
            sb.Append(JsonConvert.SerializeObject(force.DestinationPosition)).Append(delimiter);
            sb.Append(force.DestinationForceCharacterId).Append(delimiter);
            sb.Append(force.ReinforcementOriginalTargetCastleId).Append(delimiter);
            sb.Append(JsonConvert.SerializeObject(force.Data)).Append(delimiter);
            sb.Append(force.Memo).Append(delimiter);
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    public static List<SavedForce> FromCsv(string csv)
    {
        var lines = csv.Trim().Split('\n');
        var header = lines[0].Trim().Split('\t');
        var charas = new List<SavedForce>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            var chara = SavedForce.ParseCsvRow(header, line);
            charas.Add(chara);
        }
        return charas;
    }
}
