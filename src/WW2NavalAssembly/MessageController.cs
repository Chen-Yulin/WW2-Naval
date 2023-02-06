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
            ModNetworking.Callbacks[ControllerDataManager.ControllerVelMsg] += ControllerDataManager.Instance.ControllerVelReceiver;
            ModNetworking.Callbacks[TorpedoMsgReceiver.TorpedoDataMsg] += TorpedoMsgReceiver.Instance.TorpedoDataMsgReceiver;
            ModNetworking.Callbacks[TorpedoMsgReceiver.TorpedoGuidMsg] += TorpedoMsgReceiver.Instance.TorpedoGuidMsgReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.EmulateMsg] += GunnerMsgReceiver.Instance.EmulateReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.TargetMsg] += GunnerMsgReceiver.Instance.TargetReceiver;
            ModNetworking.Callbacks[GunnerMsgReceiver.GunnerActiveMsg] += GunnerMsgReceiver.Instance.GunnerActiveReceiver;
        }
    }
}
