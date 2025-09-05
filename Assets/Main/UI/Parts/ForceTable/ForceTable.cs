using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ForceTable : MainUIComponent
{
    public event EventHandler<Force> RowMouseMove;
    public event EventHandler<Force> RowMouseDown;

    private List<Force> forces;
    private Predicate<Force> clickable;

    public void Initialize()
    {
        ListView.selectionType = SelectionType.None;
        ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        ListView.makeItem = () =>
        {
            var element = UI.Assets.ForceTableRowItem.Instantiate();
            var row = new ForceTableRowItem(element);
            row.Initialize();
            row.MouseDown += OnRowMouseDown;
            row.MouseMove += OnRowMouseMove;
            element.userData = row;
            Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (ForceTableRowItem)element.userData;
            item.SetData(forces[index], clickable?.Invoke(forces[index]) ?? false);
            Debug.Log("bindItem: " + index + " -> " + forces[index]?.Character?.Name);
        };
    }

    private void OnRowMouseMove(object sender, Force e)
    {
        RowMouseMove?.Invoke(this, e);
    }

    private void OnRowMouseDown(object sender, Force e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    public void SetData(IEnumerable<Force> forces, bool clickable) => SetData(forces, _ => clickable);
    public void SetData(IEnumerable<Force> forces, Predicate<Force> clickable = null)
    {
        this.forces = forces?.ToList() ?? new List<Force>();
        this.clickable = clickable ?? (_ => false);
        ListView.itemsSource = this.forces;
    }
}