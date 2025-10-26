using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SystemSettingsWindow
{
    private SystemSettingsManager Settings => SystemSettingsManager.Instance;
    private Button[] playSpeedButtons;

    public void Initialize()
    {
        // WebGLでなおかつスマホの場合のみ画面回転設定を表示
        var isWebGLMobile = Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform;
        OrientationContainer.style.display = Util.Display(isWebGLMobile);

        if (isWebGLMobile)
        {
            var orientations = Util.EnumArray<OrientationSetting>();
            comboOrientation.index = Array.IndexOf(orientations, Settings.Orientation);
            comboOrientation.RegisterValueChangedCallback(e =>
            {
                Settings.Orientation = orientations[comboOrientation.index];
                Settings.ApplyOrientation();
            });
        }

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
                Settings.PlaySpeedIndex = index;
                GameCore.Instance?.Booter.UpdatePlaySpeed(index);
                RefreshPlaySpeedButtons();
            };
        }

        // オートセーブ頻度の初期化
        comboAutoSaveFrequency.index = (int)Settings.AutoSaveFrequency;
        comboAutoSaveFrequency.RegisterValueChangedCallback(e =>
        {
            Settings.AutoSaveFrequency = (AutoSaveFrequency)comboAutoSaveFrequency.index;
        });

        // 勢力滅亡通知の初期化
        comboCountryEliminatedNotification.index = Settings.ShowCountryEliminatedNotification ? 1 : 0;
        comboCountryEliminatedNotification.RegisterValueChangedCallback(e =>
        {
            Settings.ShowCountryEliminatedNotification = comboCountryEliminatedNotification.index == 1;
        });

        CloseButton.clicked += () => Root.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        Root.style.display = DisplayStyle.Flex;
        RefreshPlaySpeedButtons();
    }

    private void RefreshPlaySpeedButtons()
    {
        var currentSpeedIndex = Settings.PlaySpeedIndex;
        for (var i = 0; i < playSpeedButtons.Length; i++)
        {
            var btn = playSpeedButtons[i];
            btn.style.backgroundColor = i > currentSpeedIndex ? new Color(0.3f, 0.6f, 0.3f) : new Color(0.3f, 0.8f, 0.3f);
        }
    }
}

public class SystemSettingsManager
{
    public static SystemSettingsManager Instance { get; } = new();

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

    public AutoSaveFrequency AutoSaveFrequency
    {
        get => (AutoSaveFrequency)PlayerPrefs.GetInt(nameof(AutoSaveFrequency), (int)AutoSaveFrequency.EveryYear);
        set => PlayerPrefs.SetInt(nameof(AutoSaveFrequency), (int)value);
    }

    public bool ShowCountryEliminatedNotification
    {
        get => PlayerPrefs.GetInt(nameof(ShowCountryEliminatedNotification), 1) == 1;
        set => PlayerPrefs.SetInt(nameof(ShowCountryEliminatedNotification), value ? 1 : 0);
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

public enum AutoSaveFrequency
{
    None,
    EveryTwoYears,
    EveryYear,
    EverySixMonths,
    EveryThreeMonths,
    EveryPhase,
}
