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
using static iTween;
using static TutorialStepPrerequisite;

namespace WW2NavalAssembly
{
    public class AircraftLifter : MonoBehaviour
    {
        public MToggle AsLifter;

        public BlockBehaviour BB { get; internal set; }
        public int myPlayerID;
        public int myGuid;

        public Transform Vis;
        public Transform BoxCollider;
        public bool operating;

        public Vector2 Pos2D
        {
            get
            {
                return MathTool.Get2DCoordinate(transform.position);
            }
        }
        public Vector2 Right2D
        {
            get
            {
                return MathTool.Get2DCoordinate(transform.right);
            }
        }
        public Vector2 Size2D
        {
            get
            {
                return new Vector2(transform.localScale.x, transform.localScale.y);
            }
        }

        public bool GoToDeckStep()
        {
            FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
            if (deck != null)
            {
                float height = deck.height;
                Vector3 localPos = transform.InverseTransformPoint(new Vector3(0, height, 0));
                float targetZ = localPos.z;
                float currentZ = Vis.localPosition.z + 1;
                float delta = targetZ - currentZ;

                delta = Mathf.Clamp(delta, -0.05f / transform.localScale.z, 0.05f / transform.localScale.z);

                Vis.localPosition += new Vector3(0, 0, delta);
                BoxCollider.localPosition += new Vector3(0, 0, delta);
                if (Mathf.Abs(delta) >= 0.05f / transform.localScale.z - 0.001f)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
        }

        public bool GoToHangarStep(Aircraft a)
        {
            Transform hangar = a.MyHangar;
            if (hangar != null)
            {
                float height = hangar.position.y;
                Vector3 localPos = transform.InverseTransformPoint(new Vector3(0, height, 0));
                float targetZ = localPos.z;
                float currentZ = Vis.localPosition.z + 1;
                float delta = targetZ - currentZ;

                delta = Mathf.Clamp(delta, -0.05f / transform.localScale.z, 0.05f / transform.localScale.z);

                Vis.localPosition += new Vector3(0, 0, delta);
                BoxCollider.localPosition += new Vector3(0, 0, delta);
                if (Mathf.Abs(delta) >= 0.05f / transform.localScale.z - 0.001f)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return true;
            }

        }

        public virtual void SafeAwake()
        {
            AsLifter = BB.AddToggle("Aircraft Elevator", "Elevator", false);
        }
        public void Awake()
        {
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
            BB = GetComponent<BlockBehaviour>();
            try
            {
                myGuid = BB.BuildingBlock.Guid.GetHashCode();
            }
            catch
            {
                myGuid = BB.Guid.GetHashCode();
            }

            SafeAwake();
        }
        public void Start()
        {
            FlightDataBase.Instance.AddLifter(myPlayerID, myGuid, this);
            Vis = transform.Find("Vis");
            BoxCollider = transform.Find("Joint");
        }

        // add back the reference to the parent block when the simulation is stopped
        public void OnDestroy()
        {
            if (BB.isSimulating)
            {
                FlightDataBase.Instance.AddLifter(myPlayerID, myGuid, BB.BuildingBlock.gameObject.GetComponent<AircraftLifter>());
            }
            else
            {
                FlightDataBase.Instance.RemoveLifter(myPlayerID, myGuid);
            }
        }
    }
}
