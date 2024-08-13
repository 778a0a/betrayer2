using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;

public class GameCore
{
    public static GameCore Instance { get; set; }

    public Testing test { get; set; }
    public MainUI UI => MainUI.Instance;

    public GameDate GameDate { get; set; }

    public GameCore()
    {
        Instance = this;
        GameDate = new GameDate(0);
    }

    public async ValueTask DoMainLoop()
    {
        try
        {
            while (true)
            {
                //UI.Frame.SetData(this);
                await Awaitable.WaitForSecondsAsync(test.tickWait);

                await test.HoldIfNeeded();
                GameDate++;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("メインループでエラー");
            Debug.LogException(ex);
        }
    }

    public void TogglePlay()
    {
        test.hold = !test.hold;
        //UI.Frame.SetData(this);
    }
}