using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EconomyManager
{
    private WorldData world;
    public EconomyManager(WorldData world)
    {
        this.world = world;
    }

    public const float FoodGoldRate = 50;

    public float GetFoodAmount(float gold)
    {
        var food = gold * FoodGoldRate;
        return food;
    }

    public float GetGoldAmount(float food)
    {
        var gold = food / FoodGoldRate;
        return gold;
    }
}
