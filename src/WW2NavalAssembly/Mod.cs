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
			myMod = new GameObject("Morden Air Combat Mod");
			UnityEngine.Object.DontDestroyOnLoad(myMod);

			myMod.AddComponent<GunMsgReceiver>();
			myMod.AddComponent<AssetManager>();
			myMod.AddComponent<MessageController>();

			Debug.Log("Hello, this is WW2 naval mod!");
		}
	}
}
