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
    public MapManager Map { get; }
    public MainUI MainUI { get; }
    public Testing test { get; }

    public CastleActions CastleActions { get; }
    public TownActions TownActions { get; }

    public GameDate GameDate { get; set; }

    public GameCore(WorldData world, MapManager map, MainUI mainui, Testing test)
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

        await test.HoldIfNeeded();
        var player = World.Player;

        MainUI.Frame.SetDatePanelData(this);
        MainUI.Frame.SetPlayerPanelData(player);

        // 月初になったら収支計算を行う。
        if (GameDate.Day == 1)
        {
            OnIncome();
        }

        World.Forces.OnForceMove(this);

        await OnCharacterMove(player);

        GameDate++;
    }

    /// <summary>
    /// 収支計算を行う。
    /// </summary>
    private void OnIncome()
    {
        Debug.Log("収支計算");
        // 7月になったら収穫を行う。
        foreach (var castle in World.Countries.SelectMany(c => c.Castles))
        {
            // 町の収入
            foreach (var town in castle.Towns)
            {
                // ゴールド収入
                castle.Gold += town.GoldIncome;
                // 食料収入
                if (GameDate.IsFoodIncomeMonth) castle.Food += town.FoodIncome;
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

    /// <summary>
    /// キャラクターの行動を行う。
    /// </summary>
    /// <param name="player"></param>
    /// <returns></returns>
    private async Task OnCharacterMove(Character player)
    {
        // 10日毎に行動を行う。
        if (GameDate.Day == 10 || GameDate.Day == 20 || GameDate.Day == 30)
        {
            Debug.Log("行動");
            // 収入の1/3分、農業・商業・築城・訓練をランダムに行う。
            var args = new ActionArgs();
            foreach (var chara in World.Characters)
            {
                if (chara == player) continue;

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
                    args.Castle = World.CastleOf(chara);
                    args.Town = args.Castle?.Towns.RandomPick();
                    action = new ActionBase[]
                    {
                                TownActions.ImproveGoldIncome,
                                TownActions.ImproveFoodIncome,
                                CastleActions.ImproveCastleStrength,
                                CastleActions.TrainSoldiers,
                    }.RandomPickDefault();
                }
                var budget = Math.Min(chara.Gold, chara.Salary / 3);
                while (budget > 0)
                {
                    if (!action.CanDo(args)) break;
                    budget -= action.Cost(args);
                    await action.Do(args);
                }
            }
        }
    }

}