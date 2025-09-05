using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleTableRowItem
{
    public event EventHandler<Castle> MouseMove;
    public event EventHandler<Castle> MouseDown;
    public event EventHandler<Castle> MouseEnter;
    public event EventHandler<Castle> MouseLeave;

    public Castle Castle { get; private set; }

    public void Initialize()
    {
        Root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        Root.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
        Root.RegisterCallback<MouseLeaveEvent>(OnMouseLeave);
        CastleTableRowItemRoot.RegisterCallback<ClickEvent>(OnMouseDown);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        MouseMove?.Invoke(this, Castle);
    }

    private void OnMouseDown(ClickEvent evt)
    {
        MouseDown?.Invoke(this, Castle);
    }

    private void OnMouseEnter(MouseEnterEvent evt)
    {
        MouseEnter?.Invoke(this, Castle);
    }

    private void OnMouseLeave(MouseLeaveEvent evt)
    {
        MouseLeave?.Invoke(this, Castle);
    }

    public void SetData(Castle castle, bool isClickable)
    {
        Castle = castle;
        if (castle == null)
        {
            Root.style.visibility = Visibility.Hidden;
            return;
        }
        Root.style.visibility = Visibility.Visible;

        CastleTableRowItemRoot.EnableInClassList("clickable", isClickable);
        
        // 城名
        labelName.text = castle.Name;
        labelName.style.color = GameCore.Instance.World.Countries.GetRelationColor(castle.Country);

        // 国アイコン
        iconCountry.style.backgroundImage = new(Static.GetCountrySprite(castle.Country.ColorIndex));
        
        // 地方
        labelRegion.text = castle.Region;
        
        // 方針
        var objectiveText = castle.Objective switch
        {
            CastleObjective.Attack o => $"{o.TargetCastleName}攻略",
            CastleObjective.Train => $"訓練",
            CastleObjective.Fortify => $"防備",
            CastleObjective.Develop => $"開発",
            CastleObjective.Transport o => $"{o.TargetCastleName}輸送",
            _ => "なし",
        };
        labelObjective.text = objectiveText;
        
        // 城主画像
        if (castle.Boss != null)
        {
            CastleBossImage.style.backgroundImage = new(Static.GetFaceImage(castle.Boss));
        }
        else
        {
            CastleBossImage.style.backgroundImage = null;
        }
        
        // 資金・収支
        labelGold.text = castle.Gold.ToString("0");
        var balance = castle.GoldBalance;
        labelBalance.text = balance > 0 ? $"+{(int)balance}" : $"{(int)balance}";
        labelBalance.style.color = balance >= 0 ? UnityEngine.Color.green : UnityEngine.Color.red;
        
        // 収入
        labelIncome.text = castle.GoldIncome.ToString("0");
        labelMaxIncome.text = castle.GoldIncomeMax.ToString("0");
        
        // 収入バー
        const float IncomeBarMax = 200f;
        MaxIncomeBar.style.width = Length.Percent(UnityEngine.Mathf.Clamp01(castle.GoldIncomeMax / IncomeBarMax) * 100f);
        CurrentIncomeBar.style.width = Length.Percent(UnityEngine.Mathf.Clamp01(castle.GoldIncome / IncomeBarMax) * 100f);
        
        // 支出
        labelExpenditure.text = castle.GoldComsumption.ToString("0");
        
        // 発展度・総投資額
        labelDevLevel.text = castle.DevLevel.ToString();
        labelTotalInvestment.text = castle.TotalInvestment.ToString("0");
        
        // 総兵力
        labelSoldiers.text = castle.SoldierCount.ToString("0");
        labelSoldiersMax.text = castle.SoldierCountMax.ToString("0");

        // 城塞レベル
        labelStrength.text = castle.Strength.ToString("0");
        
        // 将数
        labelMembers.text = castle.Members.Count.ToString();
    }
}