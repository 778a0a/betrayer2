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
                rate *= Mathf.Pow(0.95f, chara.ConsecutiveBattleCount);
                var starving = chara.Castle.Food < 0;
                foreach (var s in chara.Soldiers)
                {
                    if (s.IsEmptySlot) continue;
                    if (starving)
                    {
                        s.HpFloat = s.HpFloat - s.MaxHp * rate;
                        if (s.HpFloat <= 0) s.IsEmptySlot = true;
                        continue;
                    }
                    if (s.HpFloat >= s.MaxHp) continue;
                    var newHp = s.HpFloat + s.MaxHp * rate;
                    s.HpFloat = Mathf.Min(s.MaxHp, newHp);
                }
            }
        }

        if (GameDate.Day == 1)
        {
            foreach (var chara in World.Characters)
            {
                var count = chara.ConsecutiveBattleCount;
                if (chara.IsMoving)
                {
                    // 根拠地から離れている場合は連戦回数を増やす。
                    if (chara.Force.Position.DistanceTo(chara.Castle) > 5)
                    {
                        chara.ConsecutiveBattleCount++;
                    }
                }
                // 出撃中でないキャラは連戦回数を減らす。
                else
                {
                    chara.ConsecutiveBattleCount = Mathf.Max(0, count - 1);
                }
            }
        }

        if (GameDate.Day == 1)
        {
            // 年初の処理
            if (GameDate.Month == 1)
            {
                // 序列を更新する。
                World.Countries.UpdateRanking();

                // 忠誠を更新する。
                foreach (var chara in World.Characters)
                {
                    if (!chara.IsVassal) continue;
                    chara.Loyalty = (chara.Loyalty - chara.LoyaltyDecreaseBase).MinWith(0);
                }
            }

            // 収入月の場合
            if (GameDate.IsIncomeMonth)
            {
                OnIncome();

                // 友好度を更新する。
                foreach (var country in World.Countries)
                {
                    var neighbors = country.Neighbors.ToArray();
                    foreach (var other in World.Countries)
                    {
                        // 重複して減らさないようにする。
                        if (country.Id > other.Id || country == other) continue;

                        var rel = country.GetRelation(other);
                        // 同盟しているなら何もしない。
                        if (rel == Country.AllyRelation) continue;

                        // 隣接する国は友好度を徐々に減らす。
                        if (neighbors.Contains(other))
                        {
                            if (rel > 30) country.SetRelation(other, rel - 1);
                        }
                        // 隣接していない国は、友好度が低いなら増やす。
                        else
                        {
                            if (rel < 50) country.SetRelation(other, rel + 1);
                        }
                    }
                }
            }

            // 収入月の前の場合
            if (GameDate.IsEndMonth)
            {
                foreach (var country in World.Countries)
                {
                    if (country.WealthBalance > -30) continue;
                    if (country.WealthSurplus > 0) continue;

                    // 赤字で物資も乏しい場合は序列の低いメンバーを解雇する。
                    var target = country.Members
                        .Where(m => !m.IsMoving)
                        .Where(m => !m.IsImportant)
                        .OrderByDescending(m => m.OrderIndex)
                        .FirstOrDefault();
                    if (target != null)
                    {
                        // TODO 解雇アクションを使う。
                        target.ChangeCastle(target.Castle, true);
                        Debug.LogError($"{country} 赤字のため、{target}を解雇しました。");
                    }
                }
            }
        }

        World.Forces.OnCheckDefenceStatus(this);
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
        foreach (var castle in World.Countries.SelectMany(c => c.Castles))
        {
            // 収入
            castle.Gold += castle.GoldIncome;
            castle.Food += castle.FoodIncome;

            // キャラ・軍隊への支払い
            foreach (var chara in castle.Members.OrderBy(m => m.OrderIndex))
            {
                // 給料支出
                // 無借金の場合
                if (castle.Gold > 0)
                {
                    castle.Gold -= chara.Salary;
                    chara.Gold += chara.Salary;
                }
                // 少額の借金がある場合は支払いを減らす
                else if (castle.Gold > castle.GoldDebtSalaryStopLine)
                {
                    castle.Gold -= chara.Salary / 2;
                    chara.Gold += chara.Salary / 2;
                    chara.Loyalty = (chara.Loyalty - chara.LoyaltyDecreaseBase).MinWith(0);
                    Debug.LogError($"{castle} 借金があるため、{chara.Name}の給料をカットします。");
                }
                // 借金が多い場合は完全に支払わない。
                else
                {
                    Debug.LogError($"{castle} 借金過多のため、{chara.Name}に給料を支払えませんでした。");
                    if (!chara.IsImportant)
                    {
                        chara.Loyalty = (chara.Loyalty - 2 * chara.LoyaltyDecreaseBase).MinWith(0);
                    }
                }
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