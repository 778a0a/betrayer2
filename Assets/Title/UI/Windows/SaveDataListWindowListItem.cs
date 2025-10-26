using System;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public partial class SaveDataListWindowListItem
{
    public event EventHandler<ButtonType> ButtonClick;
    public enum ButtonType
    {
        Main,
        Download,
        Delete,
        NoData,
    }

    private SaveDataListWindow parent;
    private LocalizationManager L => parent.L;
    private FaceImageManager FaceImages { get; } = new();

    public int SlotNo { get; private set; }
    public bool IsAutoSaveData { get; private set; }
    public SaveDataSummary Summary { get; private set; }

    public void Initialize(SaveDataListWindow parent, int slotNo, bool isAutoSaveData)
    {
        this.parent = parent;
        L.Register(this);
        SlotNo = slotNo;
        IsAutoSaveData = isAutoSaveData;
        buttonMain.clicked += () => ButtonClick?.Invoke(this, ButtonType.Main);
        buttonDownload.clicked += () => ButtonClick?.Invoke(this, ButtonType.Download);
        buttonDelete.clicked += () => ButtonClick?.Invoke(this, ButtonType.Delete);
        buttonNoData.clicked += () => ButtonClick?.Invoke(this, ButtonType.NoData);
    }

    public void SetData(SaveDataSummary data)
    {
        Summary = data;
        if (data == null)
        {
            SaveDataLisItemRoot.style.display = DisplayStyle.None;
            buttonNoData.style.display = DisplayStyle.Flex;
            buttonNoData.text = L["NEW GAME"];
            if (IsAutoSaveData)
            {
                buttonNoData.text = L["NO DATA"];
                buttonNoData.enabledSelf = false;
                parent.labelAutoSaveOriginalSlotNo.text = "";
            }
            return;
        }
        SaveDataLisItemRoot.style.display = DisplayStyle.Flex;
        buttonNoData.style.display = DisplayStyle.None;

        imageCharacter.image = FaceImages.GetImage(data.FaceImageId);
        labelTitle.text = data.Title;
        labelName.text = data.Name;
        labelOrderIndex.text = data.OrderIndex;
        labelCastle.text = data.Castle;
        labelSoldiers.text = data.SoldierCount.ToString();
        labelGameDate.text = data.GameDate;
        labelSavedTime.text = data.SavedTime.ToString();

        if (IsAutoSaveData)
        {
            parent.labelAutoSaveOriginalSlotNo.text = L["（スロット{0}）", data.SaveDataSlotNo + 1];
        }
    }
}
