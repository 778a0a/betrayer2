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

    public CountryManager(IEnumerable<Country> data)
    {
        countries.AddRange(data);
    }

    public void Remove(Country oldCountry)
    {
        // TODO 滅亡処理を整理する。
        countries.Remove(oldCountry);
    }
}
