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
    public TileInfoScreen TileInfoScreen { get; set; }
    public IScreen[] Screens { get; set; }

    private bool isInitialized = false;
    private IScreen lastVisibleScreen = null;

    private void OnEnable()
    {
        Debug.Log("MainUI.OnEnable");
        if (!isInitialized)
        {
            InitializeDocument();
        }
        else
        {
            ReinitializeDocument();
        }

        Frame.Initialize();
        MessageWindow.Initialize();
        BattleWindow.Initialize();
        
        // CameraMovementAreaを初期化
        var cameraMovement = FindObjectOfType<CameraMovement>();
        CameraMovementArea.Initialize(cameraMovement);
        
        Assets.InitializeScreens(this, isInitialized);

        if (isInitialized)
        {
            Frame.RefreshUI();
            if (lastVisibleScreen != null)
            {
                lastVisibleScreen.Render();
                lastVisibleScreen.Root.style.display = DisplayStyle.Flex;
            }
        }

        isInitialized = true;
    }

    private void OnDisable()
    {
        Debug.Log("MainUI.OnDisable");
        lastVisibleScreen = Screens.FirstOrDefault(s => s.Root.style.display != DisplayStyle.None);
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
    VisualElement Root { get; }
    void ReinitializeComponent(VisualElement element);
 
    void Initialize();
    void Reinitialize();
    void Render();
}