using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class TileInfoEditorWindow : EditorWindow
{
    private WorldData world;
    private Grid grid;

    private MapPosition targetPosition;
    private GameMapTile targetTile;
    private Country targetCountry;

    private bool prevF1;
    private bool isLocked = false;
    private bool prevF2;
    private EditMode mode = EditMode.EditBuilding;
    private enum EditMode
    {
        EditBuilding,
        EditCharacter,
    }

    [MenuItem("開発/タイル情報")]
    public static void ShowWindow()
    {
        GetWindow<TileInfoEditorWindow>("タイル情報");
    }

    void OnEnable()
    {
        Debug.Log("OnEnable");
        LoadWorld();
        grid = FindFirstObjectByType<Grid>();

        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void LoadWorld()
    {
        // Playモードの場合は現在の状態を取得する。
        if (Application.isPlaying)
        {
            world = GameCore.Instance.World;
            return;
        }

        var map = FindFirstObjectByType<UIMapManager>();
        map.Awake();
        world = DefaultData.Create(saveDir);
        world.Map.AttachUI(map);
        world.Map.Tiles.ToList().ForEach(t => t.Refresh());
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
        DefaultData.SaveToResources(world, saveDir);
        Resources.UnloadUnusedAssets();
    }


    private string saveDir = "01";
    private int countryIdForNewCastle;
    private int castleIdForNewCastle;
    private int castleIdForNewTown;
    private int relocateX;
    private int relocateY;

    void OnGUI()
    {
        targetTile = world.Map.TryGetTile(targetPosition);
        if (targetTile != null)
        {
            targetCountry = targetTile.Country;
        }

        // 特定のキーが押されたらロック状態をトグルする。
        var oldF1 = prevF1;
        prevF1 = Keyboard.current.f1Key.isPressed;
        if (prevF1 && !oldF1)
        {
            Debug.Log("Toggle Lock");
            isLocked = !isLocked;
            GUI.FocusControl(null);
            Repaint();
        }
        var oldF2 = prevF2;
        prevF2 = Keyboard.current.f2Key.isPressed;
        if (prevF2 && !oldF2)
        {
            Debug.Log("Toggle Edit Mode");
            mode = mode == EditMode.EditBuilding ? EditMode.EditCharacter : EditMode.EditBuilding;
            GUI.FocusControl(null);
            Repaint();
        }


        // ヘッダー 保存ボタンなど
        using (HorizontalLayout())
        {
            Label($"保存先:");
            saveDir = EditorGUILayout.TextField(saveDir, GUILayout.Width(50));
            if (GUILayout.Button("再読込", GUILayout.Width(70)))
            {
                LoadWorld();
            }
            if (GUILayout.Button("保存"))
            {
                Save();
                LoadWorld();
            }

            var style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = isLocked ? Color.yellow : Color.white;
            if (GUILayout.Button(isLocked ? "ﾛｯｸ解除" : "ロック", style))
            {
                isLocked = !isLocked;
            }

            if (GUILayout.Button(mode == EditMode.EditBuilding ? "施設編集" : "キャラ編集"))
            {
                mode = mode == EditMode.EditBuilding ? EditMode.EditCharacter : EditMode.EditBuilding;
            }
        }

        // 有効なタイルでなければ何もしない。
        if (targetTile == null) return;


        using (HorizontalLayout())
        {
            Label($"座標: {targetTile.Position} 地形: {targetTile.Terrain}", 150);
            Label($"Gmax: {GameMapTile.TileGoldMax(targetTile):000} Fmax:{GameMapTile.TileFoodMax(targetTile):0000}", 150);
        }

        // スクロール可能にする。
        using var _scroll = ScrollView(Vector2.zero);

        if (targetTile.Country != null)
        {
            DrawBuildingOverview();
        }

        switch (mode)
        {
            case EditMode.EditBuilding:
                DrawEditBuilding();
                break;
            case EditMode.EditCharacter:
                DrawEditCharacter();
                break;
            default:
                break;
        }
    }

    private int moveCharacterCastleId;
    private void DrawEditCharacter()
    {
        var castle = targetTile.Castle;
        if (castle == null) return;

        BoldLabel("キャラ一覧");
        foreach (var chara in castle.Members)
        {
            GUILayout.Space(5);
            using var _ = HorizontalLayout();
            CharaImage(chara);

            using var __ = VerticalLayout();
            Label($"ID:{chara.Id}\t{chara}");

            using (HorizontalLayout())
            {
                Label("城ID:");
                moveCharacterCastleId = EditorGUILayout.IntField(moveCharacterCastleId);
                if (GUILayout.Button("へ移動"))
                {
                    var newCastle = world.Castles.FirstOrDefault(c => c.Id == moveCharacterCastleId);
                    if (newCastle == null)
                    {
                        Debug.LogError("移動先の城が見つかりません。");
                        return;
                    }
                    castle.Members.Remove(chara);
                    newCastle.Members.Add(chara);
                    Save();
                    LoadWorld();
                    return;
                }

            }
        }
    }

    private void DrawEditBuilding()
    {
        var hasCountry = targetCountry != null;
        if (hasCountry) countryIdForNewCastle = targetCountry.Id;
        if (targetTile.HasCastle) castleIdForNewCastle = targetTile.Castle.Id;
        if (targetTile.HasCastle) castleIdForNewTown = targetTile.Castle.Id;

        // 城情報
        if (targetTile.HasCastle)
        {
            var castle = targetTile.Castle;
            // ヘッダー
            EditorGUILayout.BeginHorizontal();
            BoldLabel($"城情報 (ID: {castle.Id})");
            Label("X");
            relocateX = EditorGUILayout.IntField(relocateX);
            Label("Y");
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

            EditorGUILayout.BeginHorizontal();
            castle.Strength = EditorGUILayout.FloatField("Strength", castle.Strength);
            castle.StrengthMax = EditorGUILayout.FloatField("Max", castle.StrengthMax);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            castle.Gold = EditorGUILayout.FloatField("Gold", castle.Gold);
            Label($"{castle.GoldIncome} / {castle.GoldIncomeMax} ({castle.GoldBalance:+0;-#})");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            castle.Food = EditorGUILayout.FloatField("Food", castle.Food);
            Label($"{castle.FoodIncome} / {castle.FoodIncomeMax}");
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
            BoldLabel("城なし");
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
            BoldLabel($"町情報 (所属城ID: {town.Castle.Id})");

            Label($"城主: {town.Castle.Boss?.Name ?? ""}");

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
            BoldLabel("町なし");
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

        ForceLayout();
    }

    /// <summary>
    /// 施設の概要を描画します。
    /// </summary>
    private void DrawBuildingOverview()
    {
        if (targetCountry == null) return;

        var ruler = targetCountry.Ruler;

        GUILayout.BeginHorizontal();
        using (VerticalLayout(GUILayout.Width(110)))
        {
            Label($"国: {targetCountry.Id} {ruler.Name}");
            CharaImage(ruler);
        }

        if (targetTile.HasCastle)
        {
            var castle = targetTile.Castle;
            using (VerticalLayout(GUILayout.Width(100)))
            {
                Label($"城: {castle.Id} {castle.Boss?.Name ?? ""}");
                if (castle.Boss != null)
                {
                    CharaImage(castle.Boss);
                }
            }

            using (VerticalLayout(GUILayout.Width(100)))
            {
                Label($"将数: {castle.Members.Count}");
                using (HorizontalLayout())
                {
                    foreach (var chara in castle.Members.Where(m => m != castle.Boss))
                    {
                        SmallCharaImage(chara);
                    }
                }
            }
        }
        GUILayout.EndHorizontal();
        //var movings = targetTile.Castle?.Members.Where(m => m.IsMoving);
        //if (movings != null)
        //{
        //    Label("出撃中: " + string.Join(", ", movings));
        //}
        //else
        //{
        //    GUILayout.Space(30);
        //}
    }

    private int forceCreateCharacterId = -1;
    private MapPosition forceCreateDestination;
    private void ForceLayout()
    {
        BoldLabel("軍勢追加");
        forceCreateCharacterId = EditorGUILayout.IntField("キャラID", forceCreateCharacterId);
        EditorGUILayout.BeginHorizontal();
        Label("目標座標");
        forceCreateDestination.x = EditorGUILayout.IntField("X", forceCreateDestination.x);
        forceCreateDestination.y = EditorGUILayout.IntField("Y", forceCreateDestination.y);
        EditorGUILayout.EndHorizontal();
        if (GUILayout.Button("追加"))
        {
            var destTile = world.Map.GetTile(forceCreateDestination);
            if (destTile == null)
            {
                Debug.LogError("ターゲットが存在しません。");
                return;
            }
            var chara = world.Characters.First(c => c.Id == forceCreateCharacterId);
            // すでに軍勢を率いている場合はエラー
            if (world.Forces.Any(f => f.Character == chara))
            {
                Debug.LogError("すでに軍勢を率いています。");
                return;
            }

            var force = new Force(world, chara, targetTile.Position);
            world.Forces.Register(force);
            force.SetDestination(destTile);
            Save();
            LoadWorld();
            return;
        }

        BoldLabel("軍勢一覧");
        foreach (var force in targetTile.Forces)
        {
            EditorGUILayout.BeginHorizontal();
            Label($"{force.Character.Name} (To: {force.Destination}) move: {force.TileMoveRemainingDays}", -1);
            if (GUILayout.Button("削除"))
            {
                world.Forces.Unregister(force);
                Save();
                LoadWorld();
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private static void SmallCharaImage(Character chara) => CharaImage(chara, 45);
    private static void CharaImage(Character chara, int size = -1)
    {
        if (size == -1) size = 100;
        var image = FaceImageManager.Instance.GetImage(chara);
        GUILayout.Box(image, new GUIStyle()
        {
            fixedWidth = size,
            fixedHeight = size,
            margin = new(5, 5, 0, 0),
        });
    }

    private static void BoldLabel(string label, int width = -1)
    {
        Label(label, width, EditorStyles.boldLabel);
    }
    private static void Label(string label, int width = -1, GUIStyle style = null)
    {
        style ??= EditorStyles.label;
        if (width == -1)
        {
            GUILayout.Label(label, style);
            return;
        }

        GUILayout.Label(label, style, GUILayout.Width(width));
    }

    private static void Delay(Action action, int delayMilliseconds = 300)
    {
        EditorApplication.delayCall += async () =>
        {
            await Task.Delay(delayMilliseconds);
            action();
        };
    }

    private IDisposable HorizontalLayout(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginHorizontal(options);
        return Util.Defer(() => EditorGUILayout.EndHorizontal());
    }

    private IDisposable VerticalLayout(params GUILayoutOption[] options)
    {
        EditorGUILayout.BeginVertical(options);
        return Util.Defer(() => EditorGUILayout.EndVertical());
    }

    private IDisposable ScrollView(Vector2 scrollPos)
    {
        EditorGUILayout.BeginScrollView(scrollPos);
        return Util.Defer(() => EditorGUILayout.EndScrollView());
    }

}
