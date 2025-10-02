using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// キャラクター
/// </summary>
public class Character
{
    [JsonIgnore]
    private WorldData world;
    public void AttachWorld(WorldData world) => this.world = world;

    /// <summary>
    /// 勢力内序列
    /// </summary>
    [JsonIgnore] public int OrderIndex { get; set; }
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// 名前
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 攻撃
    /// </summary>
    public int Attack { get; set; }
    /// <summary>
    /// 防御
    /// </summary>
    public int Defense { get; set; }
    /// <summary>
    /// 智謀
    /// </summary>
    public int Intelligence { get; set; }
    /// <summary>
    /// 統治
    /// </summary>
    public int Governing { get; set; }

    /// <summary>
    /// 性格
    /// </summary>
    [JsonIgnore]
    public Personality Personality { get; set; }
    public string PersonalityText
    {
        get => Personality.ToString();
        set => Personality = Enum.Parse<Personality>(value);
    }

    /// <summary>
    /// 特性
    /// </summary>
    [JsonIgnore]
    public Traits Traits { get; set; }
    public string TraintsText
    {
        get => string.Join(" ", Traits.ToString().Split(", "));
        set => Traits = Enum.Parse<Traits>(string.Join(", ", value.Split(" ")));
    }

    /// <summary>
    /// 忠誠基本値
    /// </summary>
    public int LoyaltyBase { get; set; }
    /// <summary>
    /// 忠実さ
    /// </summary>
    public int Fealty { get; set; }
    /// <summary>
    /// 野心
    /// </summary>
    public int Ambition { get; set; }

    [JsonIgnore] public bool FealtyOverAmbition => Fealty >= Ambition;
    [JsonIgnore] public bool IsLoyal => this == Country.Ruler || Loyalty >= 100 || (IsImportant && Loyalty > 90 && FealtyOverAmbition);

    /// <summary>
    /// 所持金
    /// </summary>
    public float Gold { get; set; }
    /// <summary>
    /// 功績
    /// </summary>
    public float Contribution { get; set; }
    /// <summary>
    /// 名声
    /// </summary>
    public int Prestige { get; set; }
    /// <summary>
    /// 序列を決定する指標
    /// </summary>
    [JsonIgnore]
    public float Importance => Contribution + Prestige + Soldiers.Sum(s => !s.IsAlive ? 0 : s.Level);

    /// <summary>
    /// 忠誠
    /// </summary>
    public float Loyalty { get; set; }

    /// <summary>
    /// 忠誠減少基本値
    /// 忠実さが5なら-1.25、6なら-1、重臣ならやや少なめ
    /// 城主の場合は、野心に比例して0.0～0.5増加する
    /// </summary>
    [JsonIgnore] public float LoyaltyDecreaseBase =>
        (0.1f + (10f - Fealty) / 4 + (IsBoss ? 0.5f * Ambition / 10f : 0)) / (IsImportant ? 1.5f : 1);

    /// <summary>
    /// 個人行動ゲージ
    /// </summary>
    public float PersonalActionGauge { get; set; } = 0f;
    public float PersonalActionGaugeStep
    {
        get
        {
            // 通常は25日で1回行動できる。
            var x = 100f / 30;
            var cap = IsMoving ?
                Math.Max(Attack, Defense) * 0.5f + Intelligence * 0.3f + Governing * 0.2f :
                Math.Max(Attack, Defense) * 0.3f + Intelligence * 0.4f + Governing * 0.3f;
            // 能力が100なら1.5倍速、75なら1倍速、50なら0.75倍速。
            var capAdj = 1 + (cap - 75) / 25f / 2f;
            return x * capAdj;
        }
    }

    /// <summary>
    /// 戦略行動ゲージ
    /// </summary>
    public float StrategyActionGauge { get; set; } = 0f;
    public float StrategyActionGaugeStep
    {
        get
        {
            // 通常は25日で1回行動できる。
            var x = 100f / 30;
            var cap = Math.Max(Attack, Defense) * 0.2f + Intelligence * 0.4f + Governing * 0.4f;
            var capAdj = 1 + (cap - 75) / 25f;
            return x * capAdj;
        }
    }

    /// <summary>
    /// 軍勢
    /// </summary>
    public Soldiers Soldiers { get; set; }

    /// <summary>
    /// プレーヤーならtrue
    /// </summary>
    public bool IsPlayer { get; set; }

    /// <summary>
    /// 指示を出せるならtrue
    /// </summary>
    public bool CanOrder => IsPlayer || (IsVassal && (Castle.Boss.IsPlayer || Country.Ruler.IsPlayer));

    /// <summary>
    /// 重臣ならtrue
    /// </summary>
    public bool IsImportant { get; set; }

    /// <summary>
    /// 供給不足ならtrue
    /// </summary>
    public bool IsStarving { get; set; } = false;

    /// <summary>
    /// （内部データ）強さ
    /// </summary>
    [JsonIgnore]
    public int Power => (Attack + Defense + Intelligence) / 3 * Soldiers.Power;

    /// <summary>
    /// 給料
    /// </summary>
    [JsonIgnore]
    public int Salary
    {
        get
        {
            //// 内政に40G使う（20回実行する≒功績値40）と1上がる感じにしてみる。
            //return 5 + (int)(Contribution / 40).MaxWith(25);

            // 基本は上記のとおりで、
            // 給料が1下がるごとに、0.5G(1/4回)ずつ必要功績が少なくなるようにする。
            // 給料が30なら1.3ヶ月、給料が15なら2.2ヶ月、給料が5なら5.4ヶ月で次のレベルに到達する。
            const int MaxLevel = 25;
            var step = 40;
            var levelRequirement = 0;
            for (int level = 0; level < MaxLevel; level++)
            {
                levelRequirement += step - (MaxLevel - level) / 2;
                // 功績値が基準に満たないなら終了
                if (Contribution < levelRequirement) return level + 5;
            }
            return MaxLevel + 5;
        }
    }

    /// <summary>
    /// 行動力
    /// </summary>
    public int ActionPoints { get; set; }

    /// <summary>
    /// 行動不能回復までの残り日数
    /// </summary>
    public int IncapacitatedDaysRemaining { get; set; }
    [JsonIgnore]
    public bool IsIncapacitated => IncapacitatedDaysRemaining > 0;
    
    [JsonIgnore]
    public bool IsDefendable => !IsIncapacitated && !IsMoving && !Soldiers.IsAllDead;

    /// <summary>
    /// 行動不能状態にします。
    /// </summary>
    public void SetIncapacitated()
    {
        var old = IncapacitatedDaysRemaining;
        IncapacitatedDaysRemaining = 90;
        if (old == 0)
        {
            //Debug.Log($"{Name}は行動不能になりました。");
        }
        else
        {
            //Debug.Log($"{Name}は行動不能状態が延長されました。(prev: {old})");
        }
    }

    public bool CanPay(ActionCost cost) => cost.CanPay(this);
    [JsonIgnore]
    public bool IsMoving => Force != null;
    [JsonIgnore]
    public Force Force { get; set; } // メモ ForceManagerと二重管理
    [JsonIgnore]
    public Country Country { get; private set; } // メモ Castle.Countryと二重管理
    [JsonIgnore]
    public bool IsRuler => Country?.Ruler == this;
    [JsonIgnore]
    public bool IsVassal => !IsFree && !IsRuler;
    [JsonIgnore]
    public bool IsBoss => Castle?.Boss == this;
    [JsonIgnore]
    public bool IsFree => Country == null;
    [JsonIgnore]
    public bool IsRulerOrVassal => !IsFree;
    [JsonIgnore]
    public Castle Castle { get; private set; } // メモ Castle.Memberと二重管理
    public void ChangeCastle(Castle newCastle, bool asFree)
    {
        var oldCastle = Castle;
        if (oldCastle != null)
        {
            oldCastle.FreesRaw.Remove(this);
            oldCastle.MembersRaw.Remove(this);
        }

        Castle = newCastle;
        if (asFree)
        {
            newCastle.FreesRaw.Add(this);
            Country = null;
        }
        else
        {
            newCastle.MembersRaw.Add(this);
            Country = newCastle.Country;
            // TODO 城の国が変わった場合
        }
    }

    /// <summary>
    /// 地位
    /// </summary>
    public string GetTitle()
    {
        if (IsRuler)
        {
            return Country.CountryRank switch
            {
                CountryRank.Empire => "皇帝",
                CountryRank.Kingdom => "王",
                CountryRank.Duchy => "大公",
                _ => "君主",
            };
        }
        else if (IsBoss)
        {
            return "城主";
        }
        else if (IsVassal)
        {
            return "一般";
            //return new[]
            //{
            //    "従士",
            //    "従士",
            //    "士長",
            //    "将軍",
            //    "元帥",
            //    "宰相",
            //    "総督",
            //    "副王",
            //}[order];
        }
        else
        {
            return "浪士";
        }
    }

    public string csvDebugData { get; set; } = "";
    public string csvDebugMemo { get; set; } = "";

    public override string ToString() => $"{Name} O:{OrderIndex}{(IsImportant ? "!" : "")} G:{Gold} P:{Power} L:{Loyalty:0.0}";
}
