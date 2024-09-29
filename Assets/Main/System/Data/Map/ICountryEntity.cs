using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ICountryEntity
{
    Country Country { get; }
}

public static class CountryEntityExtensions
{
    public static CountryDiplomacy GetStance(this ICountryEntity self, ICountryEntity target)
    {
        if (self == target) return CountryDiplomacy.Self;
        if (self.Country == null || target.Country == null) return CountryDiplomacy.Enemy;
        if (self.Country == target.Country) return CountryDiplomacy.Self;
        // TODO 同盟国
        return CountryDiplomacy.Enemy;
    }

    public static bool IsSelf(this ICountryEntity self, ICountryEntity target) => self.GetStance(target) == CountryDiplomacy.Self;
    public static bool IsAlly(this ICountryEntity self, ICountryEntity target) => self.GetStance(target) == CountryDiplomacy.Allied;
    public static bool IsEnemy(this ICountryEntity self, ICountryEntity target) => self.GetStance(target) == CountryDiplomacy.Enemy;
    public static bool IsFriend(this ICountryEntity self, ICountryEntity target) => self.IsSelf(target) || self.IsAlly(target);
}

public enum CountryDiplomacy
{
    Self,
    Allied,
    Enemy,
}
