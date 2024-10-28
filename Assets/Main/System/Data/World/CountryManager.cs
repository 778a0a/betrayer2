using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        foreach (var rel in rels)
        {
            var a = countries.Find(c => c.Id == rel.CountryA);
            var b = countries.Find(c => c.Id == rel.CountryB);
            relations[(a, b)] = rel.Relation;
        }
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
        if (a == b) throw new ArgumentException("GetRelation a == b");
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

        relations[(a, b)] = value;
    }
}
