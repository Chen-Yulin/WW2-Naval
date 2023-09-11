using System;
using Localisation;
using System.Collections.Generic;
using System.Linq;
using Modding.Common;
using Modding;
using UnityEngine;
using System.Globalization;

namespace WW2NavalAssembly
{
    public class LanguageManager : SingleInstance<LanguageManager>
    {
        public override string Name { get; } = "Language Manager";

        public Action<string> OnLanguageChanged;

        private string currentLanguageName;
        private string lastLanguageName = "English";

        public ILanguage CurrentLanguage { get; private set; } = new English();
        Dictionary<string, ILanguage> Dic_Language = new Dictionary<string, ILanguage>
    {
        { "简体中文",new Chinese()},
        { "English",new English()},
    };

        void Awake()
        {
            OnLanguageChanged += ChangLanguage;
        }

        void Update()
        {
            currentLanguageName = LocalisationManager.Instance.currLangName;

            if (!lastLanguageName.Equals(currentLanguageName))
            {
                lastLanguageName = currentLanguageName;

                OnLanguageChanged.Invoke(currentLanguageName);
            }
        }

        void ChangLanguage(string value)
        {
            try
            {
                CurrentLanguage = Dic_Language[value];
            }
            catch
            {
                CurrentLanguage = Dic_Language["English"];
            }
        }

    }

    public interface ILanguage
    {
        //Gun
        string GunFire { get; }
        string SwitchAPHE { get; }
        List<string> GunType { get; }
        string AsTrackCannon { get; }
        string FireControl { get; }
        string Caliber { get; }

        //Controller
        string TrackCannon { get; }
        string SwitchTrackCannon { get; }
        string Lock { get; }
        string OffsetUp { get; }
        string OffsetDown { get; }
        string OffsetLeft { get; }
        string OffsetRight { get; }
        string FireControlPanelSize { get; }
        string TurretHeight { get; }

        //Aircraft
        List<string> AircraftType { get; }
        List<string> FighterType { get; }
        List<string> AircraftBombType { get; }
        List<string> AircraftTorpedoType { get; }
        List<string> AircraftRank { get; }
        string Group { get; }

        // AircraftController
        string TacticalView { get; }
        string MoveView { get; }
        string AircraftReturn { get; }
        string AircraftTakeOff { get; }
        string AircraftElevatorUp { get; }
        string AircraftElevatorDown { get; }
        string ContinuousSelection { get; }
        string AircraftAttack { get; }
        string ViewSensitivity { get; }

        // Torpedo Launcher
        string TorpedoFire { get; }
        string SwitchTorpedo { get; }
        string TorpedoType0Depth { get; }
        string TorpedoType1Depth { get; }

        //Engine
        string EngineThrust { get; }
        string EngineUp { get; }
        string EngineDown { get; }
        string EngineAxisLength { get; }
        string EngineAxisXOffset { get; }
        string EngineAxisYOffset { get; }
        string EngineAxisPitch { get; }
        string PropellerSize { get; }

        // AA Controller
        string SwitchAATarget { get; }

        // Gunner
        string SwitchActive { get; }
        string SwitchAA { get; }
        string DefaultAA { get; }
        string OrienTolerance { get; }
        string ElevationTolerance { get; }
        string GunnerSpeed { get; }

        string GunnerFire { get; }
        string GunnerLeft { get; }
        string GunnerRight { get; }
        string GunnerUp { get; }
        string GunnerDown { get; }

    }

    public class Chinese : ILanguage
    {
        //Gun
        public string GunFire { get; } = "开炮";
        public string SwitchAPHE { get; } = "切换 穿甲/高爆";
        public List<string> GunType { get; } = new List<string> { "穿甲弹", "高爆弹" };
        public string AsTrackCannon { get; } = "可用于炮弹追踪";
        public string FireControl { get; } = "可用于火控计算";
        public string Caliber { get; } = "口径 (mm)";

        //Controller
        public string TrackCannon { get; } = "炮弹追踪";
        public string SwitchTrackCannon { get; } = "切换炮弹追踪";
        public string Lock { get; } = "锁定";
        public string OffsetUp { get; } = "炮管微调上仰";
        public string OffsetDown { get; } = "炮管微调下俯";
        public string OffsetLeft { get; } = "炮管微调左转";
        public string OffsetRight { get; } = "炮管微调右转";
        public string FireControlPanelSize { get; } = "火控面板大小";
        public string TurretHeight { get; } = "炮塔高度(用于修正)";

        //Aircraft
        public List<string> AircraftType { get; } = new List<string> { "战斗机", "鱼雷机", "俯冲轰炸机"};
        public List<string> FighterType { get; } = new List<string> {
                "Zero",
                "F4U",
                "SpitFire"
            };
        public List<string> AircraftBombType { get; } = new List<string>{
                "SBD",
                "99 Type",
                "Fulmar"
            };
        public List<string> AircraftTorpedoType { get; } = new List<string> { "SB2C", "B7A2", "Barracuda" };
        public List<string> AircraftRank { get; } = new List<string> { "从机", "长机", "后备" };
        public string Group { get; } = "编组";

        // AircraftController
        public string TacticalView { get; } = "战术视角";
        public string MoveView { get; } = "拖动战术视角";
        public string AircraftReturn { get; } = "飞机返航";
        public string AircraftTakeOff { get; } = "飞机起飞";
        public string AircraftElevatorUp { get; } = "升降机抬上飞机";
        public string AircraftElevatorDown { get; } = "升降机放下飞机";
        public string ContinuousSelection { get; } = "连续选择航路点";
        public string AircraftAttack { get; } = "设置攻击点";
        public string ViewSensitivity { get; } = "视角灵敏度";

        // Torpedo Launcher
        public string TorpedoFire { get; } = "发射鱼雷";
        public string SwitchTorpedo { get; } = "切换鱼雷模式(慢/快)";
        public string TorpedoType0Depth { get; } = "鱼雷深度(慢速雷)";
        public string TorpedoType1Depth { get; } = "鱼雷深度(高速雷)";

        //Engine
        public string EngineThrust { get; } = "推力";
        public string EngineUp { get; } = "引擎档位增加";
        public string EngineDown { get; } = "引擎档位减少";
        public string EngineAxisLength { get; } = "传动轴长度";
        public string EngineAxisXOffset { get; } = "传动轴X偏移";
        public string EngineAxisYOffset { get; } = "传动轴Y偏移";
        public string EngineAxisPitch { get; } = "传动轴俯仰";
        public string PropellerSize { get; } = "螺旋桨尺寸";

        // AA Controller
        public string SwitchAATarget { get; } = "切换防空目标";

        // Gunner
        public string SwitchActive { get; } = "切换激活";
        public string SwitchAA { get; } = "开关防空模式";
        public string DefaultAA { get; } = "默认防空模式";
        public string OrienTolerance { get; } = "方向容差";
        public string ElevationTolerance { get; } = "仰角容差";
        public string GunnerSpeed { get; } = "转炮速度";

        public string GunnerFire { get; } = "模拟开炮键";
        public string GunnerLeft { get; } = "模拟炮塔左转键";
        public string GunnerRight { get; } = "模拟炮塔右转键";
        public string GunnerUp { get; } = "模拟炮塔上仰键";
        public string GunnerDown { get; } = "模拟炮塔下俯键";
    }

    public class English : ILanguage
    {
        //Gun
        public string GunFire { get; } = "Fire";
        public string SwitchAPHE { get; } = "Switch AP/HE";
        public List<string> GunType { get; } = new List<string> { "AP", "HE" };
        public string AsTrackCannon { get; } = "Track Cannon";
        public string FireControl { get; } = "Fire Control";
        public string Caliber { get; } = "Caliber (mm)";

        //Controller
        public string TrackCannon { get; } = "Track Cannon";
        public string SwitchTrackCannon { get; } = "Switch Track Cannon";
        public string Lock { get; } = "Lock";
        public string OffsetUp { get; } = "Offset UP";
        public string OffsetDown { get; } = "Offset Down";
        public string OffsetLeft { get; } = "Offset Left";
        public string OffsetRight { get; } = "Offset Right";
        public string FireControlPanelSize { get; } = "Fire Control Size";
        public string TurretHeight { get; } = "Turret Height";

        //Aircraft
        public List<string> AircraftType { get; } = new List<string> { "Fighter", "Torpedo", "Bomb" };
        public List<string> FighterType { get; } = new List<string> {
                "Zero",
                "F4U",
                "SpitFire"
            };
        public List<string> AircraftBombType { get; } = new List<string>{
                "SBD",
                "99 Type",
                "Fulmar"
            };
        public List<string> AircraftTorpedoType { get; } = new List<string> { "SB2C", "B7A2", "Barracuda" };
        public List<string> AircraftRank { get; } = new List<string> { "Leader", "Slave", "Backup" };
        public string Group { get; } = "Group";

        // AircraftController
        public string TacticalView { get; } = "Tactical View";
        public string MoveView { get; } = "Move View";
        public string AircraftReturn { get; } = "Aircraft Return";
        public string AircraftTakeOff { get; } = "Aircraft Take Off";
        public string AircraftElevatorUp { get; } = "Aircraft Elevator Up";
        public string AircraftElevatorDown { get; } = "Aircraft Take Down";
        public string ContinuousSelection { get; } = "Continuous";
        public string AircraftAttack { get; } = "Attack";
        public string ViewSensitivity { get; } = "View Sensitivity";

        // Torpedo Launcher
        public string TorpedoFire { get; } = "Launch";
        public string SwitchTorpedo { get; } = "Switch Torpedo slow/fast)";
        public string TorpedoType0Depth { get; } = "Depth for Slow";
        public string TorpedoType1Depth { get; } = "Depth for Fast";

        //Engine
        public string EngineThrust { get; } = "Thrust";
        public string EngineUp { get; } = "Engine Gear Up";
        public string EngineDown { get; } = "Engine Gear Down";
        public string EngineAxisLength { get; } = "Axle Length";
        public string EngineAxisXOffset { get; } = "Axle Postion X";
        public string EngineAxisYOffset { get; } = "Axle Postion Y";
        public string EngineAxisPitch { get; } = "Axle Pitch";
        public string PropellerSize { get; } = "Propeller Size";

        // AA Controller
        public string SwitchAATarget { get; } = "Swith AA Target";

        // Gunner
        public string SwitchActive { get; } = "Switch Active";
        public string SwitchAA { get; } = "Switch AA";
        public string DefaultAA { get; } = "Default AA";
        public string OrienTolerance { get; } = "Orien Fault Tolerance";
        public string ElevationTolerance { get; } = "Pitch Fault Tolerance";
        public string GunnerSpeed { get; } = "Speed";

        public string GunnerFire { get; } = "Fire Key";
        public string GunnerLeft { get; } = "Left Key";
        public string GunnerRight { get; } = "Right Key";
        public string GunnerUp { get; } = "Up Key";
        public string GunnerDown { get; } = "Down Key";
    }
}