using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SystemSettingsWindow
{
    public SystemSettingsManager SystemSettings => SystemSettingsManager.Instance;
    public LocalizationManager L { get; set; }
    private Button[] playSpeedButtons;

    public void Initialize()
    {
        L.Register(this);

        var orientations = Util.EnumArray<OrientationSetting>();
        comboOrientation.index = Array.IndexOf(orientations, SystemSettings.Orientation);
        comboOrientation.RegisterValueChangedCallback(e =>
        {
            SystemSettings.Orientation = orientations[comboOrientation.index];
            SystemSettings.ApplyOrientation();
        });

        // 再生速度ボタンの初期化
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
                SystemSettings.PlaySpeedIndex = index;
                GameCore.Instance?.Booter.UpdatePlaySpeed(index);
                RefreshPlaySpeedButtons();
            };
        }

        CloseButton.clicked += () => Root.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        Root.style.display = DisplayStyle.Flex;
        RefreshPlaySpeedButtons();
    }

    private void RefreshPlaySpeedButtons()
    {
        var currentSpeedIndex = SystemSettings.PlaySpeedIndex;
        for (var i = 0; i < playSpeedButtons.Length; i++)
        {
            var btn = playSpeedButtons[i];
            btn.style.backgroundColor = i > currentSpeedIndex ? new Color(0.3f, 0.6f, 0.3f) : new Color(0.3f, 0.8f, 0.3f);
        }
    }
}

public class SystemSettingsManager
{
    public static SystemSettingsManager Instance { get; } = new SystemSettingsManager();

    public OrientationSetting Orientation
    {
        get => (OrientationSetting)PlayerPrefs.GetInt(nameof(Orientation), (int)OrientationSetting.Auto);
        set => PlayerPrefs.SetInt(nameof(Orientation), (int)value);
    }

    public int PlaySpeedIndex
    {
        get => PlayerPrefs.GetInt(nameof(PlaySpeedIndex), 3);
        set => PlayerPrefs.SetInt(nameof(PlaySpeedIndex), value);
    }

    public void ApplyOrientation()
    {
        var orientation = Orientation;
        switch (orientation)
        {
            case OrientationSetting.Auto:
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                break;
            case OrientationSetting.LandscapeLeft:
                Screen.orientation = ScreenOrientation.LandscapeLeft;
                break;
            case OrientationSetting.LandscapeRight:
                Screen.orientation = ScreenOrientation.LandscapeRight;
                break;
        }
    }
}

public enum OrientationSetting
{
    Auto,
    LandscapeLeft,
    LandscapeRight,
}

public enum LanguageSetting
{
    Auto,
    Japanese,
    English,
}
