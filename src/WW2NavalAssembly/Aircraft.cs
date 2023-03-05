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

        public MMenu Type;
        public MMenu TorpedoType;
        public MMenu BombType;
        public MMenu FighterType;
        public MToggle Customize;
        public MText Group;
        public MToggle AsLeader;
        public MKey SwitchActive;
        

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public int frameCount;

        public int preType;
        public bool preCustom;
        public string preAppearance;
        public bool preIsLeader;

        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject PropellerObject;
        public GameObject UndercartObject;

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
                PropellerObject.AddComponent<MeshFilter>();
                PropellerObject.AddComponent<MeshRenderer>().material = transform.Find("Vis").GetComponent<MeshRenderer>().material;
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
            transform.Find("Vis").GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh0(craftName);
            transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex0(craftName);
            UndercartObject.GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh1(craftName);
            UndercartObject.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex1(craftName);
            UndercartObject.transform.localPosition = Vector3.zero;
            PropellerObject.GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh2(craftName);
            PropellerObject.GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex2(craftName);
            PropellerObject.transform.localPosition = new Vector3(0, AircraftAssetManager.Instance.GetOffset(craftName), 0);
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
            try
            {
                GroupLine.GetComponent<LineRenderer>().SetPosition(0, myLeader.transform.position);
                GroupLine.GetComponent<LineRenderer>().SetPosition(1, transform.position);
                GroupLine.SetActive(true);
            }
            catch { }
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

        public override void SafeAwake()
        {
            name = "Aircraft";
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
            myseed = (int)(UnityEngine.Random.value * 10);
            Customize = AddToggle("Customize Appearance", "ACCustomize", false);

            preType = 0;
            preCustom = false;
            preAppearance = "";
            preIsLeader = true;
            preSkinEnabled = OptionsMaster.skinsEnabled;
            preShowCluster = StatMaster.clusterCoded;
            
            InitPropellerUndercart();
            InitGroupLine();

            SwitchActive = AddKey("Switch Active", "SwitchActive", KeyCode.Alpha1);
            
            Group = AddText("Group", "AircraftGroup", "1");
            AsLeader = AddToggle("As Leader", "AsLeader", false);

            Type = AddMenu("Aircraft Type",0,new List<string>
            {
                "Fighter",
                "Torpedo",
                "Bomb"
            });
            TorpedoType = AddMenu("TorpedoType", 0, new List<string>
            {
                "SBD",
                "99"
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
        }
        public void Start()
        {
            name = "Aircraft";
        }
        public override void BuildingUpdate()
        {

            if (ModController.Instance.state % 10 == myseed)
            {
                Grouper.Instance.AddAircraft(myPlayerID, Group.Value, BlockBehaviour.Guid.GetHashCode(), this);
                //Debug.Log("add " + BlockBehaviour.Guid.GetHashCode());
            }
            bool appearChanged = false;
            if (preCustom != Customize.isDefaultValue)
            {
                preCustom = Customize.isDefaultValue;
                appearChanged = true;
            }
            if (preType != Type.Value)
            {
                preType = Type.Value;
                appearChanged = true;
            }
            if (appearChanged)
            {
                if (Customize.isDefaultValue)
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
                else
                {
                    TorpedoType.DisplayInMapper = false;
                    BombType.DisplayInMapper = false;
                    FighterType.DisplayInMapper = false;
                }

            }

            string nowAppearance = "";

            if (Customize.isDefaultValue)
            {
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
            }
            else
            {
                nowAppearance = "cus";
            }

            if (preAppearance != nowAppearance)
            {
                preAppearance = nowAppearance;
                UpdateAppearance(nowAppearance);
            }

            bool leaderChanged = false;
            if (preIsLeader == AsLeader.isDefaultValue)
            {
                preIsLeader = !AsLeader.isDefaultValue;
                leaderChanged = true;
            }
            if (leaderChanged)
            {
                if (preIsLeader)
                {
                    SwitchActive.DisplayInMapper = true;
                }
                else
                {
                    SwitchActive.DisplayInMapper = false;
                }
            }

        }
        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            Grouper.Instance.AddAircraft(myPlayerID, Group.Value, myGuid, this);
            if (!AsLeader.isDefaultValue)
            {
                myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                myLeader = null;
            }
            else
            {
                myGroup = new Dictionary<int, Aircraft>();
                myLeader = Grouper.Instance.GetLeader(myPlayerID, Group.Value);
            }
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
                if (Customize.isDefaultValue)
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
                else
                {
                    preAppearance = "cus";
                }
            }
            HoldAppearance();
            if (!AsLeader.isDefaultValue)
            {
                myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                myLeader = null;
            }
            else
            {
                myGroup = new Dictionary<int, Aircraft>();
                myLeader = Grouper.Instance.GetLeader(myPlayerID, Group.Value);
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

        }

        public void OnGUI()
        {
            if (!AsLeader.isDefaultValue)
            {
                GUI.Box(new Rect(100, 200, 200, 50), myGroup.Count.ToString());
            }
            
        }



    }
}
