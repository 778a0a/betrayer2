using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTableRowItem
{
    public event EventHandler<Character> MouseMove;
    public event EventHandler<Character> MouseDown;

    public Character Character { get; private set; }

    public void Initialize()
    {
        Root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        CharacterTableRowItemRoot.RegisterCallback<ClickEvent>(OnMouseDown);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        MouseMove?.Invoke(this, Character);
    }

    private void OnMouseDown(ClickEvent evt)
    {
        MouseDown?.Invoke(this, Character);
    }

    public void SetData(Character chara, bool isClickable, bool isSelected = false)
    {
        Character = chara;
        var country = chara?.Country;
        if (chara == null)
        {
            Root.style.visibility = Visibility.Hidden;
            return;
        }
        Root.style.visibility = Visibility.Visible;

        CharacterTableRowItemRoot.EnableInClassList("clickable", isClickable);
        CharacterTableRowItemRoot.EnableInClassList("selected", isSelected);
        
        labelName.text = chara.Name;
        labelDeployed.RemoveFromClassList("incapacitated");
        labelName.style.color = Color.white;
        if (chara.IsMoving)
        {
            labelDeployed.text = "出";
            labelDeployed.style.display = DisplayStyle.Flex;
            labelDeployed.style.color = Color.red;
        }
        else if (chara.IsIncapacitated)
        {
            labelDeployed.text = "不";
            labelDeployed.style.display = DisplayStyle.Flex;
            labelDeployed.style.color = Color.yellow;
            labelName.style.color = Color.gray;
        }
        else
        {
            labelDeployed.style.display = DisplayStyle.None;
        }
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        labelSoldiers.text = chara.Soldiers.SoldierCount.ToString();
        labelContribution.text = chara.Contribution.ToString("0");
        labelPrestige.text = chara.Prestige.ToString("0");
        labelLoyalty.text = (chara.IsPlayer || chara.IsRuler) ? "--" : chara.Loyalty.MaxWith(100).ToString("0");
    }
}