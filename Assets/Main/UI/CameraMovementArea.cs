using UnityEngine;
using UnityEngine.UIElements;

public partial class CameraMovementArea
{
    private CameraMovement cameraMovement;

    public void Initialize(CameraMovement cameraMovement)
    {
        this.cameraMovement = cameraMovement;
        var zones = new[]
        {
            (TopScrollZone, Vector2.up),
            (BottomScrollZone, Vector2.down),
            (LeftScrollZone, Vector2.left),
            (RightScrollZone, Vector2.right)
        };
        foreach (var (zone, direction) in zones)
        {
            zone.RegisterCallback<MouseDownEvent>(evt => StartScroll(direction));
            zone.RegisterCallback<MouseUpEvent>(evt => StopScroll());
            zone.RegisterCallback<MouseLeaveEvent>(evt => StopScroll());
        }
    }

    private void StartScroll(Vector2 direction)
    {
        Debug.Log("StartScroll: " + direction);
        cameraMovement.StartUIScroll(direction);
    }

    private void StopScroll()
    {
        cameraMovement.StopUIScroll();
    }
}