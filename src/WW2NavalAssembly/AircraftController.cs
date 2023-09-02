using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.Text.RegularExpressions;
using static WW2NavalAssembly.WeaponMsgReceiver;

namespace WW2NavalAssembly
{
    public class AircraftControllerMsgReceiver : SingleInstance<AircraftControllerMsgReceiver>
    {
        public override string Name { get; } = "Aircraft Controller Msg Receiver";

        public static MessageType MouseRouteMsg = ModNetworking.CreateMessageType(DataType.String, DataType.Vector3, DataType.Integer);// group, position, type

        public class MouseRouteInfo
        {
            public Vector3 worldPos;
            public int type;
            public string group;

            public MouseRouteInfo(string group, Vector3 worldPos, int type)
            {
                this.worldPos = worldPos;
                this.type = type;
                this.group = group;
            }
        }
        public Queue<MouseRouteInfo>[] mouseRouteInfo = new Queue<MouseRouteInfo>[16];

        public AircraftControllerMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                mouseRouteInfo[i] = new Queue<MouseRouteInfo>();
            }
        }
        public void MouseRouteMsgReceiver(Message msg)
        {
            string group = (string)msg.GetData(0);
            Vector3 worldPos = (Vector3)msg.GetData(1);
            int type = (int)msg.GetData(2);
            mouseRouteInfo[msg.Sender.NetworkId].Enqueue(new MouseRouteInfo(group, worldPos, type));
            Debug.Log("Receive mouse route msg: " + group + " | " + worldPos + " | " + type);   
        }

    }
    public class AircraftElevator : MonoBehaviour
    {
        public float delayTime = 0.2f;

        public Queue<Aircraft> UpQueue = new Queue<Aircraft>();
        public Queue<Aircraft> DownQueue = new Queue<Aircraft>();

        public bool upOperating = false;
        public bool downOperating = false;

        public void AddUpQueue(Aircraft aircraft)
        {
            Aircraft contradiction = null;
            foreach (Aircraft a in DownQueue)
            {
                if (a == aircraft)
                {
                    contradiction = a;
                    break;
                }
            }

            if (contradiction)// delete the existing aircraft
            {
                Queue<Aircraft> newDownQueue = new Queue<Aircraft>();
                foreach (Aircraft a in DownQueue)
                {
                    if (a != contradiction)
                    {
                        newDownQueue.Enqueue(a);
                    }
                }
                DownQueue = newDownQueue;
            }
            bool exist = false;
            foreach (Aircraft a in UpQueue)
            {
                if (a == aircraft)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                UpQueue.Enqueue(aircraft);
            }


        }
        public void AddDownQueue(Aircraft aircraft)
        {
            Aircraft contradiction = null;
            foreach (Aircraft a in UpQueue)
            {
                if (a == aircraft)
                {
                    contradiction = a;
                    break;
                }
            }
            if (contradiction)// delete the existing aircraft
            {
                Queue<Aircraft> newUpQueue = new Queue<Aircraft>();
                foreach (Aircraft a in UpQueue)
                {
                    if (a != contradiction)
                    {
                        newUpQueue.Enqueue(a);
                    }
                    else
                    {
                        FlightDataBase.Instance.Decks[a.myPlayerID].Occupied_num--;
                    }
                }
                UpQueue = newUpQueue;
            }
            bool exist = false;
            foreach (Aircraft a in DownQueue)
            {
                if (a == aircraft)
                {
                    exist = true;
                    break;
                }
            }
            if (!exist)
            {
                DownQueue.Enqueue(aircraft);
            }
        }

        IEnumerator LiftCoroutine()
        {
            upOperating = true;
            Aircraft a = UpQueue.Count > 0 ? UpQueue.Dequeue() : null;
            if (a && a.status == Aircraft.Status.InHangar)
            {
                a.FindDeck();
                MyLogger.Instance.Log("Elevator lift aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ")...");
                yield return new WaitForSeconds(delayTime);
                MyLogger.Instance.Log("\tFinished");
                a.SwitchToOnBoard();
                //MyLogger.Instance.Log("Finish");
            }
            upOperating = false;
            yield break;
        }

        IEnumerator DropCoroutine()
        {
            downOperating = true;
            Aircraft a = DownQueue.Count > 0 ? DownQueue.Dequeue() : null;
            if (a && a.status == Aircraft.Status.OnBoard)
            {
                MyLogger.Instance.Log("Elevator drop aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ")...");
                yield return new WaitForSeconds(delayTime);
                MyLogger.Instance.Log("\tFinished");
                a.SwitchToInHangar();
                //MyLogger.Instance.Log("Finish");
            }
            downOperating = false;
            yield break;
        }

        void Start()
        {
        }

        void Update()
        {
            if (UpQueue.Count > 0 && !upOperating)
            {
                StartCoroutine(LiftCoroutine());
            }
            if (DownQueue.Count > 0 && !downOperating)
            {
                StartCoroutine(DropCoroutine());
            }
        }

        void OnDestroy()
        {
            StopAllCoroutines();
        }
        public void OnGUI()
        {
            //GUI.Box(new Rect(100, 400, 200, 30), UpQueue.Count.ToString());
            //GUI.Box(new Rect(100, 430, 200, 30), DownQueue.Count.ToString());

        }

    }

    public class AircraftRunway : MonoBehaviour // guide the aircraft group to takeoff
    {
        public Queue<Aircraft> takeOffQueue = new Queue<Aircraft>();
        public Transform takeOffSpot;

        bool takeOffOperating = false;
        public float delayTime = 0.5f;

        public void AddAircraft(Aircraft aircraft)
        {
            if (!takeOffQueue.Contains(aircraft) && aircraft.status == Aircraft.Status.OnBoard)
            {
                takeOffQueue.Enqueue(aircraft);
            }
        }

        IEnumerator TakeOffCoroutine()
        {
            takeOffOperating = true;
            Aircraft a = takeOffQueue.Count > 0 ? takeOffQueue.Dequeue() : null;
            if (a && a.status == Aircraft.Status.OnBoard)
            {
                MyLogger.Instance.Log("Move aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ") to take off spot...");
                yield return new WaitForSeconds(delayTime);
                a.ChangeDeckSpot(FlightDataBase.Instance.DeckObjects[a.myPlayerID].transform.Find("TakeOff").GetChild(0), true);
                MyLogger.Instance.Log("\tTaking off ...");
                yield return new WaitForSeconds(delayTime);
                a.SwitchToTakingOff();
                MyLogger.Instance.Log("\tFinish taking off");
            }
            takeOffOperating = false;
            yield break;
        }

        public void Update()
        {
            if (takeOffQueue.Count > 0 && !takeOffOperating)
            {
                StartCoroutine(TakeOffCoroutine());
            }
        }

        public void OnGUI()
        {
            //GUI.Box(new Rect(100, 400, 200, 30), takeOffQueue.Count.ToString());
            //GUI.Box(new Rect(100, 430, 200, 30), DownQueue.Count.ToString());

        }
    }
    public class CruisePoint : MonoBehaviour
    {
        public Vector3 Position;
        public Vector2 Direction;
        public GameObject Icon;
        public int Type = 0; // 0: normal, 1: torpedo attack, 2: bomb attack
        public CruisePoint(Vector3 pos, Vector2 direction, int type, GameObject icon = null)
        {
            Position = pos;
            Direction = direction;
            Type = type;
            Icon = icon;
        }
        public void Awake()
        {
        }
    }

    class AircraftController : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey TacticalView;
        public MKey ReturnKey;
        public MKey TakeOffKey;
        public MKey ElevatorUp;
        public MKey ElevatorDown;
        public MKey ViewUp;
        public MKey ViewDown;
        public MKey ViewLeft;
        public MKey ViewRight;
        public MKey Continuous;
        public MKey Attack;
        public MSlider ViewSensitivity;

        bool _inTacticalView = false;
        float _orthoSize = 400f;
        public bool inTacticalView
        {
            get { return _inTacticalView; }
            set {
                _inTacticalView = value;
                SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = !_inTacticalView;
                if (_inTacticalView)
                {
                    MainCamera.orthographic = true;
                    MainCamera.transform.eulerAngles = new Vector3(90, 0, 0);
                    MainCamera.orthographicSize = _orthoSize;

                    Vector3 pos = MainCamera.transform.position;
                    pos.y = 400;
                    MainCamera.transform.position = pos;

                }
                else
                {
                    MainCamera.orthographic = false;
                }
            }
        }

        public GameObject DeckVis;
        public GameObject HangarVis;

        public bool hasDeck = false;
        public bool hasHangar = false;

        public GameObject DrawBoard;
        public Dictionary<string, LineRenderer> RouteLines = new Dictionary<string, LineRenderer>();
        public Dictionary<string, Queue<CruisePoint>> Routes = new Dictionary<string, Queue<CruisePoint>>();
        public Dictionary<string, GameObject> GroupIcon = new Dictionary<string, GameObject>();

        private Aircraft currentLeader;
        public Aircraft CurrentLeader
        {
            get
            {
                return currentLeader;
            }
            set
            {
                if (value != currentLeader)
                {
                    currentLeader = value;
                    // update the line visual
                    foreach (var line in RouteLines)
                    {
                        if (currentLeader)
                        {
                            if (line.Key != currentLeader.Group.Value)
                            {
                                line.Value.SetColors(Color.gray, Color.gray);
                                line.Value.SetWidth(0.5f, 0.5f);
                            }
                            else
                            {
                                line.Value.SetColors(Color.white, Color.white);
                                line.Value.SetWidth(1f, 1f);
                            }
                        }
                        else
                        {
                            line.Value.SetColors(Color.gray, Color.gray);
                            line.Value.SetWidth(0.5f, 0.5f);
                        }

                    }
                    foreach (var icon in GroupIcon)
                    {
                        if (currentLeader)
                        {
                            if (icon.Key != currentLeader.Group.Value)
                            {
                                icon.Value.GetComponentInChildren<SpriteRenderer>().color = Color.white * 0.5f;
                                icon.Value.GetComponentInChildren<TextMesh>().color = Color.white * 0.5f;
                            }
                            else
                            {
                                icon.Value.GetComponentInChildren<SpriteRenderer>().color = Color.white;
                                icon.Value.GetComponentInChildren<TextMesh>().color = Color.white;
                            }
                        }
                        else
                        {
                            icon.Value.GetComponentInChildren<SpriteRenderer>().color = Color.white * 0.5f;
                            icon.Value.GetComponentInChildren<TextMesh>().color = Color.white * 0.5f;
                        }
                    }
                }
            }
        }
        public AircraftElevator Elevator;
        public AircraftRunway Runway;

        public Camera _viewCamera;
        public Camera MainCamera
        {
            get
            {
                bool flag;
                if (this._viewCamera == null)
                {
                    MouseOrbit instance = SingleInstanceFindOnly<MouseOrbit>.Instance;
                    flag = (((instance != null) ? instance.cam : null) != null);
                }
                else
                {
                    flag = false;
                }
                bool flag2 = flag;
                if (flag2)
                {
                    this._viewCamera = SingleInstanceFindOnly<MouseOrbit>.Instance.cam;
                }
                bool flag3 = this._viewCamera == null;
                if (flag3)
                {
                    this._viewCamera = Camera.main;
                }
                return this._viewCamera;
            }
        }
 
        public void InitDrawBoard()
        {
            DrawBoard = new GameObject("DrawBoard");
            DrawBoard.transform.parent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            DrawBoard.SetActive(false);
            foreach (var group in Grouper.Instance.AircraftGroups[myPlayerID])
            {
                if (group.Key == "null" || group.Key == "backup")
                {
                    continue;
                }
                GameObject icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.GroupIcon);
                icon.transform.GetChild(1).GetComponent<TextMesh>().text = group.Key;
                icon.transform.parent = DrawBoard.transform;
                GroupIcon.Add(group.Key, icon);
            }
        }
        public void UpdateRouteLine(string group)
        {
            if (Grouper.Instance.AircraftLeaders[myPlayerID].ContainsKey(group))
            {
                RouteLines[group].SetVertexCount(Routes[group].Count + 1);
                RouteLines[group].SetPosition(0, Grouper.Instance.AircraftLeaders[myPlayerID][group].Value.transform.position);
                int i = 1;
                foreach (var routePoint in Routes[group])
                {
                    RouteLines[group].SetPosition(i, routePoint.Position);
                    i++;
                }
            }
            else
            {
                foreach (var point in Routes[group])
                {
                    if (point.Icon)
                    {
                        point.Icon.SetActive(false);
                    }
                }
                Routes[group].Clear();
                RouteLines[group].gameObject.SetActive(false);
            }


        }
        public void UpdateGroupIcon(string group)
        {
            if (Grouper.Instance.AircraftLeaders[myPlayerID].ContainsKey(group))
            {
                try
                {
                    Aircraft a = Grouper.Instance.AircraftLeaders[myPlayerID][group].Value;
                    Vector3 pos = a.transform.position;
                    pos.y = 60;
                    GroupIcon[group].transform.position = pos;
                    float angle = -MathTool.SignedAngle(MathTool.Get2DCoordinate(-a.transform.up), Vector2.right);
                    GroupIcon[group].transform.GetChild(0).localEulerAngles = new Vector3(90, 0, angle);
                    GroupIcon[group].transform.localScale = _orthoSize / 200f * Vector3.one;
                }
                catch { }

            }
            else
            {
                GroupIcon[group].SetActive(false);
            }
        }
        public void UpdateCurrentLeaderInfo()
        {
            if (CurrentLeader)
            {
                foreach (var icon in GroupIcon)
                {
                    TextMesh txt = icon.Value.GetComponentInChildren<TextMesh>();
                    
                    if (icon.Key == CurrentLeader.Group.Value)
                    {
                        txt.characterSize = 2;
                        string[] mateStatus = new string[CurrentLeader.targetTeamCount];
                        List<Aircraft> mateList = new List<Aircraft>(CurrentLeader.myGroup.Values);
                        for (int i = 0; i < currentLeader.targetTeamCount; i++)
                        {
                            if (i >= mateList.Count)
                            {
                                mateStatus[i] = "XXXXX";
                            }
                            else
                            {
                                mateStatus[i] = (mateList[i].hasLoad? "■" : "□") +
                                                mateList[i].status.ToString() + " " + 
                                                "[Fuel:"+(mateList[i].Fuel * 100f).ToString("F1") + "%]" +
                                                "[HP:" + (mateList[i].HP / 5f).ToString("F1") + "%]";
                            }
                        }
                        txt.text = "[" + icon.Key + "]\n\t" + string.Join("\n\t", mateStatus);
                    }
                    else
                    {
                        txt.characterSize = 3;
                        txt.text = icon.Key;
                    }
                }
            }
            else
            {
                foreach (var icon in GroupIcon)
                {
                    TextMesh txt = icon.Value.GetComponentInChildren<TextMesh>();
                    txt.characterSize = 3;
                    txt.text = icon.Key;
                }
            }
        }
        public void AddRoutePoint(string group, Vector2 position, int type = 0)
        {
            if (!Routes.ContainsKey(group))
            {
                Routes.Add(group, new Queue<CruisePoint>());
                GameObject routeRoot = new GameObject("Route_" + group);
                routeRoot.transform.parent = DrawBoard.transform;
                LineRenderer LR = routeRoot.AddComponent<LineRenderer>();
                LR.material = new Material(Shader.Find("Particles/Additive"));
                RouteLines.Add(group, LR);
            }

            Vector2 direction = Vector2.zero;
            GameObject Icon = null;

            if (Routes[group].Count > 0)
            {
                Vector3 prePosition = Routes[group].LastOrDefault().Position;
                direction = MathTool.Get2DCoordinate(new Vector3(position.x, 60f, position.y) - prePosition).normalized;
                switch (type)
                {
                    case 1:
                        Icon = (GameObject)Instantiate( AssetManager.Instance.Aircraft.TorpedoAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    case 2:
                        Icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.BombAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    default:
                        break;
                }
            }

            float height;
            switch (type)
            {
                case 0:
                    height = 60f;
                    break;
                case 1:
                    height = 21.5f;
                    break;
                case 2:
                    height = 250f;
                    break;
                default:
                    height = 60f;
                    break;
            }

            Routes[group].Enqueue(new CruisePoint(new Vector3(position.x,height,position.y), direction, type, Icon));
        }
        public void ResetRoutePoint(string group, Vector2 position, int type = 0)
        {
            if (!Routes.ContainsKey(group))
            {
                Routes.Add(group, new Queue<CruisePoint>());
                GameObject routeRoot = new GameObject("Route_" + group);
                routeRoot.transform.parent = DrawBoard.transform;
                LineRenderer LR = routeRoot.AddComponent<LineRenderer>();
                LR.material = new Material(Shader.Find("Particles/Additive"));
                RouteLines.Add(group, LR);
            }

            if (currentLeader.status != Aircraft.Status.Attacking)
            {
                float height;
                switch (type)
                {
                    case 0:
                        height = 60f;
                        break;
                    case 1:
                        height = 21.5f;
                        break;
                    case 2:
                        height = 250f;
                        break;
                    default:
                        height = 60f;
                        break;
                }

                Vector2 direction = Vector2.zero;
                GameObject Icon = null;
                Vector3 prePosition = CurrentLeader.transform.position;
                direction = MathTool.Get2DCoordinate(new Vector3(position.x, height, position.y) - prePosition).normalized;

                switch (type)
                {
                    case 1:
                        Icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.TorpedoAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    case 2:
                        Icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.BombAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    default:
                        break;
                }


                foreach (var point in Routes[group])
                {
                    if (point.Icon)
                    {
                        Destroy(point.Icon);
                    }
                }
                Routes[group].Clear();


                Routes[group].Enqueue(new CruisePoint(new Vector3(position.x, height, position.y), direction, type, Icon));
                CurrentLeader.WayPoint = position;
                CurrentLeader.WayDirection = direction;
                CurrentLeader.WayHeight = height;
                CurrentLeader.WayPointType = type;
            }
            else
            {
                //AddRoutePoint(group, position, type);
            }
        }

        public CruisePoint DequeueRoutePoint(string group)
        {
            if (Routes.ContainsKey(group))
            {
                if (Routes[group].Count > 0)
                {
                    CruisePoint point = Routes[group].Dequeue();
                    if (point.Icon)
                    {
                        Destroy(point.Icon);
                    }
                    return point;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public override void SafeAwake()
        {
            gameObject.name = "Aircraft Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);

            TacticalView = AddKey("Tactical View", "TacticalView", KeyCode.T);
            ViewUp = AddKey("View Up", "ViewUp", KeyCode.Y);
            ViewDown = AddKey("View Down", "ViewDown", KeyCode.H);
            ViewLeft = AddKey("View Left", "ViewLeft", KeyCode.G);
            ViewRight = AddKey("View Right", "ViewRight", KeyCode.J);
            ReturnKey = AddKey("Aircraft Return", "ReturnKey", KeyCode.Backspace);
            TakeOffKey = AddKey("Aircraft Take Off", "TakeOffKey", KeyCode.Q);
            ElevatorUp = AddKey("Aircraft Elevator Up", "ElevatorUp", KeyCode.UpArrow);
            ElevatorDown = AddKey("Aircraft Elevator Down", "ElevatorDown", KeyCode.DownArrow);
            Continuous = AddKey("Continuous", "Continuous", KeyCode.LeftControl);
            ViewSensitivity = AddSlider("View Sensitivity", "ViewSensitivity", 1, 0.3f, 3f);
            Attack = AddKey("Attack", "Attack", KeyCode.C);
        }

        public override void BuildingFixedUpdate()
        {
            FlightDataBase.Instance.UpdateDeck(myPlayerID, false);
            FlightDataBase.Instance.UpdateHangar(myPlayerID);
        }

        public void Start()
        {
            gameObject.name = "Aircraft Captain";
        }

        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            Elevator = gameObject.AddComponent<AircraftElevator>();
            Runway = gameObject.AddComponent<AircraftRunway>();

            // use database to generate deck and hangar
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                    if (DeckVis)
                    {
                        hasDeck = true;
                        DeckVis.SetActive(ModController.Instance.showArmour);
                    }
                    if (HangarVis)
                    {
                        hasHangar = true;
                        HangarVis.SetActive(ModController.Instance.showArmour);
                    }
                    FlightDataBase.Instance.aircraftController[myPlayerID] = this;
                }
                else if (PlayerData.localPlayer.networkId == 0)
                {
                    // generate parking spot for client in host
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                    if (DeckVis)
                    {
                        hasDeck = true;
                        DeckVis.SetActive(false);
                    }
                    if (HangarVis)
                    {
                        hasHangar = true;
                        HangarVis.SetActive(false);
                    }

                    FlightDataBase.Instance.aircraftController[myPlayerID] = this;
                } 
            }
            else
            {
                DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, BlockBehaviour.ParentMachine.transform.Find("Simulation Machine"));
                if (DeckVis)
                {
                    hasDeck = true;
                    DeckVis.SetActive(ModController.Instance.showArmour);
                }
                if (HangarVis)
                {
                    hasHangar = true;
                    HangarVis.SetActive(ModController.Instance.showArmour);
                }
                FlightDataBase.Instance.aircraftController[myPlayerID] = this;
            }

            // drawboard
            InitDrawBoard();
            CurrentLeader = null;
        }

        public override void SimulateFixedUpdateAlways()
        {
            FlightDataBase.Instance.UpdateDeck(myPlayerID, true);
            FlightDataBase.Instance.UpdateHangar(myPlayerID);
            if (hasDeck)
            {
                // low frequency
                if (ModController.Instance.state % 10 == mySeed)
                {
                    if (StatMaster.isMP)
                    {
                        if (PlayerData.localPlayer.networkId == myPlayerID)
                        {
                            DeckVis.SetActive(ModController.Instance.showArmour);
                        }
                    }
                    else
                    {
                        DeckVis.SetActive(ModController.Instance.showArmour);
                    }
                }

                // high frequency
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID)
                    {
                    }
                }
                else
                {
                }
            }
            if (hasHangar)
            {
                // low frequency
                if (ModController.Instance.state % 10 == mySeed)
                {
                    if (StatMaster.isMP)
                    {
                        if (PlayerData.localPlayer.networkId == myPlayerID)
                        {
                            HangarVis.SetActive(ModController.Instance.showArmour);
                        }
                    }
                    else
                    {
                        HangarVis.SetActive(ModController.Instance.showArmour);
                    }
                }
                // high frequency
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID)
                    {
                    }
                }
                else
                {
                }
            }
        }

        public override void SimulateUpdateAlways()
        {
            if (myPlayerID == PlayerData.localPlayer.networkId)
            {
                if (TacticalView.IsPressed)
                {
                    inTacticalView = !inTacticalView;
                    if (inTacticalView && CurrentLeader)
                    {
                        MainCamera.transform.position = CurrentLeader.transform.position;
                    }
                }

                if (StatMaster.isClient) // self is client
                {
                    if (inTacticalView)
                    {
                        DrawBoard.SetActive(true);
                        float mouseY = Input.mouseScrollDelta.y;
                        _orthoSize = Mathf.Clamp(_orthoSize * (mouseY > 0 ? 1f / (1f + mouseY * 0.2f) : (1f - mouseY * 0.2f)), 50, 2000);
                        MainCamera.transform.position += _orthoSize * ViewSensitivity.Value * Time.deltaTime * (
                                                                            Vector3.forward * (ViewUp.IsHeld ? 1 : 0) +
                                                                            Vector3.back * (ViewDown.IsHeld ? 1 : 0) +
                                                                            Vector3.left * (ViewLeft.IsHeld ? 1 : 0) +
                                                                            Vector3.right * (ViewRight.IsHeld ? 1 : 0));

                        if (CurrentLeader)
                        {
                            if (CurrentLeader.status != Aircraft.Status.Returning && CurrentLeader.status != Aircraft.Status.Landing)
                            {
                                if (Continuous.IsHeld)
                                {
                                    if (Input.GetMouseButtonDown(1))
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        AddRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), 0);
                                    }
                                    else if (Attack.IsPressed)
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        AddRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    }
                                }
                                else
                                {
                                    if (Input.GetMouseButtonDown(1))
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), 0);
                                    }
                                    else if (Attack.IsPressed)
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    }
                                }
                            }


                            if (ReturnKey.IsPressed)
                            {
                                bool allinCruise = true;
                                foreach (var a in CurrentLeader.myGroup)
                                {
                                    if (a.Value.status != Aircraft.Status.Cruise)
                                    {
                                        allinCruise = false;
                                        break;
                                    }
                                }
                                if (allinCruise)
                                {
                                    string group = CurrentLeader.Group.Value;
                                    if (Routes.ContainsKey(group))
                                    {
                                        foreach (var point in Routes[group])
                                        {
                                            if (point.Icon)
                                            {
                                                Destroy(point.Icon);
                                            }
                                        }
                                        Routes[group].Clear();
                                    }

                                    CurrentLeader.SwitchToReturn();
                                }
                                else
                                {
                                    MyLogger.Instance.Log("[" + CurrentLeader.Group.Value + "] cannot return because not all aircraft in cruise");
                                }


                            }

                        }

                    }
                }
                else
                {
                    if (inTacticalView)
                    {
                        DrawBoard.SetActive(true);
                        float mouseY = Input.mouseScrollDelta.y;
                        _orthoSize = Mathf.Clamp(_orthoSize * (mouseY > 0 ? 1f / (1f + mouseY * 0.2f) : (1f - mouseY * 0.2f)), 50, 2000);
                        MainCamera.transform.position += _orthoSize * ViewSensitivity.Value * Time.deltaTime * (
                                                                            Vector3.forward * (ViewUp.IsHeld ? 1 : 0) +
                                                                            Vector3.back * (ViewDown.IsHeld ? 1 : 0) +
                                                                            Vector3.left * (ViewLeft.IsHeld ? 1 : 0) +
                                                                            Vector3.right * (ViewRight.IsHeld ? 1 : 0));



                        if (CurrentLeader)
                        {
                            if (CurrentLeader.status != Aircraft.Status.Returning && CurrentLeader.status != Aircraft.Status.Landing)
                            {
                                if (Continuous.IsHeld)
                                {
                                    if (Input.GetMouseButtonDown(1))
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        AddRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), 0);
                                    }
                                    else if (Attack.IsPressed)
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        AddRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    }
                                }
                                else
                                {
                                    if (Input.GetMouseButtonDown(1))
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), 0);
                                    }
                                    else if (Attack.IsPressed)
                                    {
                                        Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                        ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    }
                                }
                            }


                            if (ReturnKey.IsPressed)
                            {
                                bool allinCruise = true;
                                foreach (var a in CurrentLeader.myGroup)
                                {
                                    if (a.Value.status != Aircraft.Status.Cruise)
                                    {
                                        allinCruise = false;
                                        break;
                                    }
                                }
                                if (allinCruise)
                                {
                                    string group = CurrentLeader.Group.Value;
                                    if (Routes.ContainsKey(group))
                                    {
                                        foreach (var point in Routes[group])
                                        {
                                            if (point.Icon)
                                            {
                                                Destroy(point.Icon);
                                            }
                                        }
                                        Routes[group].Clear();
                                    }

                                    CurrentLeader.SwitchToReturn();
                                }
                                else
                                {
                                    MyLogger.Instance.Log("[" + CurrentLeader.Group.Value + "] cannot return because not all aircraft in cruise");
                                }


                            }

                        }
                        foreach (var group in Routes)
                        {
                            UpdateRouteLine(group.Key);
                        }
                        foreach (var group in GroupIcon)
                        {
                            UpdateGroupIcon(group.Key);
                        }
                        UpdateCurrentLeaderInfo();
                    }
                    else
                    {
                        DrawBoard.SetActive(false);
                    }
                } // self is host


            }
            else if (!StatMaster.isClient) // host solve the msg from client
            {

            }
            
            

            if (hasDeck)
            {
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID || !StatMaster.isClient)
                    {
                        FlightDataBase.Instance.UpdateDeckTransform(myPlayerID);
                    }
                }
                else
                {
                    FlightDataBase.Instance.UpdateDeckTransform(myPlayerID);
                }
            }
            if (hasHangar)
            {
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID || !StatMaster.isClient)
                    {
                        FlightDataBase.Instance.UpdateHangarTransform(myPlayerID);
                    }
                }
                else
                {
                    FlightDataBase.Instance.UpdateHangarTransform(myPlayerID);
                }
            }
        }
        public override void SimulateUpdateHost() // key responding
        {
            if (ElevatorDown.IsPressed && CurrentLeader)
            {
                bool allOnCarrier = true;
                foreach (var aircraft in CurrentLeader.myGroup)
                {
                    if (aircraft.Value.status != Aircraft.Status.OnBoard && aircraft.Value.status != Aircraft.Status.InHangar)
                    {
                        allOnCarrier = false;
                        break;
                    }
                }
                if (allOnCarrier)
                {
                    foreach (var aircraft in CurrentLeader.myGroup.Reverse())
                    {
                        Elevator.AddDownQueue(aircraft.Value);
                    }
                }
                else
                {
                    MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] on Carrier");
                }
                
            }

            if (ElevatorUp.IsPressed && CurrentLeader)
            {
                bool allInHangar = true;
                foreach (var aircraft in CurrentLeader.myGroup)
                {
                    if (aircraft.Value.status != Aircraft.Status.InHangar)
                    {
                        allInHangar = false;
                        break;
                    }
                }
                if (!allInHangar)
                {
                    MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] in hangar");
                }
                else
                {
                    if (CurrentLeader.myGroup.Count + FlightDataBase.Instance.Decks[myPlayerID].Occupied_num > FlightDataBase.Instance.Decks[myPlayerID].Total_num)
                    {
                        MyLogger.Instance.Log("Not enough space on deck for [" + CurrentLeader.Group.Value + "]");
                        MyLogger.Instance.Log("Need: " + CurrentLeader.myGroup.Count.ToString() + ", Occupy: " +
                                                FlightDataBase.Instance.Decks[myPlayerID].Occupied_num.ToString() + "/" +
                                                FlightDataBase.Instance.Decks[myPlayerID].Total_num.ToString());
                    }
                    else
                    {
                        FlightDataBase.Instance.Decks[myPlayerID].Occupied_num += CurrentLeader.myGroup.Count;
                        foreach (var aircraft in CurrentLeader.myGroup.Reverse())
                        {
                            Elevator.AddUpQueue(aircraft.Value);
                        }
                    }
                }
                
            }

            if (TakeOffKey.IsPressed)
            {
                if (CurrentLeader)
                {
                    bool allOnBoard = true;
                    foreach (var member in CurrentLeader.myGroup)
                    {
                        if (member.Value.status != Aircraft.Status.OnBoard)
                        {
                            allOnBoard = false;
                            break;
                        }
                    }
                    if (!allOnBoard)
                    {
                        MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] are on board");
                    }
                    else
                    {
                        foreach (var member in CurrentLeader.myGroup)
                        {
                            Runway.AddAircraft(member.Value);
                        }
                    }
                }
            }

        }

        public override void SimulateLateUpdateAlways()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    return;
                }
            }

            if (inTacticalView)
            {
                inTacticalView = true;
            }


        }

        public override void OnSimulateStop()
        {
            FlightDataBase.Instance.ClearDeckHangar(myPlayerID);
            try
            {
                inTacticalView = false;
            }
            catch { }
        }
        public void OnDestroy()
        {
            FlightDataBase.Instance.ClearDeckHangar(myPlayerID);
            try
            {
                inTacticalView = false;
            }
            catch { }
        }

        public void OnGUI()
        {
            if (CurrentLeader)
            {
                GUI.Box(new Rect(100, 200, 200, 30), CurrentLeader.Group.Value.ToString() + " " + CurrentLeader.status.ToString());
            }

            if ((Camera.main.transform.position - transform.position).magnitude < 30 && BlockBehaviour.isSimulating)
            {
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
                GUI.Box(new Rect(onScreenPosition.x, Camera.main.pixelHeight - onScreenPosition.y, 200, 30), "Deck space: " + FlightDataBase.Instance.Decks[myPlayerID].Occupied_num.ToString() + "/" +
                    FlightDataBase.Instance.Decks[myPlayerID].Total_num.ToString());
            }


        }

    }
}
