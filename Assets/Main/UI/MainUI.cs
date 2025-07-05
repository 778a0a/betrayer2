using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUI : MonoBehaviour
{
    public static MainUI Instance { get; private set; }

    [field: SerializeField] public LocalizationManager L { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        InitializeDocument();
        BattleWindow.Initialize();
        Frame.Initialize();
        PersonalPhasePanel.Initialize();
        StrategyPhasePanel.Initialize();
    }

    public void ShowPersonalPhasePanel(Character chara, WorldData world)
    {
        PersonalPhasePanel.Root.style.display = DisplayStyle.Flex;
        StrategyPhasePanel.Root.style.display = DisplayStyle.None;
        PersonalPhasePanel.SetData(chara, world);
    }

    public void ShowStrategyPhasePanel(Character chara, WorldData world)
    {
        PersonalPhasePanel.Root.style.display = DisplayStyle.None;
        StrategyPhasePanel.Root.style.display = DisplayStyle.Flex;
        StrategyPhasePanel.SetData(chara, world);
    }
}