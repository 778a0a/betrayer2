using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    [field: SerializeField] public MainUIVisualTreeAssetManager Assets { get; private set; }
    [field: SerializeField] public LocalizationManager L { get; private set; }

    public PersonalPhasePanel PersonalPhasePanel { get; private set; }
    public StrategyPhasePanel StrategyPhasePanel { get; private set; }
    public SelectCharacterPanel SelectCharacterPanel { get; private set; }

    private void OnEnable()
    {
        InitializeDocument();
        Assets.InitializePanels(this);
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

public interface IPanel
{
    void Initialize();
}