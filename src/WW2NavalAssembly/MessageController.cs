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
            ModNetworking.Callbacks[GunMsgReceiver.FireMsg] += GunMsgReceiver.Instance.fireKeyMsgReceiver;
            ModNetworking.Callbacks[GunMsgReceiver.ExploMsg] += GunMsgReceiver.Instance.exploMsgReceiver;
            ModNetworking.Callbacks[GunMsgReceiver.WaterHitMsg] += GunMsgReceiver.Instance.waterHitMsgReceiver;
            ModNetworking.Callbacks[GunMsgReceiver.BulletHoleMsg] += GunMsgReceiver.Instance.bulletHoleMsgReceiver;
            ModNetworking.Callbacks[GunMsgReceiver.ReloadMsg] += GunMsgReceiver.Instance.reloadTimeMsgReceiver;
            ModNetworking.Callbacks[WellMsgReceicer.hitMsg] += WellMsgReceicer.Instance.ExploMsgReceiver;
            ModNetworking.Callbacks[LockDataManager.LockMsg] += LockDataManager.Instance.LockDataReceiver;
            ModNetworking.Callbacks[LockDataManager.CameraMsg] += LockDataManager.Instance.CameraDataReceiver;
            ModNetworking.Callbacks[BlockPoseReceiver.forwardMsg] += BlockPoseReceiver.Instance.forwardMsgReceiver;
            ModNetworking.Callbacks[LockDataManager.ControllerVelMsg] += LockDataManager.Instance.ControllerVelReceiver;
        }
    }
}
