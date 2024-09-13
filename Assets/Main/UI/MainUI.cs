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
        Frame.Initialize();
        TileInfo.Initialize();
        TileDetail.Initialize();
        TileDetail.L = L;
        CharacterInfo.Initialize();
    }

    public void OnGameCoreAttached()
    {
        TileDetail.OnGameCoreAttached();
    }
}