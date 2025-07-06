using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTableRowItem
{
    public event EventHandler<Character> MouseMove;
    public event EventHandler<Character> MouseDown;

    public Character Character { get; private set; }

    private LocalizationManager L => MainUI.Instance.L;
    public void Initialize()
    {
        L.Register(this);

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

    public void SetData(Character chara, WorldData world, bool isClickable)
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
        
        labelName.text = chara.Name;
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        labelStatus.text = chara.GetTitle(GameCore.Instance.MainUI.L);
        labelContribution.text = chara.Contribution.ToString("0");
        labelPrestige.text = chara.Prestige.ToString("0");
    }
}