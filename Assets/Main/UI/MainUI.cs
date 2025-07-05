using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    public static MainUI Instance { get; private set; }

    [field: SerializeField] public LocalizationManager L { get; private set; }
    [SerializeField] private VisualTreeAsset[] panelVisualTreeAssets;

    public PersonalPhasePanel PersonalPhasePanel { get; private set; }
    public StrategyPhasePanel StrategyPhasePanel { get; private set; }
    public SelectCharacterPanel SelectCharacterPanel { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private bool isInitialized = false;
    private void OnEnable()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            InitializeDocument();
            InitializePanels();
            BattleWindow.Initialize();
            Frame.Initialize();
        }
    }

    private void InitializePanels()
    {
        var panelProps = GetType()
            .GetProperties()
            .Where(p => p.PropertyType.GetInterface(nameof(IPanel)) != null);

        foreach (var prop in panelProps)
        {
            var asset = panelVisualTreeAssets.FirstOrDefault(a => a.name == prop.Name);
            if (asset == null)
            {
                Debug.LogError($"VisualTreeAsset not found: {prop.Name}");
                continue;
            }

            var element = asset.Instantiate();
            element.style.display = DisplayStyle.None;
            var constructor = prop.PropertyType.GetConstructor(new[] { typeof(VisualElement) });
            var panel = constructor.Invoke(new[] { element }) as IPanel;
            panel.Initialize();
            prop.SetValue(this, panel);
            UIContainer.Add(element);
        }
        if (panelProps.Any(p => p.GetValue(this) == null))
        {
            // ゲームを強制終了する。
            Debug.LogError("UI Documentが足りないので強制終了します。");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    public void ShowPersonalPhasePanel(Character chara, WorldData world)
    {
        HideAllPanels();
        PersonalPhasePanel.Root.style.display = DisplayStyle.Flex;
        PersonalPhasePanel.SetData(chara, world);
    }

    public void ShowStrategyPhasePanel(Character chara, WorldData world)
    {
        HideAllPanels();
        StrategyPhasePanel.Root.style.display = DisplayStyle.Flex;
        StrategyPhasePanel.SetData(chara, world);
    }

    private void HideAllPanels()
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