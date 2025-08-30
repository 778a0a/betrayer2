using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SimpleTable : MainUIComponent
{
    public event EventHandler<object> ItemSelected;
    public event EventHandler<int> RowMouseEnter;
    public event EventHandler<int> RowMouseLeave;

    public IReadOnlyList<object> Items { get; private set; }
    public Func<object, string> ItemToString { get; set; } = obj => obj?.ToString() ?? "";
    private object selectedItem;

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
            // 要素から外されたあとも使いまわしされるのでRegisterではなくRegisterCallbackを使う。
            label.RegisterCallback<MouseEnterEvent>(ev =>
            {
                RowMouseEnter?.Invoke(this, (int)label.userData);
            });
            label.RegisterCallback<MouseLeaveEvent>(ev =>
            {
                RowMouseLeave?.Invoke(this, (int)label.userData);
            });
            return label;
        };

        ListView.bindItem = (element, index) =>
        {
            var label = (Label)element;
            label.text = ItemToString(Items[index]);
            label.userData = index;
        };

        ListView.selectionChanged += selectedItems =>
        {
            var selected = selectedItems.FirstOrDefault();
            if (selected != null)
            {
                selectedItem = selected;
                ItemSelected?.Invoke(this, selectedItem);
            }
        };
    }
    

    public void SetData<T>(IReadOnlyList<T> items, string headerText = "項目", Func<T, string> toString = null)
    {
        Items = items?.Cast<object>().ToList() ?? new List<object>();
        labelHeader.text = headerText;
        ItemToString = obj => toString?.Invoke((T)obj) ?? obj?.ToString() ?? "";
        ListView.itemsSource = (System.Collections.IList)Items;
        ListView.ClearSelection();
        selectedItem = null;
    }

    public object GetSelectedItem()
    {
        return selectedItem;
    }

    public void SetSelectedItem(object item)
    {
        if (Items.Contains(item))
        {
            var index = Items.IndexOf(item);
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