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
        foreach (var row in Rows)
        {
            row.Initialize();
            row.MouseDown += OnRowMouseDown;
            row.MouseMove += OnRowMouseMove;
        }
    }

    private void OnRowMouseMove(object sender, Character e)
    {
        RowMouseMove?.Invoke(this, e);
    }

    private void OnRowMouseDown(object sender, Character e)
    {
        RowMouseDown?.Invoke(this, e);
    }

    private const int RowCount = 10;
    public IEnumerable<CharacterTableRowItem> Rows => Enumerable.Range(0, RowCount).Select(RowOf);
    private CharacterTableRowItem RowOf(int index) => index switch
    {
        0 => row00,
        1 => row01,
        2 => row02,
        3 => row03,
        4 => row04,
        5 => row05,
        6 => row06,
        7 => row07,
        8 => row08,
        9 => row09,
        _ => throw new ArgumentOutOfRangeException(),
    };

    public void SetData(IEnumerable<Character> charas, WorldData world, bool clickable)
        => SetData(charas, world, _ => clickable);
    public void SetData(IEnumerable<Character> charas, WorldData world, Predicate<Character> clickable = null)
    {
        var en = charas?.GetEnumerator();
        for (int i = 0; i < RowCount; i++)
        {
            if (en?.MoveNext() ?? false)
            {
                RowOf(i).SetData(en.Current, world, clickable?.Invoke(en.Current) ?? false);
            }
            else
            {
                RowOf(i).SetData(null, world, false);
            }
        }
    }
}