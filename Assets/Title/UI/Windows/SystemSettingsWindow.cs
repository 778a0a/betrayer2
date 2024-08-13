using System;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

//public partial class SystemSettingsWindow : IWindow
public partial class SystemSettingsWindow
{
    public SystemSettingsManager SystemSettings => SystemSettingsManager.Instance;
    public LocalizationManager L { get; set; }

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

        CloseButton.clicked += () => Root.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        Root.style.display = DisplayStyle.Flex;
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
