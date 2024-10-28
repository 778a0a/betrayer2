using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
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

    private bool prevFKey;
    private bool isLocked = false;
    private bool prevTKey;
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

            if (waitingClickForCharacterMove)
            {
                Event e = Event.current;
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    e.Use();
                    var tile = world.Map.TryGetTile(pos);
                    if (!tile?.HasCastle ?? true)
                    {
                        Debug.LogError("ターゲットが存在しません。");
                    }
                    else
                    {
                        var castle = tile.Castle;
                        var oldCastle = world.CastleOf(characterForCharacterMove);
                        oldCastle.Members.Remove(characterForCharacterMove);
                        castle.Members.Add(characterForCharacterMove);

                        Save();
                        LoadWorld();
                        waitingClickForCharacterMove = false;
                    }
                }
            }
        }

    }

    private void Save()
    {
        Debug.Log("保存します。");
        DefaultData.SaveToResources(world, saveDir);
        Resources.UnloadUnusedAssets();
    }


    private Vector2 scrollPosition;
    private string saveDir = "01";
    private int countryIdForNewCastle;
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

        // Ctrl+特定のキーが押されたらロック状態をトグルする。
        if (Keyboard.current.fKey.isPressed)
        {
            Debug.Log("Toggle Lock");
        }
        if (Event.current.control)
        {
            var oldFKey = prevFKey;
            prevFKey = Keyboard.current.fKey.isPressed;
            if (prevFKey && !oldFKey)
            {
                Debug.Log("Toggle Lock");
                isLocked = !isLocked;
                GUI.FocusControl(null);
                Repaint();
            }
            var oldTKey = prevTKey;
            prevTKey = Keyboard.current.tKey.isPressed;
            if (prevTKey && !oldTKey)
            {
                mode = (EditMode)((int)(mode + 1) % Enum.GetValues(typeof(EditMode)).Length);
                Debug.Log($"Change Edit Mode: {mode}");
                GUI.FocusControl(null);
                Repaint();
            }
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

            if (GUILayout.Button(mode.ToString(), GUILayout.Width(150)))
            {
                mode = (EditMode)((int)(mode + 1) % Enum.GetValues(typeof(EditMode)).Length);
            }
        }

        if (targetTile == null)
        {
            return;
        }

        using (HorizontalLayout())
        {
            Label($"座標: {targetTile.Position} 地形: {targetTile.Terrain}", 150);
            Label($"Gmax: {GameMapTile.TileGoldMax(targetTile):000} Fmax:{GameMapTile.TileFoodMax(targetTile):0000}", 150);
        }

        // スクロール可能にする。
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        using var _scroll = Util.Defer(() => EditorGUILayout.EndScrollView());

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

    private Dictionary<int, int> characterId2Order;
    private int currentPage = 0;
    private int characterPerPage = 4;
    private void DrawFree()
    {
        if (GUILayout.Button("順番リフレッシュ"))
        {
            characterId2Order = null;
            GUI.FocusControl(null);
        }

        GUILayout.Label("所属なし");
        if (characterId2Order == null)
        {
            characterId2Order = new Dictionary<int, int>();
            var characters = world.Characters
                .Where(c => c.Country == null)
                .OrderBy(c => c.csvDebugMemo)
                .ToArray();
            for (int i = 0; i < characters.Length; i++)
            {
                characterId2Order[characters[i].Id] = i;
            }
        }

        var frees = world.Characters
            .Where(c => c.Country == null)
            .OrderBy(c => characterId2Order[c.Id])
            .ToArray();
        var pageCount = frees.Length / characterPerPage;

        GUILayout.BeginHorizontal();
        // 前後ボタン
        if (GUILayout.Button("前へ"))
        {
            currentPage = Mathf.Max(0, currentPage - 1);
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("次へ"))
        {
            currentPage = Mathf.Min(pageCount, currentPage + 1);
            GUI.FocusControl(null);
        }

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageUp)
        {
            currentPage = Mathf.Max(0, currentPage - 1);
            GUI.FocusControl(null);
            Repaint();
        }
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.PageDown)
        {
            currentPage = Mathf.Min(pageCount, currentPage + 1);
            GUI.FocusControl(null);
            Repaint();
        }

        // 現在のページとページ数
        GUILayout.Label($"{currentPage + 1}/{pageCount + 1}");
        GUILayout.EndHorizontal();

        //scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        var startIndex = currentPage * characterPerPage;
        var endIndex = Mathf.Min((currentPage + 1) * characterPerPage, frees.Length);
        for (int i = startIndex; i < endIndex; i++)
        {
            DrawCharacter(frees[i]);
        }
        //GUILayout.EndScrollView();
    }

    private bool waitingClickForCharacterMove;
    private Character characterForCharacterMove;

    private int moveCharacterCastleId;
    private void DrawEditCharacter()
    {
        var castle = targetTile.Castle;
        if (castle == null)
        {
            DrawFree();
            return;
        }

        BoldLabel("キャラ一覧");
        foreach (var chara in castle.Members)
        {
            DrawCharacter(chara);
        }
        if (GUILayout.Button("新規キャラクター追加"))
        {
            var newChara = new Character
            {
                Id = world.Characters.Max(c => c.Id) + 1,
                Name = "新規キャラクター",
                Attack = 70,
                Defense = 70,
                Intelligence = 70,
                Governing = 70,
                LoyaltyBase = 80,
                // 適当に既存のデータを流用する。すぐに再読込みするので問題ない。
                Soldiers = world.Characters[0].Soldiers,
            };
            world.Characters.Add(newChara);
            castle.Members.Add(newChara);
            Save();
            LoadWorld();
        }
    }


    private void DrawCharacter(Character chara)
    {
        GUILayout.Space(5);
        using var _ = HorizontalLayout();
        var prev = chara.csvDebugData;
        chara.csvDebugData = DropableCharaImage(chara.csvDebugData, chara);
        if (prev != chara.csvDebugData)
        {
            Debug.Log("Prev: " + prev);
        }
        static string DropableCharaImage(string path, Character chara)
        {
            var dropArea = GUILayoutUtility.GetRect(200f, 200.0f, GUILayout.ExpandWidth(true));
            CharaImage(chara, rect: dropArea);
            var e = Event.current;
            switch (e.type)
            {
                // なぜかExplorerからドロップしてもDragPerformが呼ばれないので、
                // DragUpdatedで更新する。
                case EventType.DragUpdated:
                    //case EventType.DragPerform:
                    if (!dropArea.Contains(e.mousePosition)) break;
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.activeControlID = 0;
                    Event.current.Use();
                    foreach (var p in DragAndDrop.paths)
                    {
                        Debug.Log($"Accepted: {p}");
                        return p;
                    }
                    break;
            }
            return path;
        }

        using var __ = VerticalLayout();
        using (HorizontalLayout())
        {
            Label($"ID:{chara.Id}", 50);
            chara.Name = EditorGUILayout.TextField(chara.Name);
            Label("");
        }

        int ParamField(string label, int value, Color color, int max = 100)
        {
            GUILayout.BeginHorizontal();
            Label(label, 15);
            value = EditorGUILayout.IntField(value, GUILayout.Width(40));
            var rect = GUILayoutUtility.GetRect(1, 20);
            value = (int)GUI.HorizontalSlider(rect, value, 0, max);
            EditorGUI.DrawRect(rect, Color.gray);
            var rect2 = new Rect(rect.xMin, rect.yMin, rect.width * value / (float)max, rect.height);
            EditorGUI.DrawRect(rect2, color);

            GUILayoutUtility.GetRect(1, 20);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            return value;
        }

        // 能力値
        chara.Attack = ParamField("A", chara.Attack, Color.red);
        chara.Defense = ParamField("D", chara.Defense, Color.green);
        chara.Intelligence = ParamField("I", chara.Intelligence, Color.cyan);
        chara.Governing = ParamField("G", chara.Governing, new Color(1, 0.5f, 0));
        //chara.LoyaltyBase = ParamField("L", chara.LoyaltyBase, Color.yellow);
        chara.Contribution = ParamField("C", chara.Contribution, Color.black);
        //chara.Prestige = ParamField("P", chara.Prestige, Color.white);
        Label($"合計: {chara.Attack + chara.Defense + chara.Intelligence + chara.Governing}");

        void TraitCheckbox(Character chara, Traits target)
        {
            var on = chara.Traits.HasFlag(target);
            var rect = GUILayoutUtility.GetRect(0, 20, GUILayout.Width(85));
            var onAfter = GUI.Toggle(rect, on, target.ToString(), EditorStyles.miniButton);
            if (onAfter) chara.Traits |= target;
            else chara.Traits &= ~target;
        }

        //// 特性
        Label(chara.Traits.ToString());
        //GUILayout.BeginHorizontal();
        //var traits = Util.EnumArray<Traits>();
        //for (var i = 0; i < traits.Length; i++)
        //{
        //    var trait = traits[i];
        //    if (trait == Traits.None) continue;
        //    if (i % 5 == 0) GUILayout.EndHorizontal();
        //    if (i % 5 == 0) GUILayout.BeginHorizontal();
        //    TraitCheckbox(chara, trait);
        //}
        //GUILayout.EndHorizontal();

        // memo
        chara.csvDebugMemo = EditorGUILayout.TextField("Memo", chara.csvDebugMemo);


        // 顔画像パス直接入力
        //chara.csvDebugData = EditorGUILayout.TextField("顔画像", chara.csvDebugData);

        //城移動
        //using (HorizontalLayout())
        //{
        //    var moving = chara == characterForCharacterMove && waitingClickForCharacterMove;
        //    var color = moving ? Color.yellow : Color.white;
        //    var style = new GUIStyle(GUI.skin.button);
        //    style.normal.textColor = color;
        //    if (GUILayout.Button(moving ? "城をクリック!" : "マップクリックで移動", style, GUILayout.Width(120)))
        //    {
        //        var on = chara != characterForCharacterMove;
        //        waitingClickForCharacterMove = on;
        //        characterForCharacterMove = null;
        //        if (on)
        //        {
        //            characterForCharacterMove = chara;
        //        }
        //    }
        //}
    }
    private void DrawEditBuilding()
    {
        var hasCountry = targetCountry != null;
        if (hasCountry) countryIdForNewCastle = targetCountry.Id;
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
            Label("発展度");
            castle.DevelopmentLevel = EditorGUILayout.IntField(castle.DevelopmentLevel);
            Label("城塞レベル");
            castle.FortressLevel = EditorGUILayout.IntField(castle.FortressLevel);
            EditorGUILayout.EndHorizontal();

            castle.Strength = EditorGUILayout.FloatField("Strength", castle.Strength);

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

            if (GUILayout.Button("城を作成"))
            {
                var country = world.Countries.First(c => c.Id == countryIdForNewCastle);
                var newCastle = new Castle
                {
                    Id = world.Castles.Max(c => c.Id) + 1,
                    Position = targetTile.Position,
                    Strength = 0,
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
            town.GoldIncome = EditorGUILayout.FloatField("GoldIncome", town.GoldIncome, GUILayout.Width(250));
            Label($"Max: {town.GoldIncomeMax} (Base: {town.GoldIncomeMaxBase})");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            town.FoodIncome = EditorGUILayout.FloatField("FoodIncome", town.FoodIncome, GUILayout.Width(250));
            Label($"Max: {town.FoodIncomeMax} (Base: {town.FoodIncomeMaxBase})");
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
                    FoodIncome = 0,
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

                var i = 0;
                foreach (var chara in castle.Members.Where(m => m != castle.Boss))
                {
                    if (i % 6 == 0)
                    {
                        GUILayout.BeginHorizontal();
                    }

                    SmallCharaImage(chara);

                    if (i % 6 == 5)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.Space(5);
                    }
                    i++;
                }
                if (i % 6 != 0)
                {
                    GUILayout.EndHorizontal();
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
    private static void CharaImage(Character chara, int size = -1, Rect? rect = null)
    {
        if (size == -1) size = 100;
        var image = FaceImageManager.Instance.GetImage(chara);

        if (rect != null)
        {
            GUI.DrawTexture(rect.Value, image);
            return;
        }

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
}
