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
using Modding.Blocks;

namespace WW2NavalAssembly
{
    class FlightDeck : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MMenu WoodType;
        public MText HangarGroup;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public string preType;

        public virtual void SafeAwake()
        {
            WoodType = BB.AddMenu("Wood Type", 0, new List<string> { "Armour", "Flight Deck", "Hangar"});
            HangarGroup = BB.AddText("Hangar Group", "HangarGroup", "0");
        }
        public void Awake()
        {
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;

            myseed = (int)(UnityEngine.Random.value * 39);
            BB = GetComponent<BlockBehaviour>();
            try
            {
                myGuid = BB.BuildingBlock.Guid.GetHashCode();
            }
            catch
            {
                myGuid = BB.Guid.GetHashCode();
            }
            preType = "Armour";
            
            SafeAwake();
            HangarGroup.DisplayInMapper = false;

            if (BB.isSimulating) { return; }
        }

        public void Start()
        {
            if (WoodType.Selection == "Flight Deck")
            {
                FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, this);
                HangarGroup.DisplayInMapper = false;
            }
            else if (WoodType.Selection == "Hangar")
            {
                FlightDataBase.Instance.AddHangar(myPlayerID, myGuid, this);
                HangarGroup.DisplayInMapper = true;
            }
            else
            {
                HangarGroup.DisplayInMapper = false;
            }
        }

        // add back the reference to the parent block when the simulation is stopped
        public void OnDestroy()
        {
            if (BB.isSimulating)
            {
                if (WoodType.Selection == "Flight Deck")
                {
                    FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, BB.BuildingBlock.gameObject.GetComponent<FlightDeck>());
                    HangarGroup.DisplayInMapper = false;
                }
                else if (WoodType.Selection == "Hangar")
                {
                    FlightDataBase.Instance.AddHangar(myPlayerID, myGuid, BB.BuildingBlock.gameObject.GetComponent<FlightDeck>());
                    HangarGroup.DisplayInMapper = true;
                }
                else
                {
                    HangarGroup.DisplayInMapper = false;
                }
            }
        }

        public void FixedUpdate()
        {
            if (preType != WoodType.Selection)
            {
                preType = WoodType.Selection;
                if (preType == "Flight Deck")
                {
                    FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, this);
                    HangarGroup.DisplayInMapper = false;
                }
                else if (preType == "Hangar")
                {
                    FlightDataBase.Instance.AddHangar(myPlayerID, myGuid, this);
                    HangarGroup.DisplayInMapper = true;
                }
                else
                {
                    HangarGroup.DisplayInMapper = false;
                }
            }
        }
    }
}
