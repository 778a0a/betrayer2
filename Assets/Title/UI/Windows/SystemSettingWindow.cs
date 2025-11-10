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
        if (SystemSetting.IsWebGLMobile)
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

        // 画面端自動スクロールの初期化
        comboEdgeScrollEnabled.index = Setting.EdgeScrollEnabled ? 1 : 0;
        comboEdgeScrollEnabled.RegisterValueChangedCallback(e =>
        {
            Setting.EdgeScrollEnabled = comboEdgeScrollEnabled.index == 1;
        });

        // 戦闘設定の初期化
        InitializeBattleModeButtons(
            new[] { buttonBattleModeSelfManual, buttonBattleModeSelfWatch, buttonBattleModeSelfDoubleSpeed, buttonBattleModeSelfSkip },
            new[] { BattleMode.Manual, BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            mode => Setting.BattleModeSelf = mode,
            () => Setting.BattleModeSelf
        );

        InitializeBattleModeButtons(
            new[] { buttonBattleModeOwnCastleWatch, buttonBattleModeOwnCastleDoubleSpeed, buttonBattleModeOwnCastleSkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            mode => Setting.BattleModeOwnCastle = mode,
            () => Setting.BattleModeOwnCastle
        );

        InitializeBattleModeButtons(
            new[] { buttonBattleModeSubordinateWatch, buttonBattleModeSubordinateDoubleSpeed, buttonBattleModeSubordinateSkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            mode => Setting.BattleModeSubordinate = mode,
            () => Setting.BattleModeSubordinate
        );

        InitializeBattleModeButtons(
            new[] { buttonBattleModeOwnCountryWatch, buttonBattleModeOwnCountryDoubleSpeed, buttonBattleModeOwnCountrySkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            mode => Setting.BattleModeOwnCountry = mode,
            () => Setting.BattleModeOwnCountry
        );

        CloseButton.clicked += () => Root.style.display = DisplayStyle.None;
    }

    private void InitializeBattleModeButtons(Button[] buttons, BattleMode[] modes, System.Action<BattleMode> setter, System.Func<BattleMode> getter)
    {
        for (var i = 0; i < buttons.Length; i++)
        {
            var mode = modes[i];
            buttons[i].clicked += () =>
            {
                setter(mode);
                RefreshBattleModeButton(buttons, modes, getter());
            };
        }
    }

    private void RefreshBattleModeButton(Button[] buttons, BattleMode[] modes, BattleMode currentMode)
    {
        var activeColor = new Color(0.3f, 0.8f, 0.3f);
        var inactiveColor = new Color(0.25f, 0.55f, 0.25f);

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].style.backgroundColor = modes[i] == currentMode ? activeColor : inactiveColor;
        }
    }

    public void Show()
    {
        Root.style.display = DisplayStyle.Flex;
        RefreshPlaySpeedButtons();
        RefreshBattleModeButtons();
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

    private void RefreshBattleModeButtons()
    {
        RefreshBattleModeButton(
            new[] { buttonBattleModeSelfManual, buttonBattleModeSelfWatch, buttonBattleModeSelfDoubleSpeed, buttonBattleModeSelfSkip },
            new[] { BattleMode.Manual, BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            Setting.BattleModeSelf
        );

        RefreshBattleModeButton(
            new[] { buttonBattleModeOwnCastleWatch, buttonBattleModeOwnCastleDoubleSpeed, buttonBattleModeOwnCastleSkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            Setting.BattleModeOwnCastle
        );

        RefreshBattleModeButton(
            new[] { buttonBattleModeSubordinateWatch, buttonBattleModeSubordinateDoubleSpeed, buttonBattleModeSubordinateSkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            Setting.BattleModeSubordinate
        );

        RefreshBattleModeButton(
            new[] { buttonBattleModeOwnCountryWatch, buttonBattleModeOwnCountryDoubleSpeed, buttonBattleModeOwnCountrySkip },
            new[] { BattleMode.Watch, BattleMode.WatchDoubleSpeed, BattleMode.Skip },
            Setting.BattleModeOwnCountry
        );
    }
}

public class SystemSetting
{
    public static SystemSetting Instance { get; } = new();

    public static bool IsWebGLMobile =>
        Application.platform == RuntimePlatform.WebGLPlayer &&
        Application.isMobilePlatform;

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
    private AutoSaveFrequency _AutoSaveFrequency = (AutoSaveFrequency)PlayerPrefs.GetInt(nameof(AutoSaveFrequency), (int)AutoSaveFrequency.EveryThreeMonths);

    public bool ShowCountryEliminatedNotification { get => _ShowCountryEliminatedNotification; set => SetValue(ref _ShowCountryEliminatedNotification, value); }
    private bool _ShowCountryEliminatedNotification = PlayerPrefs.GetInt(nameof(ShowCountryEliminatedNotification), 1) == 1;

    public bool EdgeScrollEnabled { get => _EdgeScrollEnabled; set => SetValue(ref _EdgeScrollEnabled, value); }
    private bool _EdgeScrollEnabled = PlayerPrefs.GetInt(nameof(EdgeScrollEnabled), 1) == 1;

    public BattleMode BattleModeSelf { get => _BattleModeSelf; set => SetValue(ref _BattleModeSelf, value); }
    private BattleMode _BattleModeSelf = (BattleMode)PlayerPrefs.GetInt(nameof(BattleModeSelf), (int)BattleMode.Manual);

    public BattleMode BattleModeOwnCastle { get => _BattleModeOwnCastle; set => SetValue(ref _BattleModeOwnCastle, value); }
    private BattleMode _BattleModeOwnCastle = (BattleMode)PlayerPrefs.GetInt(nameof(BattleModeOwnCastle), (int)BattleMode.Watch);

    public BattleMode BattleModeSubordinate { get => _BattleModeSubordinate; set => SetValue(ref _BattleModeSubordinate, value); }
    private BattleMode _BattleModeSubordinate = (BattleMode)PlayerPrefs.GetInt(nameof(BattleModeSubordinate), (int)BattleMode.Watch);

    public BattleMode BattleModeOwnCountry { get => _BattleModeOwnCountry; set => SetValue(ref _BattleModeOwnCountry, value); }
    private BattleMode _BattleModeOwnCountry = (BattleMode)PlayerPrefs.GetInt(nameof(BattleModeOwnCountry), (int)BattleMode.Watch);

    public void ApplyOrientation()
    {
        try
        {
            if (!IsWebGLMobile) return;
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
        catch (Exception ex)
        {
            Debug.LogError($"画面回転設定に失敗しました: {ex}");
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

public enum BattleMode
{
    Manual = 0,
    Watch,
    WatchDoubleSpeed,
    Skip,
}
