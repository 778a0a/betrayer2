using System;
using UnityEngine;

public partial class MainUI : MonoBehaviour
{
    public static MainUI Instance { get; private set; }

    [field: SerializeField] public LocalizationManager L { get; private set; }

    // ゲームコア参照
    private GameCore gameCore;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        InitializeDocument();
        BattleWindow.Initialize();
        Frame.Initialize();
        TileInfo.Initialize();
        TileDetail.Initialize();
        TileDetail.L = L;
        CharacterInfo.Initialize();
        
        // PersonalPhasePanelの初期化
        PersonalPhasePanel.Initialize();
    }

    public void OnGameCoreAttached()
    {
        TileDetail.OnGameCoreAttached();
        
        // GameCore参照を保存
        gameCore = GameCore.Instance;
        
        // PersonalPhasePanelにGameCoreを設定
        PersonalPhasePanel.SetGameCore(gameCore);
        
        // 初期データ設定
        if (gameCore?.World?.Player != null)
        {
            PersonalPhasePanel.SetData(gameCore.World.Player, gameCore.GameDate);
        }
    }

    /// <summary>
    /// UI表示を更新（GameCoreから呼び出される）
    /// </summary>
    public void UpdateDisplay()
    {
        if (gameCore?.World?.Player != null)
        {
            PersonalPhasePanel.SetData(gameCore.World.Player, gameCore.GameDate);
        }
    }

    /// <summary>
    /// パネル表示の切り替え（将来の拡張用）
    /// </summary>
    public void ShowPersonalPhasePanel()
    {
        PersonalPhasePanel.SetVisible(true);
        // 他のパネルは非表示にする処理を追加予定
    }

    public void HidePersonalPhasePanel()
    {
        PersonalPhasePanel.SetVisible(false);
    }
}