using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TileInfoEditorWindow : EditorWindow
{
    private WorldData world;
    private Grid grid;

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
        var map = FindFirstObjectByType<MapManager>();
        map.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(map, null);
        world = DefaultData.Create(map.Map);

        grid = FindFirstObjectByType<Grid>();

        SceneView.duringSceneGui += DuringSceneGUI;
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
                targetTile = world.Map.GetTile(pos);
                if (targetTile != null)
                {
                    targetCountry = targetTile.Country;
                    GUI.FocusControl(null);
                }
            }
            Repaint();
        }
    }

    void OnGUI()
    {
        if (targetTile == null) return;

        // 横並び
        // 左詰め
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"座標: {targetTile.Position}", GUILayout.Width(80));
        EditorGUILayout.LabelField($"地形: {targetTile.Terrain}");
        EditorGUILayout.EndHorizontal();
        
        var hasCountry = targetCountry != null;
        if (hasCountry)
        {
            EditorGUILayout.BeginHorizontal();
            var ruler = targetCountry.Ruler;
            if (targetTile.Castle.Exists) EditorGUILayout.LabelField($"城あり", GUILayout.Width(50));
            if (targetTile.Town.Exists) EditorGUILayout.LabelField($"町あり", GUILayout.Width(50));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"君主: {ruler.Name ?? ""}", GUILayout.Width(100));
            var rulerImage = FaceImageManager.Instance.GetImage(ruler);
            GUILayout.Box(rulerImage, new GUIStyle() { fixedWidth = 100, fixedHeight = 100, margin = new(5, 5, 0, 0) });

            // 城情報
            if (targetTile.Castle.Exists)
            {
                var castle = targetTile.Castle;
                // ヘッダー
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"城情報 (ID: {castle.Id})", EditorStyles.boldLabel);


                EditorGUILayout.LabelField($"城主: {castle.Boss?.Name ?? ""}");
                if (castle.Boss != null)
                {
                    var bossImage = FaceImageManager.Instance.GetImage(targetTile.Castle.Boss);
                    GUILayout.Box(bossImage, new GUIStyle() { fixedWidth = 100, fixedHeight = 100, margin = new(5, 5, 0, 0) });
                }

                EditorGUILayout.LabelField($"配下数: {Mathf.Max(0, castle.Members.Count - 1)}");
                foreach (var chara in castle.Members.Except(new[] { castle.Boss }))
                {
                    EditorGUILayout.LabelField($"・{chara.Name}");
                }
                EditorGUILayout.LabelField($"Strength: {castle.Strength} / {castle.StrengthMax}");
                EditorGUILayout.LabelField($"Gold: {castle.Gold} ({castle.GoldBalance:+0;-#}) {castle.GoldIncome} / {castle.GoldIncomeMax}");
                EditorGUILayout.LabelField($"Food: {castle.Food} {castle.FoodIncome} / {castle.FoodIncomeMax}");
            }

            // 町情報
            if (targetTile.Town.Exists)
            {
                var town = targetTile.Town;
                // ヘッダー
                EditorGUILayout.Space();
                EditorGUILayout.LabelField($"町情報 (所属城ID: {town.Castle.Id})", EditorStyles.boldLabel);

                EditorGUILayout.LabelField($"城主: {town.Castle.Boss?.Name ?? ""}");
                EditorGUILayout.LabelField($"食料: {town.FoodIncome} / {town.FoodIncomeMax}");
                EditorGUILayout.LabelField($"金: {town.GoldIncome} / {town.GoldIncomeMax}");
            }

        }
    }

}
