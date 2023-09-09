using System;
using Modding;
using skpCustomModule;
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
			myMod.AddComponent<AdCustomModuleMod>();
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
			myMod.AddComponent<MyLogger>();
			myMod.AddComponent<AircraftControllerMsgReceiver>();
			myMod.AddComponent<AircraftController>();
			myMod.AddComponent<AircraftMsgReceiver>();
			Debug.Log("Hello, this is WW2 naval mod!");
		}
        public void OnEntityPrefabCreation(int entityId, GameObject prefab)
        {
            if (entityId == 1)
            {
                prefab.AddComponent<skpCustomModule.AdLevelBlockBehaviour>();
            }
        }
    }
}
