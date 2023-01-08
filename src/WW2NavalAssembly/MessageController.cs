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
        }
    }
}
