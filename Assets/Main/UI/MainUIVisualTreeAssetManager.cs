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
    [field: SerializeField] private VisualTreeAsset[] Panels { get; set; }
    [field: SerializeField] public VisualTreeAsset CharacterTableRowItem { get; set; }

    public void InitializePanels(MainUI ui)
    {
        // MainUIの画面プロパティを全て取得する。
        var panelProps = ui.GetType()
            .GetProperties()
            .Where(p => p.PropertyType.GetInterface(nameof(IPanel)) != null)
            .ToList();
        
        var shouldStop = false;
        foreach (var prop in panelProps)
        {
            // VisualTreeAssetの名前とプロパティ名が一致するものを探す。
            var asset = Panels.FirstOrDefault(a => a.name == prop.Name);
            if (asset == null)
            {
                Debug.LogError($"VisualTreeAssetが見つかりません: {prop.Name}");
                shouldStop = true;
                continue;
            }

            // インスタンス化して、MainUIのプロパティにセットする。
            var element = asset.Instantiate();
            element.style.display = DisplayStyle.None;
            var constructor = prop.PropertyType.GetConstructor(new[] { typeof(VisualElement) });
            var panel = constructor.Invoke(new[] { element }) as IPanel;
            panel.Initialize();
            prop.SetValue(ui, panel);
            // 画面コンテナに追加する。
            ui.UIContainer.Add(element);
        }

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

