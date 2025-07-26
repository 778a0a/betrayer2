using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public class MainUIComponent
{
    protected GameCore Core => GameCore.Instance;
    protected MainUI UI => Core.MainUI;
}

