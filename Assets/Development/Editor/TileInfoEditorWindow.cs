using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class TileInfoEditorWindow : EditorWindow
{
    private WorldData world;
    private Grid grid;

    private MapPosition targetPosition;
    private GameMapTile targetTile;
    private Country targetCountry;

    private bool isLocked = false;

    [MenuItem("開発/タイル情報")]
    public static void ShowWindow()
    {
        GetWindow<TileInfoEditorWindow>("タイル情報");
    }

    void OnEnable()
    {
        LoadWorld();
        grid = FindFirstObjectByType<Grid>();

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void LoadWorld()
    {
        var map = FindFirstObjectByType<UIMapManager>();
        map.Awake();
        world = DefaultData.Create();
        world.Map.AttachUI(map);
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    void DuringSceneGUI(SceneView sceneView)
    {
        var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        var hit = Physics2D.GetRayIntersection(ray);
        if (hit.collider != null)
        {
            var posGrid = grid.WorldToCell(hit.point);
            var pos = MapPosition.FromGrid(posGrid);
            if (!isLocked)
            {
                targetPosition = pos;
            }
            Repaint();
        }
    }

    private void Save()
    {
        Debug.Log("保存します。");
        DefaultData.SaveToResources(world);
        Resources.UnloadUnusedAssets();
    }


    private int countryIdForNewCastle;
    private int castleIdForNewCastle;
    private int castleIdForNewTown;
    private int relocateX;
    private int relocateY;

    void OnGUI()
    {
        targetTile = world.Map.GetTile(targetPosition);
        if (targetTile != null)
        {
            targetCountry = targetTile.Country;
        }

        // 特定のキーが押されたらロック状態をトグルする。
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F1)
        {
            Debug.Log("Toggle Lock");
            isLocked = !isLocked;
            GUI.FocusControl(null);
            Repaint();
        }

        if (targetTile == null) return;

        // 横並び
        // 左詰め
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label($"座標: {targetTile.Position} 地形: {targetTile.Terrain}", GUILayout.Width(150));
        GUILayout.Label($"Gmax: {GameMapTile.TileGoldMax(targetTile):000} Fmax:{GameMapTile.TileFoodMax(targetTile):0000}", GUILayout.Width(150));
        if (GUILayout.Button("再読み込み", GUILayout.Width(80)))
        {
            LoadWorld();
        }
        if (GUILayout.Button(isLocked ? "ロック解除" : "ロック", GUILayout.Width(100)))
        {
            isLocked = !isLocked;
        }
        if (GUILayout.Button("保存"))
        {
            Save();
            LoadWorld();
        }
        EditorGUILayout.EndHorizontal();

        // スクロール可能にする。
        EditorGUILayout.BeginScrollView(Vector2.zero);
        using var _ = Util.Defer(() => EditorGUILayout.EndScrollView());


        var hasCountry = targetCountry != null;
        if (hasCountry)
        {
            var ruler = targetCountry.Ruler;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(110));
            GUILayout.Label($"君主:{ruler.Name} 国:{targetCountry.Id}", GUILayout.Width(110));
            var rulerImage = FaceImageManager.Instance.GetImage(ruler);
            GUILayout.Box(rulerImage, new GUIStyle() { fixedWidth = 100, fixedHeight = 100, margin = new(5, 5, 0, 0) });
            EditorGUILayout.EndVertical();

            if (targetTile.HasCastle)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(100));
                var castle = targetTile.Castle;
                GUILayout.Label($"城主: {castle.Boss?.Name ?? ""}", GUILayout.Width(100));
                if (castle.Boss != null)
                {
                    var bossImage = FaceImageManager.Instance.GetImage(targetTile.Castle.Boss);
                    GUILayout.Box(bossImage, new GUIStyle() { fixedWidth = 100, fixedHeight = 100, margin = new(5, 5, 0, 0) });
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        if (hasCountry) countryIdForNewCastle = targetCountry.Id;
        if (targetTile.HasCastle) castleIdForNewCastle = targetTile.Castle.Id;
        if (targetTile.HasCastle) castleIdForNewTown = targetTile.Castle.Id;


        // 城情報
        if (targetTile.HasCastle)
        {
            var castle = targetTile.Castle;
            // ヘッダー
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"城情報 (ID: {castle.Id})", EditorStyles.boldLabel);
            GUILayout.Label("X");
            relocateX = EditorGUILayout.IntField(relocateX);
            GUILayout.Label("Y");
            relocateY = EditorGUILayout.IntField(relocateY);
            if (GUILayout.Button("再配置"))
            {
                var newPos = MapPosition.Of(relocateX, relocateY);
                var newTile = world.Map.GetTile(newPos);
                if (newTile.HasCastle)
                {
                    Debug.LogError("移動先に城があります。");
                    return;
                }
                world.Map.ReregisterCastle(newPos, castle);
                Save();
                LoadWorld();
                return;
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Label($"配下数: {Mathf.Max(0, castle.Members.Count - 1)}");
            foreach (var chara in castle.Members.Except(new[] { castle.Boss }))
            {
                GUILayout.Label($"・{chara.Name}");
            }

            EditorGUILayout.BeginHorizontal();
            castle.Strength = EditorGUILayout.FloatField("Strength", castle.Strength);
            castle.StrengthMax = EditorGUILayout.FloatField("Max", castle.StrengthMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            castle.Gold = EditorGUILayout.FloatField("Gold", castle.Gold);
            GUILayout.Label($"{castle.GoldIncome} / {castle.GoldIncomeMax} ({castle.GoldBalance:+0;-#})");
            EditorGUILayout.EndHorizontal();
                
            EditorGUILayout.BeginHorizontal();
            castle.Food = EditorGUILayout.FloatField("Food", castle.Food);
            GUILayout.Label($"{castle.FoodIncome} / {castle.FoodIncomeMax}");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("城を削除"))
            {
                // 確認ダイアログ
                if (!EditorUtility.DisplayDialog("確認", "本当に削除しますか？", "はい", "いいえ")) return;
                world.Map.UnregisterCastle(castle);

                Save();
                LoadWorld();
            }
        }
        else
        {
            GUILayout.Label("城なし", EditorStyles.boldLabel);
            countryIdForNewCastle = EditorGUILayout.IntField("国ID", countryIdForNewCastle);
            castleIdForNewCastle = EditorGUILayout.IntField("新城ID", castleIdForNewCastle);

            if (GUILayout.Button("城を作成"))
            {
                var country = world.Countries.First(c => c.Id == countryIdForNewCastle);
                var newCastle = new Castle
                {
                    Id = castleIdForNewCastle == -1 ? world.Castles.Max(c => c.Id) + 1 : castleIdForNewCastle,
                    Position = targetTile.Position,
                    Strength = 0,
                    StrengthMax = 999,
                    Gold = 0,
                    Food = 0,
                };
                world.Map.RegisterCastle(country, newCastle);
                Save();
                LoadWorld();
            }
        }

        // 町情報
        if (targetTile.HasTown)
        {
            var town = targetTile.Town;
            // ヘッダー
            EditorGUILayout.Space();
            GUILayout.Label($"町情報 (所属城ID: {town.Castle.Id})", EditorStyles.boldLabel);

            GUILayout.Label($"城主: {town.Castle.Boss?.Name ?? ""}");

            EditorGUILayout.BeginHorizontal();
            town.GoldIncome = EditorGUILayout.FloatField("GoldIncome", town.GoldIncome);
            town.GoldIncomeMax = EditorGUILayout.FloatField("Max", town.GoldIncomeMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            town.FoodIncome = EditorGUILayout.FloatField("FoodIncome", town.FoodIncome);
            town.FoodIncomeMax = EditorGUILayout.FloatField("Max", town.FoodIncomeMax);
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("町を削除"))
            {
                // 確認ダイアログ
                if (!EditorUtility.DisplayDialog("確認", "本当に削除しますか？", "はい", "いいえ")) return;
                world.Map.UnregisterTown(town);
                Save();
                LoadWorld();
            }
        }
        else
        {
            GUILayout.Label("町なし", EditorStyles.boldLabel);
            castleIdForNewTown = EditorGUILayout.IntField("所属城ID", castleIdForNewTown);
            if (GUILayout.Button("町を作成"))
            {
                var targetCastle = world.Castles.First(c => c.Id == castleIdForNewTown);
                var newTown = new Town
                {
                    Position = targetTile.Position,
                    GoldIncome = 0,
                    GoldIncomeMax = GameMapTile.TileGoldMax(targetTile),
                    FoodIncome = 0,
                    FoodIncomeMax = GameMapTile.TileFoodMax(targetTile),
                };
                world.Map.RegisterTown(targetCastle, newTown);
                Save();
                LoadWorld();
            }
        }
    }


    private static void Delay(Action action, int delayMilliseconds = 300)
    {
        EditorApplication.delayCall += async () =>
        {
            await Task.Delay(delayMilliseconds);
            action();
        };
    }

}
