using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SaveDataListWindow
{
    private TitleSceneUI uiTitle;
    private SaveDataManager saves;
    public LocalizationManager L => uiTitle.L;

    public void Initialize(TitleSceneUI uiTitle)
    {
        this.uiTitle = uiTitle;
        L.Register(this);

        var manualSlots = new[] { SaveSlot1, SaveSlot2, SaveSlot3 };
        for (int i = 0; i < manualSlots.Length; i++)
        {
            var slot = manualSlots[i];
            slot.Initialize(this, i, false);
            slot.ButtonClick += SaveSlot_ButtonClick;
        }
        SaveSlotAuto.Initialize(this, SaveDataManager.AutoSaveDataSlotNo, true);
        SaveSlotAuto.ButtonClick += SaveSlot_ButtonClick;
    }

    private async void SaveSlot_ButtonClick(object sender, SaveDataListWindowListItem.ButtonType e)
    {
        var slot = (SaveDataListWindowListItem)sender;
        switch (e)
        {
            case SaveDataListWindowListItem.ButtonType.Main:
                {
                    try
                    {
                        var saveData = saves.Load(slot.SlotNo);
                        var op = Booter.LoadScene(new MainSceneStartArguments()
                        {
                            IsNewGame = false,
                            SaveData = saveData,
                        });
                        uiTitle.OnSceneLoadingStart(op);
                    }
                    catch (Exception ex)
                    {
                        await MessageWindow.Show(L["セーブデータの読み込みに失敗しました。\n{0}", ex.ToString()]);
                        Debug.LogError($"セーブデータの読み込みに失敗しました。 {ex}");
                    }
                }
                break;
            case SaveDataListWindowListItem.ButtonType.Download:
                try
                {
                    var saveDataText = saves.LoadSaveDataText(slot.SlotNo);
                    uiTitle.ShowTextBoxWindow(
                        initialText: saveDataText.PlainText(),
                        isCopy: true);
                }
                catch (Exception ex)
                {
                    await MessageWindow.Show(L["セーブデータのダウンロードに失敗しました。\n({0})", ex.Message]);
                    Debug.LogError($"セーブデータのダウンロードに失敗しました。 {ex}");
                }
                break;
            case SaveDataListWindowListItem.ButtonType.Delete:
                try
                {
                    var res = await MessageWindow.Show(L["セーブデータを削除しますか？"], MessageBoxButton.YesNo);
                    if (res != MessageBoxResult.Yes) return;
                    SaveDataManager.Instance.Delete(slot.SlotNo);
                    slot.SetData(null);
                }
                catch (Exception ex)
                {
                    await MessageWindow.Show(L["セーブデータの削除に失敗しました。\n{0}", ex.ToString()]);
                    Debug.LogError($"セーブデータの削除に失敗しました。 {ex}");
                }
                break;
            case SaveDataListWindowListItem.ButtonType.NoData:
                {
                    if (slot.IsAutoSaveData) return;

                    uiTitle.ShowNewGameWindow(slot.SlotNo);
                }
                break;
            default:
                break;
        }
    }

    public void SetData(SaveDataManager saves)
    {
        this.saves = saves;

        var slots = new[] { SaveSlot1, SaveSlot2, SaveSlot3 };
        for (int slotNo = 0; slotNo < slots.Length; slotNo++)
        {
            var slot = slots[slotNo];

            if (!saves.HasSaveData(slotNo))
            {
                slot.SetData(null);
                continue;
            }

            var summary = saves.LoadSummary(slotNo);
            slot.SetData(summary);
        }

        if (!saves.HasAutoSaveData())
        {
            SaveSlotAuto.SetData(null);
        }
        else
        {
            var autoSummary = saves.LoadSummary(SaveDataManager.AutoSaveDataSlotNo);
            SaveSlotAuto.SetData(autoSummary);
        }
    }
}