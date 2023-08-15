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

    class Aircraft : BlockScript
    {
        public enum Status
        {
            Deprecated,
            InHangar,
            OnBoard,
            TakingOff,
            Cruise,
            Attacking,
            Returning,
            Landing
        }

        public MMenu Type;
        public MMenu TorpedoType;
        public MMenu BombType;
        public MMenu FighterType;
        public MMenu Rank;
        public MText Group;
        public MKey SwitchActive;

        public Status status = Status.Deprecated;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public Rigidbody myRigid;

        public string preType;
        public string preAppearance;
        public int preRank;

        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject PropellerObject;
        public GameObject UndercartObject;
        public Transform MyHangar;
        public Transform MyDeck;

        public PropellerBehaviour Propeller;


        public Dictionary<int, Aircraft> myGroup = new Dictionary<int, Aircraft>();

        public Aircraft myLeader;

        public GameObject GroupLine;


        
        public void DestroyComponent(GameObject go)
        {
            try
            {
                foreach (var jointComponent in go.GetComponents<ConfigurableJoint>())
                {
                    Destroy(jointComponent);
                }

                try
                {
                    Destroy(go.GetComponent<IceTag>());
                }
                catch { }
                try
                {
                    Destroy(go.GetComponent<ConstantForce>());
                }
                catch{}
                try
                {
                    Destroy(go.GetComponent<PropellorController>());
                }
                catch { }
                try
                {
                    Destroy(go.GetComponent<FlyingController>());
                }
                catch { }
                try
                {
                    Destroy(go.GetComponent<AxialDrag>());
                }
                catch { }
                try
                {
                    Destroy(go.GetComponent<FireController>());
                }
                catch { }

                try
                {
                    Destroy(go.GetComponent<Rigidbody>());
                }
                catch
                {
                    Debug.LogError("Destroy Rigid:" + go.name + " Error");
                }
                
                try
                {
                    Destroy(go.GetComponent<WWIIUnderWater>());
                }
                catch{}
                try
                {
                    Destroy(go.GetComponent<DefaultArmour>());
                }
                catch { }
                try
                {
                    Destroy(go.GetComponent<WoodenArmour>());
                }
                catch { }

            }
            catch
            {
                Debug.LogError("Destroy " + go.name + " Error");
            }
        }

        // deprecated
        public void OptimizeBlock(BlockBehaviour bb, int hierarchy = 0)
        {
            //Debug.Log("Optimizing Block");
            //Debug.Log(ClusterIndex);

            BlockCluster cluster = bb.ParentMachine.LinkManager.GetCluster(bb.ClusterIndex);
            if (cluster != null)
            {
                if (cluster.Blocks.Count > 20)
                {
                    return;
                }
                foreach (var block in cluster.Blocks)
                {
                    
                    {
                        //string outputSpace = "";
                        //for (int i = 0; i < hierarchy; i++)
                        //{
                        //    outputSpace += "  ";
                        //}
                        //Debug.Log(outputSpace + block.Block.SimBlock.name);
                    }

                    if (block.Block.SimBlock == bb)
                    {
                        Debug.Log("Same");
                        continue;
                    }


                    DestroyComponent(block.Block.SimBlock.gameObject);
                    block.Block.SimBlock.gameObject.transform.SetParent(transform);


                    if (block.Block.SimBlock.jointsToMe != null)
                    {
                        foreach (var attachedJoint in block.Block.SimBlock.jointsToMe)
                        {
                            
                            if (attachedJoint.gameObject.tag == "MechanicalTag")
                            {
                                if (attachedJoint.gameObject.GetComponent<BlockBehaviour>().ClusterIndex == bb.ClusterIndex)
                                {
                                    continue;
                                }
                                //Debug.Log(outputSpace + "<new Cluster>  {");
                                try
                                {
                                    attachedJoint.gameObject.GetComponent<ConfigurableJoint>().connectedBody = GetComponent<Rigidbody>();
                                }
                                catch
                                {
                                    Debug.LogError("Reconnect Joint Error: " + attachedJoint.gameObject.name);
                                }
                                OptimizeBlock(attachedJoint.gameObject.GetComponent<BlockBehaviour>(),hierarchy + 1);
                                //Debug.Log(outputSpace + "}");
                            }
                        }
                    }
                    
                }
            }
            
        }
        public void InitPropellerUndercart()
        {
            if (!transform.Find("Vis").Find("Propeller"))
            {
                PropellerObject = new GameObject("Propeller");
                PropellerObject.transform.SetParent(transform.Find("Vis"));
                PropellerObject.transform.localScale = Vector3.one;
                PropellerObject.transform.localEulerAngles = Vector3.zero;
                Propeller = PropellerObject.AddComponent<PropellerBehaviour>();
                Propeller.enabled = false;
                Propeller.Speed = new Vector3(0, 0, 11f);

                GameObject PropellerChild = new GameObject("PropellerChild");
                PropellerChild.transform.SetParent(PropellerObject.transform);
                PropellerChild.transform.localScale = Vector3.one;
                PropellerChild.transform.localEulerAngles = Vector3.zero;
                PropellerChild.transform.localPosition = Vector3.zero;

                PropellerChild.AddComponent<MeshFilter>();
                PropellerChild.AddComponent<MeshRenderer>().material = transform.Find("Vis").GetComponent<MeshRenderer>().material;
            }
            else
            {
                PropellerObject = transform.Find("Vis").Find("Propeller").gameObject;
            }
            if (!transform.Find("Vis").Find("Undercart"))
            {
                UndercartObject = new GameObject("Undercart");
                UndercartObject.transform.SetParent(transform.Find("Vis"));
                UndercartObject.transform.localScale = Vector3.one;
                UndercartObject.transform.localEulerAngles = Vector3.zero;
                UndercartObject.AddComponent<MeshFilter>();
                UndercartObject.AddComponent<MeshRenderer>().material = transform.Find("Vis").GetComponent<MeshRenderer>().material;
            }
            else
            {
                UndercartObject = transform.Find("Vis").Find("Undercart").gameObject;
            }

        }
        public void UpdateAppearance(string craftName)
        {
            transform.Find("Vis").GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh05(craftName);
            transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex05(craftName);
            transform.Find("Vis").localPosition = AircraftAssetManager.Instance.GetBodyOffset(craftName);

            UndercartObject.GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh1(craftName);
            UndercartObject.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex1(craftName);
            UndercartObject.transform.localPosition = Vector3.zero;

            PropellerObject.transform.GetChild(0).gameObject.GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh2(craftName);
            PropellerObject.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex2(craftName);
            PropellerObject.transform.GetChild(0).localPosition = -new Vector3(0, AircraftAssetManager.Instance.GetPropOffset(craftName), 0);
            PropellerObject.transform.localPosition = new Vector3(0, AircraftAssetManager.Instance.GetPropOffset(craftName), 0);
        }
        public void HoldAppearance()
        {
            bool changed = false;
            if (preShowCluster != StatMaster.clusterCoded)
            {
                preShowCluster = StatMaster.clusterCoded;
                changed = true;
            }
            else if (preSkinEnabled != OptionsMaster.skinsEnabled)
            {
                preSkinEnabled = OptionsMaster.skinsEnabled;
                changed = true;
            }
            if (changed)
            {
                UpdateAppearance(preAppearance);
            }
        }
        public void ShowGroupLine()
        {
            GroupLine.SetActive(false);
            if (myLeader)
            {
                GroupLine.GetComponent<LineRenderer>().SetPosition(0, myLeader.transform.position);
                GroupLine.GetComponent<LineRenderer>().SetPosition(1, transform.position);
                GroupLine.SetActive(true);
            }
        }
        public void InitGroupLine()
        {
            if (transform.Find("line"))
            {
                GroupLine = transform.Find("line").gameObject;
                GroupLine.SetActive(false);
            }
            else
            {
                GroupLine = new GameObject("line");
                GroupLine.transform.SetParent(gameObject.transform);
                LineRenderer LR = GroupLine.AddComponent<LineRenderer>();
                LR.material = new Material(Shader.Find("Particles/Additive"));
                LR.SetColors(Color.red, Color.white);
                LR.SetWidth(0.1f, 0.05f);
                GroupLine.SetActive(false);
            }
        }
        public void FindHangar()
        {
            foreach (var hangar in FlightDataBase.Instance.Hangars[myPlayerID])
            {
                if (hangar.Value.Occupied_num < hangar.Value.Total_num) // there are spare space
                {
                    hangar.Value.Occupied_num++;
                    foreach (Transform spot in FlightDataBase.Instance.HangarObjects[myPlayerID][hangar.Key].transform.Find("Vis"))
                    {
                        if (!spot.gameObject.GetComponent<ParkingSpot>().occupied)
                        {
                            MyHangar = spot;
                            spot.gameObject.GetComponent<ParkingSpot>().occupied = true;
                            status = Status.InHangar;
                            break;
                        }
                    }
                    break;
                }
            }
        }
        public void SettleHangar(bool force = false)
        {
            if (MyHangar)
            {
                if (force)
                {
                    Vector3 f = (MyHangar.position - transform.position) * 100f;
                    f.y = 0;
                    myRigid.AddForceAtPosition(f, transform.position);
                }
                else
                {
                    transform.position = MyHangar.position;
                }
                // modify rotation
                //transform.rotation = Quaternion.Euler(transform.eulerAngles.x, MyHangar.eulerAngles.y, transform.eulerAngles.z);

            }

        }


        public void InHangarBehaviour()
        {
            if (MyHangar)
            {
                SettleHangar(true);
            }
        }

        public override void SafeAwake()
        {
            name = "Aircraft";
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
            myseed = (int)(UnityEngine.Random.value * 10);

            preType = "";
            preAppearance = "";
            preRank = -1;
            preSkinEnabled = OptionsMaster.skinsEnabled;
            preShowCluster = StatMaster.clusterCoded;
            
            InitPropellerUndercart();
            InitGroupLine();

            SwitchActive = AddKey("Switch Active", "SwitchActive", KeyCode.Alpha1);
            
            Group = AddText("Group", "AircraftGroup", "1");

            Type = AddMenu("Aircraft Type",0,new List<string>
            {
                "Fighter",
                "Torpedo",
                "Bomb"
            });
            TorpedoType = AddMenu("TorpedoType", 0, new List<string>
            {
                "SB2C",
                "B7A2"
            });
            BombType = AddMenu("BombType", 0, new List<string>
            {
                "SBD",
                "99"
            });
            FighterType = AddMenu("FighterType", 0, new List<string>
            {
                "Zero",
                "F4U",
            });
            Rank = AddMenu("Rank", 0, new List<string>
            {
                "Slave",
                "Leader",
                "Backup",
            });
        }
        public void Start()
        {
            name = "Aircraft";
        }
        public override void BuildingUpdate()
        {

            if (ModController.Instance.state % 10 == myseed)
            {
                Grouper.Instance.AddAircraft(myPlayerID, Rank.Value == 2? "null" : Group.Value, BlockBehaviour.Guid.GetHashCode(), this);
                //Debug.Log("add " + BlockBehaviour.Guid.GetHashCode());
            }
            bool appearChanged = false;
            if (preType != Type.Selection)
            {
                preType = Type.Selection;
                appearChanged = true;
            }
            if (appearChanged)
            {
                switch (Type.Value)
                {
                    case 1:
                        TorpedoType.DisplayInMapper = true;
                        BombType.DisplayInMapper = false;
                        FighterType.DisplayInMapper = false;
                        break;
                    case 2:
                        TorpedoType.DisplayInMapper = false;
                        BombType.DisplayInMapper = true;
                        FighterType.DisplayInMapper = false;
                        break;
                    case 0:
                        TorpedoType.DisplayInMapper = false;
                        BombType.DisplayInMapper = false;
                        FighterType.DisplayInMapper = true;
                        break;
                    default:
                        break;
                }
            }

            string nowAppearance = "";

            switch (Type.Value)
            {
                case 1:
                    nowAppearance = TorpedoType.Selection;
                    break;
                case 2:
                    nowAppearance = BombType.Selection;
                    break;
                case 0:
                    nowAppearance = FighterType.Selection;
                    break;
                default:
                    break;
            }

            if (preAppearance != nowAppearance)
            {
                preAppearance = nowAppearance;
                UpdateAppearance(nowAppearance);
            }

            bool rankChanged = false;
            if (Rank.Value != preRank)
            {
                preRank = Rank.Value;
                rankChanged = true;
            }
            if (rankChanged)
            {
                switch (preRank)
                {
                    case 0:
                        Group.DisplayInMapper = true;
                        SwitchActive.DisplayInMapper = false;
                        break;
                    case 1:
                        Group.DisplayInMapper = true;
                        SwitchActive.DisplayInMapper = true;
                        break;
                    case 2:
                        Group.DisplayInMapper = false;
                        SwitchActive.DisplayInMapper = false;
                        break;
                }
            }
        }
        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            Grouper.Instance.AddAircraft(myPlayerID, Rank.Value == 2 ? "null" : Group.Value, myGuid, this);
            if (Rank.Value == 1)
            {
                myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                myLeader = null;
            }
            else
            {
                myGroup = new Dictionary<int, Aircraft>();
                myLeader = Grouper.Instance.GetLeader(myPlayerID, Rank.Value == 2 ? "null" : Group.Value);
            }
            MyHangar = null;
            myRigid = BlockBehaviour.Rigidbody;
        }
        public void OnDestroy()
        {
            if (BlockBehaviour.isSimulating)
            {
                Grouper.Instance.AddAircraft(myPlayerID, "null", myGuid, this);
            }
            else
            {
                Grouper.Instance.AddAircraft(myPlayerID, "null", BlockBehaviour.Guid.GetHashCode(), this);
            }
            

        }
        public void Update()
        {
            if (BlockBehaviour.isSimulating && preAppearance == "")
            {
                switch (Type.Value)
                {
                    case 1:
                        preAppearance = TorpedoType.Selection;
                        break;
                    case 2:
                        preAppearance = BombType.Selection;
                        break;
                    case 0:
                        preAppearance = FighterType.Selection;
                        break;
                    default:
                        break;
                }
            }
            HoldAppearance();
            if (Rank.Value == 1)
            {
                myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                myLeader = null;
            }
            else
            {
                myGroup = new Dictionary<int, Aircraft>();
                myLeader = Grouper.Instance.GetLeader(myPlayerID, Rank.Value == 2? "null" : Group.Value);
            }


            if (ModController.Instance.showArmour)
            {
                ShowGroupLine();
            }
            else
            {
                GroupLine.SetActive(false);
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (ModController.Instance.state % 10 == myseed)
            {
                if (!MyHangar)
                {
                    FindHangar();

                    SettleHangar();
                }
            }
            switch (status)
            {
                case Status.InHangar:
                    InHangarBehaviour();
                    break;
                default : break;
            }
        }
        public void OnGUI()
        {
            if (Rank.Value == 1)
            {
                //GUI.Box(new Rect(100, 200, 200, 50), myGroup.Count.ToString());
            }
            
        }



    }
}
