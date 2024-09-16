using System;
using System.Collections.Generic;
using System.Text;

using UnityEngine;

using Modding;
namespace WW2NavalAssembly
{
    public class MessageController : SingleInstance<MessageController>
    {
        public override string Name { get; } = "Message Controller";
        public MessageController()
        {
            ModNetworking.Callbacks[WeaponMsgReceiver.FireMsg] += WeaponMsgReceiver.Instance.fireKeyMsgReceiver;
            ModNetworking.Callbacks[WeaponMsgReceiver.ExploMsg] += WeaponMsgReceiver.Instance.exploMsgReceiver;
            ModNetworking.Callbacks[WeaponMsgReceiver.WaterHitMsg] += WeaponMsgReceiver.Instance.waterHitMsgReceiver;
            ModNetworking.Callbacks[WeaponMsgReceiver.HitHoleMsg] += WeaponMsgReceiver.Instance.hitHoleMsgReceiver;
            ModNetworking.Callbacks[WeaponMsgReceiver.ReloadMsg] += WeaponMsgReceiver.Instance.reloadTimeMsgReceiver;
            ModNetworking.Callbacks[WellMsgReceicer.hitMsg] += WellMsgReceicer.Instance.ExploMsgReceiver;
            ModNetworking.Callbacks[ControllerDataManager.LockMsg] += ControllerDataManager.Instance.LockDataReceiver;
            ModNetworking.Callbacks[ControllerDataManager.CameraMsg] += ControllerDataManager.Instance.CameraDataReceiver;
            ModNetworking.Callbacks[BlockPoseReceiver.forwardMsg] += BlockPoseReceiver.Instance.forwardMsgReceiver;
            ModNetworking.Callbacks[ControllerDataManager.ControllerSyncMsg] += ControllerDataManager.Instance.ControllerSyncReceiver;
            ModNetworking.Callbacks[TorpedoMsgReceiver.TorpedoDataMsg] += TorpedoMsgReceiver.Instance.TorpedoDataMsgReceiver;
            ModNetworking.Callbacks[TorpedoMsgReceiver.TorpedoGuidMsg] += TorpedoMsgReceiver.Instance.TorpedoGuidMsgReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.EmulateMsg] += GunnerMsgReceiver.Instance.EmulateReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.TargetMsg] += GunnerMsgReceiver.Instance.TargetReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.GunnerActiveMsg] += GunnerMsgReceiver.Instance.GunnerActiveReceiver;
            ModNetworking.Callbacks[EngineMsgReceiver.EngineStateMsg] += EngineMsgReceiver.Instance.MsgReceiver;
            ModNetworking.Callbacks[AircraftControllerMsgReceiver.MouseRouteMsg] += AircraftControllerMsgReceiver.Instance.MouseRouteMsgReceiver;
            ModNetworking.Callbacks[AircraftControllerMsgReceiver.ReturnMsg] += AircraftControllerMsgReceiver.Instance.ReturnMsgReceiver;
            ModNetworking.Callbacks[AircraftControllerMsgReceiver.CurrentLeaderMsg] += AircraftControllerMsgReceiver.Instance.CurrentLeaderMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.ChangeStatusMsg] += AircraftMsgReceiver.Instance.StatusMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.RemovedMsg] += AircraftMsgReceiver.Instance.RemovedMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.ExploMsg] += AircraftMsgReceiver.Instance.ExploMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.ShootDownMsg] += AircraftMsgReceiver.Instance.ShootDownMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.GunShootMsg] += AircraftMsgReceiver.Instance.GunShootMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.LoadMsg] += AircraftMsgReceiver.Instance.LoadMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.AddBackupMsg] += AircraftMsgReceiver.Instance.BackupMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.FuelMsg] += AircraftMsgReceiver.Instance.FuelMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.VelocityMsg] += AircraftMsgReceiver.Instance.VelocityMsgReceiver;
            ModNetworking.Callbacks[AircraftMsgReceiver.NeedVelocityMsg] += AircraftMsgReceiver.Instance.ClientNeedVelocityMsgReceiver;
            ModNetworking.Callbacks[LogMsgReceiver.LogMsg] += LogMsgReceiver.Instance.Receive;
            ModNetworking.Callbacks[AAControllerMsgReceiver.targetIndexMsg] += AAControllerMsgReceiver.Instance.targetIndexReceiver;
            ModNetworking.Callbacks[AABlockMsgReceiver.aaActiveMsg] += AABlockMsgReceiver.Instance.aaActiveReceiver;
            ModNetworking.Callbacks[AircraftLifterMsgReceiver.PositionMsg] += AircraftLifterMsgReceiver.Instance.PositionMsgReceiver;
            ModNetworking.Callbacks[AircraftLifterMsgReceiver.DestroyMsg] += AircraftLifterMsgReceiver.Instance.DestroyMsgReceiver;
            ModNetworking.Callbacks[CatapultMsgReceiver.LaunchMsg] += CatapultMsgReceiver.Instance.LaunchMsgReceiver;
            ModNetworking.Callbacks[CatapultMsgReceiver.MMsg] += CatapultMsgReceiver.Instance.MMsgReceiver;
            ModNetworking.Callbacks[CatapultMsgReceiver.JYMsg] += CatapultMsgReceiver.Instance.JYMsgReceiver;
            ModNetworking.Callbacks[CrewManager.CrewUpdateMessage] += CrewManager.Instance.ReceiveCrewRate;
            ModNetworking.Callbacks[CrewManager.OnFireMessage] += CrewManager.Instance.ReceiveOnfire;
        }
    }
}
