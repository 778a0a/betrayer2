using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class MainUIFrame : MainUIComponent
{
    private Button[] playSpeedButtons;

    public void Initialize()
    {
        buttonPlay.clicked += () =>
        {
            Core.TogglePlay();
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
                Core.Booter.UpdatePlaySpeed(index);
                RefreshUI();
            };
        }
    }

    public void RefreshUI()
    {
        labelCurrentDate.text = Core.GameDate.ToString();

        var paused = Core.Booter.hold;
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
            if (i > Core.Booter.PlaySpeedIndex)
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