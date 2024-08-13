using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UIElements;

public class LocalizationManager : MonoBehaviour
{
    [SerializeField] private LocalizedStringTable ltable;

    private readonly List<object> components = new();


    public void Register(object component)
    {
        components.Add(component);
    }

    public void Apply()
    {
        var table = ltable.GetTable();

        foreach (var c in components)
        {
            var typeName = c.GetType().Name;
            var props = c.GetType().GetProperties();
            foreach (var prop in props)
            {
                var entry = table[$"{typeName}.{prop.Name}"] ?? table[prop.Name];
                if (entry == null)
                {
                    //Debug.Log($"[L.Apply] Key not found: {prop.Name} of {typeName}");
                    continue;
                }
                var value = entry.LocalizedValue;
                const string FontSizeSepalator = "@@";
                var fontSize = -1;
                if (value.Contains(FontSizeSepalator))
                {
                    var segs = value.Split(FontSizeSepalator);
                    fontSize = int.Parse(segs[0]);
                    value = segs[1];
                }

                var propValue = prop.GetValue(c);
                switch (propValue)
                {
                    case string:
                        prop.SetValue(c, value);
                        //Debug.Log($"[L.Apply] {entry.Key} = {value}");
                        break;
                    case Label label:
                        label.text = value;
                        if (fontSize != -1) label.style.fontSize = fontSize;
                        //Debug.Log($"[L.Apply] {entry.Key} = {value}");
                        break;
                    case Button button:
                        button.text = value;
                        if (fontSize != -1) button.style.fontSize = fontSize;
                        //Debug.Log($"[L.Apply] {entry.Key} = {value}");
                        break;
                    case TextField textField:
                        textField.textEdition.placeholder = value;
                        if (fontSize != -1) textField.style.fontSize = fontSize;
                        break;
                    default:
                        Debug.LogWarning($"[L.Apply] {entry.Key} is unknown type: {propValue.GetType()}");
                        break;
                }
            }
        }
    }



    public string this[string key, params object[] args] => T(key, args);
    public string T(string key, params object[] args)
    {
        key = key.Replace("\n", "\\n");

        var table = ltable.GetTable();
        var entry = table[key];
        if (entry == null)
        {
            Debug.LogWarning($"Key not found: {key}");
            return string.Format(key, args);
        }
        var value = table[key].LocalizedValue;
        value = value.Replace("\\n", "\n");
        return string.Format(value, args);
    }

    public string TranslateName(string name)
    {
        var locale = LocalizationSettings.SelectedLocale;
        var isEn = locale.Identifier.Code == "en";
        return isEn ? NameJaToEn(name) : NameEnToJa(name);
    }

    public string NameJaToEn(string nameJa)
    {
        if (dictNameJaToEn.TryGetValue(nameJa, out var nameEn))
        {
            return nameEn;
        }
        return nameJa;
    }

    public string NameEnToJa(string nameEn)
    {
        foreach (var pair in dictNameJaToEn)
        {
            if (pair.Value == nameEn)
            {
                return pair.Key;
            }
        }
        return nameEn;
    }

    private readonly Dictionary<string, string> dictNameJaToEn = new()
    {
        {"イヴァ", "Iva"},
        {"アストレア", "Astraea"},
        {"アネモネ", "Anemone"},
        {"アマンダ", "Amanda"},
        {"アリアドネ", "Ariadne"},
        {"アリアナ", "Ariana"},
        {"アリエル", "Ariel"},
        {"アルタイル", "Altair"},
        {"アルバート", "Albert"},
        {"アーサー", "Arthur"},
        {"イカロス", "Icarus"},
        {"イグレイン", "Igraine"},
        {"イゾルデ", "Isolde"},
        {"イシュタル", "Ishtar"},
        {"エスメラルダ", "Esmeralda"},
        {"エマ", "Emma"},
        {"エルロン", "Elron"},
        {"エレノア", "Eleanor"},
        {"オスカー", "Oscar"},
        {"オベロン", "Oberon"},
        {"オリビア", "Olivia"},
        {"カトリーナ", "Katrina"},
        {"キャスパー", "Casper"},
        {"グリフィン", "Griffin"},
        {"ケイルン", "Kaelen"},
        {"ケイロス", "Kairos"},
        {"シエナ", "Sienna"},
        {"シャーロット", "Charlotte"},
        {"シルバーン", "Silvan"},
        {"ジェームズ", "James"},
        {"ジャスパー", "Jasper"},
        {"ジャスミン", "Jasmine"},
        {"ジークフリート", "Siegfried"},
        {"セバスチャン", "Sebastian"},
        {"セレーネ", "Selene"},
        {"ゼノン", "Xenon"},
        {"ソフィア", "Sophia"},
        {"ソレイユ", "Soleil"},
        {"タリシア", "Talisha"},
        {"チャールズ", "Charles"},
        {"デューン", "Dune"},
        {"ドレイク", "Drake"},
        {"ナイア", "Naiad"},
        {"ネメア", "Nemea"},
        {"ネロ", "Nero"},
        {"ノクターン", "Nocturne"},
        {"フェニックス", "Phoenix"},
        {"ヘリオス", "Helios"},
        {"ヘンリー", "Henry"},
        {"ペルシヴァル", "Percival"},
        {"ミリアム", "Miriam"},
        {"ライオネル", "Lionel"},
        {"ライラ", "Laila"},
        {"リリス", "Lilith"},
        {"ルシアン", "Lucian"},
        {"ルナ", "Luna"},
        {"ロザリンド", "Rosalind"},
        {"ロゼッタ", "Rosetta"},
        {"ローズマリー", "Rosemary"},
        {"ヴァルカン", "Vulcan"},
        {"ヴィオレット", "Violet"},
        {"レオナルド", "Leonardo"},
        {"ジョージ", "George"},
        {"セレスティア", "Celestia"},
        {"ビクトリア", "Victoria"},
        {"オーロラ", "Aurora"},
        {"ナオミ", "Naomi"},
        {"プロメテウス", "Prometheus"},
        {"トリスタン", "Tristan"},
        {"エルシア", "Elcia"},
        {"イゼルト", "Isolde"},
        {"ドラゴミール", "Dragomir"},
        {"エドワード", "Edward"},
        {"マーガレット", "Margaret"},
        {"ハイペリオン", "Hyperion"},
        {"フレデリック", "Frederick"},
        {"ガイアス", "Gaius"},
        {"レイヴン", "Raven"},
        {"ザファーラ", "Zafara"},
        {"ザンダー", "Xander"},
        {"ビアンカ", "Bianca"},
        {"オリオン", "Orion"},
        {"ウィリアム", "William"},
        {"アクエリアス", "Aquarius"},
        {"ザフィール", "Zephyr"},
        {"ローエン", "Rowan"},
        {"ナディア", "Nadia"},
        {"アステル", "Aster"},
        {"イザベラ", "Isabella"},
        {"カリオペ", "Calliope"},
        {"アリシア", "Alicia"},
        {"ゼラン", "Zelan"},
        {"フリージア", "Freesia"},
        {"レイモンド", "Raymond"},
        {"セレスト", "Celestin"},
        {"ラベンダー", "Lavender"},
        {"シャノン", "Shannon"},
        {"ケルベロス", "Cerberus"},
        {"ファエリン", "Faerin"},
        {"イリス", "Iris"},
        {"シエラ", "Sierra"},
        {"アトラス", "Atlas"},
        {"イゼリア", "Izelia"},
    };
}
