using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    [field: SerializeField] public MainUIVisualTreeAssetManager Assets { get; private set; }
    [field: SerializeField] public LocalizationManager L { get; private set; }

    public PersonalPhaseScreen PersonalPhaseScreen { get; set; }
    public StrategyPhaseScreen StrategyPhaseScreen { get; set; }
    public SelectCharacterScreen SelectCharacterScreen { get; set; }
    public IScreen[] Screens { get; set; }

    private void OnEnable()
    {
        Debug.Log("MainUI.OnEnable");
        InitializeDocument();
        Assets.InitializeScreens(this);
        BattleWindow.Initialize();
        Frame.Initialize();

        Debug.Log($"USERDATA: {SelectCharacterScreen.Root.userData}");
        if (SelectCharacterScreen.Root.userData == null)
        {
            SelectCharacterScreen.Root.userData = $"TEST{DateTime.Now}";
        }
        Debug.Log($"SET USERDATA: {SelectCharacterScreen.Root.userData}");
    }

    private void OnDisable()
    {
        Debug.Log("MainUI.OnDisable");
        Debug.Log($"D USERDATA: {SelectCharacterScreen.Root.userData}");
        foreach (var screen in Screens)
        {
        }
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