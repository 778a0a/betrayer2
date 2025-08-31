using System;
using UnityEngine;
using UnityEngine.UIElements;

public partial class ForceTableRowItem
{
    public event EventHandler<Force> MouseMove;
    public event EventHandler<Force> MouseDown;

    public Force Force { get; private set; }

    private CharacterInfoSoldierIcon[] soldierIcons;

    public void Initialize()
    {
        Root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        ForceTableRowItemRoot.RegisterCallback<ClickEvent>(OnMouseDown);
    }

    private void OnMouseMove(MouseMoveEvent evt)
    {
        MouseMove?.Invoke(this, Force);
    }

    private void OnMouseDown(ClickEvent evt)
    {
        MouseDown?.Invoke(this, Force);
    }

    public void SetData(Force force, bool isClickable)
    {
        Force = force;
        if (force == null || force.Character == null)
        {
            Root.style.visibility = Visibility.Hidden;
            return;
        }
        Root.style.visibility = Visibility.Visible;

        ForceTableRowItemRoot.EnableInClassList("clickable", isClickable);
        
        var commander = force.Character;
        
        // 指揮官情報
        imageCommanderFace.image = Static.GetFaceImage(commander);
        labelCommanderName.text = commander.Name;
        
        // 軍勢の状態表示
        labelMoving.visible = true; // 軍勢は常に出撃中
        labelMode.visible = force.Mode == ForceMode.Reinforcement;
        labelMode.text = force.Mode switch
        {
            ForceMode.Reinforcement => "援軍",
            _ => ""
        };
        
        // 目的地情報
        var destinationName = force.Destination switch
        {
            Castle castle => castle.Name,
            _ => "不明"
        };
        labelDestination.text = destinationName;
        
        // ETA（到着予定時間）
        var etaDays = (int)force.ETADays;
        labelETA.text = etaDays > 0 ? $"{etaDays}日" : "到着";
        
        // 指揮官能力値
        labelAttack.text = commander.Attack.ToString();
        labelDefense.text = commander.Defense.ToString();
        labelIntelligence.text = commander.Intelligence.ToString();
        labelSoldiers.text = commander.Soldiers.SoldierCount.ToString();
        
        // 兵士情報の更新
        soldierIcons ??= new[] { soldier00, soldier01, soldier02, soldier03, soldier04, soldier05, soldier06, soldier07, soldier08, soldier09, soldier10, soldier11, soldier12, soldier13, soldier14 };
        for (int i = 0; i < soldierIcons.Length; i++)
        {
            if (commander.Soldiers.Count <= i)
            {
                soldierIcons[i].Root.style.visibility = Visibility.Hidden;
                continue;
            }
            soldierIcons[i].Root.style.visibility = Visibility.Visible;
            soldierIcons[i].SetData(commander.Soldiers[i]);
        }
    }
}