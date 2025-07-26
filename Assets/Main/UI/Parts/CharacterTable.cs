using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTable : MainUIComponent
{
    public event EventHandler<Character> RowMouseMove;
    public event EventHandler<Character> RowMouseDown;

    private List<Character> charas;
    private Predicate<Character> clickable;

    public void Initialize()
    {
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
        this.charas = charas?.ToList() ?? new List<Character>();
        this.clickable = clickable ?? (_ => false);
        ListView.itemsSource = this.charas;
    }
}