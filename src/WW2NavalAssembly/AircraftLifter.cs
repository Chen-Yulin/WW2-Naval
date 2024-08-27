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
using System.IO;

namespace WW2NavalAssembly
{
    public class AircraftLifterMsgReceiver : SingleInstance<AircraftLifterMsgReceiver>
    {
        public override string Name { get; } = "Aircraft Lifter Msg Receiver";
        public static MessageType DestroyMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType PositionMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single);
        public List<int>[] Destroyed = new List<int>[16];
        public Dictionary<int, float>[] Position = new Dictionary<int, float>[16];

        public AircraftLifterMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                Destroyed[i] = new List<int>();
                Position[i] = new Dictionary<int, float>();
            }
        }
        public void PositionMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            float position = (float)msg.GetData(2);
            if (Position[playerid].ContainsKey(guid))
            {
                Position[playerid][guid] = position;
            }
            else
            {
                Position[playerid].Add(guid, position);
            }
        }
        public void DestroyMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Destroyed[playerid].Add(guid);
        }
    }
        public class AircraftLifter : MonoBehaviour
    {
        public MToggle AsLifter;
        public MToggle EnableRaise;
        public MToggle EnableDrop;
        public MToggle Slave;
        public MText group;

        public bool preAsLifter;

        public bool RaiseEnabled
        {
            get
            {
                return EnableRaise.isDefaultValue;
            }
        }
        public bool DropEnabled
        {
            get
            {
                return EnableDrop.isDefaultValue;
            }
        }

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

        public float Health = 300f;

        public Vector3 ClientTargetPosition = Vector3.zero;

        public bool destroyed
        {
            get
            {
                return Health == 0;
            }
        }

        public bool masterFound = false;
        public void ControlMapper()
        {
            if (preAsLifter != !AsLifter.isDefaultValue)
            {
                preAsLifter = !AsLifter.isDefaultValue;
                if (preAsLifter)
                {
                    EnableRaise.DisplayInMapper = true;
                    EnableDrop.DisplayInMapper = true;
                    Slave.DisplayInMapper = true;
                    group.DisplayInMapper = true;
                }
                else
                {
                    EnableRaise.DisplayInMapper = false;
                    EnableDrop.DisplayInMapper = false;
                    Slave.DisplayInMapper = false;
                    group.DisplayInMapper = false;
                }
            }
        }

        public void ReduceHealth(float hp)
        {
            if (Health > 0)
            {
                Health = Mathf.Clamp(Health - hp, 0, 300f);
                if(Health == 0)
                {
                    Vis.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.Destroyed_Tex;
                    GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                    Destroy(smoke, 10);
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(AircraftLifterMsgReceiver.DestroyMsg.CreateMessage(myPlayerID, myGuid));
                    }
                }
            }

        }
        public bool GoToDeckStep()
        {
            FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
            if (deck != null)
            {
                float height = deck.height;
                Vector3 localPos = transform.InverseTransformPoint(new Vector3(transform.position.x, height, transform.position.z));
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
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(AircraftLifterMsgReceiver.PositionMsg.CreateMessage(myPlayerID, myGuid, Vis.localPosition.z));
                    }
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
                Vector3 localPos = transform.InverseTransformPoint(new Vector3(transform.position.x, height, transform.position.z));
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
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(AircraftLifterMsgReceiver.PositionMsg.CreateMessage(myPlayerID, myGuid, Vis.localPosition.z));
                    }
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
            EnableRaise = BB.AddToggle("Enable Raise", "EnableRaise", true);
            EnableDrop = BB.AddToggle("Enable Drop", "EnableDrop", true);
            Slave = BB.AddToggle("Slave Lifter", "Slave", false);
            group = BB.AddText("Elevator group", "slave group", "0");
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
            if (BB.isSimulating)
            {
                if (!AsLifter.isDefaultValue)
                {
                    if (Slave.isDefaultValue)
                    {
                        FlightDataBase.Instance.AddLifter(myPlayerID, myGuid, this);
                        if (BB.isSimulating)
                        {
                            Grouper.Instance.AddLifterMaster(myPlayerID, group.Value, this);
                        }
                    }
                }
                else
                {
                    FlightDataBase.Instance.RemoveLifter(myPlayerID, myGuid);
                }
            }
            Vis = transform.Find("Vis");
            BoxCollider = transform.Find("Joint");
            preAsLifter = AsLifter.isDefaultValue;
        }
        public void BuildUpdate()
        {
            ControlMapper();
        }

        public void SimulateUpdate()
        {
            if (StatMaster.isMP && StatMaster.isClient)
            {
                if (AircraftLifterMsgReceiver.Instance.Destroyed[myPlayerID].Contains(myGuid))
                {
                    AircraftLifterMsgReceiver.Instance.Destroyed[myPlayerID].Remove(myGuid);
                    Vis.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.Destroyed_Tex;
                    GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                    Destroy(smoke, 10);
                }
                if (AircraftLifterMsgReceiver.Instance.Position[myPlayerID].ContainsKey(myGuid))
                {
                    ClientTargetPosition = new Vector3(0,0,AircraftLifterMsgReceiver.Instance.Position[myPlayerID][myGuid]);
                    AircraftLifterMsgReceiver.Instance.Position[myPlayerID].Remove(myGuid);
                }

                

            }
        }

        public void SimulateFixedUpdate()
        {
            if (StatMaster.isMP && StatMaster.isClient)
            {
                Vis.localPosition = Vector3.Lerp(Vis.localPosition, ClientTargetPosition, 0.1f);
            }
        }

        public void Update()
        {
            if (BB.isSimulating)
            {
                SimulateUpdate();
            }
            else
            {
                BuildUpdate();
            }
        }
        public void FixedUpdate()
        {
            if (BB.isSimulating)
            {
                if (!Slave.isDefaultValue &&!masterFound)
                {
                    if (Grouper.Instance.AircraftLifterMasters[myPlayerID].ContainsKey(group.Value))
                    {
                        masterFound = true;
                        try
                        {
                            Vis.parent = Grouper.Instance.AircraftLifterMasters[myPlayerID][group.Value].Vis;
                            BoxCollider.parent = Grouper.Instance.AircraftLifterMasters[myPlayerID][group.Value].BoxCollider;
                        }
                        catch { }
                        
                    }
                }
                if (Slave.isDefaultValue)
                {
                    SimulateFixedUpdate();
                }
            }
            else
            {
            }
        }

        // add back the reference to the parent block when the simulation is stopped
        public void OnDestroy()
        {
            FlightDataBase.Instance.RemoveLifter(myPlayerID, myGuid);
            if (Slave.isDefaultValue)
            {
                Grouper.Instance.RemoveLifterMaster(myPlayerID, group.Value);
            }
        }
    }
}
