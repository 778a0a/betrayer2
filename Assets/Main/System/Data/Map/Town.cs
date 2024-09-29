
/// <summary>
/// 町
/// </summary>
public class Town : ICountryEntity
{
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 町が所属する城
    /// </summary>
    public Castle Castle { get; set; }

    public Country Country => Castle.Country;

    /// <summary>
    /// 町が存在するならtrue
    /// </summary>
    public bool Exists { get; set; }

    /// <summary>
    /// 食料生産
    /// </summary>
    public float FoodIncome { get; set; }
    public float FoodIncomeMax { get; set; }

    /// <summary>
    /// 商業
    /// </summary>
    public float GoldIncome { get; set; }
    public float GoldIncomeMax { get; set; }
}
