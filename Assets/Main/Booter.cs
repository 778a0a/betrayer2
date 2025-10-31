using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームの初期化を担当します。
/// </summary>
public class Booter : MonoBehaviour
{
    private static MainSceneStartArguments s_args;
    public static AsyncOperation LoadScene(MainSceneStartArguments args)
    {
        s_args = args;
        return SceneManager.LoadSceneAsync("MainScene");
    }


    /// <summary>
    /// マップ
    /// </summary>
    [SerializeField] private UIMapManager map;
    /// <summary>
    /// UI
    /// </summary>
    [SerializeField] private MainUI ui;
    /// <summary>
    /// プレーヤー名（テスト用）
    /// </summary>
    [SerializeField] private string testPlayerName = "フレデリック";
    [SerializeField] private bool testSkipPlayerSelection = false;
    [SerializeField] private string testScenarioNo = "02";
    [SerializeField] private bool testClearSceneArgs = true;
    public bool testClearedFlagOn = false;
    /// <summary>
    /// 1tickの待機時間
    /// </summary>
    public float TickWait { get; set; }
    /// <summary>
    /// 一時停止するならtrue
    /// </summary>
    public bool hold = false;

    private GameCore core;
    private bool mainLoopStarted = false;

    void Start()
    {
        UpdatePlaySpeed(SystemSetting.Instance.PlaySpeed);

        var args = s_args ?? new MainSceneStartArguments()
        {
            IsNewGame = true,
            NewGameSaveDataSlotNo = 0,
            NewGameScenarioNo = testScenarioNo,
        };
        if (!Application.isEditor || testClearSceneArgs)
        {
            s_args = null;
        }

        if (Application.isEditor)
        {
            Random.InitState(42);
        }

        // はじめから
        if (args.IsNewGame)
        {
            // シナリオを読み込む。
            var world = DefaultData.Create(args.NewGameScenarioNo);

            world.Map.AttachUIMap(map);
            core = new GameCore(world, map, ui, this);
            core.ScenarioName = $"シナリオ{args.NewGameScenarioNo.TrimStart('0')}";
            core.SaveDataSlotNo = args.NewGameSaveDataSlotNo;

            // プレーヤーを選択する。
            if (Application.isEditor && testSkipPlayerSelection)
            {
                var player = world.Characters.FirstOrDefault(c => c.Name == testPlayerName);
                if (player != null) world.SetPlayer(player);
                core.DoMainLoop().Forget();
                mainLoopStarted = true;
            }
            else
            {
                // 操作キャラを選択してもらう。
                core.MainUI.SelectPlayerCharacterScreen.Show(world, chara =>
                {
                    Debug.Log($"Player selected: {chara?.Name}");
                    world.SetPlayer(chara);
                    core.DoMainLoop().Forget();
                    mainLoopStarted = true;
                });
                MessageWindow.Show(
                    "操作キャラを選択してください。\n\n" +
                    "<color=#aaa>※マップ操作について<size=12>\n\n</size>" +
                    "・右クリックしながらドラッグでスクロールできます\n" +
                    "・マウスホイールでズームできます\n" +
                    "</color>");
            }
        }
        // 再開
        else
        {
            // セーブデータから復元する。
            var world = args.SaveData.RestoreWorldData();

            world.Map.AttachUIMap(map);
            core = new GameCore(world, map, ui, this);
            core.ScenarioName = args.SaveData.Summary.ScenarioName;
            core.SaveDataSlotNo = args.SaveData.Summary.SaveDataSlotNo;
            core.RestoringPhase = args.SaveData.Summary.SaveTiming;
            core.DoMainLoop().Forget();
            mainLoopStarted = true;
        }
    }

    /// <summary>
    /// ホールド解除されるまで待機します。
    /// </summary>
    /// <returns></returns>
    public async ValueTask HoldIfNeeded()
    {
        while (hold)
        {
            await Awaitable.WaitForSecondsAsync(0.1f);
        }
    }

    /// <summary>
    /// 再生速度を更新します。
    /// </summary>
    public void UpdatePlaySpeed(float wait)
    {
        TickWait = wait;
        Debug.Log($"TickWait: {TickWait}");
    }

    void Update()
    {
        // スペースキーで進行フェイズの再生/停止を切り替え
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (core != null && mainLoopStarted && core.MainUI.ActionScreen.IsProgressPhase)
            {
                core.TogglePlay();
            }
        }
    }

}

public class MainSceneStartArguments
{
    public bool IsNewGame { get; set; }
    public int NewGameSaveDataSlotNo { get; set; }
    public string NewGameScenarioNo { get; set; }
    public SaveData SaveData { get; set; }
}

public enum MainSceneStartMode
{
    NewGame,
    ResumeFromLocalData,
    ResumeFromTextData
}
