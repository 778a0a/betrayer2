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

        OnForceMove();

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
        foreach (var chara in World.Characters.Where(World.IsFree))
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
    /// 軍勢の移動処理を行う。
    /// </summary>
    private void OnForceMove()
    {
        foreach (var force in World.Forces)
        {
            // 移動の必要がないなら何もしない。
            if (force.Destination == force.Position) continue;

            // タイル移動進捗を進める。
            force.TileMoveRemainingDays--;
            // 移動進捗が残っている場合は何もしない。
            if (force.TileMoveRemainingDays > 0) continue;
            
            // 移動値が溜まったら隣のタイルに移動する。
            var nextPos = force.Position.To(force.Direction);
            var nextTile = World.Map.GetTile(nextPos);

            // 移動先に自国以外の軍勢がいる場合は戦闘を行う。
            var nextEnemies = nextTile.Forces.Where(f => f.IsEnemy(force)).ToArray();
            if (nextEnemies.Length > 0)
            {
                var enemy = nextEnemies.RandomPick();
                var win = 0.5.Chance(); // TODO Battle
            }
            // 移動先が自国以外の城の場合は駐在中のキャラと戦闘を行う。
            else if (nextTile.Castle != null && force.IsEnemy(nextTile))
            {
                var castle = nextTile.Castle;
                var enemy = castle.Members.Where(e => e.CanDefend(World)).RandomPick();
                var win = enemy == null || 0.5.Chance(); // TODO Battle
                // 勝った場合
                if (win)
                {
                    // 負けた敵は行動不能状態にする。
                    enemy?.SetIncapacitated();
                    // 防衛可能な敵が残っている場合は、移動進捗を半分リセットする。
                    if (castle.Members.Any(e => e.CanDefend(World)))
                    {
                        force.ResetTileMoveProgress(World);
                        force.TileMoveRemainingDays /= 2;
                    }
                    // 全滅した場合は城を占領する。
                    else
                    {
                        // 駐在キャラの行動不能日数を再セットする。
                        foreach (var e in castle.Members.Where(e => !e.IsMoving(World)))
                        {
                            e.SetIncapacitated();
                        }

                        // 城の所有国を変更する。
                        var oldCountry = castle.Country;
                        oldCountry.Castles.Remove(castle);
                        // 全ての城を失った場合は国を消滅させる。
                        if (oldCountry.Castles.Count == 0)
                        {
                            World.Countries.Remove(oldCountry);
                            World.Forces.RemoveAll(f => f.Country == oldCountry);
                            castle.Members.Clear();
                            // TODO 他に必要な処理が色々ありそう。
                        }
                        // まだ他の城がある場合は、一番近くの城に所属を移動する。
                        else
                        {
                            var nearEnemyCastle = oldCountry.Castles
                                .OrderBy(c => c.Position.GetDirectionTo(force.Position))
                                .FirstOrDefault();
                            foreach (var e in castle.Members)
                            {
                                nearEnemyCastle.Members.Add(e);
                            }
                            castle.Members.Clear();
                        }

                        castle.Country = force.Country;
                        force.Country.Castles.Add(castle);

                        // 城の隣接タイルにいて、城が目的地で、進捗が半分以上のキャラは城に入る。
                        // TODO
                    }
                }
                // 負けた場合は本拠地へ撤退を始める。
                else
                {
                    var home = World.CastleOf(force.Character);
                    force.Destination = home.Position;
                    force.Direction = force.Position.GetDirectionTo(home.Position);
                    force.ResetTileMoveProgress(World);
                }

            }
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
                if (World.IsFree(chara))
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