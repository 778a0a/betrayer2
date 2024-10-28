
using Newtonsoft.Json;

/// <summary>
/// 町
/// </summary>
public class Town : ICountryEntity, IMapEntity
{
    /// <summary>
    /// 位置
    /// </summary>
    public MapPosition Position { get; set; }

    /// <summary>
    /// 町が所属する城
    /// </summary>
    [JsonIgnore]
    public Castle Castle { get; set; }

    [JsonIgnore]
    public Country Country => Castle.Country;

    /// <summary>
    /// 食料生産
    /// </summary>
    public float FoodIncome { get; set; }
    [JsonIgnore]
    public float FoodIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float FoodIncomeMax => CalculateMax(FoodIncomeMaxBase, Castle.DevelopmentLevel);

    /// <summary>
    /// 商業
    /// </summary>
    public float GoldIncome { get; set; }
    [JsonIgnore]
    public float GoldIncomeMaxBase { get; set; }
    [JsonIgnore]
    public float GoldIncomeMax => CalculateMax(GoldIncomeMaxBase, Castle.DevelopmentLevel);
    
    private static float CalculateMax(float baseVal, int level)
    {
        return baseVal + baseVal * 0.5f * level;
    }
}
