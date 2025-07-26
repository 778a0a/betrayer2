using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// ゲーム中・エディターウィンドウ両方で使うアセットの管理を担当します。
/// </summary>
public class Static : MonoBehaviour
{
    public static Static Instance { get; private set; }

    [SerializeField] private Texture2D soldierTexture;
    [SerializeField] private Sprite[] countrySprites;

    private FaceImageManager faces;
    private SoldierImageManager soldiers;

    public void Awake()
    {
        Initialize();
    }

    public void Initialize()
    {
        Instance = this;

        faces = new FaceImageManager();
        soldiers = new SoldierImageManager(soldierTexture);
    }

    public static Sprite GetCountrySprite(int countryIndex)
    {
        return Instance.countrySprites[countryIndex];
    }

    public static Texture2D GetFaceImage(Character chara)
    {
        return Instance.faces.GetImage(chara);
    }

    public static Texture2D GetSoldierImage(int level)
    {
        return Instance.soldiers.GetTexture(level);
    }

    public static Texture2D GetEmptySoldierImage()
    {
        return Instance.soldiers.GetEmptyTexture();
    }

}
