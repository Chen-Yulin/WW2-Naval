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
using System.Linq;
using UnityEngine.Assertions.Must;
using static ProjectileScript;

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

        public int HP = 500;
        
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
                        deckHeight = 0;
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
        public GameObject TorpedoPrefab;
        public GameObject BombPrefab;
        public Transform MyHangar;
        public Transform MyDeck;

        private PropellerBehaviour Propeller;


        public Dictionary<int, Aircraft> myGroup = new Dictionary<int, Aircraft>();

        public Aircraft myLeader;

        public GameObject GroupLine;

        public PhysicMaterial SmoothMat;
        public PhysicMaterial RegularMat;

        bool hasHitWater = false;

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
                    if (status == Status.Returning)
                    {
                        SetTeamRoll(0);
                    }
                    else
                    {
                        SetTeamRoll(value / 4f);
                    }
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
        public int myTeamIndex = 0;
        public int targetTeamCount = 0;

        // ============== for hanger ===================
        public bool hasFindBackup = false;

        // ============== for takingOff =================
        public Vector2 TakeOffDirection;
        public float TakeOffLift = 0;
        public bool deckBelow = false;
        public float deckHeight = 0;

        // ================== for cruise ==================
        public Vector2 WayPoint = new Vector2();
        public Vector2 WayDirection = Vector2.zero;
        public float WayHeight;
        public int WayPointType = 0; // 0 for 60m cruise, 1 for 21m torpedo, 2 for 270m bomb

        // ================== for attacking ==================
        public bool hasAttacked = false;
        public bool hasLoad = false;
        public bool inAttackRoutine = false;
        public float divingTime = 2.5f;

        // ================== for landing ===================
        public bool onboard = false;

        // ================== for dogfight ==================
        public Vector3 fightPosition;
        public Transform FightTarget; // remember to clear on exist dogfighting
        public GameObject MachineGun;
        public bool inTurnoverRoutine;
        public bool Shoot
        {
            set
            {
                if (MachineGun)
                {
                    MachineGun.SetActive(value);
                }
            }
        }
        
        // ================= for appearance ==================
        bool foldWing = true;
        public bool FoldWing
        {
            get
            {
                return foldWing;
            }
            set
            {
                foldWing = value;
                if (foldWing)
                {
                    transform.Find("Vis").GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh05(preAppearance);
                    if (status == Status.ShootDown || status == Status.Exploded)
                    {
                        transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.Destroyed_Tex;
                    }
                    else
                    {
                        transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex05(preAppearance);
                    }
                    
                }
                else
                {
                    transform.Find("Vis").GetComponent<MeshFilter>().sharedMesh = AircraftAssetManager.Instance.GetMesh0(preAppearance);
                    if (status == Status.ShootDown || status == Status.Exploded)
                    {
                        transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.Destroyed_Tex;
                    }
                    else
                    {
                        transform.Find("Vis").GetComponent<MeshRenderer>().material.mainTexture = AircraftAssetManager.Instance.GetTex0(preAppearance);
                    }
                }
            }
        }

        // ================= for explo ==================
        public bool inExploCoroutine = false;

        IEnumerator TorpedoCoroutine()
        {
            inAttackRoutine = true;
            Roll = 0f;
            Thrust = 53f;
            while(Vector2.Distance(MathTool.Get2DCoordinate(transform.position), WayPoint) > 10f)
            {
                yield return new WaitForFixedUpdate();
            }
            MyLogger.Instance.Log("["+ Group.Value + "](" + myTeamIndex + ") Drop Torpedo");
            foreach (var a in myGroup)
            {
                a.Value.DropLoad();
            }
            SwitchToCruise();
            inAttackRoutine = false;
            yield break;
        }
        IEnumerator BombCoroutine()
        {
            inAttackRoutine = true;
            Thrust = 20f;
            yield return new WaitForSeconds(myTeamIndex * 0.2f + UnityEngine.Random.value * 0.2f - 0.1f);
            float targetPitch = -82 + UnityEngine.Random.value * 8f;
            float targetRoll = 0;
            while(Pitch > targetPitch)
            {
                Pitch -= 0.5f;
                targetRoll += 2;
                targetRoll = Mathf.Clamp(targetRoll, 0, 180);
                Roll = targetRoll;
                yield return new WaitForFixedUpdate();
            }
            Thrust = 10f;

            float time = 0;
            targetRoll = -180;
            while (time<divingTime)
            {
                yield return new WaitForFixedUpdate();
                time += Time.fixedDeltaTime;
                targetRoll += 2;
                targetRoll = Mathf.Clamp(targetRoll, -180, 0);
                Roll = targetRoll;
            }
            
            MyLogger.Instance.Log("["+ Group.Value + "](" + myTeamIndex + ") Drop Bomb");
            DropLoad();
            SwitchToCruise();
            inAttackRoutine = false;
            yield break;
        }
        IEnumerator TurnOverCoroutine()
        {
            inTurnoverRoutine = true;
            Shoot = false;
            Vector3 horizonRight = (transform.forward.y > 0 ? 1 : -1) * Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;

            int i = 0;
            while (i < 140)
            {
                myRigid.AddTorque(-horizonRight * 10f);
                yield return new WaitForFixedUpdate();
                i++;
            }
            inTurnoverRoutine = false;
        }
        IEnumerator LandOnBoardCoroutine()
        {
            MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") land on deck successfully, transfer to hangar ...");
            onboard = true;
            yield return new WaitForSeconds(2f);
            MyLogger.Instance.Log("\tFinish transfer");
            SwitchToInHangar();
            onboard = false;
            yield break;
        }
        IEnumerator ReloadCorouting()
        {
            yield return new WaitForSeconds(2f + UnityEngine.Random.value);
            RecoverLoad();
            yield break;
        }
        IEnumerator ExploCoroutine(bool instant)
        {
            inExploCoroutine = true;
            if (!instant)
            {
                yield return new WaitForSeconds(0.5f + UnityEngine.Random.value * 2);
            }
            Explo();
            inExploCoroutine = false;
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
        void InitTorpedo()
        {
            TorpedoPrefab = new GameObject("NavalTorpedo");
            TorpedoPrefab.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            TorpedoBehaviour TBtmp = TorpedoPrefab.AddComponent<TorpedoBehaviour>();
            TBtmp.Caliber = 400f;
            TBtmp.myPlayerID = myPlayerID;
            Rigidbody RBtmp = TorpedoPrefab.AddComponent<Rigidbody>();
            RBtmp.mass = 0.2f;
            RBtmp.drag = 0.1f;
            RBtmp.useGravity = true;
            GameObject TorpedoVis = new GameObject("TorpedoVis");
            TorpedoVis.transform.SetParent(TorpedoPrefab.transform);
            TorpedoVis.transform.localPosition = Vector3.zero;
            TorpedoVis.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            TorpedoVis.transform.localScale = Vector3.one;
            MeshFilter MFtmp = TorpedoVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("Torpedo Mesh").Mesh;
            MeshRenderer MRtmp = TorpedoVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("Torpedo Texture").Texture;

            GameObject Fan1 = new GameObject("Fan1");
            Fan1.transform.SetParent(TorpedoPrefab.transform);
            Fan1.transform.localPosition = Vector3.zero;
            Fan1.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            Fan1.transform.localScale = Vector3.one;
            Fan1.AddComponent<MeshFilter>().sharedMesh = ModResource.GetMesh("TorpedoFan1 Mesh").Mesh;
            Fan1.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("TorpedoFan1 Texture").Texture;
            Fan1.AddComponent<PropellerBehaviour>().Direction = false;

            GameObject Fan2 = new GameObject("Fan2");
            Fan2.transform.SetParent(TorpedoPrefab.transform);
            Fan2.transform.localPosition = Vector3.zero;
            Fan2.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            Fan2.transform.localScale = Vector3.one;
            Fan2.AddComponent<MeshFilter>().sharedMesh = ModResource.GetMesh("TorpedoFan2 Mesh").Mesh;
            Fan2.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("TorpedoFan2 Texture").Texture;
            Fan2.AddComponent<PropellerBehaviour>().Direction = true;

            TorpedoPrefab.SetActive(false);
        }
        void InitBomb()
        {
            BombPrefab = new GameObject("BombPrefab");
            Bomb BBtmp = BombPrefab.AddComponent<Bomb>();
            BBtmp.myPlayerID = myPlayerID;
            Rigidbody RBtmp = BombPrefab.AddComponent<Rigidbody>();
            RBtmp.mass = 0.2f;
            RBtmp.drag = 0.1f;
            RBtmp.useGravity = true;

            GameObject CannonVis = new GameObject("BombVis");
            CannonVis.transform.SetParent(BombPrefab.transform);
            CannonVis.transform.localPosition = Vector3.zero;
            CannonVis.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            MeshFilter MFtmp = CannonVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("Bomb Mesh").Mesh;
            MeshRenderer MRtmp = CannonVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("Engine Texture").Texture;

            BombPrefab.SetActive(false);
        }
        GameObject InitGun()
        {
            GameObject gun = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShoot, transform);
            gun.name = "Gun";
            gun.transform.localPosition = new Vector3(0,0,0.3f);
            gun.transform.localEulerAngles = new Vector3 (90f, 0f, 0f);
            gun.SetActive(false);
            return gun;
        }
        public void UpdateAppearance(string craftName)
        {
            FoldWing = FoldWing;// refresh
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

            GenerateFormation(status == Status.Attacking);

            // team up the order
            preTeammate = null;
            List<Aircraft> groupWithoutLeader = new List<Aircraft>();
            foreach (var aircraft in myGroup)
            {
                if (aircraft.Value != this)
                {
                    aircraft.Value.myLeader = this;
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
                    groupWithoutLeader[i].myTeamIndex = i+1;

                }
            }

        }
        public void GenerateFormation(bool attack = false)
        {
            if (Rank.Value != 1)
            {
                return;
            }
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
            MachineGun = InitGun();
        }
        public void AddLoad()
        {
            LoadObject = new GameObject("Load");
            LoadObject.transform.parent = transform;
            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.2f);
            LoadObject.transform.localEulerAngles = new Vector3(5, 0, -90);
            LoadObject.transform.localScale = Vector3.one * 1.5f;
            MeshFilter mf = LoadObject.AddComponent<MeshFilter>();
            MeshRenderer mr = LoadObject.AddComponent<MeshRenderer>();
            switch (Type.Selection)
            {
                case "Bomb":
                    hasLoad = true;
                    LoadMass = 0.5f;
                    mf.sharedMesh = ModResource.GetMesh("Bomb Mesh");
                    mr.material.mainTexture = ModResource.GetTexture("Engine Texture").Texture;
                    switch (BombType.Selection)
                    {
                        case "99":
                            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.25f);
                            break;
                        case "SBD":
                            LoadObject.transform.localPosition = new Vector3(0, -0.05f, 0.2f);
                            break;
                        case "Fulmar":
                            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.25f);
                            break;
                        default:
                            break;
                    }
                    break;
                case "Torpedo":
                    hasLoad = true;
                    LoadMass = 1f;
                    mf.sharedMesh = ModResource.GetMesh("Torpedo Mesh");
                    mr.material.mainTexture = ModResource.GetTexture("Torpedo Texture").Texture;
                    switch (TorpedoType.Selection)
                    {
                        case "B7A2":
                            LoadObject.transform.localPosition = new Vector3(0, 0.2f, 0.3f);
                            break;
                        case "SB2C":
                            LoadObject.transform.localPosition = new Vector3(0, 0f, 0.25f);
                            break;
                        case "Barracuda":
                            LoadObject.transform.localPosition = new Vector3(0, 0.1f, 0.23f);
                            break;
                        default:
                            break;
                    }
                    break;
                default: break;
            }
            LoadObject.transform.parent = AircraftVis.transform;

            // init load component
            if (Type.Value == 1)
            {
                InitTorpedo();
            }else if (Type.Value == 2)
            {
                InitBomb();
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
                transform.RotateAround(transform.position, Vector3.up, -deltaAngle);

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
                FoldWing = false;
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
            return Mathf.Abs(AoA) < 25 ?
                                (-0.7f * Mathf.Atan(0.1f * Mathf.Abs(AoA) - 2) + 1) * 6f * Mathf.Sin(0.01f * (AoA + WingConst)) :
                                0.02f * AoA + WingConst;
        }
        public float CalculateCD(float AoA)
        {
            return 1f / 3000f * AoA * AoA + 0.008f;
        }
        public float AddMainWingForce(bool flap = false)
        {
            Vector3 velocity_verticle = Vector3.ProjectOnPlane(myRigid.velocity, transform.right);
            float AoA = Vector3.Angle(velocity_verticle, -transform.up);
            if (Vector3.Dot(velocity_verticle, transform.forward) > 0)
            {
                AoA = -AoA;
            }
            Vector3 lift_direction = Vector3.Cross(myRigid.velocity, transform.right).normalized;
            Vector3 drag_direction = -myRigid.velocity.normalized;

            float liftForce = CalculateLift(mainWingArea, AoA, true) * (flap ? 3 : 1);

            myRigid.AddForce(liftForce * lift_direction + CalculateDrag(mainWingArea, AoA) * drag_direction, ForceMode.Force);
            return liftForce;
        }
        public float AddAeroForce(bool flap = false)
        {
            myRigid.angularDrag = Mathf.Clamp(myRigid.velocity.magnitude * 0.5f, 0.2f,150f);
            myRigid.drag = Mathf.Clamp(myRigid.velocity.magnitude * myRigid.mass * 0.01f, 0.2f, 10f) * (flap ? 2 : 1);

            // horizon
            float liftForce = AddMainWingForce(flap);

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
            if (!inAttackRoutine)
            {
                AircraftVis.transform.localEulerAngles = new Vector3(90, Mathf.Clamp(roll, -60, 60), 0);
            }
            else
            {
                AircraftVis.transform.localEulerAngles = new Vector3(90, roll, 0);
            }
            
        }
        public void SetTeamRoll(float roll)
        {
            TeamBase.transform.localEulerAngles = new Vector3(90, Mathf.Clamp(roll, -60, 60), 0);
        }
        public void TurnToWayPoint(float turningForce = 0.5f, float HeightDeltaMax = 0.5f, bool usePitch = true, bool linearHeight = false)
        {
            Vector2 myPos = MathTool.Get2DCoordinate(transform.position);
            float dist = Vector2.Distance(myPos, WayPoint);
            Vector2 target = WayPoint - (dist > 75 ? turningForce * WayDirection * dist : Vector2.zero);
            Vector2 targetDir = target - MathTool.Get2DCoordinate(transform.position);
            Vector2 forward = MathTool.Get2DCoordinate(-transform.up);
            float angle = MathTool.SignedAngle(forward, targetDir);
            angle = Mathf.Sign(angle) * Mathf.Sqrt(Mathf.Abs(angle));
            myRigid.AddTorque(-Vector3.up * Mathf.Clamp(angle, -11,11) * 2f / myRigid.mass);
            if (linearHeight)
            {
                Vector2 vel = MathTool.Get2DCoordinate(myRigid.velocity);
                float projectedVel = Vector2.Dot(vel, WayDirection);
                float targetHeight = transform.position.y + projectedVel * Time.fixedDeltaTime / dist * (WayHeight - transform.position.y);
                SetHeight(targetHeight, true, 100f);
            }
            else
            {
                SetHeight(myRigid.position.y + Mathf.Clamp((WayHeight - myRigid.position.y) * 0.1f, -HeightDeltaMax, HeightDeltaMax), false, 1);
            }
            

            Vector3 rigidPos = myRigid.position;
            rigidPos.y = Mathf.Clamp(rigidPos.y, 21, 1000);
            myRigid.MovePosition(rigidPos);

            if (usePitch)
            {
                float targetPitch = 0;
                if (dist > 25f)
                {
                    targetPitch = 90 - (Vector3.Angle(Vector3.up, new Vector3(target.x, WayHeight, target.y) - myRigid.position));
                }


                Pitch = Pitch + Mathf.Clamp((targetPitch - Pitch) * 0.02f, -2f, 2f);
            }

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

                if (myLeader.status == Status.Cruise || myLeader.status == Status.Attacking)
                {
                    Pitch = Pitch + Mathf.Clamp((myLeader.Pitch-Pitch) * 0.2f, -1f, 1f);
                    SetHeight(myRigid.position.y + (target.y - myRigid.position.y) * 0.1f);
                }
                else if (myLeader.status != Status.Attacking)
                {
                    Pitch *= 0.98f;
                }

                Vector3 rigidTargetPosition = myRigid.position + (target - myRigid.position).normalized * Mathf.Clamp((target - transform.position).magnitude, 0, 10f) * 0.03f;
                rigidTargetPosition.y = Mathf.Clamp(rigidTargetPosition.y, 21, 1000);
                myRigid.MovePosition(rigidTargetPosition);
            }
        }
        public void DropLoad()
        {
            if (!hasLoad)
            {
                return;
            }
            if (Type.Value == 1)
            {
                LoadMass = 0;
                hasLoad = false;
                GameObject Torpedo = (GameObject)Instantiate(   TorpedoPrefab, LoadObject.transform.position, Quaternion.identity, 
                                                                BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                Torpedo.transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
                Torpedo.name = "Torpedo" + myPlayerID.ToString();
                Torpedo.SetActive(true);
                TorpedoBehaviour TB = Torpedo.GetComponent<TorpedoBehaviour>();
                TB.fire = true;
                TB.mode = 2;
                TB.parentGuid = myGuid;
                TB.depth = 0.5f;
                Destroy(Torpedo, 25f);

                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Torpedo.transform.eulerAngles));
                }
                LoadObject.SetActive(false);
            }
            else if (Type.Value == 2)
            {
                LoadMass = 0;
                hasLoad = false;
                GameObject Bomb = (GameObject)Instantiate(BombPrefab, LoadObject.transform.position, Quaternion.identity,
                                                                BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                Bomb.transform.rotation = Quaternion.LookRotation(LoadObject.transform.forward, LoadObject.transform.up);
                Bomb.transform.localScale = Vector3.one * 4;
                Bomb.GetComponent<Rigidbody>().velocity = myRigid.velocity;
                Bomb.GetComponent<Bomb>().randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f);
                Bomb.GetComponent<Bomb>().parent = gameObject;
                Bomb.name = "Bomb" + myPlayerID.ToString();


                Bomb.SetActive(true);

                LoadObject.SetActive(false);
            }
        }
        public void RecoverLoad()
        {
            if (Type.Value == 1 || Type.Value == 2)
            {
                hasLoad = true;
                LoadObject.SetActive(true);
                switch (Type.Selection)
                {
                    case "Bomb":
                        LoadMass = 0.5f;
                        break;
                    case "Torpedo":
                        LoadMass = 1f;
                        break;
                    default: break;
                }
            }
        }
        public void RemoveFromGroup()
        {
            if (Rank.Value == 0)
            {
                if (Grouper.Instance.AircraftGroups[myPlayerID].ContainsKey(Group.Value))
                {
                    Grouper.Instance.AircraftGroups[myPlayerID][Group.Value].Remove(myGuid);
                    if (myLeader)
                    {
                        myLeader.TeamUpByLeader();
                    }
                }
            }else if (Rank.Value == 1)
            {
                if (myGroup != null)
                {
                    foreach (var a in myGroup)
                    {
                        if (a.Value && a.Value != this)
                        {
                            a.Value.status = Status.Deprecated;
                        }
                    }
                }
                Grouper.Instance.AircraftGroups[myPlayerID].Remove(Group.Value);
                Grouper.Instance.AircraftLeaders[myPlayerID].Remove(Group.Value);
                if (FlightDataBase.Instance.aircraftController[myPlayerID].CurrentLeader == this)
                {
                    FlightDataBase.Instance.aircraftController[myPlayerID].CurrentLeader = null;
                }
                
            }
        }
        public void GetReturningWayPoint()
        {
            if (Rank.Value == 1)
            {
                FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
                Vector2 targetPoint = deck.Center + deck.Forward * (-deck.Length * 0.25f - 230f);
                WayDirection = Vector2.zero;
                WayPoint = targetPoint;
                WayPointType = 0;
                WayHeight = 35f;
            }
        }
        public void GetLandingWayPoint()
        {
            FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
            Vector2 targetPoint = deck.Center + deck.Forward * (-deck.Length * 0.25f);
            WayDirection = deck.Forward;
            WayPoint = targetPoint;
            WayPointType = 0;
            WayHeight = deck.height + 0.2f;
        }
        public Aircraft AlertOnCruise()
        {
            Aircraft a;
            foreach (var cv in Grouper.Instance.AircraftLeaders)
            {
                foreach (var leader in cv)
                {
                    if (leader.Value.Value == this)
                    {
                        continue;
                    }
                    a = leader.Value.Value;
                    if ((a.status == Status.Cruise || a.status == Status.Attacking || a.status == Status.Returning || a.status == Status.DogFighting)
                        && (MathTool.Get2DCoordinate(a.transform.position) - MathTool.Get2DCoordinate(transform.position)).magnitude < 130f )
                    {
                        return a;
                    }
                }
            }
            return null;
        }
        public void ReduceHP(int value = 1)
        {
            if (nextTeammate && (nextTeammate.status == Status.Cruise || nextTeammate.status == Status.DogFighting || nextTeammate.status == Status.Attacking))
            {
                nextTeammate.ReduceHP(value);
            }
            else
            {
                HP -= value;
                if (HP < 0 && status != Status.ShootDown)
                {
                    SwitchToShootDown();
                }
            }
        }
        public void BeginExplo(bool instant)
        {
            if (status == Status.Exploded)
            {
                return;
            }
            if (!inExploCoroutine)
            {
                StartCoroutine(ExploCoroutine(instant));
            }
        }
        public void SwitchToShootDown()
        {
            if (status != Status.ShootDown)
            {
                ColliderActive = true;
                status = Status.ShootDown;
                FoldWing = FoldWing;
                GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                Destroy(smoke, 10);
                myRigid.drag = 0.2f;
                myRigid.angularDrag = 0.2f;
                myRigid.useGravity = true;
                Thrust = 0f;
                DeckSliding = false;
                Shoot = false;
                try
                {
                    RemoveFromGroup();
                }
                catch { }
                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") is shot down");
            }
        }
        public void SwitchToDogFighting(Aircraft a)
        {
            Roll = 0;
            ColliderActive = false;
            if (Rank.Value == 1)
            {
                if (a.status == Status.DogFighting && myGroup.ContainsValue(a.FightTarget.gameObject.GetComponent<Aircraft>()))
                {
                    Vector3 f_pos = (transform.position + a.transform.position) / 2f;
                    f_pos.y = 60f;
                    fightPosition = f_pos;
                    a.fightPosition = f_pos;
                }
                else
                {
                    fightPosition = Vector3.zero;
                }
                bool allInCruise = true;
                foreach (var member in myGroup)
                {
                    if (member.Value.status != Status.Cruise)
                    {
                        allInCruise = false;
                        break;
                    }
                }
                if (!allInCruise)
                {
                    return;
                }

                int myGroupCount = myGroup.Count;
                int targetGroupCount = a.myGroup.Count;
                int lastTargetNum = 1;
                if (myGroupCount > targetGroupCount)
                {
                    lastTargetNum = myGroupCount - targetGroupCount + 1;
                }

                Stack<Aircraft> targetTeam = new Stack<Aircraft>(a.myGroup.Values);

                int i = 1;

                foreach (var member in myGroup.Reverse())
                {
                    if (i < lastTargetNum)
                    {
                        member.Value.FightTarget = targetTeam.Peek().transform;
                    }
                    else
                    {
                        member.Value.FightTarget = targetTeam.Pop().transform;
                    }
                    member.Value.status = Status.DogFighting;
                    i++;
                }
                if (fightPosition != Vector3.zero)
                {
                    MyLogger.Instance.Log("[" + Group.Value + "] is dogfighting with [" + a.Group.Value + "] at " + fightPosition.ToString());
                }
                else
                {
                    MyLogger.Instance.Log("[" + Group.Value + "] is dogfighting with [" + a.Group.Value + "]");
                }
                
            }
        }
        public void SwitchToLanding()
        {
            if (status == Status.Returning)
            {
                status = Status.Landing;
                FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
                Vector2 targetPoint = deck.Center + deck.Forward * (-deck.Length * 0.25f);
                WayDirection = deck.Forward;
                WayPoint = targetPoint;
                WayPointType = 0;
                WayHeight = deck.height + 0.3f;
                Thrust = 23f;
                UndercartObject.SetActive(true);
            }
        }
        public void SwitchToReturn()
        {
            if (status == Status.Cruise)
            {
                status = Status.Returning;
                foreach (var a in myGroup)
                {
                    if (a.Value.status == Status.Cruise)
                    {
                        a.Value.status = Status.Returning;
                        a.Value.Thrust = 50f;
                    }
                }
            }
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
                if (Rank.Value == 1)
                {
                    GenerateFormation();
                }
                status = Status.Cruise;
                UndercartObject.SetActive(false);
                Thrust = 60f;
            }else if (status == Status.Attacking)
            {
                if (Type.Value == 1)
                {
                    GenerateFormation();
                    foreach (var a in myGroup)
                    {
                        a.Value.status = Status.Cruise;
                        a.Value.UndercartObject.SetActive(false);
                        a.Value.Thrust = 60f;
                    }
                }else if (Type.Value == 2)
                {
                    GenerateFormation();
                    status = Status.Cruise;
                    UndercartObject.SetActive(false);
                    Thrust = 60f;
                }
            }else if (status == Status.DogFighting)
            {
                ColliderActive = true;
                Shoot = false;
                if (Rank.Value == 1)
                {
                    GenerateFormation();
                }
                status = Status.Cruise;
                UndercartObject.SetActive(false);
                Thrust = 60f;
            }
        }
        public void SwitchToTakingOff()
        {
            if (status == Status.OnBoard)
            {
                status = Status.TakingOff;
                MyDeck = null;
                deckHeight = 0;
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
                FoldWing = true;
            }
            else if (status == Status.InHangar)
            {
            }
            else if (status == Status.Landing)
            {
                hasAttacked = false;
                hasFindBackup = false;
                MyDeck = null;
                status = Status.InHangar;
                SettleSpot(MyHangar, true);
                Propeller.enabled = false;
                Thrust = 0;
                FoldWing = true;
                if (!hasLoad)
                {
                    StartCoroutine(ReloadCorouting());
                }
            }
        }
        public void InHangarBehaviourFU()
        {
            SettleSpot(MyHangar,false);
            if (Fuel < 1)
            {
                Fuel = Mathf.Clamp(Fuel + 0.0004f, 0, 1);
            }
            if (HP < 500 && myseed == ModController.Instance.state % 10)
            {
                HP = Mathf.Clamp(HP + 1, 0, 500);
            }

            if (Rank.Value == 1 && !hasFindBackup)
            {
                hasFindBackup=true;
                if (targetTeamCount > myGroup.Count)
                {
                    int vacancy = targetTeamCount - myGroup.Count;
                    int addNum = 0;
                    if (Grouper.Instance.AircraftGroups[myPlayerID].ContainsKey("backup"))
                    {
                        Stack<Aircraft> backup = new Stack<Aircraft>();

                        foreach (var a in Grouper.Instance.AircraftGroups[myPlayerID]["backup"])
                        {
                            if (a.Value.Type.Value == Type.Value && a.Value.status == Status.InHangar)
                            {
                                addNum++;
                                backup.Push(a.Value);
                            }
                            if (addNum == vacancy)
                            {
                                break;
                            }
                        }

                        foreach (var a in backup)
                        {
                            a.Rank.SetValue(0);
                            a.Group.SetValue(Group.Value);
                            Grouper.Instance.AddAircraft(myPlayerID, Group.Value, a.myGuid, a);
                        }

                        TeamUpByLeader();
                        MyLogger.Instance.Log("[" + Group.Value + "] replenish " + addNum.ToString() + "/" + vacancy.ToString());
                    }
                    else
                    {
                        MyLogger.Instance.Log("No backup queue");
                    }
                        
                }
            }
            
        }
        public void OnBoardBehaviourFU()
        {
            SettleSpot(MyDeck,false);
        }

        public void Explo()
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftExplo, transform.position, Quaternion.identity);
            Destroy(explo, 5);
            GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
            Destroy(smoke, 10);
            MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") exploded!");
            status = Status.Exploded;
            FoldWing = FoldWing;
            myRigid.drag = 0.2f;
            myRigid.angularDrag = 0.2f;
            myRigid.useGravity = true;
            Thrust = 0f;
            DeckSliding = false;
            try
            {
                RemoveFromGroup();
            }
            catch { }

            if (hasLoad)
            {
                float myPossi = 0.9f;
                explo.transform.localScale = Vector3.one;
                Collider[] ExploCol = Physics.OverlapSphere(transform.position, 2);
                foreach (Collider hitedCollider in ExploCol)
                {
                    try
                    {
                        Aircraft a = hitedCollider.attachedRigidbody.GetComponent<Aircraft>();
                        if (a)
                        {
                            float hittedPossi = a.hasLoad ? 0.9f : 1.1f;
                            if (a.status == Status.InHangar || a.status == Status.OnBoard && UnityEngine.Random.value > myPossi * hittedPossi)
                            {
                                a.BeginExplo(false);
                            }
                        }
                    
                        hitedCollider.attachedRigidbody.AddExplosionForce(a?100:8000, transform.position, 2);
                    }
                    catch { }
                }
            }
            else
            {
                float myPossi = 1.1f;
                explo.transform.localScale = Vector3.one * 0.5f;
                Collider[] ExploCol = Physics.OverlapSphere(transform.position, 1.5f);
                foreach (Collider hitedCollider in ExploCol)
                {

                    try
                    {
                        Aircraft a = hitedCollider.attachedRigidbody.GetComponent<Aircraft>();
                        if (a)
                        {
                            float hittedPossi = a.hasLoad ? 0.9f : 1.1f;
                            if (a.status == Status.InHangar || a.status == Status.OnBoard && UnityEngine.Random.value > myPossi * hittedPossi)
                            {
                                a.BeginExplo(false);
                            }
                        }
                        hitedCollider.attachedRigidbody.AddExplosionForce(a ? 50 : 4000, transform.position, 1.5f);
                    }
                    catch { }
                }
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
                "B7A2",
                "Barracuda"
            });
            BombType = AddMenu("BombType", 0, new List<string>
            {
                "SBD",
                "99",
                "Fulmar"
            });
            FighterType = AddMenu("FighterType", 0, new List<string>
            {
                "Zero",
                "F4U",
                "Spitfire"
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
                Grouper.Instance.AddAircraft(myPlayerID, Rank.Value == 2? "backup" : Group.Value, BlockBehaviour.Guid.GetHashCode(), this);
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
            Grouper.Instance.AddAircraft(myPlayerID, Rank.Value == 2 ? "backup" : Group.Value, myGuid, this);
            if (Rank.Value == 1)
            {
                myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                myLeader = null;
            }
            else
            {
                myGroup = new Dictionary<int, Aircraft>();
                myLeader = Grouper.Instance.GetLeader(myPlayerID, Rank.Value == 2 ? "backup" : Group.Value);
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
        public override void OnSimulateStop()
        {
            BlockBehaviour.BuildingBlock.GetComponent<Aircraft>().UpdateAppearance(preAppearance);
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
                if (Physics.Raycast(UnderRay1, out hit1, 0.55f))
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
            if (status == Status.Exploded || frameCount < 100)
            {
                return;
            }
            float collisionForce = collision.impulse.magnitude / Time.fixedDeltaTime;
            try
            {
                bool case1 = collisionForce > 2000f && status != Status.InHangar;
                bool case2 = (status == Status.Landing && collision.rigidbody.name == "Aircraft");
                bool case3 = true;
                try
                {
                    Aircraft a = collision.rigidbody.gameObject.GetComponent<Aircraft>();
                    if (a)
                    {
                        case3 = (a.myPlayerID != myPlayerID);
                    }
                }
                catch { }
                
                
                if ((case1 && case3) || case2)
                {
                    BeginExplo(true);
                }
            }
            catch { }
            
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
                    if ((status != Status.Exploded && status != Status.ShootDown && status != Status.Deprecated) || !BlockBehaviour.isSimulating)
                    {
                        myGroup = new Dictionary<int, Aircraft>();
                        myLeader = Grouper.Instance.GetLeader(myPlayerID, Group.Value);
                    }
                    else
                    {
                        myGroup = null;
                        myLeader = null;
                    }
                    
                    break;
                case 1:
                    myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                    myLeader = null;
                    break;
                case 2:
                    myGroup = new Dictionary<int, Aircraft>();
                    myLeader = Grouper.Instance.GetLeader(myPlayerID, "backup");
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
                    if (SwitchActive.IsPressed && status != Status.Exploded && status != Status.Deprecated && status != Status.ShootDown)
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
                    targetTeamCount = Grouper.Instance.GetAircraft(myPlayerID, Group.Value).Count();
                    TeamUpByLeader();
                }
            }
            if (ModController.Instance.state % 10 == myseed && !TriedFindHangar && frameCount > 20)
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
                    {
                        InHangarBehaviourFU();
                        DeckSliding = false;
                        break;
                    }
                case Status.OnBoard:
                    {
                        OnBoardBehaviourFU();
                        DeckSliding = false;
                        break;
                    }
                case Status.TakingOff:
                    {
                        TakeOffLift = AddAeroForce();
                        Thrust += 0.2f;
                        myRigid.angularVelocity = Vector3.zero;
                        DeckSliding = true;

                        if (TakeOffLift < myRigid.mass * 32f && deckBelow)
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
                    }
                case Status.Cruise:
                    {
                        AddAeroForce();
                        DeckSliding = false;

                        if (Rank.Value == 0)
                        {
                            SlaveFollowLeader();
                        }
                        else if (Rank.Value == 1)
                        {
                            if (Type.Value == 0 || hasAttacked)
                            {
                                Aircraft targetLeader = AlertOnCruise();
                                if (targetLeader)
                                {
                                    SwitchToDogFighting(targetLeader);
                                }
                            }


                            TurnToWayPoint();

                            float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                            if (distFromWayPoint < 200f && WayPointType != 0 && !hasAttacked)
                            {
                                SwitchToAttack();
                            }
                            else if (distFromWayPoint < 75f)
                            {
                                if (FlightDataBase.Instance.aircraftController[myPlayerID].Routes.ContainsKey(Group.Value))
                                {
                                    if (FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Count > 0)
                                    {
                                        Vector3 peekPos = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Position;
                                        Vector2 peekDir = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Direction;
                                        int type = FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Peek().Type;
                                        if (WayPoint.x == peekPos.x && WayPoint.y == peekPos.z && FlightDataBase.Instance.aircraftController[myPlayerID].Routes[Group.Value].Count > 1)
                                        {
                                            FlightDataBase.Instance.aircraftController[myPlayerID].DequeueRoutePoint(Group.Value);
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

                        Roll = Roll + (myRigid.angularVelocity.y * 45 - Roll) * 0.05f;
                        break;
                    }
                case Status.Attacking:
                    {
                        AddAeroForce(true);
                        DeckSliding = false;

                        if (Type.Value == 1 || (Type.Value == 2 && !inAttackRoutine))
                        {
                            if (Rank.Value == 0)
                            {
                                SlaveFollowLeader();
                            }
                            else if (Rank.Value == 1)
                            {
                                TurnToWayPoint();
                            }
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
                            float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                            if (!inAttackRoutine && distFromWayPoint < 95f && Rank.Value == 1)
                            {
                                foreach (var a in myGroup)
                                {
                                    a.Value.StartCoroutine(a.Value.BombCoroutine());
                                }
                            }
                        }

                        if (!inAttackRoutine)
                        {
                            Roll = Roll + (myRigid.angularVelocity.y * 45 - Roll) * 0.05f;
                        }

                        break;
                    }
                case Status.Returning:
                    {
                        AddAeroForce();
                        DeckSliding = false;
                        if (Rank.Value == 0)
                        {
                            SlaveFollowLeader();
                        }
                        else if (Rank.Value == 1)
                        {
                            GetReturningWayPoint();
                            TurnToWayPoint();

                            float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                            if (distFromWayPoint < 75f)
                            {
                                foreach (var a in myGroup.Reverse())
                                {
                                    if (a.Value.status == Status.InHangar || a.Value.status == Status.Exploded)
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        a.Value.SwitchToLanding();
                                        break;
                                    }
                                }
                            }
                        }
                        Roll = Roll + (myRigid.angularVelocity.y * 45 - Roll) * 0.05f;
                        break;
                    }
                case Status.Landing:
                    {
                        DeckSliding = false;
                        GetLandingWayPoint();
                        Vector2 VectFromWayPoint = WayPoint - MathTool.Get2DCoordinate(transform.position);
                        Vector2 forward = MathTool.Get2DCoordinate(-transform.up);
                        float dist = Vector2.Dot(WayDirection, VectFromWayPoint);

                        if (!onboard)
                        {
                            if (dist < 2f && dist > -5f && Vector2.Dot(forward, WayDirection) > 0)
                            {
                                AddAeroForce(false);
                                myRigid.useGravity = true;
                                Thrust = 0f;
                                Roll = 0;
                                StartCoroutine(LandOnBoardCoroutine());
                            }
                            else
                            {
                                myRigid.angularDrag = Mathf.Clamp(myRigid.velocity.magnitude * 0.5f, 0.2f, 150f);
                                myRigid.drag = Mathf.Clamp(myRigid.velocity.magnitude * myRigid.mass * 0.01f, 0.2f, 10f) * 2;

                                // vertical
                                Vector3 velocity = myRigid.velocity;
                                velocity = transform.InverseTransformDirection(velocity);
                                velocity.x *= 0.9f;
                                myRigid.velocity = transform.TransformDirection(velocity);

                                myRigid.useGravity = false;
                                Pitch = 3f;
                                TurnToWayPoint(0.6f, 0.1f, false, true);
                                Roll = Roll + (myRigid.angularVelocity.y * 45 - Roll) * 0.05f;
                            }
                        }

                        Pitch = Mathf.Clamp(Pitch, -10, 30);

                        break;
                    }
                case Status.DogFighting:
                    {
                        ColliderActive = false;
                        Aircraft targetAircraft = FightTarget.GetComponent<Aircraft>();
                        if (targetAircraft.status != Status.Cruise && targetAircraft.status != Status.Attacking && targetAircraft.status != Status.DogFighting)
                        {
                            if (targetAircraft.preTeammate)
                            {
                                FightTarget = targetAircraft.preTeammate.transform;
                                targetAircraft = targetAircraft.preTeammate;
                            }
                            else
                            {
                                SwitchToCruise();
                                break;
                            }
                        }

                        Thrust = 60f;
                        DeckSliding = false;
                        myRigid.angularDrag = 10f;
                        myRigid.drag = 0.6f;
                        float dist = (transform.position - FightTarget.position).magnitude;
                        if (dist < 15f)
                        {
                            if (!inTurnoverRoutine)
                            {
                                StartCoroutine(TurnOverCoroutine());
                            }
                            
                        }
                        else
                        {
                            Vector3 targetDirection = FightTarget.position - transform.position;
                            float AngleDiff = Vector3.Angle(-transform.up, targetDirection);
                            Vector3 torque = Vector3.Cross(-transform.up, targetDirection).normalized * Mathf.Clamp(Mathf.Sqrt(AngleDiff) * 2f, -7, 7);
                            Vector3 StableTorque = (transform.forward.y > 0? 1 : -1) * Vector3.Cross(transform.right, Vector3.up) * 2f;
                            myRigid.AddTorque((torque + StableTorque) * (!inTurnoverRoutine?1:0.3f) );

                            bool canShoot = AngleDiff < 3;
                            Shoot = canShoot;
                            if (canShoot)
                            {
                                targetAircraft.ReduceHP(1);
                            }
                        }
                        Vector3 v_angularVel = myRigid.angularVelocity;
                        Vector3 RollTorque = Vector3.Cross(transform.right, -v_angularVel.normalized) * 5;
                        myRigid.AddTorque(RollTorque);


                        Vector3 velocity = myRigid.velocity;
                        velocity = transform.InverseTransformDirection(velocity);
                        velocity.x *= 0.9f;
                        velocity.z *= 0.9f;
                        myRigid.velocity = transform.TransformDirection(velocity);

                        Vector3 rigidTargetPosition = myRigid.position;
                        rigidTargetPosition.y = Mathf.Clamp(rigidTargetPosition.y, 21, 1000);
                        myRigid.MovePosition(rigidTargetPosition);

                        // restrict fight position
                        if (fightPosition != Vector3.zero)
                        {
                            myRigid.AddForce((fightPosition - transform.position) * 1f);
                        }

                        break;
                    }
                default : break;
            }

            // fuel calculation
            if (Fuel > 0)
            {
                myRigid.AddForce(Thrust * (-transform.up));
                Fuel -= Thrust / 3000000f;
            }

            // water hit
            if (!hasHitWater && ModController.Instance.showSea && status != Status.OnBoard && status != Status.InHangar)
            {
                if (transform.position.y<20f && transform.position.y > 18f && myRigid.velocity.y < 2f)
                {
                    hasHitWater = true;
                    try
                    {
                        RemoveFromGroup();
                    }
                    catch { }
                    
                    status = Status.Deprecated;
                    MyLogger.Instance.Log("["+ Group.Value + "](" + myTeamIndex + ") drop sea!");

                    GameObject waterhit;
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = 250f/381f * Vector3.one;
                    Destroy(waterhit, 3);
                    ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), 250f));
                }
            }


        }
        public void OnGUI()
        {
            if (Rank.Value == 1)
            {
                //GUI.Box(new Rect(100, 300, 200, 30), (Grouper.Instance.GetLeader(myPlayerID, Group.Value) == this).ToString());
            }
        }
    }
}
