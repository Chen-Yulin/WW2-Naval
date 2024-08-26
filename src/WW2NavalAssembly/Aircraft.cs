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
using UnityEngine.UI;
using Modding.Common;

namespace WW2NavalAssembly
{
    public class AircraftMsgReceiver : SingleInstance<AircraftMsgReceiver>
    {
        public override string Name { get; } = "Aircraft Msg Receiver";

        public static MessageType ChangeStatusMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Integer);// playerid, guid, status
        public static MessageType RemovedMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType ExploMsg = ModNetworking.CreateMessageType(DataType.Vector3);
        public static MessageType ShootDownMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType GunShootMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean);
        public static MessageType AddBackupMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.String, DataType.Integer);
        public static MessageType LoadMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean); // 0 for drop( deprecated, implemented by weaponMsg), 1 for recover
        public static MessageType FuelMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single);
        public static MessageType VelocityMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3);
        public static MessageType NeedVelocityMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean);

        public Dictionary<int, Aircraft.Status>[] ChangedStatus = new Dictionary<int, Aircraft.Status>[16];
        public Dictionary<int, bool>[] GunShoot = new Dictionary<int, bool>[16];
        public Dictionary<int, bool>[] LoadChange = new Dictionary<int, bool>[16];
        public List<int>[] Removed = new List<int>[16];
        public List<int>[] ShootDown = new List<int>[16];
        public Dictionary<string, List<int>>[] Backup = new Dictionary<string, List<int>>[16];
        public Dictionary<int, float>[] Fuel = new Dictionary<int, float>[16];
        public Dictionary<int, Vector3>[] Velocity = new Dictionary<int, Vector3>[16];

        public Dictionary<int, List<int>>[] ClientNeedVelocity = new Dictionary<int, List<int>>[16];

        public AircraftMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                ChangedStatus[i] = new Dictionary<int, Aircraft.Status>();
                GunShoot[i] = new Dictionary<int, bool>();
                LoadChange[i] = new Dictionary<int, bool>();
                Removed[i] = new List<int>();
                ShootDown[i] = new List<int>();
                Backup[i] = new Dictionary<string, List<int>>();
                Fuel[i] = new Dictionary<int, float>();
                Velocity[i] = new Dictionary<int, Vector3>();
                ClientNeedVelocity[i] = new Dictionary<int, List<int>>();
            }
        }

        public void StatusMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Aircraft.Status status = (Aircraft.Status)msg.GetData(2);
            if (ChangedStatus[playerid].ContainsKey(guid))
            {
                ChangedStatus[playerid][guid] = status;
            }
            else
            {
                ChangedStatus[playerid].Add(guid, status);
            }
        }
        public void GunShootMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            bool shoot = (bool)msg.GetData(2);
            if (GunShoot[playerid].ContainsKey(guid))
            {
                GunShoot[playerid][guid] = shoot;
            }
            else
            {
                GunShoot[playerid].Add(guid, shoot);
            }
        }
        public void LoadMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            bool load = (bool)msg.GetData(2);
            if (LoadChange[playerid].ContainsKey(guid))
            {
                LoadChange[playerid][guid] = load;
            }
            else
            {
                LoadChange[playerid].Add(guid, load);
            }
        }
        public void RemovedMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Removed[playerid].Add(guid);
        }
        public void ExploMsgReceiver(Message msg)
        {
            Vector3 pos = (Vector3)msg.GetData(0);
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftExplo, pos, Quaternion.identity);
            Destroy(explo, 5);
        }
        public void ShootDownMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            ShootDown[playerid].Add(guid);
        }
        public void BackupMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            string group = (string)msg.GetData(1);
            int guid = (int)msg.GetData(2);
            if (Backup[playerid].ContainsKey(group))
            {
                Backup[playerid][group].Add(guid);
            }
            else
            {
                Backup[playerid].Add(group, new List<int> { guid });
            }
        }
        public void FuelMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            float fuel = (float)msg.GetData(2);
            if (Fuel[playerid].ContainsKey(guid))
            {
                Fuel[playerid][guid] = fuel;
            }
            else
            {
                Fuel[playerid].Add(guid, fuel);
            }
        }
        public void VelocityMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Vector3 velocity = (Vector3)msg.GetData(2);
            if (Velocity[playerid].ContainsKey(guid))
            {
                Velocity[playerid][guid] = velocity;
            }
            else
            {
                Velocity[playerid].Add(guid, velocity);
            }
            //Debug.Log(PlayerData.localPlayer.networkId + " receive velocity: " + velocity);
        }
        public void ClientNeedVelocityMsgReceiver(Message msg)
        {
            int client = msg.Sender.NetworkId;
            if (client == 0)
            {
                return;
            }
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            bool need = (bool)msg.GetData(2);
            if (need)
            {
                if (ClientNeedVelocity[playerid].ContainsKey(guid))
                {
                    ClientNeedVelocity[playerid][guid].Add(client);
                }
                else
                {
                    ClientNeedVelocity[playerid].Add(guid, new List<int> { client });
                }
            }
            else
            {
                if (ClientNeedVelocity[playerid].ContainsKey(guid))
                {
                    ClientNeedVelocity[playerid][guid].Remove(client);
                }
            }
            //Debug.Log("Receive client " + client + " " + need);
            
        }
    }

    public class Aircraft : BlockScript
    {
        public enum Status
        {
            Deprecated,
            InHangar,
            OnLifter,
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
        public MKey FPVKey;

        private Status _status = Status.Deprecated;
        public Status status
        {
            get { return _status; }
            set {
                if (value != _status)
                {
                    _status = value;
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(AircraftMsgReceiver.ChangeStatusMsg.CreateMessage(myPlayerID, myGuid, (int)_status));
                    }
                }
            }
        }

        public int myseed;
        public int myLongerSeed;
        public int myPlayerID;
        public int myGuid;

        public int frameCount = 0;

        public Rigidbody myRigid;

        int MaxHP = 800;
        public int HP = 800;
        
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

        public int preType;
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

        Vector3 LastFoward;
        float DeltaTurning;
        public float TurningRate
        {
            get
            {
                return DeltaTurning / Time.fixedDeltaTime;
            }
        }

        public Vector3[] lastClientVelocity = new Vector3[16];
        public Vector3 myVelocity
        {
            get
            {
                if (StatMaster.isClient)
                {
                    Vector3 velocity = Vector3.zero;
                    try
                    {
                        velocity = AircraftMsgReceiver.Instance.Velocity[myPlayerID][myGuid];
                        //velocity = localVelocity;
                    }
                    catch
                    {
                        velocity = localVelocity;
                    }
                    return velocity;
                }
                else
                {
                    return myRigid.velocity;
                }
            }
        }
        public bool isSelf
        {
            get
            {
                return StatMaster.isMP ? myPlayerID == PlayerData.localPlayer.networkId : true;
            }
        }

        // ============== for aircraft mass =================
        float _fuel = 1;
        float _loadmass = 0;
        float _loadCoeff = 0.3f;

        public float Fuel
        {
            set
            {
                _fuel = Mathf.Clamp(value, 0f, 1f);
                if (myRigid)
                {
                    myRigid.mass = 0.9f + _fuel * 0.1f + _loadmass * _loadCoeff;
                }
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
        public float CruiseHeight = Constants.CruiseHeight + Constants.SeaHeight;
        public float PropellerSpeed
        {
            set
            {
                if (Propeller)
                {
                    if (value <= 0.1f)
                    {
                        Propeller.enabled = false;
                    }
                    else
                    {
                        Propeller.enabled = true;
                        Propeller.Speed = new Vector3(0, 0, value);
                    }
                }
                
            }
        }
        public bool isFlying
        {
            get
            {
                return (status == Status.Cruise || status == Status.Attacking || status == Status.DogFighting || status == Status.Returning);
            }
        }

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
        public int WayPointType = 0; // 0 for cruise, 1 for torpedo, 2 for bomb

        // ================== for attacking ==================
        public bool hasAttacked = false;
        public bool hasLoad = false;
        public bool inAttackRoutine = false;

        public float DiveAnxiety = 0;

        // ================== for returning ==================
        public bool waitQueueing = true;

        // ================== for landing ===================
        public bool onboard = false;
        public int landingTime = 0;

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
                    if (MachineGun.activeSelf != value)
                    {
                        if (StatMaster.isMP && !StatMaster.isClient)
                        {
                            ModNetworking.SendToAll(AircraftMsgReceiver.GunShootMsg.CreateMessage(myPlayerID, myGuid, value));
                        }
                        
                        MachineGun.SetActive(value);
                    }
                }
            }
        }

        // ================== for lifter ==================
        public Transform MyLifter;

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
                if (foldWing != value)
                {
                    foldWing = value;
                }
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

        // ================= for local velocity ===============
        public Vector3 previousLocalPos = Vector3.zero;
        public Vector3 localVelocity = Vector3.zero;


        IEnumerator TorpedoCoroutine()
        {
            inAttackRoutine = true;
            Roll = 0f;
            Thrust = 53f;

            // find target for torpedo
            int target_PlayerID = -1;
            List<Engine> target_engines = new List<Engine>();
            float MaxDist = 200f;
            float MaxWidth = 150f;
            foreach (var ship in FlightDataBase.Instance.engines)
            {
                foreach (var engine in ship)
                {
                    Vector2 enginePos = MathTool.Get2DCoordinate(engine.transform.position);
                    Vector2 dir = enginePos - WayPoint;
                    float dist = Vector2.Dot(dir, WayDirection.normalized);
                    float width = Mathf.Sqrt(dir.magnitude* dir.magnitude - dist*dist);
                    float minDist = MaxDist;
                    if (dist < minDist && dist > 0 && width < MaxWidth)
                    {
                        minDist = dist;
                        target_PlayerID = engine.myPlayerID;
                        target_engines.Clear();
                        target_engines.Add(engine);
                    }
                }
            }
            if (target_PlayerID != -1) // target found
            {
                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") Find target", myPlayerID);
                Vector3 target_vel = Vector3.zero;
                Vector3 target_pos = Vector3.zero;
                float drop_dist = 80f;
                Vector3 predict_pos = target_pos;
                Vector3 pre_predict_pos = predict_pos;
                while (MathTool.Get2DDistance(transform.position, predict_pos) > drop_dist || predict_pos == Vector3.zero)
                {
                    
                    foreach (var engine in target_engines)
                    {
                        target_pos = engine.transform.position;
                        target_vel = engine.Rigidbody.velocity;
                    }
                    target_pos.y = Constants.SeaHeight + 1f;
                    target_vel.y = 0;

                    //MyLogger.Instance.Log(target_pos.ToString(), myPlayerID);

                    float tor_time = drop_dist/10f;// drop at a dist of 100m, vel of torpedo 8.3m/s

                    Vector3 preDirection = target_pos - transform.position;
                    Vector2 dir2D = MathTool.Get2DCoordinate(preDirection);
                    Vector2 vel2D = MathTool.Get2DCoordinate(myRigid.velocity);

                
                    if (Mathf.Abs(Vector2.Angle(vel2D, dir2D)) >= 90) // the angle is not ideal at all
                    {
                        hasAttacked = false;
                        SwitchToCruise();
                        inAttackRoutine = false;
                        yield break;
                    }
                    else
                    {
                        float ProjVel = 0f;
                        float EstimatedTime = 0;
                        Vector2 pred_dir2D;
                        pre_predict_pos = predict_pos;
                        for (int i = 0; i < 6; i++)
                        {
                            pred_dir2D = MathTool.Get2DCoordinate(predict_pos - transform.position);
                            ProjVel = vel2D.magnitude;
                            predict_pos = target_pos + EstimatedTime * target_vel;
                            EstimatedTime = (pred_dir2D.magnitude - drop_dist)/ProjVel + tor_time;
                            //EstimatedTime = 0;
                        }
                        predict_pos = Vector3.Lerp(predict_pos, pre_predict_pos, 0.95f);

                        // turn to target
                        preDirection = predict_pos - transform.position;
                        float AngleDiff = Vector3.Angle(myRigid.velocity, preDirection);
                        Vector3 torque = Vector3.Cross(myRigid.velocity, preDirection).normalized * Mathf.Clamp(Mathf.Pow(AngleDiff, 1.5f)*0.2f, -15, 15);
                        torque.x = 0;
                        torque.z = 0;
                        myRigid.AddTorque(torque);

                        SetHeight(myRigid.position.y + Mathf.Clamp((WayHeight - myRigid.position.y) * 0.1f, -0.5f, 0.5f), false, 1);

                        Vector3 rigidPos = myRigid.position;
                        rigidPos.y = Mathf.Clamp(rigidPos.y, 21, 1000);
                        myRigid.MovePosition(rigidPos);

                        float targetPitch = 0;
                        if (preDirection.magnitude > 25f)
                        {
                            targetPitch = 90 - (Vector3.Angle(Vector3.up, preDirection));
                        }


                        Pitch = Pitch + Mathf.Clamp((targetPitch - Pitch) * 0.02f, -2f, 2f);
                    }

                    yield return new WaitForFixedUpdate();
                }

                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") Drop Torpedo", myPlayerID);
                foreach (var a in myGroup)
                {
                    a.Value.DropLoad();
                }

            }
            else // no target found
            {
                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") Find no target", myPlayerID);
                while (Vector2.Distance(MathTool.Get2DCoordinate(transform.position), WayPoint) > 10f)
                {
                    TurnToWayPoint();
                    yield return new WaitForFixedUpdate();
                }
                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") Drop Torpedo", myPlayerID);
                foreach (var a in myGroup)
                {
                    a.Value.DropLoad();
                }
            }
            SwitchToCruise();
            inAttackRoutine = false;
            yield break;
        }
        IEnumerator BombCoroutine()
        {
            int target_PlayerID = -1;
            List<Engine> target_engines = new List<Engine>();
            float err = UnityEngine.Random.value;

            float AttackHeight = transform.position.y;
            inAttackRoutine = true;
            DiveAnxiety = 0;
            Thrust = 20f;
            yield return new WaitForSeconds(myTeamIndex * 0.2f + UnityEngine.Random.value * 0.2f - 0.1f);
            float targetPitch = -82 + UnityEngine.Random.value * 8f;
            float targetRoll = 0;
            while(Pitch > targetPitch)
            {
                Pitch -= 1f;
                targetRoll += 4;
                targetRoll = Mathf.Clamp(targetRoll, 0, 180);
                Roll = targetRoll;
                yield return new WaitForFixedUpdate();
            }
            Thrust = 10f;

            targetRoll = -180;
            Vector3 LastTurn_pos = Vector3.zero;
            while (transform.position.y > Constants.SeaHeight + Constants.BombDropHeight + DiveAnxiety)
            {
                yield return new WaitForFixedUpdate();
                targetRoll += 4;
                targetRoll = Mathf.Clamp(targetRoll, -180, 0);
                Roll = targetRoll;


                // find target
                if (target_PlayerID == -1)
                {
                    float dist = 100f;
                    foreach (var ship in FlightDataBase.Instance.engines)
                    {
                        foreach (var engine in ship)
                        {
                            float MyDist = MathTool.Get2DDistance(engine.transform.position, transform.position);
                            if ( MyDist < dist)
                            {
                                dist = MyDist;
                                target_PlayerID = engine.myPlayerID;
                                target_engines.Clear();
                                target_engines.Add(engine);
                            }
                        }
                    }
                }
                else
                {
                    Vector3 target_vel = Vector3.zero;
                    Vector3 target_pos = Vector3.zero;
                    foreach (var engine in target_engines)
                    {
                        target_pos += engine.transform.position;
                        target_vel += engine.Rigidbody.velocity;
                    }
                    target_pos.y = Constants.SeaHeight;
                    target_vel.y = 0;
                    //target_vel *= (0.5f + err);

                    target_pos += (0.5f - err) * target_vel * 3f;

                    Vector3 targetDirection = target_pos - transform.position;
                    Vector2 dir2D = MathTool.Get2DCoordinate(targetDirection);
                    Vector2 vel2D = MathTool.Get2DCoordinate(myRigid.velocity);
                    Vector2 target_vel2D = MathTool.Get2DCoordinate(target_vel);


                    Vector3 turn_pos = Vector3.zero;
                    // velocity modifier
                    if (target_vel2D.magnitude < vel2D.magnitude)
                    {
                        turn_pos.y = transform.position.y - Mathf.Sqrt(myRigid.velocity.sqrMagnitude - target_vel2D.sqrMagnitude);
                    }
                    else
                    {
                        turn_pos.y = transform.position.y - 5f;
                    }
                    turn_pos.x = transform.position.x + target_vel2D.x;
                    turn_pos.z = transform.position.z + target_vel2D.y;


                    // pos modifier
                    turn_pos.x += targetDirection.x * 2f;
                    turn_pos.z += targetDirection.z * 2f;

                    if (LastTurn_pos == Vector3.zero)
                    {
                        LastTurn_pos = turn_pos;
                    }

                    turn_pos = Vector3.Lerp(turn_pos, LastTurn_pos, 0.8f);
                    LastTurn_pos = turn_pos;

                    // turn to target
                    targetDirection = turn_pos - transform.position;
                    float AngleDiff = Vector3.Angle(myRigid.velocity, targetDirection);
                    Vector3 torque = Vector3.Cross(myRigid.velocity, targetDirection).normalized * Mathf.Clamp(AngleDiff * 1f, -15, 15);
                    myRigid.AddTorque(torque);
                    //Vector3 v_angularVel = myRigid.angularVelocity;
                    //Vector3 RollTorque = Vector3.Cross(transform.right, -v_angularVel.normalized) * 7;//5=>7
                    //myRigid.AddTorque(RollTorque);
                }
            }
            
            MyLogger.Instance.Log("["+ Group.Value + "](" + myTeamIndex + ") Drop Bomb", myPlayerID);
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
            while (i < 70)
            {
                myRigid.AddTorque(-horizonRight * 12f);
                yield return new WaitForFixedUpdate();
                i++;
            }
            inTurnoverRoutine = false;
            yield break;
        }
        IEnumerator LandOnBoardCoroutine()
        {
            MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") land on deck successfully, transfer to hangar ...", myPlayerID);
            onboard = true;
            yield return new WaitForSeconds(1f);
            FlightDataBase.Instance.aircraftController[myPlayerID].Elevator.AddDownQueue(this);
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
        public IEnumerator DisturbedCoroutine(int time, float force)
        {
            int i = 0;
            Vector3 torque = new Vector3(-0.1f * UnityEngine.Random.value, 0, 2 - 4 * UnityEngine.Random.value) * force * 0.3f;
            while (i < time)
            {
                myRigid.AddRelativeTorque(torque);
                yield return new WaitForFixedUpdate();
                i++;
            }
            yield break;
        }
        public void UpdateRoll(bool host = true)
        {
            Roll = Roll + (Mathf.Clamp(TurningRate, -60,60) - Roll) * 0.05f;
        }
        public void UpdateLocalVel() // use in simulate update
        {
            localVelocity = (transform.position - previousLocalPos) / Time.deltaTime;
            previousLocalPos = transform.position;
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
                PropellerSpeed = 0f;

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
            Transform PrefabParent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            string PrefabName = "NavalAircraftTorpedo [" + myPlayerID + "](" + 400 + ")";
            if (PrefabParent.Find(PrefabName))
            {
                TorpedoPrefab = PrefabParent.Find(PrefabName).gameObject;
            }
            else
            {
                TorpedoPrefab = new GameObject(PrefabName);
                TorpedoPrefab.transform.parent = PrefabParent;
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
        }
        void InitBomb(bool AP = false)
        {
            Transform PrefabParent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            string PrefabName = "NavalAircraftBomb [" + myPlayerID + "](" + (AP?"AP":"HE") + ")";
            if (PrefabParent.Find(PrefabName))
            {
                BombPrefab = PrefabParent.Find(PrefabName).gameObject;
            }
            else
            {
                BombPrefab = new GameObject(PrefabName);
                BombPrefab.transform.parent = PrefabParent;
                Bomb BBtmp = BombPrefab.AddComponent<Bomb>();
                BBtmp.myPlayerID = myPlayerID;
                BBtmp.BombType = AP ? 1 : 0;
                Rigidbody RBtmp = BombPrefab.AddComponent<Rigidbody>();
                RBtmp.mass = 0.2f;
                RBtmp.drag = 0.02f;
                RBtmp.useGravity = true;

                GameObject CannonVis = new GameObject("BombVis");
                if (AP)
                {
                    CannonVis.transform.localScale = new Vector3(2f, 1, 1f);
                }
                CannonVis.transform.SetParent(BombPrefab.transform);
                CannonVis.transform.localPosition = Vector3.zero;
                CannonVis.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                MeshFilter MFtmp = CannonVis.AddComponent<MeshFilter>();
                MFtmp.sharedMesh = ModResource.GetMesh("Bomb Mesh").Mesh;
                MeshRenderer MRtmp = CannonVis.AddComponent<MeshRenderer>();
                MRtmp.material.mainTexture = ModResource.GetTexture("Engine Texture").Texture;

                BombPrefab.SetActive(false);
            }
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
                    case 3:
                        for (int i = 0; i < TeammateSpot.Count; i++)
                        {
                            TeammateSpot[i].transform.localPosition = new Vector3(0, 0, -(i + 1) * 5f);
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
            switch (Type.Value)
            {
                case 3:
                    hasLoad = true;
                    LoadMass = 0.7f;
                    LoadObject.transform.localScale = new Vector3(2f, 1f, 1f);
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
                case 2:
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
                case 1:
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
            }
            else if (Type.Value == 2)
            {
                InitBomb();
            }
            else if (Type.Value == 3)
            {
                InitBomb(true);
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
                    transform.position = spot.position + Vector3.up * 0.05f;
                    transform.rotation = spot.GetChild(0).rotation;
                }
                // modify rotation

                float deltaAngle = MathTool.SignedAngle(-new Vector2(transform.up.x, transform.up.z), new Vector2(spot.forward.x, spot.forward.z));
                transform.RotateAround(transform.position, Vector3.up, -deltaAngle);

            }

        }
        public void SettleLifter(Transform lifter, bool direct = false)
        {
            if (lifter)
            {
                Vector3 target = lifter.position + lifter.parent.localScale.z * Vector3.up - FlightDataBase.Instance.aircraftController[myPlayerID].transform.up * 0.5f;
                if (!direct)
                {
                    if ((transform.position - target).magnitude > 1f || Vector3.Angle(transform.forward, Vector3.up) > 30f)
                    {
                        transform.position = target;
                        myRigid.drag = 100f;
                        myRigid.angularDrag = 1000;
                    }
                    else
                    {
                        Vector3 targetPosition = Vector3.Lerp(transform.position, target, 0.1f);
                        targetPosition.y = transform.position.y;
                        transform.position = targetPosition;
                        myRigid.drag = 0.2f;
                        myRigid.angularDrag = 0.2f;
                    }

                }
                else
                {
                    transform.position = target;
                }

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
            myRigid.angularDrag = Mathf.Clamp(myRigid.velocity.magnitude * 0.5f, 0.2f,150f) * (flap && Type.Value >=2 ? 2.5f : 1);
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
                if (myLeader.status == Status.Cruise || myLeader.status == Status.Returning)
                {
                    myRigid.AddTorque(-Vector3.up * angle * 1f);
                }
                else if (myLeader.status == Status.Attacking)
                {
                    myRigid.AddTorque(-Vector3.up * angle * 0.2f);
                }

                if (myLeader.status == Status.Cruise || myLeader.status == Status.Attacking || myLeader.status == Status.Returning)
                {
                    Pitch = Pitch + Mathf.Clamp((myLeader.Pitch-Pitch) * 0.2f, -1f, 1f);
                    SetHeight(myRigid.position.y + (target.y - myRigid.position.y) * 0.1f);
                }
                else
                {
                    Pitch *= 0.98f;
                }
                /*
                Vector3 rigidTargetPosition = myRigid.position + (target - myRigid.position).normalized * Mathf.Clamp((target - transform.position).magnitude, 0, 10f) * 0.03f;
                rigidTargetPosition.y = Mathf.Clamp(rigidTargetPosition.y, 21, 1000);
                myRigid.MovePosition(rigidTargetPosition);*/

                Vector3 rigidTargetPosition = myRigid.position + (target - myRigid.position).normalized * Mathf.Clamp((target - transform.position).magnitude, 0, 50f) * 0.2f;
                myRigid.AddForce((rigidTargetPosition - myRigid.position) * 100f);
                if (myRigid.position.y < 21)
                {
                    myRigid.velocity = new Vector3(myRigid.velocity.x, 0, myRigid.velocity.z);
                    Vector3 pos = myRigid.position;
                    pos.y = 21;
                    myRigid.position = pos;
                }
            }
        }
        public void DropLoad(bool client = false, Vector3 rotation = default, Vector3 vel = default, Vector3 randomForce = default)
        {
            if (!hasLoad)
            {
                return;
            }
            if (Type.Value == 1)
            {
                hasLoad = false;
                GameObject Torpedo = null;
                if (!client)
                {
                    LoadMass = 0;
                    Torpedo = (GameObject)Instantiate(TorpedoPrefab, LoadObject.transform.position, transform.rotation,
                                                                BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                }
                else
                {
                    Torpedo = (GameObject)Instantiate(TorpedoPrefab, LoadObject.transform.position, transform.rotation,
                                                                BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                }
                    
                
                Torpedo.name = "Torpedo" + myPlayerID.ToString();
                Torpedo.SetActive(true);
                TorpedoBehaviour TB = Torpedo.GetComponent<TorpedoBehaviour>();
                TB.fire = true;
                TB.mode = 2;
                TB.parentGuid = myGuid;
                TB.depth = 0.5f;
                TB.launchedByAircraft = true;

                if (!client)
                {
                    Torpedo.GetComponent<Rigidbody>().velocity = myRigid.velocity;
                    if (StatMaster.isMP)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Torpedo.transform.eulerAngles, myRigid.velocity, (float)20));
                    }
                }
                else
                {
                    Torpedo.GetComponent<Rigidbody>().velocity = vel;
                }
                
                LoadObject.SetActive(false);
                Destroy(Torpedo, Constants.FastTorpedoTime);
            }
            else if (Type.Value == 2 || Type.Value ==3)
            {
                if (!client)
                {
                    LoadMass = 0;
                }
                hasLoad = false;
                GameObject Bomb = (GameObject)Instantiate(BombPrefab, LoadObject.transform.position, Quaternion.identity,
                                                                BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                Bomb.transform.rotation = Quaternion.LookRotation(LoadObject.transform.forward, LoadObject.transform.up);
                Bomb.transform.localScale = Vector3.one * 2;

                Bomb.GetComponent<Bomb>().parent = gameObject;
                Bomb.name = "Bomb" + myPlayerID.ToString();


                Bomb.SetActive(true);

                if (!client)
                {
                    Bomb.GetComponent<Rigidbody>().velocity = myRigid.velocity;
                    Bomb.GetComponent<Bomb>().randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f);

                    if (StatMaster.isMP)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Bomb.GetComponent<Bomb>().randomForce, Bomb.transform.eulerAngles, myRigid.velocity, (float)20));
                    }
                }
                else
                {
                    Bomb.GetComponent<Rigidbody>().velocity = vel;
                    Bomb.GetComponent<Bomb>().randomForce = randomForce;
                    //Debug.Log(Bomb.GetComponent<Bomb>().randomForce);
                }

                LoadObject.SetActive(false);
            }
        }
        public void RecoverLoad()
        {
            if (Type.Value == 1 || Type.Value == 2 || Type.Value == 3)
            {
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(AircraftMsgReceiver.LoadMsg.CreateMessage(myPlayerID, myGuid, true));
                }
                hasLoad = true;
                LoadObject.SetActive(true);
                switch (Type.Value)
                {
                    case 3:
                        LoadMass = 0.7f;
                        break;
                    case 2:
                        LoadMass = 0.5f;
                        break;
                    case 1:
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
                if (StatMaster.isMP && !StatMaster.isClient)
                {
                    ModNetworking.SendToAll(AircraftMsgReceiver.RemovedMsg.CreateMessage(myPlayerID, myGuid));
                }
                if (Grouper.Instance.AircraftGroups[myPlayerID].ContainsKey(Group.Value))
                {
                    Grouper.Instance.AircraftGroups[myPlayerID][Group.Value].Remove(myGuid);
                    if (myLeader)
                    {
                        myLeader.TeamUpByLeader();
                    }
                }
            }
            else if (Rank.Value == 1)
            {
                if (StatMaster.isMP && !StatMaster.isClient)
                {
                    ModNetworking.SendToAll(AircraftMsgReceiver.RemovedMsg.CreateMessage(myPlayerID, myGuid));
                }
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
        public void RemoveFromGroupClient()
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
            }
            else if (Rank.Value == 1)
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
            }
        }
        public void GetReturningWayPoint()
        {
            if (Rank.Value == 1)
            {
                FlightDataBase.Deck deck = FlightDataBase.Instance.Decks[myPlayerID];
                FlightDataBase.Instance.CheckLandingQueue(myPlayerID);
                int queueIndex = MathTool.GetQueueIndex(FlightDataBase.Instance.LandingQueue[myPlayerID], this);
                if (queueIndex == -1)
                {
                    queueIndex = FlightDataBase.Instance.LandingQueue[myPlayerID].Count;
                }
                waitQueueing = (queueIndex != 0);
                Vector2 targetPoint = deck.Center + deck.Forward * (-deck.Length * 0.25f - 230f);
                WayDirection = Vector2.zero;
                WayPoint = targetPoint;
                WayPointType = 0;
                WayHeight = deck.height + Constants.LandHeight + queueIndex * 15;
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
        public Aircraft AlertOnCruise(bool iff = true)
        {
            Aircraft a;
            foreach (var cv in Grouper.Instance.AircraftLeaders)
            {
                foreach (var leader in cv)
                {
                    try
                    {
                        a = leader.Value.Value;
                        if (a == this)
                        {
                            continue;
                        }
                        if (iff)
                        {
                            if (a.myPlayerID == myPlayerID || a.BlockBehaviour.Team.Equals(BlockBehaviour.Team))
                            {
                                continue;
                            }
                        }
                        
                        if ((a.status == Status.Cruise || a.status == Status.Attacking || a.status == Status.Returning || a.status == Status.DogFighting)
                            && (MathTool.Get2DCoordinate(a.transform.position) - MathTool.Get2DCoordinate(transform.position)).magnitude < 130f)
                        {
                            return a;
                        }
                    }
                    catch { }
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
        public void IncreaseAnxiety(float value)
        {
            DiveAnxiety += value/2f;
            if (preTeammate)
            {
                preTeammate.IncreaseAnxiety(value / 2f);
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
                if (StatMaster.isMP && !StatMaster.isClient)
                {
                    ModNetworking.SendToAll(AircraftMsgReceiver.ShootDownMsg.CreateMessage(myPlayerID, myGuid));
                }
                ColliderActive = true;
                status = Status.ShootDown;
                FoldWing = FoldWing;
                GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                Destroy(smoke, 10);
                myRigid.drag = 0.2f;
                myRigid.angularDrag = 0.2f;
                myRigid.useGravity = true;
                Thrust = 0f;
                PropellerSpeed = 0f;
                DeckSliding = false;
                Shoot = false;
                try
                {
                    RemoveFromGroup();
                }
                catch { }
                MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") is shot down", myPlayerID);
            }
        }
        public void SwitchToDogFighting(Aircraft a)
        {
            Roll = 0;
            ColliderActive = false;
            if (Rank.Value == 1)
            {
                Vector3 f_pos;
                if (a.status == Status.DogFighting && myGroup.ContainsValue(a.FightTarget.gameObject.GetComponent<Aircraft>()))
                {
                    f_pos = (transform.position + a.transform.position) / 2f;
                    f_pos.y = Constants.SeaHeight + Constants.CruiseHeight;

                    foreach (var a_member in a.myGroup)
                    {
                        a_member.Value.fightPosition = f_pos;
                    }
                }
                else
                {
                    f_pos = Vector3.zero;
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

                foreach (var member in myGroup)
                {
                    member.Value.fightPosition = f_pos;
                }

                if (fightPosition != Vector3.zero)
                {
                    MyLogger.Instance.Log("[" + Group.Value + "] is dogfighting with [" + a.Group.Value + "] at " + fightPosition.ToString(), myPlayerID);
                }
                else
                {
                    MyLogger.Instance.Log("[" + Group.Value + "] is dogfighting with [" + a.Group.Value + "]", myPlayerID);
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
                landingTime = 0;
                onboard = false;
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
                }else if (Type.Value == 2 || Type.Value == 3)
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
                switch (Type.Value) {
                
                    case 0:
                        Thrust = Constants.FighterInitialThrust;
                        break;
                    case 1:
                        Thrust = Constants.TorpedoInitialThrust;
                        break;
                    case 2:
                        Thrust = Constants.BombInitialThrust;
                        break;
                    case 3:
                        Thrust = Constants.BombInitialThrust;
                        break;
                }
                status = Status.TakingOff;
                MyDeck = null;
                deckHeight = 0;
                TakeOffDirection = new Vector2(-transform.up.x, -transform.up.z);
                PropellerSpeed = 11f;
                WayPoint = MathTool.Get2DCoordinate(transform.position - transform.up * 300f);
                WayHeight = CruiseHeight;
                WayPointType = 0;
            }
        }
        public void SwitchToOnBoard()
        {
            if (status == Status.InHangar)
            {
            }else if (status == Status.OnLifter)
            {
                status = Status.OnBoard;
                SettleSpot(MyDeck, true);
                PropellerSpeed = 3f;
                Thrust = 0;
            }
        }
        public void SwitchToInHangar()
        {
            if (status == Status.OnBoard)
            {
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
                PropellerSpeed = 0;
                Thrust = 0;
                FoldWing = true;
                if (!hasLoad)
                {
                    StartCoroutine(ReloadCorouting());
                }
            }else if (status == Status.OnLifter)
            {
                SettleSpot(MyHangar, true);
                status = Status.InHangar;
            }
        }

        public void SwitchToOnLifter(AircraftLifter lifter)
        {
            MyLifter = lifter.transform.Find("Vis");
            if (status == Status.OnBoard)
            {
                MyDeck.gameObject.GetComponent<ParkingSpot>().occupied = false;
                FlightDataBase.Instance.GetTakeOffPosition(myPlayerID);
                MyDeck = null;
                status = Status.OnLifter;
                SettleLifter(MyLifter, true);
                PropellerSpeed = 0;
                Thrust = 0;
                FoldWing = true;
            }else if (status == Status.InHangar)
            {
                if (!MyDeck)
                {
                    FindDeck();
                }
                deckHeight = 0;
                status = Status.OnLifter;
                SettleLifter(MyLifter, true);
            }
            else if (status == Status.Landing)
            {
                status = Status.OnLifter;
                SettleLifter(MyLifter, true);
                PropellerSpeed = 0;
                Thrust = 0;
                FoldWing = true;
                hasAttacked = false;
                hasFindBackup = false;
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
            if (HP < MaxHP && myseed == ModController.Instance.state % 10)
            {
                HP = Mathf.Clamp(HP + 1, 0, MaxHP);
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
                            if (StatMaster.isMP && !StatMaster.isClient)
                            {
                                ModNetworking.SendToAll(AircraftMsgReceiver.AddBackupMsg.CreateMessage(myPlayerID, Group.Value, a.myGuid));
                            }
                        }

                        TeamUpByLeader();
                        MyLogger.Instance.Log("[" + Group.Value + "] replenish " + addNum.ToString() + "/" + vacancy.ToString(), myPlayerID);
                    }
                    else
                    {
                        MyLogger.Instance.Log("No backup queue" , myPlayerID);
                    }
                        
                }
            }
            
        }

        public void OnLifterBehaviourFU()
        {
            SettleLifter(MyLifter, false);
        }
        public void OnBoardBehaviourFU()
        {
            SettleSpot(MyDeck,false);
        }
        public bool ArmourBetween(Vector3 pos, Vector3 target)
        {
            Ray Ray = new Ray(pos, target - pos);
            RaycastHit[] hitList = Physics.RaycastAll(Ray, (target - pos).magnitude);
            bool hasArmour = false;
            foreach (RaycastHit raycastHit in hitList)
            {
                try
                {
                    if (raycastHit.collider.attachedRigidbody.GetComponent<WoodenArmour>())
                    {
                        hasArmour = true;
                    }
                }
                catch
                {
                }

            }
            return hasArmour;
        }
        public void Explo()
        {
            if (StatMaster.isMP && !StatMaster.isClient)
            {
                ModNetworking.SendToAll(AircraftMsgReceiver.ExploMsg.CreateMessage(transform.position));
                ModNetworking.SendToAll(AircraftMsgReceiver.ShootDownMsg.CreateMessage(myPlayerID, myGuid));
            }
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftExplo, transform.position, Quaternion.identity);
            Destroy(explo, 5);
            GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
            Destroy(smoke, 10);
            MyLogger.Instance.Log("[" + Group.Value + "](" + myTeamIndex + ") exploded!", myPlayerID);
            status = Status.Exploded;
            FoldWing = FoldWing;
            myRigid.drag = 0.2f;
            myRigid.angularDrag = 0.2f;
            myRigid.constraints = RigidbodyConstraints.None;
            myRigid.useGravity = true;
            Thrust = 0f;
            PropellerSpeed = 0f;
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
                    
                    if (ArmourBetween(myRigid.centerOfMass + transform.position, hitedCollider.transform.position))
                    {
                        continue;
                    }

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
                    if (ArmourBetween(myRigid.centerOfMass + transform.position, hitedCollider.transform.position))
                    {
                        continue;
                    }
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
            myLongerSeed = (int)(UnityEngine.Random.value * 400);

            preType = -1;
            preAppearance = "";
            preRank = -1;
            preSkinEnabled = OptionsMaster.skinsEnabled;
            preShowCluster = StatMaster.clusterCoded;
            
            InitPropellerUndercart();
            InitGroupLine();

            SwitchActive = AddKey(LanguageManager.Instance.CurrentLanguage.SwitchActive, "SwitchActive", KeyCode.Alpha1);
            FPVKey = AddKey("FPV", "FPV", KeyCode.None);
            Group = AddText(LanguageManager.Instance.CurrentLanguage.Group, "AircraftGroup", "1");

            Type = AddMenu("Aircraft Type",0, LanguageManager.Instance.CurrentLanguage.AircraftType);
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
            Rank = AddMenu("Rank", 0, LanguageManager.Instance.CurrentLanguage.AircraftRank);
            for (int i = 0; i < 16; i++)
            {
                lastClientVelocity[i] = Vector3.zero;
            }
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
            if (preType != Type.Value)
            {
                preType = Type.Value;
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
                    case 3:
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
                case 3:
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
            previousLocalPos = transform.position;
        }
        public override void OnSimulateStop()
        {
            BlockBehaviour.BuildingBlock.GetComponent<Aircraft>().UpdateAppearance(preAppearance);
            ModCameraController.Instance.DisableModCamerFPV();
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
            ModCameraController.Instance.DisableModCamerFPV();

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
                    if (deckHeight == 0)
                    {
                        deckHeight = hit1.point.y;
                    }
                    else
                    {
                        deckHeight = Mathf.Lerp(deckHeight, hit1.point.y, 0.1f);
                    }
                    deckBelow = true;
                }
                else
                {
                    if (Physics.Raycast(UnderRay2, out hit2, 0.55f))
                    {
                        if (deckHeight == 0)
                        {
                            deckHeight = hit2.point.y;
                        }
                        else
                        {
                            deckHeight = Mathf.Lerp(deckHeight, hit2.point.y, 0.1f);
                        }
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
                    case 3:
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



            if (ModController.Instance.ShowArmour)
            {
                ShowGroupLine();
            }
            else
            {
                GroupLine.SetActive(false);
            }
        }
        public void FixedUpdate()
        {
            if (BlockBehaviour.isSimulating)
            {
                if (StatMaster.isClient)
                {
                    MySimulateFixedUpdateClient();
                }
            }
        }
        public override void SimulateUpdateAlways()
        {
            UpdateLocalVel();
            if (isSelf)
            {
                if (FPVKey.IsPressed)
                {
                    if (ModCameraController.Instance.FPV.Base == AircraftVis.transform && ModCameraController.Instance.FPV.IsActive == true)
                    {
                        ModCameraController.Instance.DisableModCamerFPV();
                    }
                    else
                    {
                        ModCameraController.Instance.EnableModCameraFPV(AircraftVis.transform);
                    }
                }
            }
        }
        public override void SimulateUpdateHost()
        {
            // send leader velocity if needed
            if (Rank.Value == 1)
            {
                if (AircraftMsgReceiver.Instance.ClientNeedVelocity[myPlayerID].ContainsKey(myGuid))
                {
                    foreach (var playerID in AircraftMsgReceiver.Instance.ClientNeedVelocity[myPlayerID][myGuid])
                    {
                        try
                        {
                            Player p = Player.From((ushort)playerID);
                            if ((myRigid.velocity - lastClientVelocity[playerID]).magnitude > 5f)
                            {
                                lastClientVelocity[playerID] = myRigid.velocity;
                                ModNetworking.SendTo(p, AircraftMsgReceiver.VelocityMsg.CreateMessage(myPlayerID, myGuid, lastClientVelocity[playerID]));
                            }
                        }
                        catch
                        {
                            AircraftMsgReceiver.Instance.ClientNeedVelocity[myPlayerID][myGuid].Remove(playerID);
                        }
                    }
                }
            }
            switch (Rank.Value)
            {
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
                default:
                    break;
            }

        }
        public override void SimulateUpdateClient()
        {
            if (AircraftMsgReceiver.Instance.ChangedStatus[myPlayerID].ContainsKey(myGuid))
            {
                status = AircraftMsgReceiver.Instance.ChangedStatus[myPlayerID][myGuid];
                FoldWing = FoldWing;
                AircraftMsgReceiver.Instance.ChangedStatus[myPlayerID].Remove(myGuid);
            }

            // sync remove behavior
            if (AircraftMsgReceiver.Instance.Removed[myPlayerID].Contains(myGuid))
            {
                AircraftMsgReceiver.Instance.Removed[myPlayerID].Remove(myGuid);
                RemoveFromGroupClient();
            }

            // sync shootdown behavior
            if (AircraftMsgReceiver.Instance.ShootDown[myPlayerID].Contains(myGuid))
            {
                AircraftMsgReceiver.Instance.ShootDown[myPlayerID].Remove(myGuid);
                GameObject smoke = (GameObject)Instantiate(AssetManager.Instance.Aircraft.AircraftShootDown, transform.position, Quaternion.identity, transform);
                Destroy(smoke, 10);
            }

            // sync gun shoot behavior
            if (AircraftMsgReceiver.Instance.GunShoot[myPlayerID].ContainsKey(myGuid))
            {
                Shoot = AircraftMsgReceiver.Instance.GunShoot[myPlayerID][myGuid];
                AircraftMsgReceiver.Instance.GunShoot[myPlayerID].Remove(myGuid);
            }

            // sync add backup
            if (Rank.Value == 1)
            {
                if (AircraftMsgReceiver.Instance.Backup[myPlayerID].ContainsKey(Group.Value))
                {
                    foreach (var backup in AircraftMsgReceiver.Instance.Backup[myPlayerID][Group.Value])
                    {
                        Aircraft backup_a = null;
                        foreach (var a in Grouper.Instance.GetAircraft(myPlayerID, "backup"))
                        {
                            if (a.Key == backup)
                            {
                                backup_a = a.Value;
                                break;
                            }
                        }
                        if (backup_a)
                        {
                            backup_a.Rank.SetValue(0);
                            backup_a.Group.SetValue(Group.Value);
                            Grouper.Instance.AddAircraft(myPlayerID, Group.Value, backup_a.myGuid, backup_a);
                            //Debug.Log("add backup" + backup);
                        }
                    }
                    AircraftMsgReceiver.Instance.Backup[myPlayerID][Group.Value].Clear();
                }
            }

            // for change load vis
            if (AircraftMsgReceiver.Instance.LoadChange[myPlayerID].ContainsKey(myGuid))
            {
                LoadObject.SetActive(AircraftMsgReceiver.Instance.LoadChange[myPlayerID][myGuid]);
                hasLoad = AircraftMsgReceiver.Instance.LoadChange[myPlayerID][myGuid];
                AircraftMsgReceiver.Instance.LoadChange[myPlayerID].Remove(myGuid);
            }

            // for sync fuel
            if (AircraftMsgReceiver.Instance.Fuel[myPlayerID].ContainsKey(myGuid))
            {
                Fuel = AircraftMsgReceiver.Instance.Fuel[myPlayerID][myGuid];
                AircraftMsgReceiver.Instance.Fuel[myPlayerID].Remove(myGuid);
                //Debug.Log("Sync Fuel to" + Fuel);
            }

            // for drop load
            if (WeaponMsgReceiver.Instance.Fire[myPlayerID].ContainsKey(myGuid))
            {
                Vector3 vel = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].vel;
                Vector3 rotation = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].forward;
                Vector3 randomForce = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                WeaponMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
                DropLoad(true, rotation, vel, randomForce);
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            Vector3 nowForward = -transform.up;
            Vector3 nowUp = transform.forward;

            Vector3 preForward = Vector3.ProjectOnPlane(LastFoward, transform.forward);
            float angle = Vector3.Angle(preForward, nowForward);
            if (Vector3.Dot(Vector3.Cross(preForward, nowForward), nowUp) > 0)
            {
                DeltaTurning = angle;
            }
            else
            {
                DeltaTurning = -angle;
            }

            LastFoward = nowForward;
            if (frameCount == 0)
            {
                if (Rank.Value == 1)
                {
                    targetTeamCount = Grouper.Instance.GetAircraft(myPlayerID, Group.Value).Count();
                    TeamUpByLeader();
                }
                frameCount++;
            }
            if (ModController.Instance.state % 10 == myseed && !TriedFindHangar && frameCount > 40)
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
                case Status.OnLifter:
                    {
                        OnLifterBehaviourFU();
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
                        switch (Type.Value)
                        {

                            case 0:
                                Thrust += Constants.FighterAccel;
                                break;
                            case 1:
                                Thrust += Constants.TorpedoAccel;
                                break;
                            case 2:
                                Thrust += Constants.BombAccel;
                                break;
                            case 3:
                                Thrust += Constants.BombAccel;
                                break;

                        }
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
                        else
                        {
                            Pitch = Mathf.Clamp(Pitch, 8f,25f);
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
                                bool allInCruise = true;
                                foreach (var item in myGroup)
                                {
                                    if (item.Value.status != Status.Cruise)
                                    {
                                        allInCruise = false;
                                        break;
                                    }
                                }
                                if (allInCruise)
                                {
                                    SwitchToAttack();
                                }
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

                        UpdateRoll();
                        break;
                    }
                case Status.Attacking:
                    {
                        AddAeroForce(true);
                        DeckSliding = false;

                        if (Type.Value == 1)
                        {
                            if (Rank.Value == 0)
                            {
                                SlaveFollowLeader();
                            }
                            else if (Rank.Value == 1)
                            {
                                //TurnToWayPoint();
                            }
                        }
                        if ((Type.Value == 2 || Type.Value == 3) && !inAttackRoutine)
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
                        else if (Type.Value == 2 || Type.Value == 3)
                        {
                            float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                            if (!inAttackRoutine && distFromWayPoint < 50f && Rank.Value == 1)
                            {
                                foreach (var a in myGroup)
                                {
                                    a.Value.StartCoroutine(a.Value.BombCoroutine());
                                }
                            }
                        }

                        if (!inAttackRoutine)
                        {
                            UpdateRoll();
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
                            if (distFromWayPoint < 200f) // queue range
                            {
                                Queue<Aircraft> queue = FlightDataBase.Instance.LandingQueue[myPlayerID];
                                if (!queue.Contains(this))
                                {
                                    queue.Enqueue(this);
                                }

                                if (distFromWayPoint < 75f && !waitQueueing) // 
                                {

                                    foreach (var a in myGroup.Reverse())
                                    {
                                        if (a.Value.status == Status.InHangar || a.Value.status == Status.Exploded || (a.Value.status == Status.Landing && a.Value.landingTime > 800f))
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
                        }
                        UpdateRoll();
                        break;
                    }
                case Status.Landing:
                    {
                        landingTime++;
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
                                UpdateRoll();
                            }
                        }

                        Pitch = Mathf.Clamp(Pitch, -10, 30);

                        break;
                    }
                case Status.DogFighting:
                    {
                        ColliderActive = false;
                        if (!FightTarget)
                        {
                            SwitchToCruise();
                            break;
                        }
                        Aircraft targetAircraft = FightTarget.GetComponent<Aircraft>();
                        if (!targetAircraft)
                        {
                            SwitchToCruise();
                            break;
                        }
                        if (!targetAircraft.isFlying)
                        {
                            if (targetAircraft.preTeammate)
                            {
                                FightTarget = targetAircraft.preTeammate.transform;
                                break;
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
                        if (dist < 8f)
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

                            bool canShoot = AngleDiff < 6;
                            Shoot = canShoot;
                            if (canShoot)
                            {
                                targetAircraft.ReduceHP(Type.Value == 0? 5 : 3);
                                targetAircraft.IncreaseAnxiety(Type.Value == 0 ? 3 : 2);
                            }
                        }
                        Vector3 v_angularVel = myRigid.angularVelocity;
                        Vector3 RollTorque = Vector3.Cross(transform.right, -v_angularVel.normalized) * 10;//5=>10
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

            if (Thrust > 0)
            {
                // send fuel message
                if (myLongerSeed == ModController.Instance.longerState)
                {
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(AircraftMsgReceiver.FuelMsg.CreateMessage(myPlayerID, myGuid, Fuel));
                    }
                }

            }

            // water hit
            if (!hasHitWater && ModController.Instance.showSea && status != Status.OnBoard && status != Status.InHangar && status != Status.OnLifter)
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
                    MyLogger.Instance.Log("["+ Group.Value + "](" + myTeamIndex + ") drop sea!", myPlayerID);

                    GameObject waterhit;
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = 250f/381f * Vector3.one;
                    Destroy(waterhit, 3);
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), 250f));
                    }
                }
            }


        }
        public void MySimulateFixedUpdateClient()
        {
            Vector3 nowForward = -transform.up;
            Vector3 nowUp = transform.forward;

            Vector3 preForward = Vector3.ProjectOnPlane(LastFoward, transform.forward);
            float angle = Vector3.Angle(preForward, nowForward);
            if (Vector3.Dot(Vector3.Cross(preForward, nowForward), nowUp) > 0)
            {
                DeltaTurning = angle;
            }
            else
            {
                DeltaTurning = -angle;
            }

            LastFoward = nowForward;
            if (frameCount == 0)
            {
                if (Rank.Value == 1)
                {
                    targetTeamCount = Grouper.Instance.GetAircraft(myPlayerID, Group.Value).Count();
                    myGroup = Grouper.Instance.GetAircraft(myPlayerID, Group.Value);
                }else if (Rank.Value == 0)
                {
                    myLeader = Grouper.Instance.GetLeader(myPlayerID, Group.Value);
                    //Debug.Log("get leader " + (myLeader?"true":"false"));
                }
                frameCount++;
            }
            else
            {
                frameCount++;
            }

            switch (status)
            {
                case Status.InHangar:
                    Thrust = 0;
                    Roll = 0;
                    PropellerSpeed = 0;
                    FoldWing = true;
                    UndercartObject.SetActive(true);
                    if (Fuel < 1)
                    {
                        Fuel = Mathf.Clamp(Fuel + 0.0004f, 0, 1);
                    }
                    break;
                case Status.OnBoard:
                    Thrust = 0;
                    Roll = 0;
                    PropellerSpeed = 3;
                    FoldWing = true;
                    UndercartObject.SetActive(true);
                    break;
                case Status.TakingOff:
                    Thrust = 50;
                    Roll = 0;
                    PropellerSpeed = 11;
                    FoldWing = false;
                    UndercartObject.SetActive(true);
                    break;
                case Status.Cruise:
                    {
                        Thrust = 60f;
                        UpdateRoll(false);
                        PropellerSpeed = 11;
                        FoldWing = false;
                        UndercartObject.SetActive(false);
                        if (Rank.Value == 1)
                        {
                            float distFromWayPoint = (MathTool.Get2DCoordinate(transform.position) - WayPoint).magnitude;
                            if (distFromWayPoint < 75f)
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
                        
                        break;
                    }
                case Status.Returning:
                    Thrust = 50;
                    PropellerSpeed = 11;
                    FoldWing = false;
                    UndercartObject.SetActive(false);
                    UpdateRoll(false);
                    break;
                case Status.Attacking:
                    Thrust = 60;
                    PropellerSpeed = 11;
                    FoldWing = false;
                    UndercartObject.SetActive(false);
                    UpdateRoll(false);
                    break;
                case Status.DogFighting:
                    Thrust = 60;
                    PropellerSpeed = 11;
                    FoldWing = false;
                    UndercartObject.SetActive(false);
                    Roll = 0;
                    break;
                case Status.Landing:
                    Thrust = 23;
                    PropellerSpeed = 11;
                    FoldWing = false;
                    UndercartObject.SetActive(true);
                    Roll = 0;
                    break;
                default:
                    Thrust = 0;
                    PropellerSpeed = 0;
                    Roll = 0;
                    break;
            }

            if (Fuel > 0)
            {
                Fuel -= Thrust / 3000000f;
            }

            

        }
        public void OnGUI()
        {
            if (!(StatMaster.isMP && myPlayerID == PlayerData.localPlayer.networkId))
            {
                return;
            }
            if (Rank.Value == 1 && isFlying)
            {
                AircraftController ac = FlightDataBase.Instance.aircraftController[myPlayerID];
                if (ac)
                {
                    if (ac.inTacticalView)
                    {
                        foreach (var cv in Grouper.Instance.AircraftLeaders)
                        {
                            foreach (var a in cv.Values)
                            {
                                try
                                {
                                    Aircraft target = a.Value;
                                    if (target.myPlayerID == myPlayerID)
                                    {
                                        break;
                                    }

                                    if (Vector3.Distance(target.transform.position, transform.position) < (target.myGroup.Count * 150) + 400 &&
                                        target.isFlying)
                                    {
                                        Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                                        if (target.BlockBehaviour.Team.Equals(BlockBehaviour.Team))
                                        {
                                            GUI.contentColor = Color.yellow;
                                        }
                                        else
                                        {
                                            GUI.contentColor = Color.red;
                                        }
                                        GUI.Box(new Rect(onScreenPosition.x - 50, Camera.main.pixelHeight - onScreenPosition.y - 12, 100, 25), target.Type.Selection.ToString() + " *" + target.myGroup.Count.ToString() + "*");
                                    }
                                }
                                catch { }
                            }
                        }
                        
                    }
                }
            }
        }
    }
}
