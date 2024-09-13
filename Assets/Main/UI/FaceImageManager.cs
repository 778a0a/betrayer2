using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FaceImageManager
{
    public static FaceImageManager Instance { get; } = new();

    private readonly Dictionary<string, Texture2D> cacheImages = new();
    public void ClearCache() => cacheImages.Clear();

    public Texture2D GetImage(Character chara) => GetImage(chara.Id);
    public Texture2D GetImage(int charaId)
    {
        var path = $"CharacterImages/{charaId:0000}";
        if (cacheImages.TryGetValue(path, out var tex))
        {
            return tex;
        }

        tex = Resources.Load<Texture2D>(path);
        cacheImages[path] = tex;
        return tex;
    }

}
