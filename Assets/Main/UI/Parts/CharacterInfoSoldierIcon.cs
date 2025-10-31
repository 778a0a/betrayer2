using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterInfoSoldierIcon
{
    public void SetData(Soldier s)
    {
        if (s == null || s.IsEmptySlot)
        {
            imageSoldier.image = Static.GetEmptySoldierImage();
            imageSoldier.style.opacity = 0.0f;
            labelLevel.text = "--";
            labelHp.text = "--";
            HPBar.style.visibility = Visibility.Hidden;
            EXPBar.style.visibility = Visibility.Hidden;
            return;
        }
        imageSoldier.image = Static.GetSoldierImage(s.Level);
        imageSoldier.style.opacity = 1.0f;
        labelLevel.text = s.Level.ToString();
        labelHp.text = s.Hp.ToString();
        HPBar.style.visibility = Visibility.Visible;
        HPBarValue.style.width = new Length(s.Hp * 100 / s.MaxHp, LengthUnit.Percent);
        EXPBar.style.visibility = Visibility.Visible;
        EXPBarValue.style.width = new Length(100f * s.Experience / Soldier.GetNextLevelExperience(s.Level), LengthUnit.Percent);

        //if (s == null || s.IsEmptySlot)
        //{
        //    imageSoldier.image = Static.Instance.GetSoldierImage(0); // Use level 0 for empty texture
        //    labelHp.text = "--";
        //    labelLevel.text = "--";
        //    HPBar.style.visibility = Visibility.Hidden;
        //    return;
        //}
        //HPBar.style.visibility = Visibility.Visible;

        ////panelContainer.tooltip = s.ToString();
        //imageSoldier.image = Static.Instance.GetSoldierImage(s.Level);
        //labelLevel.text = s.Level.ToString();
        //labelHp.text = s.Hp.ToString();

        //HPBarValue.style.width = new Length(s.Hp * 100 / s.MaxHp, LengthUnit.Percent);
    }
}