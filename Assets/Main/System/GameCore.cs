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
    public Booter Booter { get; }
    public AI AI { get; }

    public PersonalActions PersonalActions { get; }
    public StrategyActions StrategyActions { get; }

    public GameDate GameDate { get; set; }

    public GameCore(WorldData world, UIMapManager map, MainUI mainui, Booter booter)
    {
        Instance = this;
        World = world;
        Map = map;
        MainUI = mainui;
        Booter = booter;
        GameDate = new(0);

        AI = new AI(this);
        PersonalActions = new(this);
        StrategyActions = new(this);
    }

    public void TogglePlay()
    {
        Booter.hold = !Booter.hold;
        MainUI.Frame.RefreshUI();
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
        await Awaitable.WaitForSecondsAsync(Booter.TickWait);

        var player = World.Player;

        // 戦闘の検証用
        //while (true)
        //{
        //    UnityEngine.Random.seed = (int)DateTime.Now.Ticks;
        //    var force = new Force(World, player, player.Castle.Position);
        //    force.SetDestination(World.Castles.First());
        //    World.Forces.Register(force);
        //    var enemyChara = World.Characters.Where(c => c != player).Where(c => !c.IsFree).RandomPick();
        //    force.UpdatePosition(World.Map.GetTile(enemyChara.Castle).Neighbors.RandomPick().Position);
        //    var battle = BattleManager.PrepareSiegeBattle(force, enemyChara);
        //    //var enemy = new Force(World, enemyChara, enemyChara.Castle.Position);
        //    //force.UpdatePosition(World.Map.GetTile(enemy).Neighbors.RandomPick().Position);
        //    //var battle = BattleManager.PrepareFieldBattle(force, enemy);
        //    await battle.Do();
        //}

        // 月初の処理
        if (GameDate.Day == 1)
        {
            // 年初の処理
            if (GameDate.Month == 1)
            {
                // 序列を更新する。
                await World.Countries.UpdateRanking();

                // 国の方針を更新する。
                foreach (var country in World.Countries)
                {
                    if (country.Ruler.IsPlayer) continue;
                    var prevObjective = country.Objective;
                    var newObjective = AI.SelectCountryObjective(country, prevObjective);
                    country.Objective = newObjective;
                    if (newObjective != prevObjective)
                    {
                        Debug.Log($"国方針更新: {newObjective} <- {prevObjective} at {country}");
                    }
                    else
                    {
                        Debug.Log($"国方針継続: {newObjective} at {country}");
                    }
                }
            }

            // 収入月の場合
            if (GameDate.IsIncomeMonth)
            {
                // 城の収入処理を行う。
                OnCastleIncome();

                // 四半期の戦略行動済フラグをリセットする。
                foreach (var country in World.Countries)
                {
                    country.QuarterActionDone = false;
                }
                foreach (var castle in World.Castles)
                {
                    castle.QuarterActionDone = false;
                }

                // 各城の方針を更新する。
                foreach (var country in World.Countries)
                {
                    if (country.Ruler.IsPlayer) continue;
                    foreach (var castle in country.Castles)
                    {
                        // 各城の方針を設定する。
                        var prevObjective = castle.Objective;
                        var newObjective = AI.SelectCastleObjective(castle);
                        castle.Objective = newObjective;
                        if (newObjective != prevObjective)
                        {
                            Debug.Log($"方針更新: {newObjective} <- {prevObjective} at {castle}");
                        }
                        else
                        {
                            Debug.Log($"方針継続: {newObjective} at {castle}");
                        }
                    }
                }

                // ゲーム開始直後は忠誠・友好度の減少を行わない。
                if (!GameDate.IsGameFirstDay)
                {
                    // 忠誠を更新する。
                    foreach (var chara in World.Characters)
                    {
                        if (!chara.IsVassal) continue;
                        var val = chara.LoyaltyDecreaseBase;
                        // 城のメンバーが多すぎるなら減少量を増やす。
                        if (chara.Castle.IsMemberOver) val *= 2;
                        chara.Loyalty = (chara.Loyalty - val).MinWith(0);
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
            }

            // キャラへの給料支払いを行う。
            OnCharacterIncome();
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
            if (chara.IsStarving) rate *= 0.1f;
            if (!chara.IsFree && chara.Castle.IsMemberOver) rate *= 0.5f;
            foreach (var s in chara.Soldiers)
            {
                if (s.IsEmptySlot) continue;
                if (s.HpFloat >= s.MaxHp) continue;
                var newHp = s.HpFloat + s.MaxHp * rate;
                s.HpFloat = Mathf.Min(s.MaxHp, newHp);
            }

            // 行動ゲージを貯める。
            chara.PersonalActionGauge += chara.PersonalActionGaugeStep;
            if (chara.IsBoss)
            {
                chara.StrategyActionGauge += chara.StrategyActionGaugeStep;
            }
        }

        // 軍勢関連の処理を行う。
        World.Forces.UpdateDangerStatus(this);
        await World.Forces.OnForceMove(this);

        // キャラの行動を行う。
        await OnCharacterMove(player);

        // 表示を更新する。
        MainUI.Frame.RefreshUI();
        await Booter.HoldIfNeeded();

        GameDate++;
    }

    /// <summary>
    /// 城の収入処理（四半期）
    /// </summary>
    private void OnCastleIncome()
    {
        foreach (var castle in World.Countries.SelectMany(c => c.Castles))
        {
            castle.Gold += castle.GoldIncome;
            // 1000以上超えたら、投資額に加算する。
            if (castle.Gold >= 1000)
            {
                var amari = castle.Gold - 1000;
                castle.Gold = 1000;

                var adj = PersonalActions.InvestAction.TerrainAdjustment(castle);
                castle.TotalInvestment += adj * 0.2f;
            }
        }
    }

    /// <summary>
    /// 個人の収入処理（毎月）
    /// </summary>
    private void OnCharacterIncome()
    {
        // 城に所属しているキャラは給料を得る。
        foreach (var castle in World.Countries.SelectMany(c => c.Castles))
        {
            DistributeSalary(castle);
        }

        // 未所属のキャラはランダムに収入を得る。
        foreach (var chara in World.Characters.Where(c => c.IsFree))
        {
            chara.Gold += UnityEngine.Random.Range(1, 5);
            chara.IsStarving = false;
        }

        // 行動ポイントを補充する。
        foreach (var chara in World.Characters.Where(c => c.IsBoss))
        {
            chara.ActionPoints = (chara.ActionPoints + (chara.Intelligence + chara.Governing) / 10).MaxWith(255);
        }
    }

    /// <summary>
    /// 城の給料支払処理
    /// </summary>
    private static void DistributeSalary(Castle castle)
    {
        var members = castle.Members.ToList();
        if (members.Count == 0) return;

        // 資金が十分ある場合は全額支払う。
        var totalSalary = members.Sum(m => m.Salary);
        if (castle.Gold >= totalSalary)
        {
            foreach (var chara in members)
            {
                castle.Gold -= chara.Salary;
                chara.Gold += chara.Salary;
                chara.IsStarving = false;
            }
            return;
        }

        const int HumanRate = 5;

        // 全く支払えない場合
        if (castle.Gold == 0)
        {
            foreach (var chara in members)
            {
                chara.IsStarving = true;
                chara.Loyalty = (chara.Loyalty - HumanRate * chara.LoyaltyDecreaseBase).MinWith(0);
            }
            Debug.LogWarning($"{castle} 給料未払: 全員");
            return;
        }

        // 資金不足の場合は、不足分を平等に減らす。
        var availableGold = castle.Gold;
        var reduceds = "";
        var notPaids = "";

        // 全体の不足額
        var totalShortage = totalSalary - availableGold;
        // 一人当たりの不足額
        var shortagePerPerson = (int)Math.Ceiling((float)totalShortage / members.Count);

        foreach (var chara in members)
        {
            var paid = (chara.Salary - shortagePerPerson).MinWith(0);
            chara.Gold += paid;
            castle.Gold -= paid;
            var unpaidRate = 1 - (float)paid / chara.Salary;
            chara.IsStarving = unpaidRate > 0.5f;

            // 給料カットによる忠誠度低下
            if (paid < chara.Salary)
            {
                var rate = HumanRate * unpaidRate;
                chara.Loyalty = (chara.Loyalty - rate * chara.LoyaltyDecreaseBase).MinWith(0);
                reduceds += $"{chara.Name}, ";
            }
        }
        castle.Gold = 0;

        if (reduceds.Length > 0 || notPaids.Length > 0)
        {
            Debug.LogWarning($"{castle} 給料カット: [{reduceds}] 未払: [{notPaids}]");
        }
    }
}