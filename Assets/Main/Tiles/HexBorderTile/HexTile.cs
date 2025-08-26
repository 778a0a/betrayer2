using System;
using TMPro;
using UnityEngine;

[ExecuteAlways]
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
    private static readonly Props defaultProps = new()
    {
        color = Color.gray,
        order = 40,
        width = 0.025f,
    };
    private static readonly Props highlightProps = new()
    {
        color = Color.yellow,
        order = 41,
        width = 0.05f,
    };

    [SerializeField] private SpriteRenderer castleSprite;
    [SerializeField] private SpriteRenderer townSprite;
    [SerializeField] private SpriteRenderer countryFlagSprite;
    [SerializeField] private SpriteRenderer forceSprite;
    [SerializeField] private SpriteRenderer forceFlagSprite;
    [SerializeField] private TextMeshPro debugText;
    [SerializeField] private SpriteRenderer clickHighlight;

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

    public void SetClickHighlight(bool on)
    {
        clickHighlight.gameObject.SetActive(on);
    }

    public void SetCastle(bool exists)
    {
        castleSprite.enabled = exists;
    }

    public void SetTown(bool exists)
    {
        townSprite.enabled = exists;
    }

    public void SetCountryFlag(Sprite sprite)
    {
        countryFlagSprite.enabled = sprite != null;
        countryFlagSprite.sprite = sprite;
    }

    public void SetForce(Force force)
    {
        forceSprite.enabled = force != null;
        if (force != null)
        {
            forceSprite.transform.rotation = Quaternion.Euler(0, 0, 30 - 60 * (int)force.Direction);
        }
    }

    public void SetForceFlag(Sprite sprite)
    {
        forceFlagSprite.enabled = sprite != null;
        forceFlagSprite.sprite = sprite;
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

    public void ShowDebugText(string text, float durationSeconds = -1, Color? color = null)
    {
        debugText.gameObject.SetActive(true);
        debugText.text = text;
        debugText.color = color ?? Color.white;
        if (durationSeconds > 0)
        {
            Invoke(nameof(HideDebugText), durationSeconds);
        }

        void HideDebugText()
        {
            debugText.gameObject.SetActive(false);
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
