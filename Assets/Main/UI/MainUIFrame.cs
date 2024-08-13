using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUIFrame
{
    public void Initialize()
    {
        buttonPlay.clicked += () =>
        {
            GameCore.Instance.TogglePlay();
        };
    }

    public void SetData(GameCore core)
    {
        labelCurrentDate.text = core.GameDate.ToString();
        buttonPlay.text = core.test.hold ? "停止" : "再生";
    }
}