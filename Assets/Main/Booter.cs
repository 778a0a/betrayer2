using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// ゲームの初期化を担当します。
/// </summary>
public class Booter : MonoBehaviour
{
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
    /// <summary>
    /// 1tickの待機時間
    /// </summary>
    public float TickWait { get; set; }
    /// <summary>
    /// 一時停止するならtrue
    /// </summary>
    public bool hold = false;

    /// <summary>
    /// ゲームの再生速度のインデックス
    /// </summary>
    public int PlaySpeedIndex { get; private set; } = 3;
    /// <summary>
    /// 再生速度のテーブル
    /// </summary>
    private float[] PlaySpeedTable { get; } = new[] { 0.5f, 0.25f, 0.125f, 0.05f, 0f, };

    private GameCore core;

    void Start()
    {
        UpdatePlaySpeed(PlaySpeedIndex);

        // ワールドを作成する。
        var world = DefaultData.Create("02");
        world.Map.AttachUIMap(map);
        
        // プレーヤーをセットする。
        var player = world.Characters.FirstOrDefault(c => c.Name == testPlayerName);
        if (player != null) world.SetPlayer(player);

        // ゲームループを開始する。
        core = new GameCore(world, map, ui, this);
        core.DoMainLoop().Forget();
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
    public void UpdatePlaySpeed(int index)
    {
        PlaySpeedIndex = index;
        TickWait = PlaySpeedTable[index];
        Debug.Log($"PlaySpeedIndex: {PlaySpeedIndex}, TickWait: {TickWait}");
    }

}

