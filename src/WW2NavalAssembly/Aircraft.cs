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

    public class Aircraft : BlockScript
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
            Landing,
            ShootDown,
            Exploded,
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

        
        public bool TriedFindHangar = false;
        public bool ColliderActive
        {
            set
            {
                if (value != colliderActive)
                {
                    transform.Find("Colliders").gameObject.SetActive(value);
                    colliderActive = value;
                }
            }
        }
        public bool RigidActive
        {
            set
            {
                if (value != rigidActive)
                {
                    myRigid.isKinematic = !value;
                    rigidActive = value;
                }
            }
        }

        public bool SmoothActive
        {
            set
            {
                if (value != smoothActive)
                {
                    if (value)
                    {
                        transform.Find("Colliders").GetChild(0).GetComponent<CapsuleCollider>().material = SmoothMat;
                        transform.Find("Colliders").GetChild(1).GetComponent<CapsuleCollider>().material = SmoothMat;
                    }
                    else
                    {
                        transform.Find("Colliders").GetChild(0).GetComponent<CapsuleCollider>().material = RegularMat;
                        transform.Find("Colliders").GetChild(1).GetComponent<CapsuleCollider>().material = RegularMat;
                    }
                    
                    smoothActive = value;
                }
            }
        }

        bool colliderActive = true;
        bool rigidActive = true;
        bool smoothActive = false;

        public string preType;
        public string preAppearance;
        public int preRank;

        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject PropellerObject;
        public GameObject UndercartObject;
        public GameObject AircraftVis;
        public Transform MyHangar;
        public Transform MyDeck;

        private PropellerBehaviour Propeller;


        public Dictionary<int, Aircraft> myGroup = new Dictionary<int, Aircraft>();

        public Aircraft myLeader;

        public GameObject GroupLine;

        public PhysicMaterial SmoothMat;
        public PhysicMaterial RegularMat;

        // ============== for aero dynamics =================
        public float AirDensity = 0.0015f;
        public float mainWingArea = 20f;
        public float verticleWingArea = 5f;

        // ================== for flight ==================
        float thrust = 0f;
        public float Thrust
        {
            set
            {
                thrust = Mathf.Clamp(value, 0f, 60f);
            }
            get
            {
                return thrust;
            }
        }
        public float Pitch
        {
            set
            {
                if (Mathf.Abs(value)<0.1f)
                {
                    SetPitch(0);
                }
                else
                {
                    SetPitch(value);
                }
                
            }
            get
            {
                float angle = Vector3.Angle(Vector3.up, -transform.up);
                return 90 - angle;
            }
        }
       public float CruiseHeight = 60f;


        // ============== for takingOff =================
        public Vector2 TakeOffDirection;

        // ================== for cruise ==================
        public Queue<Vector2> WayPoints = new Queue<Vector2>();

        
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
                Propeller = PropellerObject.transform.GetComponent<PropellerBehaviour>();
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
        public void GetPartsOnSimulateStart()
        {
            PropellerObject = transform.Find("Vis").Find("Propeller").gameObject;
            Propeller = PropellerObject.transform.GetComponent<PropellerBehaviour>();
            UndercartObject = transform.Find("Vis").Find("Undercart").gameObject;
            AircraftVis = transform.Find("Vis").gameObject;
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
                            break;
                        }
                    }
                    break;
                }
            }
        }
        public void FindDeck()
        {
            FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
            if (deck.Occupied_num < deck.Total_num)
            {
                foreach (Transform spot in FlightDataBase.Instance.DeckObjects[myPlayerID].transform.Find("Vis"))
                {
                    if (!spot.gameObject.GetComponent<ParkingSpot>().occupied)
                    {
                        MyDeck = spot;
                        spot.gameObject.GetComponent<ParkingSpot>().occupied = true;
                        FlightDataBase.Instance.GetTakeOffPosition(myPlayerID);
                        break;
                    }
                }
            }
        }
        public void SettleSpot(Transform spot, bool direct = false)
        {
            if (spot)
            {
                if (!direct)
                {
                    if ((transform.position - spot.position).magnitude > 1f || Vector3.Angle(transform.forward, Vector3.up) > 30f)
                    {
                        transform.position = spot.position + Vector3.up * 0.05f;
                        transform.rotation = spot.GetChild(0).rotation;
                        myRigid.drag = 100f;
                        myRigid.angularDrag = 1000;
                    }
                    else
                    {
                        Vector3 targetPosition = Vector3.Lerp(transform.position, spot.position, 0.1f);
                        targetPosition.y = transform.position.y;
                        transform.position = targetPosition;
                        myRigid.drag = 0.2f;
                        myRigid.angularDrag = 0.2f;
                    }

                }
                else
                {
                    transform.position = MyHangar.position;
                }
                // modify rotation

                float deltaAngle = MathTool.SignedAngle(-new Vector2(transform.up.x, transform.up.z), new Vector2(spot.forward.x, spot.forward.z));
                transform.RotateAround(transform.position, transform.up, deltaAngle);

            }

        }
        public void ChangeDeckSpot(Transform spot, bool takeoffSpot)
        {
            MyDeck.gameObject.GetComponent<ParkingSpot>().occupied = false;
            FlightDataBase.Instance.GetTakeOffPosition(myPlayerID);
            MyDeck = spot;
            if (!takeoffSpot)
            {
                MyDeck.GetComponent<ParkingSpot>().occupied = true;
            }
            else
            {
                FlightDataBase.Instance.Decks[myPlayerID].Occupied_num--;
                transform.Find("Vis").GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh0(preAppearance);
                transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex0(preAppearance);
            }
        }
        public virtual float CalculateLift(float WingArea, float AoA, bool mainWing)
        {
            float CL = CalculateCL(AoA, mainWing?5:0);
            float Lift = 0.5f * AirDensity * myRigid.velocity.sqrMagnitude * WingArea * CL;
            return Lift;
        }
        public float CalculateDrag(float WingArea, float AoA)
        {
            float CD = CalculateCD(AoA);
            float Drag = 0.5f * AirDensity * myRigid.velocity.sqrMagnitude * WingArea * CD;
            return Drag;
        }

        public float CalculateCL(float AoA ,int WingConst)
        {
            return (-0.7f * Mathf.Atan(0.1f * Mathf.Abs(AoA) - 2) + 1)
                    * 6f * Mathf.Sin(0.01f * (AoA + WingConst));
        }
        public float CalculateCD(float AoA)
        {
            return 1f / 3000f * AoA * AoA + 0.008f;
        }
        public void AddMainWingForce()
        {
            Vector3 velocity_verticle = Vector3.ProjectOnPlane(myRigid.velocity, transform.right);
            float AoA = Vector3.Angle(velocity_verticle, -transform.up);
            if (Vector3.Dot(velocity_verticle, transform.forward) > 0)
            {
                AoA = -AoA;
            }
            Vector3 lift_direction = Vector3.Cross(myRigid.velocity, transform.right).normalized;
            Vector3 drag_direction = -myRigid.velocity.normalized;

            myRigid.AddForce(CalculateLift(mainWingArea, AoA, true) * lift_direction + CalculateDrag(mainWingArea, AoA) * drag_direction, ForceMode.Force);
        }
        public void AddAeroForce()
        {
            myRigid.angularDrag = Mathf.Clamp(myRigid.velocity.magnitude * 0.5f, 0.2f,150f);
            myRigid.drag = Mathf.Clamp(myRigid.velocity.magnitude * 0.01f, 0.2f, 10f);

            // horizon
            AddMainWingForce();

            // vertical
            Vector3 velocity = myRigid.velocity;
            velocity = transform.InverseTransformDirection(velocity);
            velocity.x *= 0.9f;
            myRigid.velocity = transform.TransformDirection(velocity);
        }
        public void SetPitch(float p)
        {
            if (p == 0) p = 0.1f;
            Vector2 forward_h = new Vector2(-transform.up.x, -transform.up.z).normalized;
            float forward_v = forward_h.magnitude * Mathf.Tan(p * Mathf.Deg2Rad);

            Vector2 newforward_h = forward_v * -forward_h;
            float newforward_v = forward_h.magnitude;

            Vector3 newforward = new Vector3(newforward_h.x, newforward_v, newforward_h.y).normalized;
            transform.rotation = Quaternion.LookRotation(newforward, (p>0?-1:1)*Vector3.up);
        }
        public void SetHeight(float h)
        {
            Vector3 velocity = myRigid.velocity;
            velocity.y += (h-transform.position.y)*0.05f;
            myRigid.velocity = velocity;
        }
        public void SetVisRoll()
        {
            AircraftVis.transform.localEulerAngles = new Vector3(90, Mathf.Clamp(myRigid.angularVelocity.y * 45,-60,60), 0);
        }
        public void TurnToWayPoint()
        {
            Vector2 target = WayPoints.Peek();
            Vector2 targetDir = target - MathTool.Get2DCoordinate(transform.position);
            Vector2 forward = MathTool.Get2DCoordinate(-transform.up);
            float angle = MathTool.SignedAngle(forward, targetDir);
            myRigid.AddTorque(-Vector3.up * angle * 0.15f);
        }

        public void SwitchToCruise()
        {
            if (status == Status.TakingOff)
            {
                status = Status.Cruise;
                UndercartObject.SetActive(false);
                Thrust = 100f;
            }
        }
        public void SwitchToTakingOff()
        {
            if (status == Status.OnBoard)
            {
                status = Status.TakingOff;
                MyDeck = null;
                TakeOffDirection = new Vector2(-transform.up.x, -transform.up.z);
                Propeller.enabled = true;
                Propeller.Speed = new Vector3(0, 0, 11);
                Thrust = 40f;
                WayPoints.Enqueue(MathTool.Get2DCoordinate(transform.position));
            }
        }
        public void SwitchToOnBoard()
        {
            if (status == Status.InHangar)
            {
                if (!MyDeck)
                {
                    FindDeck();
                }
                status = Status.OnBoard;
                SettleSpot(MyDeck, true);
                Propeller.enabled = true;
                Propeller.Speed = new Vector3(0, 0, 3);
                Thrust = 0;
            }
        }
        public void SwitchToInHangar()
        {
            if (status == Status.OnBoard)
            {
                MyDeck.gameObject.GetComponent<ParkingSpot>().occupied = false;
                FlightDataBase.Instance.Decks[myPlayerID].Occupied_num--;
                FlightDataBase.Instance.GetTakeOffPosition(myPlayerID);
                MyDeck = null;
                status = Status.InHangar;
                SettleSpot(MyHangar, true);
                Propeller.enabled = false;
                Thrust = 0;
            }
            else if (status == Status.InHangar)
            {
            }
        }
        public void InHangarBehaviourFU()
        {
            SettleSpot(MyHangar,false);
        }
        public void InHangarBehaviourUpdate()
        {
            
        }
        public void OnBoardBehaviourFU()
        {
            SettleSpot(MyDeck,false);
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
            ColliderActive = false;
            RigidActive = false;
            GetPartsOnSimulateStart();
            SmoothMat = new PhysicMaterial();
            SmoothMat.bounciness = 0f;
            SmoothMat.bounceCombine = PhysicMaterialCombine.Minimum;
            SmoothMat.staticFriction = 0f;
            SmoothMat.dynamicFriction = 0f;
            SmoothMat.frictionCombine = PhysicMaterialCombine.Minimum;
            RegularMat = transform.Find("Colliders").GetChild(0).GetComponent<CapsuleCollider>().material;
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
        public override void OnSimulateCollisionEnter(Collision collision)
        {
            if (status == Status.Exploded)
            {
                return;
            }
            float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            if (collisionForce > 2000f )
            {
                GameObject explo = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftExplo, transform.position, Quaternion.identity);
                Destroy(explo, 5);
                Debug.Log("Aircraft exploded");
                status = Status.Exploded;
                myRigid.drag = 0.2f;
                myRigid.angularDrag = 0.2f;
                Thrust = 0f;
                SmoothActive = false;
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

            switch (Rank.Value)
            {
                case 0:
                    myGroup = new Dictionary<int, Aircraft>();
                    myLeader = Grouper.Instance.GetLeader(myPlayerID, Group.Value);
                    break;
                case 1:
                    myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                    myLeader = null;
                    break;
                case 2:
                    myGroup = new Dictionary<int, Aircraft>();
                    myLeader = Grouper.Instance.GetLeader(myPlayerID, "null");
                    break;
                default:
                    break;
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
        public override void SimulateUpdateHost()
        {
            switch (status)
            {
                case Status.InHangar:
                    break;
                default: break;
            }

            switch (Rank.Value)
            {
                case 0:
                    break;
                case 1:
                    if (SwitchActive.IsPressed)
                    {
                        if (FlightDataBase.Instance.aircraftController[myPlayerID].CurrentLeader == this)
                        {
                            FlightDataBase.Instance.aircraftController[myPlayerID].CurrentLeader = null;
                        }
                        else
                        {
                            FlightDataBase.Instance.aircraftController[myPlayerID].CurrentLeader = this;
                        }
                    }
                    break;
                case 2:
                    break;
                default:
                    break;
            }

        }

        public override void SimulateFixedUpdateHost()
        {
            if (ModController.Instance.state % 10 == myseed && !TriedFindHangar)
            {
                if (!MyHangar)
                {
                    ColliderActive = true;
                    FindHangar();
                    status = Status.InHangar;
                    SettleSpot(MyHangar, true);
                    TriedFindHangar = true;
                }
            }
            // after first trying, active rigid and collider
            if (TriedFindHangar)
            {
                RigidActive = true;
                ColliderActive = true;
            }

            switch (status)
            {
                case Status.InHangar:
                    InHangarBehaviourFU();
                    SmoothActive = false;
                    break;
                case Status.OnBoard:
                    OnBoardBehaviourFU();
                    SmoothActive = false;
                    break;
                case Status.TakingOff:
                    AddAeroForce();
                    Thrust += 0.2f;
                    myRigid.angularVelocity = Vector3.zero;
                    SmoothActive = true;
                    if (transform.position.y >= CruiseHeight)
                    {
                        SwitchToCruise();
                    }
                    break;
                case Status.Cruise:
                    AddAeroForce();
                    SmoothActive = false;
                    Pitch *= 0.98f;
                    SetHeight(CruiseHeight);
                    TurnToWayPoint();
                    SetVisRoll();
                    break;
                default : break;
            }

            myRigid.AddForce(Thrust * (-transform.up));

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
