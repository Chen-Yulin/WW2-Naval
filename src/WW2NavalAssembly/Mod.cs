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
			myMod.AddComponent<WeaponMsgReceiver>();
			myMod.AddComponent<AssetManager>();
			myMod.AddComponent<MessageController>();
			myMod.AddComponent<CannonTrackManager>();
			myMod.AddComponent<Grouper>();
			myMod.AddComponent<FireControlManager>();
			myMod.AddComponent<ControllerDataManager>();
			myMod.AddComponent<BlockPoseReceiver>();
			myMod.AddComponent<WellMsgReceicer>();
			myMod.AddComponent<Sea>();
			myMod.AddComponent<TorpedoMsgReceiver>();
			myMod.AddComponent<GunnerMsgReceiver>();
			myMod.AddComponent<GunnerDataBase>();
			myMod.AddComponent<AircraftAssetManager>();
			myMod.AddComponent<FlightDataBase>();
			myMod.AddComponent<EngineMsgReceiver>();

			Debug.Log("Hello, this is WW2 naval mod!");
		}
	}
}
