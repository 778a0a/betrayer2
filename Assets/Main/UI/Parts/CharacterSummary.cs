using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterSummary
{
    private CharacterInfoSoldierIcon[] soldierIcons;
    private bool isInitialized = false;

    private void InitializeTooltipsIfNeeded()
    {
        if (isInitialized) return;
        isInitialized = true;

        RegisterTraitTooltip(traitKnight, @"【騎士】
<color=green>戦闘補正↑</color>
<color=green>訓練効果↑</color>
<color=#ff7f50>内政効果↓</color>
");
        RegisterTraitTooltip(traitDrillmaster, @"【教官】
<color=green>同拠点全員の訓練効果↑</color>
");
        RegisterTraitTooltip(traitPirate, @"【海賊】
<color=green>大河・川での戦闘・移動補正↑↑↑</color>
<color=#ff7f50>陸地での戦闘・移動補正↓↓（川沿い除く）</color>
");
        RegisterTraitTooltip(traitAdmiral, @"【提督】
<color=green>大河・川での戦闘・移動補正↑↑</color>
<color=#ff7f50>陸地での戦闘・移動補正↓（川沿い除く）</color>
");
        RegisterTraitTooltip(traitHunter, @"【狩人】
<color=green>森林での戦闘・移動補正↑↑</color>
<color=green>山岳・丘陵での戦闘・移動補正↑</color>
<color=#ff7f50>平地での戦闘補正↓</color>
");
        RegisterTraitTooltip(traitMountaineer, @"【山岳兵】
<color=green>山岳での戦闘・移動補正↑↑↑</color>
<color=green>森林・丘陵での戦闘・移動補正↑</color>
<color=#ff7f50>平地での戦闘補正↓</color>
");
        RegisterTraitTooltip(traitMerchant, @"【商人】
<color=green>投資効果↑↑</color>
<color=green>内政効果↑</color>
<color=#ff7f50>戦闘補正↓</color>
");
        RegisterTraitTooltip(traitDivineSpeed, @"【迅速】
<color=green>移動補正↑↑</color>
");
    }

    private void RegisterTraitTooltip(Label traitLabel, string description)
    {
        traitLabel.RegisterCallback<MouseEnterEvent>(evt =>
        {
            labelTraitTooltip.text = description;
            labelTraitTooltip.style.display = DisplayStyle.Flex;
        });
        traitLabel.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            labelTraitTooltip.style.display = DisplayStyle.None;
        });
    }

    public void SetData(Character chara)
    {
        InitializeTooltipsIfNeeded();

        if (chara == null)
        {
            Root.style.opacity = 0;
            return;
        }
        Root.style.opacity = 1;

        // 基本情報の更新
        imagePlayerFace.image = Static.GetFaceImage(chara);
        imagePlayerFace.style.opacity = chara.IsIncapacitated ? 0.4f : 1.0f;
        labelPlayerName.text = chara.Name;
        labelPlayerTitle.text = chara.GetTitle();
        labelMoving.visible = chara.IsMoving;
        labelIncapacitated.visible = chara.IsIncapacitated;
        if (chara.IsIncapacitated)
        {
            labelIncapacitated.text = $"行動不能({chara.IncapacitatedDaysRemaining}日)";
        }
        iconCountry.visible = !chara.IsFree;
        if (!chara.IsFree)
        {
            iconCountry.style.backgroundImage = new(Static.GetCountrySprite(chara.Country.ColorIndex));
        }
        // 能力値の更新
        labelAttack.text = chara.Attack.ToString();
        labelDefense.text = chara.Defense.ToString();
        labelIntelligence.text = chara.Intelligence.ToString();
        labelGoverning.text = chara.Governing.ToString();
        labelSoldiers.text = chara.Soldiers.SoldierCount.ToString();
        labelSoldiersMax.text = chara.Soldiers.SoldierCountMax.ToString();
        // その他の更新
        labelSalary.text = chara.Salary.ToString("0");
        labelContribution.text = chara.Contribution.ToString("0");
        labelPrestige.text = chara.Prestige.ToString("0");
        OrderContainer.style.display = Util.Display(!chara.IsFree);
        labelOrder.text = (chara.OrderIndex + 1).ToString();
        Util.SetLoyalty(labelLoyalty, chara);

        // 特性の更新
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