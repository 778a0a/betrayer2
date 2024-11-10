using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SoldierImageManager
{
    private static readonly Color[] colors = new Color[]
    {
        Util.Color("#666"),
        Util.Color("#04f"),
        Util.Color("#0ff"),
        Util.Color("#0a0"),
        Util.Color("#8f0"),
        Util.Color("#ff0"),
        Util.Color("#f80"),
        Util.Color("#f00"),
        Util.Color("#f0f"),
        Util.Color("#80f"),
        Util.Color("#000"),
        Util.Color("#fff"),
    };

    private readonly Dictionary<int, Texture2D> textures = new();
    private readonly Dictionary<int, Sprite> sprites = new();
    private Texture2D emptyTexture;

    public SoldierImageManager(Texture2D original)
    {
        emptyTexture = new Texture2D(0, 0);
        var replaceColors = new[]
        {
            (Util.Color("#8f563b"), 0.2f, 0.7f), // 服
            (Util.Color("#663931"), 0.8f, 0.8f), // 靴、スカーフ、帽子
            (Util.Color("#696a6a"), 0.8f, 0.8f), // 胴の鎧
            (Util.Color("#595652"), 0.3f, 0.7f), // その他の鎧
        };
        Color Merge(Color original, Color newColor)
        {
            foreach (var (targetColor, newColorWeight, newColorWeightHighLevel) in replaceColors)
            {
                if (original != targetColor) continue;
                var w = newColorWeight;
                if (newColor == colors[0] || newColor == colors[1])
                {
                    w = newColorWeightHighLevel;
                }

                var oldColorWeight = 1 - w;
                var r = original.r * oldColorWeight + newColor.r * w;
                var g = original.g * oldColorWeight + newColor.g * w;
                var b = original.b * oldColorWeight + newColor.b * w;
                return new Color(r, g, b);
            }
            return original;
        }
        for (int i = 0; i < colors.Length; i++)
        {
            var tex = new Texture2D(
                original.width,
                original.height,
                original.format,
                original.mipmapCount,
                false);
            Graphics.CopyTexture(original, tex);
            tex.filterMode = FilterMode.Point;
            var replacedPixels = tex.GetPixels()
                .Select(p => Merge(p, colors[i]))
                .ToArray();
            tex.SetPixels(replacedPixels);
            tex.Apply();
            textures[i] = tex;

            var rect = new Rect(0, 0, tex.width, tex.height);
            var pivot = new Vector2(0.5f, 0.5f);
            var newSprite = Sprite.Create(tex, rect, pivot);
            sprites[i] = newSprite;
        }
    }

    public Color GetColor(int level)
    {
        if (level >= colors.Length) level = colors.Length - 1;
        return colors[level];
    }

    public Texture2D GetTexture(int level)
    {
        if (level >= colors.Length) level = colors.Length - 1;
        return textures[level];
    }

    public Sprite GetSprite(int level)
    {
        if (level >= colors.Length) level = colors.Length - 1;
        return sprites[level];
    }

    public Texture2D GetEmptyTexture()
    {
        return emptyTexture;
    }
}
