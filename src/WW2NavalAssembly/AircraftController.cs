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
        public GameObject HangarVis;

        public bool hasDeck = false;
        public bool hasHangar = false;

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
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
                    if (DeckVis)
                    {
                        hasDeck = true;
                        DeckVis.SetActive(ModController.Instance.showArmour);
                    }
                    if (HangarVis)
                    {
                        hasHangar = true;
                        HangarVis.SetActive(ModController.Instance.showArmour);
                    }
                }
                else if (PlayerData.localPlayer.networkId == 0)
                {
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
                    if (DeckVis)
                    {
                        hasDeck = true;
                        DeckVis.SetActive(false);
                    }
                    if (HangarVis)
                    {
                        hasHangar = true;
                        HangarVis.SetActive(false);
                    }
                } // generate parking spot for client in host
            }
            else
            {
                DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
                if (DeckVis)
                {
                    hasDeck = true;
                    DeckVis.SetActive(ModController.Instance.showArmour);
                }
                if (HangarVis)
                {
                    hasHangar = true;
                    HangarVis.SetActive(ModController.Instance.showArmour);
                }
            }
        }

        public override void SimulateFixedUpdateAlways()
        {
            if (hasDeck)
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
            if (hasHangar)
            {
                // local frequency
                if (ModController.Instance.state % 10 == mySeed)
                {
                    if (StatMaster.isMP)
                    {
                        if (PlayerData.localPlayer.networkId == myPlayerID)
                        {
                            HangarVis.SetActive(ModController.Instance.showArmour);
                        }
                    }
                    else
                    {
                        HangarVis.SetActive(ModController.Instance.showArmour);
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
        }

        public override void SimulateUpdateAlways()
        {
            if(hasDeck)
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
            if (hasHangar)
            {
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID)
                    {
                        FlightDataBase.Instance.UpdateHangarTransform(myPlayerID);
                    }
                }
                else
                {
                    FlightDataBase.Instance.UpdateHangarTransform(myPlayerID);
                }
            }

        }


        public override void OnSimulateStop()
        {
        }

    }
}
