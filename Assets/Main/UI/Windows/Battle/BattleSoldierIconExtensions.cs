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

        el.SoldierImage.image = soldier.Image;

        el.labelHP.text = soldier.Hp.ToString();

        var hpBarLength = new Length(soldier.Hp / (float)soldier.MaxHp * 100, LengthUnit.Percent);
        el.HPBarValue.style.width = hpBarLength;

        // 死亡扱いなら画像を消す。
        el.SoldierImage.style.opacity = soldier.IsDeadInBattle ? 0 : 1;
        el.Root.style.opacity = soldier.Hp == 0 ? 0.3f : 1f;

        // 死亡扱いの場合
        if (soldier.IsDeadInBattle)
        {
            var red = Util.Color("#FF0000");
            el.labelHP.style.color = red;
        }
        // 残りHPが少ない場合
        else if (soldier.Hp <= 10)
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