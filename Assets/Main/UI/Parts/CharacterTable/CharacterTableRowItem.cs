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

    public void SetData(Character chara, bool isClickable, bool isSelected = false, bool isAlternateDisplayMode = false)
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

        // 忠誠は両モードで常に表示
        Util.SetLoyalty(labelLoyalty, chara);

        if (isAlternateDisplayMode)
        {
            // 切替後の表示（序列・役職・所属城）
            labelAttack.style.display = DisplayStyle.None;
            labelDefense.style.display = DisplayStyle.None;
            labelIntelligence.style.display = DisplayStyle.None;
            labelGoverning.style.display = DisplayStyle.None;
            labelSoldiers.style.display = DisplayStyle.None;
            labelContribution.style.display = DisplayStyle.None;
            labelPrestige.style.display = DisplayStyle.None;

            labelImportance.style.display = DisplayStyle.Flex;
            labelOrderIndex.style.display = DisplayStyle.Flex;
            labelRole.style.display = DisplayStyle.Flex;
            labelCastle.style.display = DisplayStyle.Flex;

            // 序列
            labelOrderIndex.text = chara.IsFree ? "--" : (chara.OrderIndex + 1).ToString();
            // 役職
            labelRole.text = chara.GetTitle();
            // 所属城
            labelCastle.text = chara.IsFree ? "--" : chara.Castle.Name;
            // 権威
            labelImportance.text = chara.Importance.ToString("0");
        }
        else
        {
            // 通常表示（ステータス）
            labelAttack.style.display = DisplayStyle.Flex;
            labelDefense.style.display = DisplayStyle.Flex;
            labelIntelligence.style.display = DisplayStyle.Flex;
            labelGoverning.style.display = DisplayStyle.Flex;
            labelSoldiers.style.display = DisplayStyle.Flex;
            labelContribution.style.display = DisplayStyle.Flex;
            labelPrestige.style.display = DisplayStyle.Flex;

            labelImportance.style.display = DisplayStyle.None;
            labelOrderIndex.style.display = DisplayStyle.None;
            labelRole.style.display = DisplayStyle.None;
            labelCastle.style.display = DisplayStyle.None;

            labelAttack.text = chara.Attack.ToString();
            labelDefense.text = chara.Defense.ToString();
            labelIntelligence.text = chara.Intelligence.ToString();
            labelGoverning.text = chara.Governing.ToString();
            labelSoldiers.text = chara.Soldiers.SoldierCount.ToString();
            labelContribution.text = chara.Contribution.ToString("0");
            labelPrestige.text = chara.Prestige.ToString("0");
        }
    }
}