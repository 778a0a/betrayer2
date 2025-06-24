using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUIFrame
{
    private Button[] playSpeedButtons;

    public void Initialize()
    {
        buttonPlay.clicked += () =>
        {
            GameCore.Instance.TogglePlay();
        };

        playSpeedButtons = new[]
        {
            buttonPlaySpeed0,
            buttonPlaySpeed1,
            buttonPlaySpeed2,
            buttonPlaySpeed3,
            buttonPlaySpeed4,
        };
        for (var i = 0; i < playSpeedButtons.Length; i++)
        {
            var index = i;
            playSpeedButtons[i].clicked += () =>
            {
                GameCore.Instance.test.UpdatePlaySpeed(index);
                SetDatePanelData(GameCore.Instance);
            };
        }
    }

    public void SetDatePanelData(GameCore core)
    {
        labelCurrentDate.text = core.GameDate.ToString();

        var paused = core.test.hold;
        if (paused)
        {
            buttonPlay.text = "停止";
        }
        else
        {
            buttonPlay.text = "再生";
        }

        for (var i = 0; i < playSpeedButtons.Length; i++)
        {
            var btn = playSpeedButtons[i];
            if (i > GameCore.Instance.test.PlaySpeedIndex)
            {
                btn.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f));
            }
            else
            {
                if (paused)
                {
                    btn.style.backgroundColor = new StyleColor(new Color(0.8f, 0.3f, 0.3f));
                }
                else
                {
                    btn.style.backgroundColor = new StyleColor(new Color(0.3f, 0.8f, 0.3f));
                }
            }
        }
    }
}