using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// MainUIで使用するUIDocumentのVisualTreeAssetを管理します。
/// </summary>
public class MainUIVisualTreeAssetManager : MonoBehaviour
{
    [field: SerializeField] private VisualTreeAsset[] Screens { get; set; }
    [field: SerializeField] public VisualTreeAsset CharacterTableRowItem { get; set; }

    public void InitializeScreens(MainUI ui, bool reinitialize)
    {
        // MainUIの画面プロパティを全て取得する。
        var panelProps = ui.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.GetInterface(nameof(IScreen)) != null)
            .ToList();
        
        var shouldStop = false;
        foreach (var prop in panelProps)
        {
            // VisualTreeAssetの名前とプロパティ名が一致するものを探す。
            var asset = Screens.FirstOrDefault(a => a.name == prop.Name);
            if (asset == null)
            {
                Debug.LogError($"VisualTreeAssetが見つかりません: {prop.Name}");
                shouldStop = true;
                continue;
            }

            // インスタンス化して、MainUIのプロパティにセットする。
            var element = (VisualElement)asset.Instantiate();
            element.style.display = DisplayStyle.None;
            if (!reinitialize)
            {
                var constructor = prop.PropertyType.GetConstructor(new[] { typeof(VisualElement) });
                var screen = (IScreen)constructor.Invoke(new[] { element });
                screen.Initialize();
                prop.SetValue(ui, screen);
            }
            // 再初期化の場合
            else
            {
                var screen = (IScreen)prop.GetValue(ui);
                screen.ReinitializeComponent(element);
                screen.Reinitialize();
            }
            // 画面コンテナに追加する。
            ui.UIContainer.Add(element);
        }

        // ついでにMainUIのScreensプロパティもセットする。
        ui.Screens = panelProps.Select(p => (IScreen)p.GetValue(ui)).ToArray();

        // ついでに自身のプロパティにnullがないかもチェックする。
        var selfNullProps = GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(VisualTreeAsset) && p.GetValue(this) == null)
            .ToList();
        foreach (var prop in selfNullProps)
        {
            Debug.LogError($"VisualTreeAssetがセットされていません: {prop.Name}");
            shouldStop = true;
        }

        if (shouldStop)
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
    }
}

