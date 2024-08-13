using UnityEngine;
using UnityEngine.InputSystem;

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


    private void Start()
    {
        TryGetComponent(out camera);
        var rect = camera.rect;
        topPanBorder = Screen.height * rect.yMax - Screen.height * rect.height * panBorderThickness;
        bottomPanBorder = Screen.height * rect.yMin + Screen.height * rect.height * panBorderThickness;
        leftPanBorder = Screen.width * rect.xMin + Screen.width * rect.width * panBorderThickness;
        rightPanBorder = Screen.width * rect.xMax - Screen.width * rect.width * panBorderThickness;
    }

    void LateUpdate()
    {
        Vector3 pos = transform.position;
        var mousePosition = Mouse.current.position.value;

        // マウス中央ボタンのドラッグ処理
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            Debug.Log("middle button pressed");
            isDragging = true;
            lastMousePosition = mousePosition;
        }
        else if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            Debug.Log("middle button released");
            isDragging = false;
        }

        var prevMousePosition = lastMousePosition;
        if (isDragging)
        {
            Vector2 mouseDelta = lastMousePosition - mousePosition;
            Debug.Log("dragging " + mouseDelta);
            pos += new Vector3(mouseDelta.x, mouseDelta.y, 0) / 100;
            lastMousePosition = mousePosition;
        }
        
        // 画面端でのカメラ移動処理
        if (false)
        {
            if (mousePosition.y >= topPanBorder)
            {
                vAccelaration = Mathf.Min(1, vAccelaration + Time.deltaTime * accelerationMultiplier);

            }
            else if (mousePosition.y <= bottomPanBorder)
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
        var zoom = camera.orthographicSize - scroll.y / 2;
        camera.orthographicSize = Mathf.Clamp(zoom, zoomMin, zoomMax);
    }
}
