using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTable : MainUIComponent
{
    public event EventHandler<Character> RowMouseMove;
    public event EventHandler<Character> RowMouseDown;
    public event EventHandler<List<Character>> SelectionChanged;

    private List<Character> originalCharas;
    private List<Character> charas;
    private Predicate<Character> clickable;

    // 複数選択機能
    private bool isMultiSelectMode = false;
    private HashSet<Character> selectedCharacters = new HashSet<Character>();

    // ソート状態管理
    private enum SortState { None, Ascending, Descending }
    private enum SortColumn { Name, Attack, Defence, Intelligence, Governing, Soldiers, Contribution, Prestige, Loyalty, Importance, OrderIndex, Role, Castle }

    private SortColumn? currentSortColumn;
    private SortState currentSortState = SortState.None;

    // 表示切替機能
    private bool isAlternateDisplayMode = false;

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
            //Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (CharacterTableRowItem)element.userData;
            var character = charas[index];
            var isClickable = clickable?.Invoke(character) ?? false;
            var isSelected = selectedCharacters.Contains(character);
            item.SetData(character, isClickable, isSelected, isAlternateDisplayMode);
            //Debug.Log("bindItem: " + index + " -> " + character?.Name);
        };

        // ヘッダークリック処理を設定
        SetupHeaderClickHandlers();

        // 切替ボタンのクリック処理を設定
        buttonToggleDisplay.RegisterCallback<ClickEvent>(OnToggleDisplayClick);
    }

    private void OnRowMouseMove(object sender, Character e)
    {
        RowMouseMove?.Invoke(this, e);
    }

    private void OnRowMouseDown(object sender, Character e)
    {
        if (isMultiSelectMode)
        {
            ToggleSelection(e);
        }
        else
        {
            RowMouseDown?.Invoke(this, e);
        }
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
        labelHeaderImportance.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Importance));
        labelHeaderOrderIndex.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.OrderIndex));
        labelHeaderRole.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Role));
        labelHeaderCastle.RegisterCallback<ClickEvent>(evt => OnHeaderClick(SortColumn.Castle));
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
            SortColumn.Importance => ascending ? characters.OrderBy(c => c.Importance) : characters.OrderByDescending(c => c.Importance),
            SortColumn.OrderIndex => ascending ? characters.OrderBy(c => c.OrderIndex) : characters.OrderByDescending(c => c.OrderIndex),
            SortColumn.Role => ascending ? characters.OrderBy(c => GetRoleSortKey(c)) : characters.OrderByDescending(c => GetRoleSortKey(c)),
            SortColumn.Castle => ascending ? characters.OrderBy(c => c.Castle.Name) : characters.OrderByDescending(c => c.Castle.Name),
            _ => ascending ? characters.OrderBy(c => c.Name) : characters.OrderByDescending(c => c.Name)
        };
        return ordered;
    }

    /// <summary>
    /// 役職のソートキーを取得（君主→城主→一般→浪士の順）
    /// </summary>
    private int GetRoleSortKey(Character character)
    {
        if (character.IsRuler) return 100;
        if (character.IsBoss) return 10;
        if (character.IsVassal) return 1;
        return 0; // 浪士
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
        ClearSortIndicator(labelHeaderImportance);
        ClearSortIndicator(labelHeaderOrderIndex);
        ClearSortIndicator(labelHeaderRole);
        ClearSortIndicator(labelHeaderCastle);

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
        SortColumn.Importance => labelHeaderImportance,
        SortColumn.OrderIndex => labelHeaderOrderIndex,
        SortColumn.Role => labelHeaderRole,
        SortColumn.Castle => labelHeaderCastle,
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

    /// <summary>
    /// 複数選択モードを設定
    /// </summary>
    public void SetMultiSelectMode(bool enabled)
    {
        isMultiSelectMode = enabled;
        if (!enabled)
        {
            selectedCharacters.Clear();
        }
        RefreshDisplay();
    }

    /// <summary>
    /// 選択状態をトグル
    /// </summary>
    private void ToggleSelection(Character character)
    {
        if (!(clickable?.Invoke(character) ?? false)) return;

        if (selectedCharacters.Contains(character))
        {
            selectedCharacters.Remove(character);
        }
        else
        {
            selectedCharacters.Add(character);
        }
        
        RefreshDisplay();
        SelectionChanged?.Invoke(this, selectedCharacters.ToList());
    }

    /// <summary>
    /// 選択をクリア
    /// </summary>
    public void ClearSelection()
    {
        selectedCharacters.Clear();
        RefreshDisplay();
        SelectionChanged?.Invoke(this, selectedCharacters.ToList());
    }

    /// <summary>
    /// 現在の選択を取得
    /// </summary>
    public List<Character> GetSelectedCharacters()
    {
        return selectedCharacters.ToList();
    }

    /// <summary>
    /// 選択を設定
    /// </summary>
    public void SetSelection(IEnumerable<Character> characters)
    {
        selectedCharacters.Clear();
        foreach (var character in characters)
        {
            selectedCharacters.Add(character);
        }
        RefreshDisplay();
        SelectionChanged?.Invoke(this, selectedCharacters.ToList());
    }

    /// <summary>
    /// 表示を更新
    /// </summary>
    private void RefreshDisplay()
    {
        ListView.RefreshItems();
    }

    /// <summary>
    /// 表示切替ボタンがクリックされた時の処理
    /// </summary>
    private void OnToggleDisplayClick(ClickEvent evt)
    {
        isAlternateDisplayMode = !isAlternateDisplayMode;
        UpdateDisplayMode();
    }

    /// <summary>
    /// 表示モードを更新
    /// </summary>
    private void UpdateDisplayMode()
    {
        if (isAlternateDisplayMode)
        {
            // 切替後の表示（序列・役職・所属城）
            labelHeaderAttack.style.display = DisplayStyle.None;
            labelHeaderDefence.style.display = DisplayStyle.None;
            labelHeaderIntelligence.style.display = DisplayStyle.None;
            labelHeaderGoverning.style.display = DisplayStyle.None;
            labelHeaderSoldiers.style.display = DisplayStyle.None;
            labelHeaderContribution.style.display = DisplayStyle.None;
            labelHeaderPrestige.style.display = DisplayStyle.None;

            labelHeaderImportance.style.display = DisplayStyle.Flex;
            labelHeaderOrderIndex.style.display = DisplayStyle.Flex;
            labelHeaderRole.style.display = DisplayStyle.Flex;
            labelHeaderCastle.style.display = DisplayStyle.Flex;
        }
        else
        {
            // 通常表示（ステータス）
            labelHeaderAttack.style.display = DisplayStyle.Flex;
            labelHeaderDefence.style.display = DisplayStyle.Flex;
            labelHeaderIntelligence.style.display = DisplayStyle.Flex;
            labelHeaderGoverning.style.display = DisplayStyle.Flex;
            labelHeaderSoldiers.style.display = DisplayStyle.Flex;
            labelHeaderContribution.style.display = DisplayStyle.Flex;
            labelHeaderPrestige.style.display = DisplayStyle.Flex;

            labelHeaderImportance.style.display = DisplayStyle.None;
            labelHeaderOrderIndex.style.display = DisplayStyle.None;
            labelHeaderRole.style.display = DisplayStyle.None;
            labelHeaderCastle.style.display = DisplayStyle.None;
        }

        // 各行の表示も更新
        RefreshDisplay();
    }
}