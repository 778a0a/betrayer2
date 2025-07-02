using System;
using System.Collections;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using System.Linq;
using Mono.Cecil.Cil;

public partial class GameCore
{
    public static GameCore Instance { get; set; }

    public WorldData World { get; }
    public UIMapManager Map { get; }
    public MainUI MainUI { get; }
    public Testing test { get; }
    public AI AI { get; }

    public PersonalActions PersonalActions { get; }
    public StrategyActions StrategyActions { get; }
    public CommonActions CommonActions { get; }

    public GameDate GameDate { get; set; }

    public GameCore(WorldData world, UIMapManager map, MainUI mainui, Testing test)
    {
        Instance = this;
        World = world;
        Map = map;
        MainUI = mainui;
        this.test = test;
        GameDate = new(0);

        AI = new AI(this);
        PersonalActions = new(this);
        StrategyActions = new(this);
        CommonActions = new(this);
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

        // 月初の処理
        if (GameDate.Day == 1)
        {
            // 年初の処理
            if (GameDate.Month == 1)
            {
                // 序列を更新する。
                World.Countries.UpdateRanking();
            }

            // 収入月の場合
            if (GameDate.IsIncomeMonth)
            {
                // 四半期の戦略行動済フラグをリセットする。
                foreach (var country in World.Countries)
                {
                    country.QuarterActionDone = false;
                }
                foreach (var castle in World.Castles)
                {
                    castle.QuarterActionDone = false;
                }

                // 収入処理を行う。
                OnIncome();

                // 忠誠を更新する。
                foreach (var chara in World.Characters)
                {
                    if (!chara.IsVassal) continue;
                    chara.Loyalty = (chara.Loyalty - chara.LoyaltyDecreaseBase).MinWith(0);
                }

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

            // 各キャラの連戦回数を更新する。
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

        // 行動不能状態の更新・兵士の回復・行動ゲージの更新を行う。
        foreach (var chara in World.Characters)
        {
            // 行動不能なら行動不能残り日数を更新する。
            if (chara.IsIncapacitated)
            {
                chara.IncapacitatedDaysRemaining--;
                continue;
            }

            // 兵士を回復させる。
            var rate = 0.01f;
            if (chara.IsMoving) rate = 0.003f;
            rate *= Mathf.Pow(0.95f, chara.ConsecutiveBattleCount);
            if (chara.IsStarving) rate *= 0.1f;
            foreach (var s in chara.Soldiers)
            {
                if (s.IsEmptySlot) continue;
                if (s.HpFloat >= s.MaxHp) continue;
                var newHp = s.HpFloat + s.MaxHp * rate;
                s.HpFloat = Mathf.Min(s.MaxHp, newHp);
            }

            // 行動ゲージを貯める。
            chara.PersonalActionGauge += chara.PersonalActionGaugeStep;
            chara.StrategyActionGauge += chara.StrategyActionGaugeStep;
        }

        // 軍勢関連の処理を行う。
        World.Forces.UpdateDangerStatus(this);
        await World.Forces.OnForceMove(this);

        // キャラの行動を行う。
        await OnCharacterMove(player);

        // 表示を更新する。
        MainUI.Frame.SetDatePanelData(this);
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

            // キャラ・軍隊への支払い
            var reduceds = "";
            var notPaids = "";
            foreach (var chara in castle.Members.OrderBy(m => m.OrderIndex))
            {
                // 給料支出
                // 無借金の場合
                if (castle.Gold > 0)
                {
                    castle.Gold -= chara.Salary;
                    chara.Gold += chara.Salary;
                    chara.IsStarving = false;
                }
                // 少額の借金がある場合は支払いを減らす
                else if (castle.Gold > castle.GoldDebtSalaryStopLine)
                {
                    castle.Gold -= chara.Salary / 2;
                    chara.Gold += chara.Salary / 2;
                    chara.IsStarving = true;
                    chara.Loyalty = (chara.Loyalty - chara.LoyaltyDecreaseBase).MinWith(0);
                    reduceds += $"{chara.Name}, ";
                }
                // 借金が多い場合は完全に支払わない。
                else
                {
                    chara.IsStarving = true;
                    if (!chara.IsImportant)
                    {
                        chara.Loyalty = (chara.Loyalty - 2 * chara.LoyaltyDecreaseBase).MinWith(0);
                    }
                    notPaids += $"{chara.Name}, ";
                }
            }
            if (reduceds.Length > 0 || notPaids.Length > 0)
            {
                //Debug.LogWarning($"{castle} 給料カット: [{reduceds}] 未払: [{notPaids}]");
            }
        }
        // 未所属のキャラはランダムに収入を得る。
        foreach (var chara in World.Characters.Where(c => c.IsFree))
        {
            chara.Gold += UnityEngine.Random.Range(1, 10);
            chara.IsStarving = false;
        }

        // 行動ポイントを補充する。
        foreach (var chara in World.Characters)
        {
            chara.ActionPoints = (chara.ActionPoints + chara.Intelligence / 10).MaxWith(255);
        }
    }
}