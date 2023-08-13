using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;

namespace WW2NavalAssembly
{
    class AircraftController : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey ReturnKey;
        public MKey TakeOffKey;

        public GameObject DeckVis;

        public override void SafeAwake()
        {
            gameObject.name = "Aircraft Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);


            ReturnKey = AddKey("Aircraft Return", "ReturnKey", KeyCode.Backspace);
            TakeOffKey = AddKey("Aircraft Take Off", "TakeOffKey", KeyCode.Q);
        }

        public void Start()
        {
            gameObject.name = "Aircraft Captain";
        }

        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent).transform.Find("Vis").gameObject;
                    DeckVis.SetActive(ModController.Instance.showArmour);
                }
            }
            else
            {
                DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent).transform.Find("Vis").gameObject;
                DeckVis.SetActive(ModController.Instance.showArmour);
            }
        }

        public override void SimulateFixedUpdateAlways()
        {
            // local frequency
            if (ModController.Instance.state % 10 == mySeed)
            {
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID)
                    {
                        DeckVis.SetActive(ModController.Instance.showArmour);
                    }
                }
                else
                {
                    DeckVis.SetActive(ModController.Instance.showArmour);
                }
            }

            // high frequency
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                }
            }
            else
            {
            }
        }

        public override void SimulateUpdateAlways()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    FlightDataBase.Instance.UpdateDeckTransform(myPlayerID);
                }
            }
            else
            {
                FlightDataBase.Instance.UpdateDeckTransform(myPlayerID);
            }
        }


        public override void OnSimulateStop()
        {
        }

    }
}
