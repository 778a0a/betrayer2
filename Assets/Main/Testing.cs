using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

public class Testing : MonoBehaviour
{
    public float tickWait = 0.3f;
    public bool hold = false;

    private GameCore core;

    void Start()
    {
        core = new GameCore();
        core.test = this;
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

