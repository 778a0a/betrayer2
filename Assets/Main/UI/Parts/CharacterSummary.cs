using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterSummary
{
    private CharacterInfoSoldierIcon[] soldierIcons;

    public void SetData(Character chara)
    {
        if (chara == null)
        {
            Root.style.opacity = 0;
            return;
        }
        Root.style.opacity = 1;

        // 基本情報の更新
        imagePlayerFace.image = Static.GetFaceImage(chara);
        labelPlayerName.text = chara.Name;
        labelPlayerTitle.text = chara.GetTitle();
        labelMoving.visible = chara.IsMoving;
        labelIncapacitated.visible = chara.IsIncapacitated;
        iconCountry.style.backgroundImage = new(Static.GetCountrySprite(chara.Country.ColorIndex));
        // 能力値の更新
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        // 資産情報の更新
        PlayerInfoContainer.style.display = Util.Display(chara.IsPlayer);
        NonPlayerInfoContainer.style.display = Util.Display(!chara.IsPlayer);
        labelSalary.text = chara.Salary.ToString("0");
        labelPlayerGold.text = chara.Gold.ToString("0");
        labelPlayerAP.text = chara.ActionPoints.ToString();
        labelContribution.text = chara.Contribution.ToString("0");
        labelSoldiers.text = chara.Soldiers.SoldierCount.ToString();
        labelPrestige.text = chara.Prestige.ToString("0");
        labelLoyalty.text = chara.Loyalty.MaxWith(100).ToString("0");
        // 特性情報の更新
        traitKnight.style.display = Util.Display(chara.Traits.HasFlag(Traits.Knight));
        traitDrillmaster.style.display = Util.Display(chara.Traits.HasFlag(Traits.Drillmaster));
        traitPirate.style.display = Util.Display(chara.Traits.HasFlag(Traits.Pirate));
        traitAdmiral.style.display = Util.Display(chara.Traits.HasFlag(Traits.Admiral));
        traitHunter.style.display = Util.Display(chara.Traits.HasFlag(Traits.Hunter));
        traitMountaineer.style.display = Util.Display(chara.Traits.HasFlag(Traits.Mountaineer));
        traitMerchant.style.display = Util.Display(chara.Traits.HasFlag(Traits.Merchant));
        traitDivineSpeed.style.display = Util.Display(chara.Traits.HasFlag(Traits.DivineSpeed));

        // 兵士情報の更新
        soldierIcons ??= new[] { soldier00, soldier01, soldier02, soldier03, soldier04, soldier05, soldier06, soldier07, soldier08, soldier09, soldier10, soldier11, soldier12, soldier13, soldier14 };
        for (int i = 0; i < soldierIcons.Length; i++)
        {
            if (chara.Soldiers.Count <= i)
            {
                soldierIcons[i].Root.style.visibility = Visibility.Hidden;
                continue;
            }
            soldierIcons[i].Root.style.visibility = Visibility.Visible;
            soldierIcons[i].SetData(chara.Soldiers[i]);
        }
    }
}