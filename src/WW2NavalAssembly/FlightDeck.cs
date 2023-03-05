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
        public MToggle AsDeck;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public bool preAsDeck;

        public virtual void SafeAwake()
        {
            AsDeck = BB.AddToggle("As Flight Deck", "AsDeck", false);
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
            preAsDeck = false;
            FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, this);
            SafeAwake();

            if (BB.isSimulating) { return; }
        }

        public void Start()
        {
            preAsDeck = !AsDeck.isDefaultValue;
            FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, this);
        }

        public void OnDestroy()
        {
            if (BB.isSimulating)
            {
                FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, BB.BuildingBlock.gameObject.GetComponent<FlightDeck>());
            }
        }

        public void FixedUpdate()
        {
            if (preAsDeck == AsDeck.isDefaultValue)
            {
                preAsDeck = !AsDeck.isDefaultValue;
                if (preAsDeck)
                {
                    FlightDataBase.Instance.AddDeck(myPlayerID, myGuid, this);
                }
            }
        }
    }
}
