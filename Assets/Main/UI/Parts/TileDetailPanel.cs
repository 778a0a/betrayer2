using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class TileDetailPanel
{
    public bool IsVisible => Root.style.display == DisplayStyle.Flex;

    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;
    }

    public GameMapTile CurrentData { get; private set; }
    public void SetData(GameMapTile tile)
    {
        CurrentData = tile;
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
            var foodRemainingMonths = castle.FoodRemainingMonths(GameCore.Instance.GameDate);
            var foodRemainingMonthsText = foodRemainingMonths >= 30 ?
                "" :
                $"(残り{foodRemainingMonths}ヶ月)";
            labelCastleFood.text = $"{castle.Food:0} {foodRemainingMonthsText}";
            labelCastleGold.text = $"{castle.Gold:0} ({castle.GoldBalance:+0;-0})";
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