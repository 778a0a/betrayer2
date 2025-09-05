using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CountryTable : MainUIComponent
{
    public event EventHandler<Country> RowMouseDown;
    public event EventHandler<int> RowMouseEnter;
    public event EventHandler<int> RowMouseLeave;

    private List<Country> countries;
    private Predicate<Country> clickable;

    public void Initialize()
    {
        ListView.selectionType = SelectionType.None;
        ListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        ListView.makeItem = () =>
        {
            var element = UI.Assets.CountryTableRowItem.Instantiate();
            var row = new CountryTableRowItem(element);
            row.Initialize();
            row.MouseDown += OnRowMouseDown;
            row.MouseEnter += OnRowMouseEnter;
            row.MouseLeave += OnRowMouseLeave;
            element.userData = row;
            Debug.Log("makeItem: " + element.name);
            return element;
        };
        ListView.bindItem = (element, index) =>
        {
            var item = (CountryTableRowItem)element.userData;
            item.SetData(countries[index], clickable?.Invoke(countries[index]) ?? false);
            Debug.Log("bindItem: " + index + " -> " + countries[index]?.Ruler?.Name);
        };
    }

    private void OnRowMouseDown(object sender, Country e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    private void OnRowMouseEnter(object sender, Country e)
    {
        var index = countries.IndexOf(e);
        if (index >= 0)
        {
            RowMouseEnter?.Invoke(this, index);
        }
    }

    private void OnRowMouseLeave(object sender, Country e)
    {
        var index = countries.IndexOf(e);
        if (index >= 0)
        {
            RowMouseLeave?.Invoke(this, index);
        }
    }

    public void SetData(IEnumerable<Country> countries, bool clickable) => SetData(countries, _ => clickable);
    public void SetData(IEnumerable<Country> countries, Predicate<Country> clickable = null)
    {
        this.countries = countries?.ToList() ?? new List<Country>();
        this.clickable = clickable ?? (_ => false);
        ListView.itemsSource = this.countries;
    }
}