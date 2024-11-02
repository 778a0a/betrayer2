using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterInfoPanel
{
    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;

        buttonPrev.clicked += () =>
        {
        };

        buttonNext.clicked += () =>
        {
        };

        buttonClose.clicked += () =>
        {
            Root.style.display = DisplayStyle.None;
        };
    }

    public void SetData(Character chara, WorldData world)
    {
        Root.style.display = DisplayStyle.Flex;

        imageChara.style.backgroundImage = FaceImageManager.Instance.GetImage(chara);

        var isVasal = chara.IsVassal;
        var ruler = chara.Country.Ruler;
        RulerImageContainer.style.display = Util.Display(isVasal);
        if (isVasal)
        {
            imageRuler.style.backgroundImage = FaceImageManager.Instance.GetImage(ruler);

        }
        var boss = chara.Castle.Boss;
        var isBoss = chara == boss;
        BossImageContainer.style.display = Util.Display(!isBoss && ruler != boss);
        if (!isBoss)
        {
            imageBoss.style.backgroundImage = FaceImageManager.Instance.GetImage(boss);
        }


        labelName.text = $"{chara.Name}"; // TODO show title
        //labelStatus.text = ...

        labelAttack.text = $"{chara.Attack}";
        labelDefense.text = $"{chara.Defense}";
        labelIntelligence.text = $"{chara.Intelligence}";
        labelGoverning.text = $"{chara.Governing}";
        labelGold.text = $"{chara.Gold}";
        labelPrestige.text = $"{chara.Prestige}";
        labelContribution.text = $"{chara.Contribution}";
        labelLoyalty.text = $"{chara.Loyalty}";

        labelSoldierCount.text = $"{chara.Soldiers.SoldierCount}";
    }
}