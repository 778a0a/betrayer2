using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    [field: SerializeField] public MainUIVisualTreeAssetManager Assets { get; private set; }
    [field: SerializeField] public LocalizationManager L { get; private set; }

    public ActionScreen ActionScreen { get; set; }
    public SelectCharacterScreen SelectCharacterScreen { get; set; }
    public SelectPlayerCharacterScreen SelectPlayerCharacterScreen { get; set; }
    public BonusScreen BonusScreen { get; set; }
    public SelectCastleScreen SelectCastleScreen { get; set; }
    public SelectCountryScreen SelectCountryScreen { get; set; }
    public TransportScreen TransportScreen { get; set; }
    public IScreen[] Screens { get; set; }

    private bool isInitialized = false;
    private IScreen lastVisibleScreen = null;

    public CameraMovement Camera { get; set; }

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

        MessageWindow.Initialize();
        BattleWindow.Initialize();
        SystemMenuWindow.Initialize();
        SystemSettingWindow.Initialize();

        // CameraMovementAreaを初期化
        Camera = FindObjectOfType<CameraMovement>();
        CameraMovementArea.Initialize(Camera);
        
        Assets.InitializeScreens(this, isInitialized);

        if (isInitialized)
        {
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