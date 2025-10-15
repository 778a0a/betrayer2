using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

public class CountryManager : IReadOnlyList<Country>
{
    private readonly List<Country> countries = new();

    public Country this[int index] => countries[index];
    public int Count => countries.Count;
    IEnumerator<Country> IEnumerable<Country>.GetEnumerator() => countries.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => countries.GetEnumerator();

    public CountryManager(IEnumerable<Country> data, List<SavedCountryRelation> rels)
    {
        countries.AddRange(data);
        foreach (var c in data)
        {
            c.manager = this;
        }
        foreach (var rel in rels)
        {
            var a = countries.Find(c => c.Id == rel.CountryA);
            var b = countries.Find(c => c.Id == rel.CountryB);
            relations[(a, b)] = rel.Relation;
        }
    }

    public void Add(Country newCountry)
    {
        countries.Add(newCountry);
        newCountry.manager = this;
    }

    public void Remove(Country oldCountry)
    {
        // TODO 滅亡処理を整理する。
        countries.Remove(oldCountry);
    }

    private Dictionary<(Country, Country), float> relations = new();
    public float GetRelation(Country a, Country b)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (a == b) throw new ArgumentException($"GetRelation a == b ({a.Ruler.Name})");
        if (a.Id > b.Id) (a, b) = (b, a);

        var key = (a, b);
        if (relations.TryGetValue(key, out var value))
        {
            return value;
        }
        return 50;
    }

    public void SetRelation(Country a, Country b, float value)
    {
        if (a == null) throw new ArgumentNullException(nameof(a));
        if (b == null) throw new ArgumentNullException(nameof(b));
        if (a == b) throw new ArgumentException("SetRelation a == b");
        if (a.Id > b.Id) (a, b) = (b, a);

        relations[(a, b)] = Mathf.Clamp(value, 0, 100);
    }

    /// <summary>
    /// 勢力の序列を更新します。
    /// </summary>
    public async ValueTask UpdateRanking(Country target = null)
    {
        var player = GameCore.Instance.World.Player;
        var playerIsVassal = player?.IsVassal ?? false;
        var playerIsBoss = player?.IsBoss ?? false;
        var playerIsRegionBoss = player?.CanBeRegionBoss ?? false;

        var regions = GameCore.Instance.World.Castles.GroupBy(c => c.Region);

        foreach (var country in countries)
        {
            if (target != null && country != target) continue;

            // 完全制覇している地域の数
            var regionCount = regions.Count(g => g.All(c => c.Country == country));

            var members = country.Members.OrderByDescending(m => m.Importance).ToList();
            members.Remove(country.Ruler);
            country.Ruler.OrderIndex = 0;
            country.Ruler.IsImportant = true;
            for (int i = 0; i < members.Count; i++)
            {
                members[i].OrderIndex = i + 1;
                members[i].CanBeRegionBoss = i + 1 < regionCount;
                members[i].IsImportant = i + 1 < country.MaxImportantMemberCount;
            }
        }

        // プレーヤーの地位に変動があれば通知する。
        // 国主になった場合
        if (!playerIsRegionBoss && (player?.IsRegionBoss ?? false))
        {
            await MessageWindow.ShowOk("国主に昇進しました。\n近隣の城にも命令を出せるようになります。");
        }
        // 城主になった場合
        else if (!playerIsBoss && (player?.IsBoss ?? false))
        {
            await MessageWindow.ShowOk("城主に昇進しました。");
        }
        // 国主から城主に降格した場合
        else if (playerIsRegionBoss && (!player?.IsRegionBoss ?? false))
        {
            await MessageWindow.ShowOk("国主を解任されました...");
        }
        // 城主を解任された場合
        else if (playerIsBoss && (!player?.IsBoss ?? false))
        {
            await MessageWindow.ShowOk("城主を解任されました...");
        }
    }

    public Color GetRelationColor(Country a, Country b = null)
    {
        b ??= GameCore.Instance.World.Player?.Country;
        if (b == null) return Color.white;
        if (a == b) return Color.white;
        var rel = GetRelation(a, b);
        var color = Util.RelationToColor(rel);
        return color;
    }

    public string GetRelationText(Country a, Country b = null, bool includeNeighbor = false)
    {
        b ??= GameCore.Instance.World.Player?.Country;
        if (b == null) return "";

        var relation = GetRelation(a, b);
        var specials = new List<string>();
        if (includeNeighbor && a.Neighbors.Contains(b))
        {
            specials.Add("隣接");
        }
        if (a.IsAlly(b))
        {
            specials.Add("同盟");
        }
        else if (a.IsEnemy(b))
        {
            specials.Add("敵対");
        }
        var specialsText = specials.Count > 0 ? $" ({string.Join("、", specials)})" : "";
        return $"{relation:0}{specialsText}";
    }

}
