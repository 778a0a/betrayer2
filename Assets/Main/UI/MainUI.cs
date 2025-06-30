using System;
using UnityEngine;

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
    }
}