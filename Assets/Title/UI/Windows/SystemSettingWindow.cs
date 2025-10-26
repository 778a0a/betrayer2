using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SystemSettingWindow
{
    private SystemSetting Setting => SystemSetting.Instance;
    private Button[] playSpeedButtons;

    public void Initialize()
    {
        Root.style.display = DisplayStyle.None;

        // WebGLでなおかつスマホの場合のみ画面回転設定を表示
        var isWebGLMobile = Application.platform == RuntimePlatform.WebGLPlayer && Application.isMobilePlatform;
        OrientationContainer.style.display = Util.Display(isWebGLMobile);

        if (isWebGLMobile)
        {
            var orientations = Util.EnumArray<OrientationSetting>();
            comboOrientation.index = Array.IndexOf(orientations, Setting.Orientation);
            comboOrientation.RegisterValueChangedCallback(e =>
            {
                Setting.Orientation = orientations[comboOrientation.index];
                Setting.ApplyOrientation();
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
                Setting.PlaySpeedIndex = index;
                GameCore.Instance?.Booter.UpdatePlaySpeed(Setting.PlaySpeed);
                RefreshPlaySpeedButtons();
            };
        }

        // オートセーブ頻度の初期化
        comboAutoSaveFrequency.index = (int)Setting.AutoSaveFrequency;
        comboAutoSaveFrequency.RegisterValueChangedCallback(e =>
        {
            Setting.AutoSaveFrequency = (AutoSaveFrequency)comboAutoSaveFrequency.index;
        });

        // 勢力滅亡通知の初期化
        comboCountryEliminatedNotification.index = Setting.ShowCountryEliminatedNotification ? 1 : 0;
        comboCountryEliminatedNotification.RegisterValueChangedCallback(e =>
        {
            Setting.ShowCountryEliminatedNotification = comboCountryEliminatedNotification.index == 1;
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
        var currentSpeedIndex = Setting.PlaySpeedIndex;
        for (var i = 0; i < playSpeedButtons.Length; i++)
        {
            var button = playSpeedButtons[i];
            button.style.backgroundColor = i > currentSpeedIndex ? new Color(0.25f, 0.55f, 0.25f) : new Color(0.3f, 0.8f, 0.3f);
        }
    }
}

public class SystemSetting
{
    public static SystemSetting Instance { get; } = new();

    public OrientationSetting Orientation { get => _Orientation; set => SetValue(ref _Orientation, value); }
    private OrientationSetting _Orientation = (OrientationSetting)PlayerPrefs.GetInt(nameof(Orientation), (int)OrientationSetting.Auto);

    public float PlaySpeed => PlaySpeedIndex switch
    {
        0 => 0.25f,
        1 => 0.125f,
        2 => 0.05f,
        3 => 0.025f,
        4 or _ => 0f,
    };
    public int PlaySpeedIndex { get => _PlaySpeedIndex; set => SetValue(ref _PlaySpeedIndex, value); }
    private int _PlaySpeedIndex = PlayerPrefs.GetInt(nameof(PlaySpeedIndex), 2);

    public AutoSaveFrequency AutoSaveFrequency { get => _AutoSaveFrequency; set => SetValue(ref _AutoSaveFrequency, value); }
    private AutoSaveFrequency _AutoSaveFrequency = (AutoSaveFrequency)PlayerPrefs.GetInt(nameof(AutoSaveFrequency), (int)AutoSaveFrequency.EveryYear);

    public bool ShowCountryEliminatedNotification { get => _ShowCountryEliminatedNotification; set => SetValue(ref _ShowCountryEliminatedNotification, value); }
    private bool _ShowCountryEliminatedNotification = PlayerPrefs.GetInt(nameof(ShowCountryEliminatedNotification), 1) == 1;

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

    private void SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
    {
        field = value;

        if (typeof(T) == typeof(int))
        {
            PlayerPrefs.SetInt(propertyName, (int)(object)value);
        }
        else if (typeof(T).IsEnum)
        {
            PlayerPrefs.SetInt(propertyName, (int)(object)value);
        }
        else if (typeof(T) == typeof(bool))
        {
            PlayerPrefs.SetInt(propertyName, (bool)(object)value ? 1 : 0);
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
