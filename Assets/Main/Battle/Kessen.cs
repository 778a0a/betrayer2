using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Kessen
{
    public static Kessen Prepare(Country country, Country target)
    {
        var atk = new CountryInBattle(country, true);
        atk.Initialize();
        var def = new CountryInBattle(target, false);
        def.Initialize();
        atk.Opponent = def;
        def.Opponent = atk;
        var battle = new Kessen(atk, def);
        return battle;
    }

    private Kessen(CountryInBattle atk, CountryInBattle def)
    {
        Attacker = atk;
        Defender = def;
    }

    public CountryInBattle Attacker { get; set; }
    public CountryInBattle Defender { get; set; }
    private int TickCount { get; set; }

    private CountryInBattle Atk => Attacker;
    private CountryInBattle Def => Defender;

    private KessenWindow UI => GameCore.Instance.MainUI.KessenWindow;
    public bool NeedInteraction => Attacker.HasPlayer || Defender.HasPlayer;
    private bool NeedWatchBattle => Test.Instance.showOthersBattle;
    private LocalizationManager L => MainUI.Instance.L;

    public async ValueTask<BattleResult> Do()
    {
        var needInteraction = NeedInteraction;
        if (NeedWatchBattle || needInteraction)
        {
            UI.Root.style.display = DisplayStyle.Flex;
            UI.SetData(this, needInteraction: needInteraction);
        }

        bool NeedInteractionCurrent()
        {
            return NeedInteraction && Atk.Members.Concat(Def.Members)
                .Where(m => m.Character.IsPlayer)
                .Where(m => m.State == KessenMemberState.Alive)
                .Where(m => m.Character.Force.Soldiers.Any(s => s.IsAlive))
                .Any();
        }

        var result = default(BattleResult);
        while (true)
        {
            // 撤退判断を行う。
            if (NeedWatchBattle || needInteraction)
            {
                UI.SetData(this, needInteraction: NeedInteractionCurrent());
            }

            foreach (var member in Atk.Members.Concat(Def.Members))
            {
                // プレーヤーの場合はUIで指定してもらう。
                if (member.Character.IsPlayer)
                {
                    if (member.Character.Force.Soldiers.All(s => !s.IsAlive))
                    {
                        member.State = KessenMemberState.Retreated;
                    }
                    if (member.State == KessenMemberState.Retreated)
                    {
                        // 撤退済みなら何もしない。
                        await Awaitable.WaitForSecondsAsync(0.1f);
                    }
                    else
                    {
                        var shouldContinue = await UI.WaitPlayerClick();
                        if (!shouldContinue)
                        {
                            member.State = KessenMemberState.Retreated;
                        }
                    }
                }
                // CPUの場合は自動で判断する。
                else if (member.State != KessenMemberState.Retreated)
                {
                    if (member.ShouldRetreat(TickCount) || member.Character.Force.Soldiers.All(s => !s.IsAlive))
                    {
                        member.State = KessenMemberState.Retreated;
                    }
                }
            }
            if (!needInteraction && NeedWatchBattle)
            {
                await Awaitable.WaitForSecondsAsync(0.025f);
            }

            if (Atk.NoSoldierExists)
            {
                result = BattleResult.DefenderWin;
                break;
            }
            if (Def.NoSoldierExists)
            {
                result = BattleResult.AttackerWin;
                break;
            }

            Tick();
        }
        Debug.Log($"[決戦処理] 結果: {result}");
        var winner = result == BattleResult.AttackerWin ? Atk : Def;
        var loser = result == BattleResult.AttackerWin ? Def : Atk;

        // 画面を更新する。
        if (NeedWatchBattle || needInteraction)
        {
            UI.SetData(this, result, needInteraction: needInteraction);

            await MessageWindow.Show(L["{0}が決戦に勝利しました。", winner.Country.Ruler.Name]);
            if (needInteraction)
            {
                await UI.WaitPlayerClick();
            }
            //else
            //{
            //    await Awaitable.WaitForSecondsAsync(0.5f);
            //}
            UI.Root.style.display = DisplayStyle.None;
        }

        // 死んだ兵士のスロットを空にする。
        foreach (var sol in Atk.Members.Concat(Def.Members).SelectMany(m => m.Character.Force.Soldiers))
        {
            if (sol.Hp == 0)
            {
                sol.IsEmptySlot = true;
            }
        }

        // 兵士の回復処理を行う。
        winner.Recover(true);
        loser.Recover(false);

        // 名声の処理を行う。
        var totalGainPrestige = 0;
        foreach (var m in loser.Members)
        {
            var loss = m.Character.Prestige / 3;
            m.Character.Prestige -= loss;
            totalGainPrestige += loss;
        }
        var gainPerMember = 10 + totalGainPrestige / winner.Members.Length;
        foreach (var m in winner.Members)
        {
            m.Character.Prestige += gainPerMember;
        }

        // 攻撃側の勝ち
        if (result == BattleResult.AttackerWin)
        {
            foreach (var m in Attacker.Members) m.Character.Contribution += 30;
            foreach (var m in Defender.Members) m.Character.Contribution += 5;
        }
        // 防衛側の勝ち
        else
        {
            foreach (var m in Attacker.Members) m.Character.Contribution += 5;
            foreach (var m in Defender.Members) m.Character.Contribution += 30;
        }

        return result;
    }

    private void Tick()
    {
        TickCount += 1;

        // 両方の兵士をランダムな順番の配列にいれる。
        var all = Atk.Members.Concat(Def.Members)
            .Where(m => m.State == KessenMemberState.Alive)
            .SelectMany(m => m.Character.Force.Soldiers.Select(s => (soldier: s, owner: m)))
            .Where(x => x.soldier.IsAlive)
            .ToArray()
            .ShuffleInPlace();

        static float BaseAdjustment(CountryInBattle.Member chara, CountryInBattle.Member op, int tickCount)
        {
            var adj = 1f;
            adj += (chara.Strength - 50) / 100f;
            adj -= (op.Strength - 50) / 100f;
            adj += (chara.Character.Intelligence - 50) / 100f * Mathf.Min(1, tickCount / 10f);
            adj -= (op.Character.Intelligence - 50) / 100f * Mathf.Min(1, tickCount / 10f);
            return adj;
        }

        var attackerTotalDamage = 0f;
        var defenderTotalDamage = 0f;
        foreach (var (soldier, owner) in all)
        {
            if (!soldier.IsAlive) continue;
            var opponent = owner.Country.Opponent.Members
                .Where(m =>
                    m.State == KessenMemberState.Alive &&
                    m.Character.Force.Soldiers.Any(s => s.IsAlive))
                .RandomPickDefault();
            var target = opponent?.Character.Force.Soldiers.Where(s => s.IsAlive).RandomPickDefault();
            if (target == null) continue;

            var adj = BaseAdjustment(owner, opponent, TickCount);
            adj += Random.Range(-0.2f, 0.2f);
            adj += soldier.Level / 10f;

            var damage = Math.Max(0, adj);
            target.HpFloat = (int)Math.Max(0, target.HpFloat - damage);
            soldier.AddExperience(owner.Character);

            if (owner.Country.IsAttacker)
            {
                attackerTotalDamage += damage;
            }
            else
            {
                defenderTotalDamage += damage;
            }
        }

        if (Atk.HasPlayer || Def.HasPlayer)
        {
            Debug.Log($"[決戦処理] " +
                $"{Atk}の総ダメージ: {attackerTotalDamage} " +
                $"{Def}の総ダメージ: {defenderTotalDamage}");
        }
    }
}
