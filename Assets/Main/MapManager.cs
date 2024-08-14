using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    [SerializeField] public Grid grid;
    [SerializeField] public Tilemap uiTilemap;
    [SerializeField] public Tilemap terrainTilemap;
    [SerializeField] public Tilemap castleTilemap;
    [SerializeField] public Tile[] terrainTiles;
    [SerializeField] public Tile[] countryTiles;

    public GameMap Map { get; set; }

    private void Awake()
    {
        Map = new GameMap(this);
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
            if (Map.IsValid(currentMousePosition))
            {
                Map.GetTile(currentMousePosition)?.UI.SetCellBorder(false);
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
        var isValidPos = Map.IsValid(pos);
        if (currentMousePosition != pos)
        {
            // ハイライトを更新する。
            var prevPos = currentMousePosition;
            Map.GetTile(prevPos)?.UI.SetCellBorder(false);

            if (isValidPos)
            {
                currentMousePosition = pos;
                Map.GetTile(pos).UI.SetCellBorder(true);
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
}

