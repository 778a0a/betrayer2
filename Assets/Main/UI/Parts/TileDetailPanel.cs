using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TileDetailPanel
{
    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;
    }

    public void SetData(GameMapTile tile)
    {
        if (tile == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        var country = tile.Country;
        if (country == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }

        Root.style.display = DisplayStyle.Flex;

        labelTileOwner.text = country.GetTerritoryName();
        imageTileCountryColor.style.backgroundImage = new StyleBackground(country.Sprite);

        var castle = tile.Castle;
        CastleInfo.style.display = Util.Display(castle != null);
        if (castle != null)
        {
            labelGovernor.text = castle.Members.FirstOrDefault()?.Name;
            labelCastleStrength.text = castle.Strength.ToString("0");
            labelCastleFood.text = castle.Food.ToString("0");
            labelCastleGold.text = castle.Gold.ToString("0");
            labelCastleFoodIncome.text = $"{castle.FoodIncome:0} / {castle.FoodIncomeMax:0}";
            labelCastleGoldIncome.text = $"{castle.GoldIncome:0} / {castle.GoldIncomeMax:0}";
        }

        var town = tile.Town;
        TownInfo.style.display = Util.Display(town != null);
        if (town != null)
        {
            labelTownFoodIncome.text = $"{town.FoodIncome:0} / {town.FoodIncomeMax:0}";
            labelTownGoldIncome.text = $"{town.GoldIncome:0} / {town.GoldIncomeMax:0}";
        }
    }
}