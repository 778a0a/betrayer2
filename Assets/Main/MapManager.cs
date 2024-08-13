using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap uiTilemap;
    [SerializeField] private Tilemap terrainTilemap;
    [SerializeField] private Tilemap fortTilemap;

    public TilemapHelper Helper { get; set; }

    private void Awake()
    {
        Helper = new TilemapHelper(uiTilemap, terrainTilemap, fortTilemap);
    }

    private MapPosition currentMousePosition = MapPosition.Of(0, 0);
    void Update()
    {
        var mousePoint = Mouse.current.position.ReadValue();
        // マウスカーソル上のセルを取得する。
        var ray = Camera.main.ScreenPointToRay(mousePoint);
        var hit = Physics2D.GetRayIntersection(ray);
        // マウスカーソルがマップ上にない場合は何もしない。
        if (hit.collider == null)
        {
            // 必要ならハイライトを消す。
            if (Helper.IsValid(currentMousePosition))
            {
                Helper.GetUITile(currentMousePosition)?.SetCellBorder(false);
                currentMousePosition = MapPosition.Invalid;
            }
            return;
        }

        //var uiScale = MainUI.Root.panel.scaledPixelsPerPoint;
        //var uiPoint = new Vector2(mousePoint.x, Screen.height - mousePoint.y) / uiScale;
        //var element = MainUI.Root.panel.Pick(uiPoint);
        //// マウスカーソル上にUI要素（メッセージウィンドウなど）がある場合は何もしない。
        //if (element != null)
        //{
        //    // 必要ならハイライトを消す。
        //    if (currentMousePosition.IsValid)
        //    {
        //        Helper.GetUITile(currentMousePosition).SetCellBorder(false);
        //        currentMousePosition = MapPosition.Invalid;
        //    }
        //    return;
        //}

        // セルの位置を取得する。
        var posGrid = grid.WorldToCell(hit.point);
        var pos = MapPosition.FromGrid(posGrid);
        var isValidPos = Helper.IsValid(pos);
        if (currentMousePosition != pos)
        {
            // ハイライトを更新する。
            var prevPos = currentMousePosition;
            if (Helper.IsValid(prevPos)) Helper.GetUITile(prevPos).SetCellBorder(false);

            if (isValidPos)
            {
                currentMousePosition = pos;
                Helper.GetUITile(pos).SetCellBorder(true);
            }
            else
            {
                currentMousePosition = MapPosition.Invalid;
            }
        }
        // 必要ならクリックイベントを起こす。
        if (Mouse.current.leftButton.wasPressedThisFrame && isValidPos)
        {
            InvokeCellClickHandler(pos);
        }
    }


    private long currentClickHandlerId = 0;
    private EventHandler<MapPosition> cellClickHandler;
    private void DefaultCellClickHandler(object sender, MapPosition pos)
    {
        Debug.Log($"Clicked {pos}");
        //MainUI.CountryInfo.ShowCellInformation(World, pos);
        //MainUI.ShowCountryInfoScreen();
    }

    private void InvokeCellClickHandler(MapPosition pos)
    {
        if (cellClickHandler == null)
        {
            DefaultCellClickHandler(this, pos);
            return;
        }
        cellClickHandler?.Invoke(this, pos);
    }

    public IDisposable SetCellClickHandler(EventHandler<MapPosition> handler)
    {
        var id = ++currentClickHandlerId;
        cellClickHandler = handler;
        return Util.Defer(() =>
        {
            if (currentClickHandlerId != id) return;
            cellClickHandler = null;
        });
    }

    public class TilemapHelper
    {
        private readonly Tilemap uiTilemap;
        private readonly Tilemap terrainTilemap;
        private readonly Tilemap fortTilemap;

        private readonly Dictionary<MapPosition, HexTile> uiTiles;

        public TilemapHelper(Tilemap uiTilemap, Tilemap terrainTilemap, Tilemap fortTilemap)
        {
            this.uiTilemap = uiTilemap;
            this.terrainTilemap = terrainTilemap;
            this.fortTilemap = fortTilemap;

            uiTiles = uiTilemap.GetComponentsInChildren<HexTile>()
                .ToDictionary(h => MapPosition.FromGrid(uiTilemap.WorldToCell(h.transform.position)));
        }

        public bool IsValid(MapPosition pos) => uiTiles.ContainsKey(pos);

        public HexTile GetUITile(MapPosition pos)
        {
            if (!IsValid(pos)) return null;

            return uiTiles[pos];
        }
    }
}
