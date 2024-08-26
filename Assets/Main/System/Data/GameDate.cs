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

    public const int FoodIncomeMonth = 6;

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

    public readonly bool IsFoodIncomeMonth => Month == FoodIncomeMonth;

    public override readonly string ToString()
    {
        return $"{Year:000}/{Month:00}/{Day:00}";
    }
}
