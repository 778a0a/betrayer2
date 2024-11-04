using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;

public partial class GameCore
{
    public static GameCore Instance { get; set; }

    public WorldData World { get; }
    public UIMapManager Map { get; }
    public MainUI MainUI { get; }
    public Testing test { get; }
    public AI AI { get; }

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

        AI = new AI(this);
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

        foreach (var chara in World.Characters)
        {
            // 行動不能残り日数を更新する。
            if (chara.IsIncapacitated)
            {
                chara.IncapacitatedDaysRemaining--;
            }
            else
            {
                // 兵士を回復させる。
                var rate = 0.01f;
                if (chara.IsMoving) rate = 0.005f;
                foreach (var s in chara.Soldiers)
                {
                    if (s.IsEmptySlot) continue;
                    if (s.HpFloat >= s.MaxHp) continue;
                    var newHp = s.HpFloat + s.MaxHp * rate;
                    s.HpFloat = Mathf.Min(s.MaxHp, newHp);
                }
            }
        }

        // 収入月の場合
        if (GameDate.Day == 1 && GameDate.IsIncomeMonth)
        {
            OnIncome();

            // 隣接する国の友好度を徐々に減らす。
            foreach (var country in World.Countries)
            {
                foreach (var neighbor in country.Neighbors)
                {
                    // 重複して減らさないようにする。
                    if (country.Id > neighbor.Id) continue;

                    var rel = country.GetRelation(neighbor);
                    // 同盟しているなら減らさない。
                    if (rel == Country.AllyRelation) continue;
                    country.SetRelation(neighbor, rel - 1);
                }
            }
        }

        await World.Forces.OnForceMove(this);

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
}