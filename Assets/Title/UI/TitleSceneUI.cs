using System;
using System.Collections;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public partial class TitleSceneUI : MonoBehaviour
{
    private int currentSelectedSlotNo = 0;

    [SerializeField] public LocalizationManager L;

    private bool isInitialized = false;
    private void OnEnable()
    {
        if (!isInitialized)
        {
            Root.visible = false;
            InitializeDocument();
            isInitialized = true;
        }
        else
        {
            // uxmlを編集した場合は再初期化する。
            ReinitializeDocument();
            StartCoroutine(Start());
        }
    }

    private IEnumerator Start()
    {
        SystemSettingsManager.Instance.ApplyOrientation();

        yield return LocalizationSettings.InitializationOperation;

        InitializeNewGameWindow();
        InitializeTextBoxWindow();
        InitializeProgressWindow();
        InitializeLicenseWindow();
        MessageWindow.Initialize();
        SystemSettingsWindow.L = L;
        SystemSettingsWindow.Initialize();
        SaveDataList.Initialize(this);

        //L.Register(this);
        //L.Apply();

        buttonCloseApplication.clicked += () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };

        buttonShowLicense.clicked += () =>
        {
            ShowLicenseWindow();
        };

        buttonShowSystemSettings.clicked += () =>
        {
            SystemSettingsWindow.Show();
        };

        SaveDataList.SetData(SaveDataManager.Instance);

        Root.visible = true;
    }

    #region NewGameWindow
    private readonly int[] slotNoList = new[] { 0, 1, 2 };
    private Button[] CopySlotButtons => new[] { buttonCopyFromSlot1, buttonCopyFromSlot2, buttonCopyFromSlot3 };

    private void InitializeNewGameWindow()
    {
        // 閉じるボタン
        buttonCloseNewGameWindow.clicked += () => NewGameMenu.style.display = DisplayStyle.None;
        
        // はじめから - シナリオ1
        buttonStartNewGameScenario1.clicked += () =>
        {
            NewGameMenu.style.display = DisplayStyle.None;
            var op = Booter.LoadScene(new MainSceneStartArguments()
            {
                IsNewGame = true,
                NewGameSaveDataSlotNo = currentSelectedSlotNo,
                NewGameScenarioNo = "01",
            });
            OnSceneLoadingStart(op);
        };

        // はじめから - シナリオ2
        buttonStartNewGameScenario2.clicked += () =>
        {
            NewGameMenu.style.display = DisplayStyle.None;
            var op = Booter.LoadScene(new MainSceneStartArguments()
            {
                IsNewGame = true,
                NewGameSaveDataSlotNo = currentSelectedSlotNo,
                NewGameScenarioNo = "02",
            });
            OnSceneLoadingStart(op);
        };

        // テキストデータ読み込み
        buttonLoadTextData.clicked += () =>
        {
            ShowTextBoxWindow(text =>
            {
                try
                {
                    var saveDataText = SaveDataText.FromPlainText(text);
                    
                    // セーブデータのスロット番号を書き換える。
                    var saveData = saveDataText.Deserialize();
                    saveData.Summary.SaveDataSlotNo = currentSelectedSlotNo;
                    saveDataText = SaveDataText.Serialize(saveData);

                    SaveDataManager.Instance.Save(currentSelectedSlotNo, saveDataText);
                    SaveDataList.SetData(SaveDataManager.Instance);
                    TextBoxWindow.style.display = DisplayStyle.None;
                    NewGameMenu.style.display = DisplayStyle.None;
                }
                catch (Exception ex)
                {
                    MessageWindow!.Show(L["セーブデータの読み込みに失敗しました。\n({0})", ex.Message]);
                    Debug.LogError($"セーブデータの読み込みに失敗しました。 {ex}");
                }
            }, isCopy: false);
        };

        // スロットからコピー
        for (var i = 0; i < slotNoList.Length; i++)
        {
            var slotNo = slotNoList[i];
            var button = CopySlotButtons[i];
            button.clicked += () =>
            {
                SaveDataManager.Instance.Copy(slotNo, currentSelectedSlotNo);
                SaveDataList.SetData(SaveDataManager.Instance);
                NewGameMenu.style.display = DisplayStyle.None;
            };
        }

        // オートセーブスロットからコピー
        buttonCopyFromSlotAuto.clicked += () =>
        {
            SaveDataManager.Instance.Copy(SaveDataManager.AutoSaveDataSlotNo, currentSelectedSlotNo);
            SaveDataList.SetData(SaveDataManager.Instance);
            NewGameMenu.style.display = DisplayStyle.None;
        };

        NewGameMenu.style.display = DisplayStyle.None;
    }

    public void ShowNewGameWindow(int selectedSlotNo)
    {
        currentSelectedSlotNo = selectedSlotNo;
        NewGameMenu.style.display = DisplayStyle.Flex;

        for (var i = 0; i < slotNoList.Length; i++)
        {
            var slotNo = slotNoList[i];
            var isSelectedSlot = slotNo == selectedSlotNo;
            var hasSaveData = SaveDataManager.Instance.HasSaveData(slotNo);
            CopySlotButtons[i].SetEnabled(!isSelectedSlot && hasSaveData);
        }
        
        var hasAutoSaveData = SaveDataManager.Instance.HasSaveData(SaveDataManager.AutoSaveDataSlotNo);
        buttonCopyFromSlotAuto.SetEnabled(hasAutoSaveData);
    }
    #endregion

    #region TextBoxWindow
    private Action<string> onTextSubmit;

    private void InitializeTextBoxWindow()
    {
        buttonCloseTextBoxWindow.clicked += () =>
        {
            TextBoxWindow.style.display = DisplayStyle.None;
        };
        buttonClearText.clicked += () =>
        {
            textTextBoxWindow.value = "";
        };
        buttonPasteText.clicked += () =>
        {
            try
            {
                textTextBoxWindow.value = GUIUtility.systemCopyBuffer;
            }
            catch (Exception ex)
            {
                MessageWindow!.Show(L["クリップボードからの貼り付けに失敗しました。\n({0})", ex.Message]);
                Debug.LogError($"クリップボードからの貼り付けに失敗しました。 {ex}");
            }
        };
        buttonCopyText.clicked += () =>
        {
            try
            {
                GUIUtility.systemCopyBuffer = textTextBoxWindow.value;
            }
            catch (Exception ex)
            {
                MessageWindow!.Show(L["クリップボードへのコピーに失敗しました。\n({0})", ex.Message]);
                Debug.LogError($"クリップボードへのコピーに失敗しました。 {ex}");
            }
        };
        buttonSubmitText.clicked += () =>
        {
            var text = textTextBoxWindow.value;
            onTextSubmit?.Invoke(text);
            if (onTextSubmit == null)
            {
                TextBoxWindow.style.display = DisplayStyle.None;
            }
        };
        TextBoxWindow.style.display = DisplayStyle.None;
    }

    public void ShowTextBoxWindow(
        Action<string> onTextSubmit = null,
        string initialText = "", 
        bool isCopy = true)
    {
        this.onTextSubmit = onTextSubmit;
        textTextBoxWindow.value = initialText;
        TextBoxWindow.style.display = DisplayStyle.Flex;

        buttonSubmitText.text = isCopy ? L["閉じる"] : L["確定"];
        buttonClearText.style.display = Util.Display(!isCopy);
#if UNITY_WEBGL
        // WebGLではクリップボードのコピー・ペーストができないので非表示にする。
        buttonPasteText.style.display = DisplayStyle.None;
        buttonCopyText.style.display = DisplayStyle.None;
#else
        buttonPasteText.style.display = Util.Display(!isCopy);
        buttonCopyText.style.display = Util.Display(isCopy);
#endif
        labelTextBoxWindowTitle.text = isCopy ? 
            L["以下のテキストをコピーして保存してください"] :
            L["セーブデータを以下にペーストしてください"];
    }
    #endregion

    #region ProgressWindow
    private void InitializeProgressWindow()
    {
        ProgressWindow.style.display = DisplayStyle.None;
    }

    public async void OnSceneLoadingStart(AsyncOperation op)
    {
        ProgressWindow.style.display = DisplayStyle.Flex;
        progressLoading.value = op.progress;
        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            await Awaitable.NextFrameAsync();
            progressLoading.value = op.progress * 100;
        }
        progressLoading.value = 90;
        op.allowSceneActivation = true;
    }
    #endregion

    #region LicenseWindow
    private void InitializeLicenseWindow()
    {
        buttonCloseLicenseWindow.clicked += () =>
        {
            LicenseWindow.style.display = DisplayStyle.None;
        };
        LicenseWindow.style.display = DisplayStyle.None;
    }

    public void ShowLicenseWindow()
    {
        var license = GetLicense().Trim();
        if (!license.Equals(textLicenseWindow.text))
        {
            textLicenseWindow.value = license;
        }

        LicenseWindow.style.display = DisplayStyle.Flex;
    }

    private string GetLicense() =>
        L["本ソフトの配布にあたって、同梱されているサードパーティーコンポーネントとそのライセンス情報を以下に示します。"] + @"

Noto Sans Japanese
==================

Copyright 2014-2021 Adobe (http://www.adobe.com/), with Reserved Font Name 'Source'

This Font Software is licensed under the SIL Open Font License, Version 1.1.
This license is copied below, and is also available with a FAQ at:
https://openfontlicense.org


-----------------------------------------------------------
SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
-----------------------------------------------------------

PREAMBLE
The goals of the Open Font License (OFL) are to stimulate worldwide
development of collaborative font projects, to support the font creation
efforts of academic and linguistic communities, and to provide a free and
open framework in which fonts may be shared and improved in partnership
with others.

The OFL allows the licensed fonts to be used, studied, modified and
redistributed freely as long as they are not sold by themselves. The
fonts, including any derivative works, can be bundled, embedded, 
redistributed and/or sold with any software provided that any reserved
names are not used by derivative works. The fonts and derivatives,
however, cannot be released under any other type of license. The
requirement for fonts to remain under this license does not apply
to any document created using the fonts or their derivatives.

DEFINITIONS
""Font Software"" refers to the set of files released by the Copyright
Holder(s) under this license and clearly marked as such. This may
include source files, build scripts and documentation.

""Reserved Font Name"" refers to any names specified as such after the
copyright statement(s).

""Original Version"" refers to the collection of Font Software components as
distributed by the Copyright Holder(s).

""Modified Version"" refers to any derivative made by adding to, deleting,
or substituting -- in part or in whole -- any of the components of the
Original Version, by changing formats or by porting the Font Software to a
new environment.

""Author"" refers to any designer, engineer, programmer, technical
writer or other person who contributed to the Font Software.

PERMISSION & CONDITIONS
Permission is hereby granted, free of charge, to any person obtaining
a copy of the Font Software, to use, study, copy, merge, embed, modify,
redistribute, and sell modified and unmodified copies of the Font
Software, subject to the following conditions:

1) Neither the Font Software nor any of its individual components,
in Original or Modified Versions, may be sold by itself.

2) Original or Modified Versions of the Font Software may be bundled,
redistributed and/or sold with any software, provided that each copy
contains the above copyright notice and this license. These can be
included either as stand-alone text files, human-readable headers or
in the appropriate machine-readable metadata fields within text or
binary files as long as those fields can be easily viewed by the user.

3) No Modified Version of the Font Software may use the Reserved Font
Name(s) unless explicit written permission is granted by the corresponding
Copyright Holder. This restriction only applies to the primary font name as
presented to the users.

4) The name(s) of the Copyright Holder(s) or the Author(s) of the Font
Software shall not be used to promote, endorse or advertise any
Modified Version, except to acknowledge the contribution(s) of the
Copyright Holder(s) and the Author(s) or with their explicit written
permission.

5) The Font Software, modified or unmodified, in part or in whole,
must be distributed entirely under this license, and must not be
distributed under any other license. The requirement for fonts to
remain under this license does not apply to any document created
using the Font Software.

TERMINATION
This license becomes null and void if any of the above conditions are
not met.

DISCLAIMER
THE FONT SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO ANY WARRANTIES OF
MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT
OF COPYRIGHT, PATENT, TRADEMARK, OR OTHER RIGHT. IN NO EVENT SHALL THE
COPYRIGHT HOLDER BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
INCLUDING ANY GENERAL, SPECIAL, INDIRECT, INCIDENTAL, OR CONSEQUENTIAL
DAMAGES, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
FROM, OUT OF THE USE OR INABILITY TO USE THE FONT SOFTWARE OR FROM
OTHER DEALINGS IN THE FONT SOFTWARE.
";
    #endregion
}
