using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleTable : MainUIComponent
{
    public event EventHandler<Castle> RowMouseDown;
    public event EventHandler<int> RowMouseEnter;
    public event EventHandler<int> RowMouseLeave;

    private List<Castle> castles;
    private Predicate<Castle> clickable;
    private Castle selectedCastle;

    public void Initialize()
    {
        ListView.selectionType = SelectionType.None;
        ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        ListView.makeItem = () =>
        {
            var element = UI.Assets.CastleTableRowItem.Instantiate();
            var row = new CastleTableRowItem(element);
            row.Initialize();
            row.MouseDown += OnRowMouseDown;
            row.MouseEnter += OnRowMouseEnter;
            row.MouseLeave += OnRowMouseLeave;
            element.userData = row;
            //Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (CastleTableRowItem)element.userData;
            var castle = castles[index];
            var isClickable = clickable?.Invoke(castle) ?? false;
            item.SetData(castle, isClickable, selectedCastle == castle);
            //Debug.Log("bindItem: " + index + " -> " + castles[index]?.Name);
        };
    }

    private void OnRowMouseDown(object sender, Castle e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    private void OnRowMouseEnter(object sender, Castle e)
    {
        var index = castles.IndexOf(e);
        if (index >= 0)
        {
            RowMouseEnter?.Invoke(this, index);
        }
    }

    private void OnRowMouseLeave(object sender, Castle e)
    {
        var index = castles.IndexOf(e);
        if (index >= 0)
        {
            RowMouseLeave?.Invoke(this, index);
        }
    }

    public void SetData(IEnumerable<Castle> castles, bool clickable) => SetData(castles, _ => clickable);
    public void SetData(IEnumerable<Castle> castles, Predicate<Castle> clickable = null)
    {
        this.castles = castles?.ToList() ?? new List<Castle>();
        this.clickable = clickable ?? (_ => false);
        if (selectedCastle != null && !this.castles.Contains(selectedCastle))
        {
            selectedCastle = null;
        }

        ListView.itemsSource = this.castles;
        ListView?.RefreshItems();
    }

    public void SetSelection(Castle castle)
    {
        if (castle != null && !castles.Contains(castle))
        {
            return;
        }

        if (selectedCastle == castle) return;
        selectedCastle = castle;
        ListView?.RefreshItems();
    }

    public void ClearSelection()
    {
        if (selectedCastle == null) return;
        selectedCastle = null;
        ListView?.RefreshItems();
    }

    public Castle GetSelectedCastle() => selectedCastle;
}
