using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public interface IBattleSoldierIcon
{
    public VisualElement Root { get; }
    public Image SoldierImage { get; }
    public Label labelHP { get; }
    public VisualElement HPBarValue { get; }
}

public static class BattleSoldierIconExtensions
{
    public static void SetData(this IBattleSoldierIcon el, Soldier soldier)
    {
        if (soldier == null || soldier.IsEmptySlot)
        {
            el.Root.style.visibility = Visibility.Hidden;
            return;
        }
        el.Root.style.visibility = Visibility.Visible;

        el.SoldierImage.image = SoldierImageManager.Instance.GetTexture(soldier.Level);

        el.labelHP.text = soldier.Hp.ToString();

        var hpBarLength = new Length(soldier.Hp / (float)soldier.MaxHp * 100, LengthUnit.Percent);
        el.HPBarValue.style.width = hpBarLength;

        var hpIsLow = soldier.Hp <= 10;
        if (hpIsLow)
        {
            var orange = Util.Color("#FFA500");
            el.labelHP.style.color = orange;
            el.HPBarValue.style.backgroundColor = orange;
        }
        else
        {
            el.labelHP.style.color = Color.white;
            el.HPBarValue.style.backgroundColor = Color.cyan;
        }
    }
}