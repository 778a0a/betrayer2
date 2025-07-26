using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    [field: SerializeField] public MainUIVisualTreeAssetManager Assets { get; private set; }
    [field: SerializeField] public LocalizationManager L { get; private set; }

    public PersonalPhaseScreen PersonalPhaseScreen { get; private set; }
    public StrategyPhaseScreen StrategyPhaseScreen { get; private set; }
    public SelectCharacterScreen SelectCharacterScreen { get; private set; }

    private void OnEnable()
    {
        InitializeDocument();
        Assets.InitializeScreens(this);
        BattleWindow.Initialize();
        Frame.Initialize();
    }

    public void HideAllPanels()
    {
        foreach (var child in UIContainer.Children())
        {
            child.style.display = DisplayStyle.None;
        }
    }
}

public interface IScreen
{
    void Initialize();
}