using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲーム中・エディターウィンドウ両方で使うアセットの管理を担当します。
/// </summary>
public class StaticAssets : MonoBehaviour
{
    public static StaticAssets Instance { get; private set; }

    [SerializeField] private Texture2D soldierTexture;
    [SerializeField] private Sprite[] countrySprites;

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        Debug.Log("StaticAssets.Initialize");
        Instance = this;

        FaceImageManager.Instance.ClearCache();
        SoldierImageManager.Instance.Initialize(soldierTexture);
        Debug.Log("StaticAssets.Initialize Done");
    }

    public Sprite GetCountrySprite(int countryIndex)
    {
        return countrySprites[countryIndex];
    }
}
