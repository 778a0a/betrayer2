using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterInfo
{
    public Character Character { get; private set; }

    private const int SoldierIconCount = 15;
    private CharacterInfoSoldierIcon SoldierIconOf(int index) => index switch
    {
        0 => soldier00,
        1 => soldier01,
        2 => soldier02,
        3 => soldier03,
        4 => soldier04,
        5 => soldier05,
        6 => soldier06,
        7 => soldier07,
        8 => soldier08,
        9 => soldier09,
        10 => soldier10,
        11 => soldier11,
        12 => soldier12,
        13 => soldier13,
        14 => soldier14,
        _ => throw new System.ArgumentOutOfRangeException(),
    };

    public LocalizationManager L => MainUI.Instance.L;
    public void Initialize()
    {
        L.Register(this);
    }

    public void SetData(Character chara)
    {
        //Character = chara;

        //if (chara == null)
        //{
        //    Root.style.visibility = Visibility.Hidden;
        //    for (int i = 0; i < SoldierIconCount; i++)
        //    {
        //        var icon = SoldierIconOf(i);
        //        icon.SetData(null);
        //    }
        //    return;
        //}
        //Root.style.visibility = Visibility.Visible;

        //labelCharaName.text = chara.Name;
        //labelCharaAttack.text = chara.Attack.ToString();
        //labelCharaDefense.text = chara.Defense.ToString();
        //labelCharaIntelligence.text = chara.Intelligence.ToString();
        //labelCharaStatus.text = chara.GetTitle(GameCore.Instance.World, GameCore.Instance.MainUI.L);
        //if (country == null)
        //{
        //    labelCharaLoyalty.text = "--";
        //    labelCharaContribution.text = "--";
        //    labelCharaSalaryRatio.text = "--";
        //}
        //else
        //{
        //    labelCharaLoyalty.text = chara.GetLoyaltyText(GameCore.Instance.World);
        //    labelCharaContribution.text = chara.Contribution.ToString();
        //    labelCharaSalaryRatio.text = chara.SalaryRatio.ToString();
        //}
        //labelCharaPrestige.text = chara.Prestige.ToString();
        //labelCharaSoldierCount.text = chara.Force.SoldierCount.ToString();

        //imageChara.image = FaceImageManager.Instance.GetImage(chara);

        //for (int i = 0; i < SoldierIconCount; i++)
        //{
        //    var sol = chara.Force.Soldiers[i];
        //    var icon = SoldierIconOf(i);
        //    icon.SetData(sol);
        //}
    }
}