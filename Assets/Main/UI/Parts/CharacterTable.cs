using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public partial class CharacterTable
{
    public event EventHandler<Character> RowMouseMove;
    public event EventHandler<Character> RowMouseDown;

    public LocalizationManager L => MainUI.Instance.L;
    public void Initialize()
    {
        L.Register(this);

        ListView.makeItem = () =>
        {
            var element = MainUI.Instance.characterTableRowItem.Instantiate();
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
            item.SetData(charas[index], world, clickable?.Invoke(charas[index]) ?? false);
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

    private WorldData world;
    private List<Character> charas;
    private Predicate<Character> clickable;
    public void SetData(IEnumerable<Character> charas, WorldData world, bool clickable) => SetData(charas, world, _ => clickable);
    public void SetData(IEnumerable<Character> charas, WorldData world, Predicate<Character> clickable = null)
    {
        this.charas = charas?.ToList() ?? new List<Character>();
        this.world = world ?? throw new ArgumentNullException(nameof(world));
        this.clickable = clickable ?? (_ => false);
        ListView.itemsSource = this.charas;
    }
}