using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class Testing : MonoBehaviour
{
    public float TickWait { get; set; }
    public bool hold = false;

    public int PlaySpeedIndex { get; private set; } = 3;
    private float[] PlaySpeedTable { get; } = new[] { 0.5f, 0.25f, 0.125f, 0.05f, 0f, };
    public void UpdatePlaySpeed(int index)
    {
        PlaySpeedIndex = index;
        TickWait = PlaySpeedTable[index];
        Debug.Log($"PlaySpeedIndex: {PlaySpeedIndex}, TickWait: {TickWait}");
    }

    private GameCore core;

    void Start()
    {
        var world = DefaultData.Create(MapManager.Instance.Map);
        world.Characters.FirstOrDefault(c => c.Name == "フレデリック").IsPlayer = true;

        UpdatePlaySpeed(PlaySpeedIndex);
        core = new GameCore(world, MapManager.Instance, MainUI.Instance, this);
        core.DoMainLoop().Foreget();
    }

    public async ValueTask HoldIfNeeded()
    {
        while (hold)
        {
            await Awaitable.WaitForSecondsAsync(0.1f);
        }
    }
}

