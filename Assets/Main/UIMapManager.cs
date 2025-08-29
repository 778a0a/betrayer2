using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class UIMapManager : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] public Tilemap tilemap;
    [SerializeField] private MainUI ui;

    public event EventHandler<MapPosition> CellMouseOver;

    public GameMapManager Map { get; private set; }
    public void AttachGameMap(GameMapManager map)
    {
        Map = map;
    }

    private MapPosition currentMousePosition = MapPosition.Of(0, 0);
    void Update()
    {
        if (Map == null) return;

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

        var uiScale = ui.Root.panel.scaledPixelsPerPoint;
        var uiPoint = new Vector2(mousePoint.x, Screen.height - mousePoint.y) / uiScale;
        var element = ui.Root.panel.Pick(uiPoint);
        // マウスカーソル上にUI要素（メッセージウィンドウなど）がある場合は何もしない。
        if (element != null)
        {
            //Debug.Log($"UI Element: {element}");
            // 必要ならハイライトを消す。
            if (currentMousePosition.IsValid)
            {
                Map.TryGetTile(currentMousePosition)?.UI.SetCellBorder(false);
                currentMousePosition = MapPosition.Invalid;
            }
            return;
        }

        // セルの位置を取得する。
        var posGrid = grid.WorldToCell(hit.point);
        var pos = MapPosition.FromGrid(posGrid);
        var isValidPos = Map.IsValid(pos);
        if (currentMousePosition != pos)
        {
            // ハイライトを更新する。
            var prevPos = currentMousePosition;
            Map.TryGetTile(prevPos)?.UI.SetCellBorder(false);

            if (isValidPos)
            {
                currentMousePosition = pos;
                var tile = Map.GetTile(pos);
                tile.UI.SetCellBorder(true);
                CellMouseOver?.Invoke(this, pos);
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

            var tile = Map.GetTile(pos);
            tile.UI.SetClickHighlight(true);
            StartCoroutine(aaa());
            IEnumerator aaa()
            {
                while (Mouse.current.leftButton.isPressed)
                {
                    yield return null;
                }
                tile.UI.SetClickHighlight(false);
            }

        }
    }


    private long currentClickHandlerId = 0;
    private EventHandler<MapPosition> cellClickHandler;
    private void DefaultCellClickHandler(object sender, MapPosition pos)
    {
        Debug.Log($"Clicked {pos}");
        ui.TileInfoScreen.Show(pos);
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

