using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public struct GameDate
{
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;

    private readonly int ticks;
    public readonly int Year => ticks / DaysPerYear + 1;
    public readonly int Month => (ticks % DaysPerYear) / DaysPerMonth + 1;
    public readonly int Day => ticks % DaysPerMonth + 1;
    public readonly int Ticks => ticks;

    public GameDate(int ticks)
    {
        this.ticks = ticks;
    }

    public readonly GameDate AddDays(int days) => new(ticks + days);
    public readonly GameDate NextDay() => AddDays(1);
    public readonly GameDate NextMonth() => AddDays(DaysPerMonth);

    public static GameDate operator ++(GameDate date) => date.NextDay();
    public static GameDate operator +(GameDate date, int days) => date.AddDays(days);
    public static int operator -(GameDate date, GameDate date2) => date.Ticks - date2.Ticks;

    /// <summary>
    /// 1月、4月、7月、10月は収入月
    /// </summary>
    public readonly bool IsIncomeMonth => Month % 3 == 1;
    public readonly bool IsMidMonth => Month % 3 == 2;
    public readonly bool IsEndMonth => Month % 3 == 0;
    
    public readonly bool IsGameFirstDay => Year == 1 && Month == 1 && Day == 1;

    public override readonly string ToString()
    {
        return $"{Year:000}/{Month:00}/{Day:00}";
    }
}
