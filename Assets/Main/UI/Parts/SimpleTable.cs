using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SimpleTable : MainUIComponent
{
    public event EventHandler<string> ItemSelected;

    private List<string> items;
    private string selectedItem;

    public void Initialize()
    {
        ListView.selectionType = SelectionType.Single;
        ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

        // なぜか明示的にドラッグ禁止にしておかないと、2回目のリスト表示時にマウスオーバーで選択イベントが発生してしまう。
        ListView.canStartDrag += (evt) => false; // ドラッグ禁止

        ListView.makeItem = () =>
        {
            var label = new Label();
            label.AddToClassList("SimpleTableRowItem");
            return label;
        };
        
        ListView.bindItem = (element, index) =>
        {
            var label = (Label)element;
            label.text = items[index];
        };
        
        ListView.selectionChanged += OnSelectionChange;
    }

    private void OnSelectionChange(IEnumerable<object> selectedItems)
    {
        var selected = selectedItems.FirstOrDefault();
        if (selected != null)
        {
            selectedItem = selected.ToString();
            ItemSelected?.Invoke(this, selectedItem);
        }
    }

    public void SetData(IEnumerable<string> items, string headerText = "項目")
    {
        this.items = items?.ToList() ?? new List<string>();
        labelHeader.text = headerText;
        ListView.itemsSource = this.items;
        ListView.ClearSelection();
        selectedItem = null;
    }

    public string GetSelectedItem()
    {
        return selectedItem;
    }

    public void SetSelectedItem(string item)
    {
        if (items.Contains(item))
        {
            var index = items.IndexOf(item);
            ListView.SetSelection(index);
            selectedItem = item;
        }
    }

    public void ClearSelection()
    {
        ListView.ClearSelection();
        selectedItem = null;
    }
}