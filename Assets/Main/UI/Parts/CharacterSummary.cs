using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterSummary
{
    private CharacterInfoSoldierIcon[] soldierIcons;

    public void SetData(Character chara)
    {
        // 基本情報の更新
        imagePlayerFace.image = Static.GetFaceImage(chara);
        labelPlayerName.text = chara.Name;
        labelPlayerTitle.text = chara.GetTitle();
        labelMoving.visible = chara.IsMoving;
        // 能力値の更新
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        // 資産情報の更新
        labelPlayerGold.text = chara.Gold.ToString();
        labelPlayerContribution.text = chara.Contribution.ToString("0");
        labelPlayerPrestige.text = chara.Prestige.ToString("0");

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