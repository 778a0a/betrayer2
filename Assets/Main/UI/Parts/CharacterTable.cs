using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTable : MainUIComponent
{
    public event EventHandler<Character> RowMouseMove;
    public event EventHandler<Character> RowMouseDown;

    private List<Character> originalCharas;
    private List<Character> charas;
    private Predicate<Character> clickable;

    // ソート状態管理
    private enum SortState { None, Ascending, Descending }
    private enum SortColumn { Name, Attack, Defence, Intelligence, Governing, Soldiers, Contribution, Prestige, Loyalty }
    
    private SortColumn? currentSortColumn;
    private SortState currentSortState = SortState.None;

    public void Initialize()
    {
        ListView.selectionType = SelectionType.None;
        ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        ListView.makeItem = () =>
        {
            var element = UI.Assets.CharacterTableRowItem.Instantiate();
            var row = new CharacterTableRowItem(element);
            row.Initialize();
            row.MouseDown += OnRowMouseDown;
            row.MouseMove += OnRowMouseMove;
            element.userData = row;
            Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (CharacterTableRowItem)element.userData;
            item.SetData(charas[index], clickable?.Invoke(charas[index]) ?? false);
            Debug.Log("bindItem: " + index + " -> " + charas[index]?.Name);
        };

        // ヘッダークリック処理を設定
        SetupHeaderClickHandlers();
    }

    private void OnRowMouseMove(object sender, Character e)
    {
        RowMouseMove?.Invoke(this, e);
    }

    private void OnRowMouseDown(object sender, Character e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    public void SetData(IEnumerable<Character> charas, bool clickable) => SetData(charas, _ => clickable);
    public void SetData(IEnumerable<Character> charas, Predicate<Character> clickable = null)
    {
        this.originalCharas = charas?.ToList() ?? new List<Character>();
        this.clickable = clickable ?? (_ => false);
        
        // ソート状態をリセット
        currentSortColumn = null;
        currentSortState = SortState.None;
        
        // データを表示
        RefreshData();
        UpdateSortIndicators();
    }

    /// <summary>
    /// ヘッダークリック処理を設定
    /// </summary>
    private void SetupHeaderClickHandlers()
    {
        labelHeaderName.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Name));
        labelHeaderAttack.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Attack));
        labelHeaderDefence.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Defence));
        labelHeaderIntelligence.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Intelligence));
        labelHeaderGoverning.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Governing));
        labelHeaderSoldiers.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Soldiers));
        labelHeaderContribution.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Contribution));
        labelHeaderPrestige.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Prestige));
        labelHeaderLoyalty.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Loyalty));
    }

    /// <summary>
    /// ヘッダーがクリックされた時の処理
    /// </summary>
    private void OnHeaderClick(SortColumn column)
    {
        if (currentSortColumn == column)
        {
            // 同じ列の場合はソート状態を循環
            currentSortState = currentSortState switch
            {
                SortState.None => SortState.Descending,
                SortState.Descending => SortState.Ascending,
                SortState.Ascending => SortState.None,
                _ => SortState.Descending
            };
        }
        else
        {
            // 別の列の場合は降順から開始
            currentSortColumn = column;
            currentSortState = SortState.Descending;
        }

        RefreshData();
        UpdateSortIndicators();
    }

    /// <summary>
    /// データを更新（ソートを適用）
    /// </summary>
    private void RefreshData()
    {
        if (currentSortState == SortState.None || currentSortColumn == null)
        {
            // ソートなしの場合は元の順序
            charas = originalCharas.ToList();
        }
        else
        {
            // ソートを適用
            var sorted = SortCharacters(originalCharas, currentSortColumn.Value, currentSortState == SortState.Ascending);
            charas = sorted.ToList();
        }

        ListView.itemsSource = charas;
    }

    /// <summary>
    /// キャラクターリストをソート
    /// </summary>
    private IEnumerable<Character> SortCharacters(IEnumerable<Character> characters, SortColumn column, bool ascending)
    {
        IOrderedEnumerable<Character> ordered = column switch
        {
            SortColumn.Name => ascending ? characters.OrderBy(c => c.Name) : characters.OrderByDescending(c => c.Name),
            SortColumn.Attack => ascending ? characters.OrderBy(c => c.Attack) : characters.OrderByDescending(c => c.Attack),
            SortColumn.Defence => ascending ? characters.OrderBy(c => c.Defense) : characters.OrderByDescending(c => c.Defense),
            SortColumn.Intelligence => ascending ? characters.OrderBy(c => c.Intelligence) : characters.OrderByDescending(c => c.Intelligence),
            SortColumn.Governing => ascending ? characters.OrderBy(c => c.Governing) : characters.OrderByDescending(c => c.Governing),
            SortColumn.Soldiers => ascending ? characters.OrderBy(c => c.Soldiers.SoldierCount) : characters.OrderByDescending(c => c.Soldiers.SoldierCount),
            SortColumn.Contribution => ascending ? characters.OrderBy(c => c.Contribution) : characters.OrderByDescending(c => c.Contribution),
            SortColumn.Prestige => ascending ? characters.OrderBy(c => c.Prestige) : characters.OrderByDescending(c => c.Prestige),
            SortColumn.Loyalty => ascending ? characters.OrderBy(c => c.Loyalty) : characters.OrderByDescending(c => c.Loyalty),
            _ => ascending ? characters.OrderBy(c => c.Name) : characters.OrderByDescending(c => c.Name)
        };
        return ordered;
    }

    /// <summary>
    /// ソートインジケーターを更新
    /// </summary>
    private void UpdateSortIndicators()
    {
        // 全ヘッダーからソート表示を削除
        ClearSortIndicator(labelHeaderName);
        ClearSortIndicator(labelHeaderAttack);
        ClearSortIndicator(labelHeaderDefence);
        ClearSortIndicator(labelHeaderIntelligence);
        ClearSortIndicator(labelHeaderGoverning);
        ClearSortIndicator(labelHeaderSoldiers);
        ClearSortIndicator(labelHeaderContribution);
        ClearSortIndicator(labelHeaderPrestige);
        ClearSortIndicator(labelHeaderLoyalty);

        // 現在のソート列にインジケーターを追加
        if (currentSortColumn.HasValue && currentSortState != SortState.None)
        {
            var headerLabel = GetHeaderLabel(currentSortColumn.Value);
            if (headerLabel != null)
            {
                SetSortIndicator(headerLabel, currentSortState);
            }
        }
    }

    /// <summary>
    /// 指定された列のヘッダーラベルを取得
    /// </summary>
    private Label GetHeaderLabel(SortColumn column) => column switch
    {
        SortColumn.Name => labelHeaderName,
        SortColumn.Attack => labelHeaderAttack,
        SortColumn.Defence => labelHeaderDefence,
        SortColumn.Intelligence => labelHeaderIntelligence,
        SortColumn.Governing => labelHeaderGoverning,
        SortColumn.Soldiers => labelHeaderSoldiers,
        SortColumn.Contribution => labelHeaderContribution,
        SortColumn.Prestige => labelHeaderPrestige,
        SortColumn.Loyalty => labelHeaderLoyalty,
        _ => null
    };

    /// <summary>
    /// ソートインジケーターを設定
    /// </summary>
    private void SetSortIndicator(Label label, SortState state)
    {
        var originalText = label.text.Replace("↑", "").Replace("↓", "");
        label.text = state switch
        {
            SortState.Ascending => originalText + "↑",
            SortState.Descending => originalText + "↓",
            _ => originalText
        };
    }

    /// <summary>
    /// ソートインジケーターをクリア
    /// </summary>
    private void ClearSortIndicator(Label label)
    {
        if (label != null)
        {
            label.text = label.text.Replace("↑", "").Replace("↓", "");
        }
    }
}