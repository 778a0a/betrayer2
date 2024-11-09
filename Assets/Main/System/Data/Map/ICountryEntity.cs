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
    /// <summary>
    /// 自国ならtrue
    /// </summary>
    public static bool IsSelf(this ICountryEntity self, ICountryEntity target) => self.Country == target.Country;

    /// <summary>
    /// 同盟国ならtrue
    /// </summary>
    public static bool IsAlly(this ICountryEntity self, ICountryEntity target) => self.Country.GetRelation(target.Country) == Country.AllyRelation;

    /// <summary>
    /// 敵対国(過去に戦闘したことがある)ならtrue
    /// </summary>
    public static bool IsEnemy(this ICountryEntity self, ICountryEntity target) => self.Country.GetRelation(target.Country) == Country.EnemyRelation;

    /// <summary>
    /// 自国か同盟国でないならtrue
    /// </summary>
    public static bool IsAttackable(this ICountryEntity self, ICountryEntity target) => !self.IsSelf(target) && !self.IsAlly(target);
}
