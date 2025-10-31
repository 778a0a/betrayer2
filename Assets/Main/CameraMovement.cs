using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private float panSpeed = 5f;
    [SerializeField] private float panBorderThickness = 0.1f;
    [SerializeField] private Transform panLimitUpLeftObject;
    private Vector2 panLimitUpLeft => panLimitUpLeftObject.position;
    [SerializeField] private Transform panLimitDownRightObject;
    private Vector2 panLimitDownRight => panLimitDownRightObject.position;
    private new Camera camera;

    private float zoomMin = 2f;
    private float zoomMax = 10f;

    private float accelerationMultiplier = 4;
    private float vAccelaration = 0;
    private float hAccelaration = 0;

    private float topPanBorder;
    private float bottomPanBorder;
    private float leftPanBorder;
    private float rightPanBorder;

    private Vector2 lastMousePosition;
    private bool isDragging = false;

    // UI scroll control variables
    private bool isUIScrolling = false;
    private Vector2 uiScrollDirection = Vector2.zero;

    // Screen size change detection
    private int lastScreenWidth;
    private int lastScreenHeight;


    private void Start()
    {
        TryGetComponent(out camera);
        UpdatePanBorders();
    }

    private void UpdatePanBorders()
    {
        var rect = camera.rect;
        topPanBorder = Screen.height * rect.yMax - Screen.height * rect.height * panBorderThickness;
        bottomPanBorder = Screen.height * rect.yMin + Screen.height * rect.height * panBorderThickness;
        leftPanBorder = Screen.width * rect.xMin + Screen.width * rect.width * panBorderThickness;
        rightPanBorder = Screen.width * rect.xMax - Screen.width * rect.width * panBorderThickness;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    void LateUpdate()
    {
        // 画面サイズが変更された場合、画面端の位置を更新
        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdatePanBorders();
        }

        Vector3 pos = transform.position;
        var mousePosition = Mouse.current.position.value;

        // マウス右ボタンのドラッグ処理（中央ボタンが良かったけどWebGLだとブラウザの動作と競合して問題があった）
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            isDragging = true;
            lastMousePosition = mousePosition;
        }
        else if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        var prevMousePosition = lastMousePosition;
        if (isDragging)
        {
            Vector2 mouseDelta = lastMousePosition - mousePosition;

            // スクリーン座標をワールド座標の移動量に変換
            // カメラの表示範囲（高さ）を基準にする
            float worldHeight = camera.orthographicSize * 2f;
            float worldWidth = worldHeight * camera.aspect;

            // ピクセル移動量をワールド座標に変換
            Vector3 worldDelta = new Vector3(
                mouseDelta.x / Screen.width * worldWidth,
                mouseDelta.y / Screen.height * worldHeight,
                0
            );

            pos += worldDelta;
            lastMousePosition = mousePosition;
        }

        // 画面端でのカメラ移動処理
        if (!isDragging)
        {
            // 右側のUI領域（画面幅の約44%）にマウスがある場合は上下スクロールを無効化
            // フルHD(1920x1080)での840px ≈ 43.75%
            const float UIAreaRatio = 0.4375f; // 840/1920
            bool isInUIArea = mousePosition.x >= Screen.width * (1f - UIAreaRatio);

            if (!isInUIArea && mousePosition.y >= topPanBorder)
            {
                vAccelaration = Mathf.Min(1, vAccelaration + Time.deltaTime * accelerationMultiplier);

            }
            else if (!isInUIArea && mousePosition.y <= bottomPanBorder)
            {
                vAccelaration = Mathf.Max(-1, vAccelaration - Time.deltaTime * accelerationMultiplier);
            }
            else
            {
                vAccelaration = vAccelaration * 0.9f;
            }
            pos.y += panSpeed * Time.deltaTime * vAccelaration;

            if (mousePosition.x >= rightPanBorder)
            {
                hAccelaration = Mathf.Min(1, hAccelaration + Time.deltaTime * accelerationMultiplier);
            }
            else if (mousePosition.x <= leftPanBorder)
            {
                hAccelaration = Mathf.Max(-1, hAccelaration - Time.deltaTime * accelerationMultiplier);
            }
            else
            {
                hAccelaration = hAccelaration * 0.9f;
            }
            pos.x += panSpeed * Time.deltaTime * hAccelaration;
        }

        // UI scroll processing
        if (isUIScrolling)
        {
            pos.x += panSpeed * Time.deltaTime * uiScrollDirection.x;
            pos.y += panSpeed * Time.deltaTime * uiScrollDirection.y;
        }


        // 移動しないならマウス位置も移動しない。
        var needWarp = false;
        var newX = Mathf.Clamp(pos.x, panLimitUpLeft.x, panLimitDownRight.x);
        if (isDragging && (
            pos.x <= panLimitUpLeft.x ||
            pos.x >= panLimitDownRight.x ||
            lastMousePosition.x <= 0 ||
            lastMousePosition.x >= Screen.width))
        {
            lastMousePosition.x = prevMousePosition.x;
            needWarp = true;
        }
        var newY = Mathf.Clamp(pos.y, panLimitDownRight.y, panLimitUpLeft.y);
        if (isDragging && (
            pos.y >= panLimitUpLeft.y ||
            pos.y <= panLimitDownRight.y ||
            lastMousePosition.y <= 0 ||
            lastMousePosition.y >= Screen.height
            ))
        {
            lastMousePosition.y = prevMousePosition.y;
            needWarp = true;
        }

        if (needWarp)
        {
            Mouse.current.WarpCursorPosition(lastMousePosition);
        }

        pos.x = newX;
        pos.y = newY;


        transform.position = pos;


        // ズーム処理
        var scroll = Mouse.current.scroll.ReadValue();
        if (scroll.y != 0)
        {
            // マウスカーソル上のセルを取得する。
            var mousePoint = Mouse.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(mousePoint);
            var hit = Physics2D.GetRayIntersection(ray);
            // マウスカーソルがマップ上にない場合は何もしない。
            if (hit.collider == null)
            {
                return;
            }
            var uiScale = GameCore.Instance.MainUI.Root.panel.scaledPixelsPerPoint;
            var uiPoint = new Vector2(mousePoint.x, Screen.height - mousePoint.y) / uiScale;
            var element = GameCore.Instance.MainUI.Root.panel.Pick(uiPoint);
            // マウスカーソル上にUI要素（メッセージウィンドウなど）がある場合は何もしない。
            if (element != null)
            {
                return;
            }

            // マウスのワールド座標を取得
            Vector3 mouseWorldPos = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            // 新しいズームレベルを計算
            var newZoom = Mathf.Clamp(camera.orthographicSize - scroll.y / 2, zoomMin, zoomMax);
            var zoomDelta = newZoom / camera.orthographicSize;
            
            // カメラの新しい位置を計算
            var newPos = transform.position;
            newPos = mouseWorldPos + (newPos - mouseWorldPos) * zoomDelta;
            
            // 適用
            camera.orthographicSize = newZoom;
            transform.position = new Vector3(
                Mathf.Clamp(newPos.x, panLimitUpLeft.x, panLimitDownRight.x),
                Mathf.Clamp(newPos.y, panLimitDownRight.y, panLimitUpLeft.y),
                newPos.z
            );
        }
    }

    public ValueTask ScrollTo(Vector3 worldPosition, float duration = 0.1f, float? speed = null)
    {
        var tcs = new ValueTaskCompletionSource();
        StartCoroutine(Do());
        IEnumerator Do()
        {
            const float ScreenWidth = 1920f;
            const float UIWidth = 840f;
            const float MapAreaWidth = ScreenWidth - UIWidth; // 1080px
            // スクリーンの中央からマップ領域の中央までのオフセット
            const float ScreenOffsetX = (MapAreaWidth - ScreenWidth) / 2; // -420px
            // 現在のカメラの表示範囲（ワールド座標系）
            float cameraWidth = camera.orthographicSize * 2f * camera.aspect;
            // スクリーンオフセットをワールド座標系に変換
            float worldOffsetX = ScreenOffsetX / ScreenWidth * cameraWidth;

            // worldPositionがゲーム領域の中央に来るようにカメラを配置する。
            var startPos = transform.position;
            var targetPos = startPos;
            targetPos.x = (worldPosition.x - worldOffsetX).Clamp(panLimitUpLeft.x, panLimitDownRight.x);
            targetPos.y = worldPosition.y.Clamp(panLimitDownRight.y, panLimitUpLeft.y);

            if (speed.HasValue)
            {
                var distance = Vector3.Distance(startPos, targetPos);
                duration = distance / speed.Value;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0, 1, elapsed / duration);
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            // 最終的にターゲット位置に合わせる。
            transform.position = targetPos;

            tcs.SetResult();
        }
        return tcs.Task;
    }

    public void StartUIScroll(Vector2 direction)
    {
        isUIScrolling = true;
        uiScrollDirection = direction;
    }

    public void StopUIScroll()
    {
        isUIScrolling = false;
        uiScrollDirection = Vector2.zero;
    }
}
