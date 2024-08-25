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

    private CastleActions CastleActions { get; }

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
                MainUI.Frame.SetData(this);
                Debug.Log(GameDate);

                // 月初になったら収支計算を行う。
                if (GameDate.Day == 1)
                {
                    Debug.Log("収支計算");
                    // 7月になったら収穫を行う。
                    var needHarvest = GameDate.Month == 6;
                    foreach (var castle in World.Countries.SelectMany(c => c.Castles))
                    {
                        // 町の収入
                        foreach (var town in castle.Towns)
                        {
                            // ゴールド収入
                            castle.Gold += town.GoldIncome;
                            // 食料収入
                            if (needHarvest) castle.Food += town.FoodIncome;
                        }
                        // キャラ・軍隊への支払い
                        foreach (var chara in castle.Members)
                        {
                            // 給料支出
                            castle.Gold -= chara.Salary;
                            chara.Gold += chara.Salary;
                            // 食料消費
                            castle.Food -= chara.Force.Soldiers.Sum(s => s.MaxHp);
                            // TODO ゴールド・食料が足りない場合
                        }
                    }
                    // 未所属のキャラはランダムに収入を得る。
                    foreach (var chara in World.Characters.Where(World.IsFree))
                    {
                        chara.Gold += UnityEngine.Random.Range(1, 10);
                    }

                    // 行動ポイントを補充する。
                    foreach (var chara in World.Characters)
                    {
                        chara.ActionPoint = Mathf.Min(255, chara.ActionPoint + chara.Intelligence / 10);
                    }
                }

                // 10日毎に行動を行う。
                if (GameDate.Day == 10 || GameDate.Day == 20 || GameDate.Day == 30)
                {
                    Debug.Log("行動");
                    // 収入の1/3分、農業・商業・築城・訓練をランダムに行う。
                    foreach (var chara in World.Characters)
                    {
                        var action = World.IsFree(chara) ?
                            CastleActions.TrainSoldiers :
                            new ActionBase[]
                            {
                                CastleActions.ImproveGoldIncome,
                                CastleActions.ImproveFoodIncome,
                                CastleActions.ImproveCastleStrength,
                                CastleActions.TrainSoldiers,
                            }.RandomPickDefault();
                        var budget = Math.Min(chara.Gold, chara.Salary / 3);
                        while (budget > 0)
                        {
                            if (!action.CanDo(chara)) break;
                            await action.Do(chara);
                            budget -= action.Cost(chara);
                        }
                    }
                }

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
        MainUI.Frame.SetData(this);
    }
}