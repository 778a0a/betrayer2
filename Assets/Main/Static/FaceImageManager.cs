using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class FaceImageManager
{
    private readonly Dictionary<string, Texture2D> cacheImages = new();
    public void ClearCache() => cacheImages.Clear();

    public Texture2D GetImage(Character chara)
    {
#if UNITY_EDITOR
        if (File.Exists(chara.csvDebugData))
        {
            return GetImage(chara.csvDebugData);
        }
#endif
        var path = $"CharacterImages/{chara.Id:0000}";
        return GetImage(path);
    }

    public Texture2D GetImage(int characterId)
    {
        var path = $"CharacterImages/{characterId:0000}";
        return GetImage(path);
    }

    public Texture2D GetImage(string path)
    {
        if (cacheImages.TryGetValue(path, out var tex))
        {
            return tex;
        }

#if UNITY_EDITOR
        if (File.Exists(path))
        {
            var bytes = File.ReadAllBytes(path);
            tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            cacheImages[path] = tex;
            return tex;
        }
#endif

        tex = Resources.Load<Texture2D>(path);
        cacheImages[path] = tex;
        return tex;
    }

}
