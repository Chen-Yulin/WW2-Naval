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
            DogFighting,
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

        public int frameCount = 0;

        public Rigidbody myRigid;

        
        public bool TriedFindHangar = false;
        public bool ColliderActive
        {
            set
            {
                if (value != colliderActive)
                {
                    transform.Find("Colliders").gameObject.SetActive(value);
                    myRigid.useGravity = value;
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

        public bool DeckSliding
        {
            get { return deckSliding; }
            set
            {
                if (value != deckSliding)
                {
                    if (value)
                    {
                        transform.Find("Colliders").GetChild(0).GetComponent<CapsuleCollider>().isTrigger = true;
                        transform.Find("Colliders").GetChild(1).GetComponent<CapsuleCollider>().material = SmoothMat;
                    }
                    else
                    {
                        deckBelow = false;
                        transform.Find("Colliders").GetChild(0).GetComponent<CapsuleCollider>().isTrigger=false;
                        transform.Find("Colliders").GetChild(1).GetComponent<CapsuleCollider>().material = RegularMat;
                    }
                    
                    deckSliding = value;
                }
            }
        }

        bool colliderActive = true;
        bool rigidActive = true;
        bool deckSliding = false;

        public string preType;
        public string preAppearance;
        public int preRank;

        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject PropellerObject;
        public GameObject UndercartObject;
        public GameObject AircraftVis;
        public GameObject LoadObject;
        public Transform MyHangar;
        public Transform MyDeck;

        private PropellerBehaviour Propeller;


        public Dictionary<int, Aircraft> myGroup = new Dictionary<int, Aircraft>();

        public Aircraft myLeader;

        public GameObject GroupLine;

        public PhysicMaterial SmoothMat;
        public PhysicMaterial RegularMat;

        // ============== for aircraft mass =================
        float _fuel = 1;
        float _loadmass = 0;
        float _loadCoeff = 0.3f;

        public float Fuel
        {
            set
            {
                _fuel = Mathf.Clamp(value, 0f, 1f);
                myRigid.mass = 0.9f + _fuel * 0.1f + _loadmass *_loadCoeff;
            }
            get
            {
                return _fuel;
            }
        }
        public float LoadMass
        {
            set
            {
                _loadmass = Mathf.Clamp(value, 0f, 1f);
                myRigid.mass = 0.9f + _fuel * 0.1f + _loadmass * _loadCoeff;
            }
            get
            {
                return _loadmass;
            }
        }

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
        public float Roll
        {
            set
            {
                SetVisRoll(value);
                if (Rank.Value == 1 && TeamBase)
                {
                    SetTeamRoll(value / 4f);
                }
            }
            get
            {
                float angle = Vector3.Angle(Vector3.up, AircraftVis.transform.right);
                return -90+angle;
            }
        }
        public float CruiseHeight = 60f;

        // ================== for team up ==================
        GameObject TeamBase = null;
        public List<Transform> TeammateSpot = new List<Transform>();
        public Aircraft nextTeammate = null;
        public Aircraft preTeammate = null;
        public Transform GroupTargetSpot = null;

        // ============== for takingOff =================
        public Vector2 TakeOffDirection;
        public float TakeOffLift = 0;
        public bool deckBelow = false;
        public float deckHeight = 0;

        // ================== for cruise ==================
        public Vector2 WayPoint = new Vector2();
        public Vector2 WayDirection = Vector2.zero;
        public float WayHeight;
        public int WayPointType = 0;

        // ================== for attacking ==================
        public bool hasAttacked = false;
        public bool inAttackRoutine = false;

        IEnumerator TorpedoCoroutine()
        {
            inAttackRoutine = true;
            Thrust = 53f;
            while(Vector2.Distance(MathTool.Get2DCoordinate(transform.position), WayPoint) > 20f)
            {
                yield return new WaitForFixedUpdate();
            }
            Debug.Log("Drop Torpedo");
            SwitchToCruise();
            inAttackRoutine = false;
            yield break;
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
        public void TeamUpByLeader()
        {
            myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
            GenerateFormation();

            // team up the order
            preTeammate = null;
            List<Aircraft> groupWithoutLeader = new List<Aircraft>();
            foreach (var aircraft in myGroup)
            {
                if (aircraft.Value != this)
                {
                    groupWithoutLeader.Add(aircraft.Value);
                }
            }
            if (groupWithoutLeader.Count == 0)// no fellow
            {
                nextTeammate = null;
            }
            else
            {
                nextTeammate = groupWithoutLeader[0];
                groupWithoutLeader[0].preTeammate = this;
                for (int i = 0; i < groupWithoutLeader.Count; i++)
                {
                    if (i < groupWithoutLeader.Count - 1)
                    {
                        groupWithoutLeader[i].nextTeammate = groupWithoutLeader[i + 1];
                        groupWithoutLeader[i + 1].preTeammate = groupWithoutLeader[i];
                    }
                    groupWithoutLeader[i].GroupTargetSpot = TeammateSpot[i];

                }
            }
            

            

            // assign teammate to target


        }
        public void GenerateFormation(bool attack = false)
        {
            if (!TeamBase)
            {
                TeamBase = new GameObject("TeamBase");
                TeamBase.transform.parent = transform;
                TeamBase.transform.localPosition = Vector3.zero;
                TeamBase.transform.localEulerAngles = new Vector3(90, 0, 0);

                for (int i = 0; i < myGroup.Count - 1; i++)
                {
                    GameObject Spot = new GameObject("Spot " + i.ToString());
                    Spot.transform.parent = TeamBase.transform;
                    Spot.transform.localEulerAngles = Vector3.zero;
                    TeammateSpot.Add(Spot.transform);
                }
            }

            if (!attack)
            {
                for (int i = 0; i < TeammateSpot.Count; i++)
                {
                    TeammateSpot[i].transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * (i / 2 + 1) * 6, 0, -(i / 2 + 1) * 3);
                }
            }
            else
            {
                switch (Type.Value)
                {
                    case 0:
                        for (int i = 0; i < TeammateSpot.Count; i++)
                        {
                            TeammateSpot[i].transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * (i / 2 + 1) * 6, 0, -(i / 2 + 1) * 3);
                        }
                        break;
                    case 1:
                        for (int i = 0; i < TeammateSpot.Count; i++)
                        {
                            TeammateSpot[i].transform.localPosition = new Vector3((i % 2 == 0 ? -1 : 1) * (i / 2 + 1) * 10, 0, 0);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < TeammateSpot.Count; i++)
                        {
                            TeammateSpot[i].transform.localPosition = new Vector3(0, 0, - (i+1) * 5f);
                        }
                        break;
                    default: break;
                }
            }
            
            
        }
        public void GetPartsOnSimulateStart()
        {
            PropellerObject = transform.Find("Vis").Find("Propeller").gameObject;
            Propeller = PropellerObject.transform.GetComponent<PropellerBehaviour>();
            UndercartObject = transform.Find("Vis").Find("Undercart").gameObject;
            AircraftVis = transform.Find("Vis").gameObject;
            AddLoad();
        }
        public void AddLoad()
        {
            LoadObject = new GameObject("Load");
            LoadObject.transform.parent = transform;
            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.2f);
            LoadObject.transform.localEulerAngles = new Vector3(0, 0, -90);
            LoadObject.transform.localScale = Vector3.one * 1.5f;
            MeshFilter mf = LoadObject.AddComponent<MeshFilter>();
            MeshRenderer mr = LoadObject.AddComponent<MeshRenderer>();
            switch (Type.Selection)
            {
                case "Bomb":
                    LoadMass = 0.5f;
                    mf.sharedMesh = ModResource.GetMesh("Bomb Mesh");
                    mr.material.mainTexture = ModResource.GetTexture("Engine Texture").Texture;
                    switch (BombType.Selection)
                    {
                        case "99":
                            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.28f);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Torpedo":
                    LoadMass = 1f;
                    mf.sharedMesh = ModResource.GetMesh("Torpedo Mesh");
                    mr.material.mainTexture = ModResource.GetTexture("Torpedo Texture").Texture;
                    switch (TorpedoType.Selection)
                    {
                        case "B7A2":
                            LoadObject.transform.localPosition = new Vector3(0, 0.3f, 0.3f);
                            break;
                        case "SB2C":
                            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.25f);
                            break;
                        default:
                            break;
                    }
                    break;
                default: break;
            }
            LoadObject.transform.parent = AircraftVis.transform;
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
        public float AddMainWingForce()
        {
            Vector3 velocity_verticle = Vector3.ProjectOnPlane(myRigid.velocity, transform.right);
            float AoA = Vector3.Angle(velocity_verticle, -transform.up);
            if (Vector3.Dot(velocity_verticle, transform.forward) > 0)
            {
                AoA = -AoA;
            }
            Vector3 lift_direction = Vector3.Cross(myRigid.velocity, transform.right).normalized;
            Vector3 drag_direction = -myRigid.velocity.normalized;

            float liftForce = CalculateLift(mainWingArea, AoA, true) * (status == Status.Attacking ? 3 : 1);

            myRigid.AddForce(liftForce * lift_direction + CalculateDrag(mainWingArea, AoA) * drag_direction, ForceMode.Force);
            return liftForce;
        }
        public float AddAeroForce()
        {
            myRigid.angularDrag = Mathf.Clamp(myRigid.velocity.magnitude * 0.5f, 0.2f,150f);
            myRigid.drag = Mathf.Clamp(myRigid.velocity.magnitude * myRigid.mass * 0.01f, 0.2f, 10f) * (status == Status.Attacking ? 2 : 1);

            // horizon
            float liftForce = AddMainWingForce();

            // vertical
            Vector3 velocity = myRigid.velocity;
            velocity = transform.InverseTransformDirection(velocity);
            velocity.x *= 0.9f;
            myRigid.velocity = transform.TransformDirection(velocity);
            return liftForce;
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
        public void SetHeight(float h, bool direct = false, float force = 0.1f)
        {
            if (!direct)
            {
                Vector3 velocity = myRigid.velocity;
                velocity.y += (h - transform.position.y) * force;
                myRigid.velocity = velocity;
            }
            else
            {
                Vector3 velocity = myRigid.velocity;
                velocity.y = (h - transform.position.y) * force;
                myRigid.velocity = velocity;
            }
            
        }
        public void SetVisRoll(float roll)
        {
            AircraftVis.transform.localEulerAngles = new Vector3(90, Mathf.Clamp(roll,-60,60), 0);
        }
        public void SetTeamRoll(float roll)
        {
            TeamBase.transform.localEulerAngles = new Vector3(90, Mathf.Clamp(roll, -60, 60), 0);
        }
        public void LeaderTurnToWayPoint()
        {
            Vector2 myPos = MathTool.Get2DCoordinate(transform.position);
            float dist = Vector2.Distance(myPos, WayPoint);
            Vector2 target = WayPoint - (dist > 75 ? 0.5f * WayDirection * dist : Vector2.zero);
            Vector2 targetDir = target - MathTool.Get2DCoordinate(transform.position);
            Vector2 forward = MathTool.Get2DCoordinate(-transform.up);
            float angle = MathTool.SignedAngle(forward, targetDir);
            angle = Mathf.Sign(angle) * Mathf.Sqrt(Mathf.Abs(angle));
            myRigid.AddTorque(-Vector3.up * Mathf.Clamp(angle, -11,11) * 2f / myRigid.mass);
            SetHeight(myRigid.position.y + (WayHeight - myRigid.position.y) * 0.1f);

            float targetPitch = 0;
            if (dist > 25f)
            {
                targetPitch = 90 - (Vector3.Angle(Vector3.up, new Vector3(target.x, WayHeight, target.y) - myRigid.position));
            }
            

            Pitch = Pitch + (targetPitch - Pitch) * 0.02f;
        }
        public void SlaveFollowLeader()
        {
            if (GroupTargetSpot && myLeader)
            {
                Vector3 target = GroupTargetSpot.position;
                Vector2 targetDir = - new Vector2(myLeader.transform.up.x, myLeader.transform.up.z);
                Vector2 forward = MathTool.Get2DCoordinate(-transform.up);
                float angle = MathTool.SignedAngle(forward, targetDir);
                myRigid.AddTorque(-Vector3.up * angle * 0.5f);

                myRigid.MovePosition(transform.position + (target - transform.position).normalized * Mathf.Clamp((target - transform.position).magnitude, 0, 10f) * 0.03f);

                if (myLeader.status == Status.Cruise)
                {
                    Pitch = Pitch + (myLeader.Pitch-Pitch) * 0.02f;
                }
                else
                {
                    Pitch *= 0.98f;
                }
                
            }
        }
        public void DropLoad()
        {

        }
        public void SwitchToAttack()
        {
            if (hasAttacked)
            {
                return;
            }
            if (status == Status.Cruise)
            {
                hasAttacked = true;
                foreach (var a in myGroup)
                {
                    a.Value.status = Status.Attacking;
                }
                GenerateFormation(true);
            }
        }
        public void SwitchToCruise()
        {
            if (status == Status.TakingOff)
            {
                GenerateFormation();
                status = Status.Cruise;
                UndercartObject.SetActive(false);
                Thrust = 60f;
            }else if (status == Status.Attacking)
            {
                GenerateFormation();
                foreach (var a in myGroup)
                {
                    a.Value.status = Status.Cruise;
                    a.Value.UndercartObject.SetActive(false);
                    a.Value.Thrust = 60f;
                }
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
                WayPoint = MathTool.Get2DCoordinate(transform.position - transform.up * 300f);
                WayHeight = 60;
                WayPointType = 0;
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

            Fuel = 1;
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
        public override void OnSimulateTriggerStay(Collider collision)
        {
            if (DeckSliding)
            {
                Ray UnderRay1 = new Ray(transform.position - 0.1f * transform.up + 0.5f * transform.forward + 0.5f * transform.right, -transform.forward);
                RaycastHit hit1;
                Ray UnderRay2 = new Ray(transform.position - 0.1f * transform.up + 0.5f * transform.forward - 0.5f * transform.right, -transform.forward);
                RaycastHit hit2;
                if (Physics.Raycast(UnderRay1, out hit1, 1f))
                {
                    deckHeight = Mathf.Max(deckHeight, hit1.point.y);
                    deckBelow = true;
                }
                else
                {
                    if (Physics.Raycast(UnderRay2, out hit2, 1f))
                    {
                        deckHeight = Mathf.Max(deckHeight, hit2.point.y);
                        deckBelow = true;
                    }
                    else
                    {
                        deckBelow = false;
                    }
                }
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
                GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                Destroy(smoke, 10);
                Debug.Log("Aircraft exploded");
                status = Status.Exploded;
                myRigid.drag = 0.2f;
                myRigid.angularDrag = 0.2f;
                Thrust = 0f;
                DeckSliding = false;
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
            if (frameCount == 0)
            {
                if (Rank.Value == 1)
                {
                    TeamUpByLeader();
                }
            }
            if (ModController.Instance.state % 10 == myseed && !TriedFindHangar && frameCount > 10)
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
            else
            {
                frameCount++;
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
                    DeckSliding = false;
                    break;
                case Status.OnBoard:
                    OnBoardBehaviourFU();
                    DeckSliding = false;
                    break;
                case Status.TakingOff:
                    TakeOffLift = AddAeroForce();
                    Thrust += 0.2f;
                    myRigid.angularVelocity = Vector3.zero;
                    DeckSliding = true;

                    if (TakeOffLift < myRigid.mass * 30 && deckBelow)
                    {
                        Vector3 pos = transform.position;
                        pos.y = deckHeight - 0.05f;
                        transform.position = pos;
                        myRigid.constraints = RigidbodyConstraints.FreezePositionY;
                    }
                    else
                    {
                        myRigid.constraints = RigidbodyConstraints.None;
                    }

                    if (myRigid.velocity.magnitude > 50f)
                    {
                        Pitch = Pitch + (30 - Pitch) * 0.05f;
                    }

                    if (transform.position.y >= CruiseHeight)
                    {
                        SwitchToCruise();
                    }
                    deckBelow = false;
                    break;
                case Status.Cruise:
                    AddAeroForce();
                    DeckSliding = false;
                    
                    if (Rank.Value == 0)
                    {
                        SlaveFollowLeader();
                    }
                    else if(Rank.Value == 1)
                    {
                        LeaderTurnToWayPoint();
                        //Debug.Log((MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude);

                        float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                        if (distFromWayPoint < 200f && WayPointType != 0)
                        {
                            SwitchToAttack();
                        }
                        else if ( distFromWayPoint < 75f )
                        {
                            //Debug.Log(FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Count);
                            if (FlightDataBase.Instance.aircraftController[myPlayerID].Routes.ContainsKey(Group.Value))
                            {
                                if (FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Count > 0)
                                {
                                    Vector3 peekPos = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Position;
                                    Vector2 peekDir = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Direction;
                                    int type = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Type;
                                    if (WayPoint.x == peekPos.x && WayPoint.y == peekPos.z && FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Count > 1)
                                    {
                                        FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Dequeue();
                                        peekPos = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Position;
                                        peekDir = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Direction;
                                        type = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Type;
                                    }
                                    WayPoint = new Vector2(peekPos.x, peekPos.z);
                                    WayDirection = peekDir;
                                    WayHeight = peekPos.y;
                                    WayPointType = type;
                                }
                            }

                        }
                    }
                    
                    Roll = Roll + (myRigid.angularVelocity.y * 45-Roll) * 0.05f;
                    break;
                case Status.Attacking:
                    AddAeroForce();
                    DeckSliding = false;

                    if (Rank.Value == 0)
                    {
                        SlaveFollowLeader();
                    }
                    else if (Rank.Value == 1)
                    {
                        LeaderTurnToWayPoint();
                    }

                    if (Type.Value == 1)
                    {
                        if (Rank.Value == 1 && !inAttackRoutine)
                        {
                            StartCoroutine(TorpedoCoroutine());
                        }
                    }
                    else if (Type.Value == 2)
                    {
                        
                    }

                    Roll = Roll + (myRigid.angularVelocity.y * 45 - Roll) * 0.05f;
                    break;
                default : break;
            }

            if (Fuel > 0)
            {
                myRigid.AddForce(Thrust * (-transform.up));
                Fuel -= Thrust / 1000000f;
            }
            
        }
        public void OnGUI()
        {
            if (status == Status.Cruise && Rank.Value == 1)
            {
                //GUI.Box(new Rect(100, 300, 200, 30), WayHeight.ToString());
            }
        }



    }
}
