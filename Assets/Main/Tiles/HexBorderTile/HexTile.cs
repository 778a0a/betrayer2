using System;
using UnityEngine;

public class HexTile : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private Props PropsOn(bool highlight) => highlight ? highlightProps : defaultProps;

    private class Props
    {
        public Color color;
        public int order;
        public float width;
    }
    private readonly Props defaultProps = new()
    {
        color = Color.gray,
        order = 40,
        width = 0.025f,
    };
    private readonly Props highlightProps = new()
    {
        color = Color.yellow,
        order = 41,
        width = 0.05f,
    };

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        Width = PropsOn(false).width;
        Color = PropsOn(false).color;
        lineRenderer.positionCount = 7;
        for (int i = 0; i < 7; i++)
        {
            float angle = 2 * Mathf.PI / 6 * i;
            var pos = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle), 0) / 2 * transform.lossyScale.x;
            lineRenderer.SetPosition(i, transform.position + pos);
        }
    }

    public void SetCellBorder(bool highlight)
    {
        Width = PropsOn(highlight).width;
        Color = PropsOn(highlight).color;
        lineRenderer.sortingOrder = PropsOn(highlight).order;
    }

    public Color Color
    {
        get => lineRenderer.startColor;
        set
        {
            lineRenderer.startColor = value;
            lineRenderer.endColor = value;
        }
    }

    public float Width
    {
        get => lineRenderer.widthCurve.keys[0].value;
        set
        {
            lineRenderer.widthCurve = AnimationCurve.Linear(0, value, 1, value);
        }
    }

}
