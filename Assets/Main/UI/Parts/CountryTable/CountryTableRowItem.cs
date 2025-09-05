using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CountryTableRowItem
{
    private GameCore Core => GameCore.Instance;
    public event EventHandler<Country> MouseMove;
    public event EventHandler<Country> MouseDown;
    public event EventHandler<Country> MouseEnter;
    public event EventHandler<Country> MouseLeave;

    public Country Country { get; private set; }

    public void Initialize()
    {
        Root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        Root.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        Root.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        CountryTableRowItemRoot.RegisterCallback<ClickEvent>(OnMouseDown);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        MouseMove?.Invoke(this, Country);
    }

    private void OnMouseDown(ClickEvent evt)
    {
        MouseDown?.Invoke(this, Country);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        MouseEnter?.Invoke(this, Country);
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        MouseLeave?.Invoke(this, Country);
    }

    public void SetData(Country country, bool isClickable)
    {
        Country = country;
        if (country == null)
        {
            Root.style.visibility = Visibility.Hidden;
            return;
        }
        Root.style.visibility = Visibility.Visible;

        CountryTableRowItemRoot.EnableInClassList("clickable", isClickable);

        // 統治者名と国アイコン
        iconRuler.style.backgroundImage = new(Static.GetFaceImage(country.Ruler));
        labelRulerName.text = country.Ruler.Name;
        labelRulerName.style.color = Core.World.Countries.GetRelationColor(country);
        iconCountry.style.backgroundImage = new(Static.GetCountrySprite(country.ColorIndex));
        // 友好度
        var playerCountry = Core.World.Player?.Country;
        var showRelation = playerCountry != null && country != playerCountry;
        RelationContainer.style.display = Util.Display(showRelation);
        if (showRelation)
        {
            labelRelation.text = Core.World.Countries.GetRelationText(playerCountry, country, includeNeighbor: true);
            RelationContainer.style.color = Core.World.Countries.GetRelationColor(playerCountry, country);
        }

        // 方針
        var objectiveText = country.Objective switch
        {
            CountryObjective.RegionConquest co => $"{co.TargetRegionName}攻略",
            CountryObjective.CountryAttack co => $"{co.TargetRulerName}打倒",
            CountryObjective.StatusQuo => "現状維持",
            _ => "現状維持",
        };
        labelObjective.text = objectiveText;

        // 総資金・収支
        var totalGold = country.Castles.Sum(c => c.Gold);
        var totalBalance = country.Castles.Sum(c => c.GoldBalance);
        labelTotalGold.text = totalGold.ToString("F0");
        labelTotalBalance.text = totalBalance > 0 ? $"+{(int)totalBalance}" : $"{(int)totalBalance}";
        labelTotalBalance.style.color = totalBalance >= 0 ? Color.green : Color.red;
        
        // 総収入・総支出
        var totalIncome = country.Castles.Sum(c => c.GoldIncome);
        var totalExpenditure = country.Castles.Sum(c => c.GoldComsumption);
        labelTotalIncome.text = totalIncome.ToString("F0");
        labelTotalExpenditure.text = totalExpenditure.ToString("F0");
        
        // 城数・将数
        labelCastleCount.text = country.Castles.Count.ToString();
        var totalGeneralCount = country.Castles.Sum(c => c.Members.Count);
        labelTotalGeneralCount.text = totalGeneralCount.ToString();
        
        // 総兵力
        var totalPower = country.Castles.Sum(c => c.SoldierCount);
        var totalPowerMax = country.Castles.Sum(c => c.SoldierCountMax);
        labelTotalPower.text = totalPower.ToString("F0");
        labelTotalPowerMax.text = totalPowerMax.ToString("F0");
    }
}