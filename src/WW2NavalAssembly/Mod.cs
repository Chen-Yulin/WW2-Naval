using System;
using Modding;
using UnityEngine;

namespace WW2NavalAssembly
{
	public class Mod : ModEntryPoint
	{
		public static GameObject myMod;
		public override void OnLoad()
		{
			myMod = new GameObject("WW2 Naval Mod");
			UnityEngine.Object.DontDestroyOnLoad(myMod);

			myMod.AddComponent<CustomBlockController>();
			myMod.AddComponent<ModController>();
			myMod.AddComponent<GunMsgReceiver>();
			myMod.AddComponent<AssetManager>();
			myMod.AddComponent<MessageController>();
			myMod.AddComponent<CannonTrackManager>();

			Debug.Log("Hello, this is WW2 naval mod!");
		}
	}
}
