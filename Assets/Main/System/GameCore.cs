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

    public WorldData World { get; }
    public UIMapManager Map { get; }
    public MainUI MainUI { get; }
    public Testing test { get; }

    public CastleActions CastleActions { get; }
    public TownActions TownActions { get; }

    public GameDate GameDate { get; set; }

    public GameCore(WorldData world, UIMapManager map, MainUI mainui, Testing test)
    {
        Instance = this;
        World = world;
        Map = map;
        MainUI = mainui;
        this.test = test;
        GameDate = new GameDate(0);

        CastleActions = new CastleActions(this);
        TownActions = new TownActions(this);

        MainUI.OnGameCoreAttached();
    }

    public void TogglePlay()
    {
        test.hold = !test.hold;
        MainUI.Frame.SetDatePanelData(this);
    }

    public async ValueTask DoMainLoop()
    {
        try
        {
            while (true)
            {
                await Tick();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("メインループでエラー");
            Debug.LogException(ex);
        }
    }

    /// <summary>
    /// 一日の更新処理
    /// </summary>
    private async ValueTask Tick()
    {
        await Awaitable.WaitForSecondsAsync(test.TickWait);

        var player = World.Player;

        // 収入月の場合
        if (GameDate.IsIncomeMonth)
        {
            OnIncome();
        }

        World.Forces.OnForceMove(this);

        await OnCharacterMove(player);

        MainUI.Frame.SetDatePanelData(this);
        MainUI.Frame.SetPlayerPanelData(player);
        await test.HoldIfNeeded();

        GameDate++;
    }

    /// <summary>
    /// 収支計算を行う。
    /// </summary>
    private void OnIncome()
    {
        Debug.Log("収支計算");
        foreach (var castle in World.Countries.SelectMany(c => c.Castles))
        {
            // 町の収入
            foreach (var town in castle.Towns)
            {
                castle.Gold += town.GoldIncome;
                castle.Food += town.FoodIncome;
            }
            // キャラ・軍隊への支払い
            foreach (var chara in castle.Members)
            {
                // 給料支出
                castle.Gold -= chara.Salary;
                chara.Gold += chara.Salary;
                // 食料消費
                castle.Food -= chara.FoodConsumption;
                // TODO ゴールド・食料が足りない場合
            }
        }
        // 未所属のキャラはランダムに収入を得る。
        foreach (var chara in World.Characters.Where(c => c.IsFree))
        {
            chara.Gold += UnityEngine.Random.Range(1, 10);
        }

        // 行動ポイントを補充する。
        foreach (var chara in World.Characters)
        {
            chara.ActionPoints = Mathf.Min(255, chara.ActionPoints + chara.Intelligence / 10);
        }

        // UIを更新する。
        if (MainUI.TileDetail.IsVisible)
        {
            MainUI.TileDetail.SetData(MainUI.TileDetail.CurrentData);
        }
    }

    private readonly Lazy<ActionBase[]> vassalActions = new(() => new ActionBase[]
    {
        Instance.TownActions.ImproveGoldIncome,
        Instance.TownActions.ImproveFoodIncome,
        Instance.CastleActions.ImproveCastleStrength,
        Instance.CastleActions.TrainSoldiers,
    });

    /// <summary>
    /// キャラクターの行動を行う。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private async Task OnCharacterMove(Character player)
    {
        // 収入月の場合は未所属キャラを移動させる。
        if (GameDate.IsIncomeMonth && GameDate.Day == 1)
        {
            foreach (var chara in World.Characters.Where(c => c.IsFree))
            {
                if (chara == player) continue;

                // ランダムに拠点を移動する。
            }
        }

        // 月初の場合
        if (GameDate.Day == 1)
        {
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;
                if (chara.IsFree) continue;

                // 所属ありの場合
                var castle = chara.Castle;

                // 君主の場合
                if (chara.IsRuler)
                {
                    // 収入月の場合
                    if (GameDate.IsIncomeMonth)
                    {
                        // 各城の方針を設定する。
                    }
                    // 外交を行う。
                    // 同盟
                    // 親善
                }
                // 城主の場合（君主も含む）
                if (castle.Boss == chara)
                {
                    // 採用を行うか判定する。
                    // 追放を行うか判定する。
                    // 町建設・城増築・投資を行うか判定する。
                    // 君主でない場合反乱を起こすか判定する。
                    // 進軍を行うか判定する。
                    // 売買を行うか判定する。
                    // 挑発を行うか判定する。
                    // 反乱を起こすか判定する。
                }
                // 配下の場合
                else
                {
                    // 反乱を起こすか判定する。
                }
            }
        }

        // 15日毎に行動を行う。
        if (GameDate.Day == 15 || GameDate.Day == 30)
        {
            // 収入の1/6分、農業・商業・築城・訓練をランダムに行う。
            var args = new ActionArgs();
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;
                if (chara.IsMoving) continue;

                args.Character = chara;

                var action = default(ActionBase);
                if (chara.IsFree)
                {
                    args.Castle = null;
                    args.Town = null;
                    action = CastleActions.TrainSoldiers;
                }
                else
                {
                    args.Castle = chara.Castle;
                    args.Town = args.Castle?.Towns.RandomPick();
                    action = vassalActions.Value.RandomPick();
                }

                var budget = Math.Min(chara.Gold, chara.Salary / 6);
                do
                {
                    if (!action.CanDo(args)) break;
                    budget -= action.Cost(args);
                    await action.Do(args);
                }
                while (budget > 0);
            }
        }
    }

}