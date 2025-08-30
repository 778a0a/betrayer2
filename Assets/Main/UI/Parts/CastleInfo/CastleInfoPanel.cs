using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleInfoPanel
{
    private GameCore Core => GameCore.Instance;
    private GameMapTile targetTile;
    private Castle targetCastle;

    private InfoTab currentTab;
    private Button CurrentTabButton => currentTab switch
    {
        InfoTab.Castle => TabButtonCastle,
        InfoTab.Country => TabButtonCountry,
        InfoTab.Diplomacy => TabButtonDiplomacy,
        _ => throw new NotImplementedException(),
    };
    private enum InfoTab
    {
        Castle,
        Country, 
        Diplomacy
    }

    public void Initialize()
    {
        TabButtonCastle.clicked += () => SwitchTab(InfoTab.Castle);
        TabButtonCountry.clicked += () => SwitchTab(InfoTab.Country);
        TabButtonDiplomacy.clicked += () => SwitchTab(InfoTab.Diplomacy);

        // CastleDetailTab初期化
        CastleDetailTab.Initialize();
        
        // CountryDetailTab初期化
        CountryDetailTab.Initialize();

        SwitchTab(InfoTab.Castle);
    }

    private void SwitchTab(InfoTab tab)
    {
        currentTab = tab;

        // タブボタンの色を更新する。
        TabButtonCastle.RemoveFromClassList("active");
        TabButtonCountry.RemoveFromClassList("active");
        TabButtonDiplomacy.RemoveFromClassList("active");
        CurrentTabButton.AddToClassList("active");

        CastleInfoTab.style.display = Util.Display(currentTab == InfoTab.Castle);
        CountryInfoTab.style.display = Util.Display(currentTab == InfoTab.Country);
        DiplomacyInfoTab.style.display = Util.Display(currentTab == InfoTab.Diplomacy);
    }

    public void SetData(GameMapTile tile, Character characterSummaryTargetDefault)
    {
        targetTile = tile;
        targetCastle = tile.Castle;
        if (targetCastle != null)
        {
            CastleDetailTab.SetData(targetCastle, characterSummaryTargetDefault);
            CountryDetailTab.SetData(targetCastle?.Country, characterSummaryTargetDefault);
        }

        Render();
    }

    private void Render()
    {
        if (targetCastle == null)
        {
            Root.style.display = DisplayStyle.None;
            return;
        }
        Root.style.display = DisplayStyle.Flex;

        SetDiplomacyData(targetCastle.Country);
        CastleDetailTab.Render();
        CountryDetailTab.Render();
    }



    /// <summary>
    /// 外交関係タブを設定します。
    /// </summary>
    private void SetDiplomacyData(Country country)
    {
        var container = DiplomacyRelations;
        container.Clear();
        
        // 他の国との関係を表示（TileInfoEditorWindow:309-341を参考）
        var world = Core.World;
        var otherCountries = world.Countries
            .Where(c => c != country)
            .Where(o => o.GetRelation(country) != 50)
            .OrderBy(o => o.GetRelation(country));
            
        foreach (var other in otherCountries)
        {
            var relation = country.GetRelation(other);
            var relationItem = CreateDiplomacyRelationItem(other, relation, country);
            container.Add(relationItem);
        }
    }

    private VisualElement CreateDiplomacyRelationItem(Country other, float relation, Country myCountry)
    {
        var item = new VisualElement();
        item.style.flexDirection = FlexDirection.Row;
        item.style.alignItems = Align.Center;
        item.style.marginBottom = 5;
        item.style.paddingTop = 5;
        item.style.paddingBottom = 5;
        item.style.borderBottomWidth = 1;
        item.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
        
        // 統治者の顔画像
        var faceImage = new VisualElement();
        faceImage.style.width = 40;
        faceImage.style.height = 40;
        faceImage.style.marginRight = 10;
        faceImage.style.backgroundImage = new StyleBackground(Static.GetFaceImage(other.Ruler));
        
        // 国名と統治者名
        var nameLabel = new Label($"{other.Ruler.Name}");
        nameLabel.style.fontSize = 20;
        nameLabel.style.color = Color.white;
        nameLabel.style.flexGrow = 1;
        nameLabel.style.marginRight = 10;
        
        // 関係性ラベル
        var statusLabel = new Label();
        statusLabel.style.fontSize = 18;
        statusLabel.style.marginRight = 10;
        
        if (myCountry.IsAlly(other))
        {
            statusLabel.text = "同盟";
            statusLabel.style.color = Color.green;
        }
        else if (myCountry.IsEnemy(other))
        {
            statusLabel.text = "敵対";
            statusLabel.style.color = Color.red;
        }
        else if (myCountry.Neighbors.Contains(other))
        {
            statusLabel.text = "隣接";
            statusLabel.style.color = Color.yellow;
        }
        else
        {
            statusLabel.text = "";
        }
        
        // 関係度
        var relationLabel = new Label(relation.ToString());
        relationLabel.style.fontSize = 20;
        relationLabel.style.color = relation > 50 ? Color.Lerp(Color.white, Color.green, (relation - 50) / 50f) :
                                     relation < 50 ? Color.Lerp(Color.red, Color.white, relation / 50f) :
                                     Color.gray;
        
        item.Add(faceImage);
        item.Add(nameLabel);
        item.Add(statusLabel);
        item.Add(relationLabel);
        
        return item;
    }



}