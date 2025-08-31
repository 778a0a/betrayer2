using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CastleTable : MainUIComponent
{
    public event EventHandler<Castle> RowMouseMove;
    public event EventHandler<Castle> RowMouseDown;

    private List<Castle> castles;
    private Predicate<Castle> clickable;

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
            row.MouseMove += OnRowMouseMove;
            element.userData = row;
            Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (CastleTableRowItem)element.userData;
            item.SetData(castles[index], clickable?.Invoke(castles[index]) ?? false);
            Debug.Log("bindItem: " + index + " -> " + castles[index]?.Name);
        };
    }

    private void OnRowMouseMove(object sender, Castle e)
    {
        RowMouseMove?.Invoke(this, e);
    }

    private void OnRowMouseDown(object sender, Castle e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    public void SetData(IEnumerable<Castle> castles, bool clickable) => SetData(castles, _ => clickable);
    public void SetData(IEnumerable<Castle> castles, Predicate<Castle> clickable = null)
    {
        this.castles = castles?.ToList() ?? new List<Castle>();
        this.clickable = clickable ?? (_ => false);
        ListView.itemsSource = this.castles;
    }
}