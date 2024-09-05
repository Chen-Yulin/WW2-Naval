using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.Text.RegularExpressions;
using static WW2NavalAssembly.FlightDataBase;

namespace WW2NavalAssembly
{
    public class AircraftControllerMsgReceiver : SingleInstance<AircraftControllerMsgReceiver>
    {
        public override string Name { get; } = "Aircraft Controller Msg Receiver";

        public static MessageType MouseRouteMsg = ModNetworking.CreateMessageType(DataType.String, DataType.Vector3, DataType.Integer, DataType.Boolean);// group, position, type, reset
        public static MessageType CurrentLeaderMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.String);// playerID, group
        public static MessageType ReturnMsg = ModNetworking.CreateMessageType(DataType.String);
        public class MouseRouteInfo
        {
            public Vector3 worldPos;
            public int type;
            public string group;
            public bool reset;
            public MouseRouteInfo(string group, Vector3 worldPos, int type, bool reset)
            {
                this.worldPos = worldPos;
                this.type = type;
                this.group = group;
                this.reset = reset;
            }
        }
        public class CurrentLeaderInfo
        {
            public bool valid = false;
            public string group;
            public CurrentLeaderInfo()
            {
                valid = false;
            }
            public CurrentLeaderInfo(string group)
            {
                this.group = group;
                valid = true;
            }
        }
        public class ReturnInfo
        {
            public bool valid = false;
            public string group;
            public ReturnInfo()
            {
                valid = false;
            }
            public ReturnInfo(string group)
            {
                this.group = group;
                valid = true;
            }
        }
        public Queue<MouseRouteInfo>[] mouseRouteInfo = new Queue<MouseRouteInfo>[16];
        public CurrentLeaderInfo[] currentLeaderInfos = new CurrentLeaderInfo[16];
        public ReturnInfo[] returnPressed = new ReturnInfo[16];

        public AircraftControllerMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                mouseRouteInfo[i] = new Queue<MouseRouteInfo>();
                currentLeaderInfos[i] = new CurrentLeaderInfo();
                returnPressed[i] = new ReturnInfo();
            }
        }
        public void MouseRouteMsgReceiver(Message msg)
        {
            string group = (string)msg.GetData(0);
            Vector3 worldPos = (Vector3)msg.GetData(1);
            int type = (int)msg.GetData(2);
            bool reset = (bool)msg.GetData(3);
            mouseRouteInfo[msg.Sender.NetworkId].Enqueue(new MouseRouteInfo(group, worldPos, type, reset));
            Debug.Log("Receive mouse route msg: " + group + " | " + worldPos + " | " + type + " | " + reset);
        }
        public void CurrentLeaderMsgReceiver(Message msg)
        {
            int playerID = (int)msg.GetData(0);
            string group = (string)msg.GetData(1);
            currentLeaderInfos[playerID].group = group;
            currentLeaderInfos[playerID].valid = true;
            Debug.Log("Player " + playerID + " receive current leader msg: " + group);
        }
        public void ReturnMsgReceiver(Message msg)
        {
            string group = (string)msg.GetData(0);
            returnPressed[msg.Sender.NetworkId].group = group;
            returnPressed[msg.Sender.NetworkId].valid = true;
            Debug.Log("Receive return msg: " + group);
        }
    }
    public class AircraftLiftManager : MonoBehaviour
    {
        public float delayTime = 0.2f;

        public Queue<Aircraft> UpQueue = new Queue<Aircraft>();
        public Queue<Aircraft> DownQueue = new Queue<Aircraft>();


        public int myPlayerID = 0;

        public void AddUpQueue(Aircraft aircraft) // return whether duplication exists
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

            if (contradiction)// delete the existing contradicted aircraft
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
                FlightDataBase.Instance.Decks[aircraft.myPlayerID].Occupied_num++;
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

        IEnumerator LiftCoroutine(AircraftLifter lifter)
        {
            lifter.operating = true;
            Aircraft a = UpQueue.Count > 0 ? UpQueue.Dequeue() : null;
            if (a && a.status == Aircraft.Status.InHangar)
            {
                while (!lifter.GoToHangarStep(a))
                {
                    yield return new WaitForFixedUpdate();
                }
                a.FindDeck();
                a.SwitchToOnLifter(lifter);
                MyLogger.Instance.Log("Elevator lift aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ")...", myPlayerID);
                
                yield return new WaitForSeconds(delayTime);
                
                while (!lifter.GoToDeckStep())
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForSeconds(delayTime);
                a.SwitchToOnBoard();
                MyLogger.Instance.Log("\tFinished", myPlayerID);
            }
            lifter.operating = false;
            yield break;
        }

        IEnumerator DropCoroutine(AircraftLifter lifter)
        {
            Aircraft a = DownQueue.Count > 0 ? DownQueue.Dequeue() : null;
            if (a && (a.status == Aircraft.Status.OnBoard || (a.status == Aircraft.Status.Landing && a.onboard)))
            {
                while (!lifter.GoToDeckStep())
                {
                    yield return new WaitForFixedUpdate();
                }
                MyLogger.Instance.Log("Elevator drop aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ")...", myPlayerID);
                a.SwitchToOnLifter(lifter);
                yield return new WaitForSeconds(delayTime);
                while (!lifter.GoToHangarStep(a))
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return new WaitForSeconds(delayTime);
                a.SwitchToInHangar();
                MyLogger.Instance.Log("\tFinished", myPlayerID);
                FlightDataBase.Instance.Decks[myPlayerID].Occupied_num--;
                while (!lifter.GoToDeckStep())
                {
                    yield return new WaitForFixedUpdate();
                }
                //MyLogger.Instance.Log("Finish");
            }
            lifter.operating = false;
            yield break;
        }

        void Start()
        {
        }

        void Update()
        {
            if (UpQueue.Count > 0)
            {
                bool solvable = false;
                foreach (var lifter in FlightDataBase.Instance.MasterLifters[myPlayerID])
                {
                    if (lifter.Value)
                    {
                        if (lifter.Value.RaiseEnabled && !lifter.Value.destroyed)
                        {
                            solvable = true;
                            if (!lifter.Value.operating)
                            {
                                lifter.Value.operating = true;
                                StartCoroutine(LiftCoroutine(lifter.Value));
                                break;
                            }
                        }
                    }
                }
                if (!solvable)
                {
                    foreach (var lifter in FlightDataBase.Instance.MasterLifters[myPlayerID])
                    {
                        if (lifter.Value)
                        {
                            if (!lifter.Value.operating && !lifter.Value.destroyed)
                            {
                                lifter.Value.operating = true;
                                StartCoroutine(LiftCoroutine(lifter.Value));
                                break;
                            }
                        }
                    }
                }

            }
            if (DownQueue.Count > 0)
            {
                bool solvable = false;
                foreach (var lifter in FlightDataBase.Instance.MasterLifters[myPlayerID])
                {
                    if (lifter.Value)
                    {
                        if (lifter.Value.DropEnabled && !lifter.Value.destroyed)
                        {
                            solvable = true;
                            if (!lifter.Value.operating)
                            {
                                lifter.Value.operating = true;
                                StartCoroutine(DropCoroutine(lifter.Value));
                                break;
                            }
                        }
                        
                    }
                }
                if (!solvable)
                {
                    foreach (var lifter in FlightDataBase.Instance.MasterLifters[myPlayerID])
                    {
                        if (lifter.Value)
                        {
                            if (!lifter.Value.operating && !lifter.Value.destroyed)
                            {
                                lifter.Value.operating = true;
                                StartCoroutine(DropCoroutine(lifter.Value));
                                break;
                            }
                        }
                    }
                }

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
        public int myPlayerID;

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
                MyLogger.Instance.Log("Move aircraft: [" + a.Group.Value + "](" + a.myTeamIndex + ") to take off spot...", myPlayerID);
                yield return new WaitForSeconds(delayTime);
                a.ChangeDeckSpot(FlightDataBase.Instance.DeckObjects[a.myPlayerID].transform.Find("TakeOff").GetChild(0), true);
                MyLogger.Instance.Log("\tTaking off ...", myPlayerID);
                yield return new WaitForSeconds(delayTime);
                a.SwitchToTakingOff();
                MyLogger.Instance.Log("\tFinish taking off", myPlayerID);
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

    public class AircraftCatapults : MonoBehaviour
    {
        public int myPlayerID;
        public List<Catapult> catapults = new List<Catapult>();
        public Queue<Aircraft> takeOffQueue = new Queue<Aircraft>();
        public float delayTime = 0.5f;
        public void AddAircraft(Aircraft aircraft)
        {
            if (!takeOffQueue.Contains(aircraft) && aircraft.status == Aircraft.Status.OnBoard)
            {
                takeOffQueue.Enqueue(aircraft);
            }
        }
        public void AddCatapult(Catapult c)
        {
            catapults.Add(c);
        }
        IEnumerator TakeOffCoroutine(Catapult device)
        {
            device.operating = true;
            Aircraft a = takeOffQueue.Count > 0 ? takeOffQueue.Dequeue() : null;
            if (a && a.status == Aircraft.Status.OnBoard)
            {
                device.SwitchHook(a);
                MyLogger.Instance.Log("Move seaplane: [" + a.Group.Value + "](" + a.myTeamIndex + ") to take off spot...", myPlayerID);
                a.MyDeck.gameObject.GetComponent<ParkingSpot>().occupied = false;
                FlightDataBase.Instance.Decks[a.myPlayerID].Occupied_num--;
                a.FoldWing = false;
                yield return new WaitForSeconds(delayTime);
                MyLogger.Instance.Log("\tTaking off ...", myPlayerID);
                a.transform.position = device.HookPos.position;
                a.transform.rotation = device.transform.rotation;
                a.SwitchToTakingOff();
                float speed = -15f;
                while (device.energy > 0)
                {
                    yield return new WaitForFixedUpdate();
                    device.energy -= Mathf.Clamp(speed, 0f, 15f);
                    speed += 1f;
                    a.transform.position = device.HookPos.position;
                    a.transform.rotation = device.transform.rotation;
                }
                a.Rigidbody.velocity = -device.transform.up * 45f * device.transform.localScale.y;
                MyLogger.Instance.Log("\tFinish taking off", myPlayerID);
                device.energy = 0;
            }
            device.operating = false;
            device.EmitSmoke();
            yield break;
        }

        public void Update()
        {
            if (!StatMaster.isClient)
            {
                foreach (var catapult in catapults)
                {
                    if (!catapult.Ready && !catapult.operating)
                    {
                        catapult.energy += Time.deltaTime * 2;
                    }
                }
            }
            Catapult available = null;
            foreach (var c in catapults)
            {
                if (c.Ready)
                {
                    available = c;
                    break;
                }

            }
            if (takeOffQueue.Count > 0 && available)
            {
                StartCoroutine(TakeOffCoroutine(available));
            }
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

    public class AircraftController : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey TacticalView;
        public MKey ReturnKey;
        public MKey TakeOffKey;
        public MKey ElevatorUp;
        public MKey ElevatorDown;
        public MKey ViewMove;
        public MKey ResetView;
        public MKey Continuous;
        public MKey Attack;
        public MSlider ViewSensitivity;

        public bool _inTacticalView = false;
        float _orthoSize = 400f;
        public bool inTacticalView
        {
            get { return _inTacticalView; }
            set {
                if (_inTacticalView != value)
                {
                    _inTacticalView = value;
                    if (_inTacticalView)
                    {
                        ModCameraController.Instance.EnableModCameraTAC(transform, ViewSensitivity.Value, ResetView, ViewMove, this);

                    }
                    else
                    {
                        ModCameraController.Instance.DisableModCameraTAC();
                    }
                }
            }
        }

        public GameObject DeckVis;
        public GameObject HangarVis;

        public bool hasDeck = false;
        public bool hasHangar = false;

        public GameObject DrawBoard;
        public GameObject Ruler;
        public Transform RulerDir;
        public Transform RulerSize;
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

                    if (StatMaster.isMP)
                    {
                        if (!StatMaster.isClient && PlayerData.localPlayer.networkId != myPlayerID)
                        {
                            if (value)
                            {
                                Player p = Player.From((ushort)myPlayerID);
                                ModNetworking.SendTo(p, AircraftControllerMsgReceiver.CurrentLeaderMsg.CreateMessage(myPlayerID, value.Group.Value));
                            }
                            else
                            {
                                Player p = Player.From((ushort)myPlayerID);
                                ModNetworking.SendTo(p, AircraftControllerMsgReceiver.CurrentLeaderMsg.CreateMessage(myPlayerID, "null"));
                            }
                        }
                    }


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
        public AircraftLiftManager Elevator;
        public AircraftRunway Runway;
        public AircraftCatapults Catapults;

        public Vector3 dragOrigin;

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
            Ruler = (GameObject)Instantiate(AssetManager.Instance.Aircraft.Ruler, DrawBoard.transform);
            Ruler.SetActive(false);
            RulerDir = Ruler.transform.GetChild(0);
            RulerSize = Ruler.transform.GetChild(1);
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
                    GroupIcon[group].transform.localScale = ModCameraController.Instance.TAC._orthoSize / 200f * Vector3.one;
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
                                                "[Fuel:"+(mateList[i].Fuel * 100f).ToString("F1") + "%]";
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
                direction = MathTool.Get2DCoordinate(new Vector3(position.x, Constants.CruiseHeight + Constants.SeaHeight, position.y) - prePosition).normalized;
                switch (type)
                {
                    case 1:
                        Icon = (GameObject)Instantiate( AssetManager.Instance.Aircraft.TorpedoAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        Icon.transform.localScale = new Vector3(3f, 1, 1);
                        break;
                    case 2:
                        Icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.BombAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    case 3:
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
                    height = Constants.CruiseHeight + Constants.SeaHeight;
                    break;
                case 1:
                    height = Constants.TorpedoAttackHeight + Constants.SeaHeight;
                    break;
                case 2:
                    height = Constants.BombAttackHeight + Constants.SeaHeight;
                    break;
                case 3:
                    height = Constants.BombAttackHeight + Constants.SeaHeight;
                    break;
                default:
                    height = Constants.CruiseHeight + Constants.SeaHeight;
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
                        height = Constants.CruiseHeight + Constants.SeaHeight;
                        break;
                    case 1:
                        height = Constants.TorpedoAttackHeight + Constants.SeaHeight;
                        break;
                    case 2:
                        height = Constants.BombAttackHeight + Constants.SeaHeight;
                        break;
                    case 3:
                        height = Constants.BombAttackHeight + Constants.SeaHeight;
                        break;
                    default:
                        height = Constants.CruiseHeight + Constants.SeaHeight;
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
                        Icon.transform.localScale = new Vector3(3f, 1, 1);
                        break;
                    case 2:
                        Icon = (GameObject)Instantiate(AssetManager.Instance.Aircraft.BombAim, new Vector3(position.x, 20.5f, position.y), Quaternion.identity,
                                                        DrawBoard.transform);
                        Icon.transform.rotation = Quaternion.LookRotation(Vector3.up, new Vector3(direction.x, 0, direction.y));
                        break;
                    case 3:
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

        public float CalculateRouteDist(Aircraft a)
        {
            if (!a)
            {
                return 0;
            }
            string group = a.Group.Value;
            if (!Routes.ContainsKey(group))
            {
                return 0f;
            }
            else
            {
                float dist = 0f;
                Vector3 prePosition = a.transform.position;
                foreach (var point in Routes[group])
                {
                    dist += Vector3.Distance(prePosition, point.Position);
                    prePosition = point.Position;
                }
                return dist;
            }
        }
        public override void SafeAwake()
        {
            gameObject.name = "Aircraft Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);

            TacticalView = AddKey("Tactical View", "TacticalView", KeyCode.T);
            ViewMove = AddKey("Move View", "Move View", KeyCode.Mouse2);
            ResetView = AddKey("Reset View", "Reset View", KeyCode.None);
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
            Elevator = gameObject.AddComponent<AircraftLiftManager>();
            Elevator.myPlayerID = myPlayerID;
            Runway = gameObject.AddComponent<AircraftRunway>();
            Runway.myPlayerID = myPlayerID;
            Catapults = gameObject.AddComponent<AircraftCatapults>();
            Catapults.myPlayerID = myPlayerID;

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
                        DeckVis.SetActive(ModController.Instance.ShowArmour);
                    }
                    if (HangarVis)
                    {
                        hasHangar = true;
                        HangarVis.SetActive(ModController.Instance.ShowArmour);
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
                    DeckVis.SetActive(ModController.Instance.ShowArmour);
                }
                if (HangarVis)
                {
                    hasHangar = true;
                    HangarVis.SetActive(ModController.Instance.ShowArmour);
                }
                FlightDataBase.Instance.aircraftController[myPlayerID] = this;
            }

            // drawboard
            InitDrawBoard();
            CurrentLeader = null;
        }

        public void FixedUpdate()
        {
            if (BlockBehaviour && BlockBehaviour.isSimulating)
            {
                MySimulateFixedUpdateAlways();
            }
        }

        public void MySimulateFixedUpdateAlways()
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
                            DeckVis.SetActive(ModController.Instance.ShowArmour);
                        }
                    }
                    else
                    {
                        DeckVis.SetActive(ModController.Instance.ShowArmour);
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
                            HangarVis.SetActive(ModController.Instance.ShowArmour);
                        }
                    }
                    else
                    {
                        HangarVis.SetActive(ModController.Instance.ShowArmour);
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

        public override void SimulateUpdateClient()
        {
            if (AircraftControllerMsgReceiver.Instance.currentLeaderInfos[myPlayerID].valid)
            {
                string group = AircraftControllerMsgReceiver.Instance.currentLeaderInfos[myPlayerID].group;
                if (group == "null")
                {
                    CurrentLeader = null;
                }
                else
                {
                    CurrentLeader = Grouper.Instance.GetLeader(myPlayerID, group);
                }
                
                Debug.Log(CurrentLeader != null);
                Debug.Log(Grouper.Instance.AircraftLeaders[myPlayerID].Count());
                AircraftControllerMsgReceiver.Instance.currentLeaderInfos[myPlayerID].valid = false;
            }
        }
        public override void SimulateUpdateAlways()
        {
            if (!StatMaster.isMP || myPlayerID == PlayerData.localPlayer.networkId)
            {
                if (TacticalView.IsPressed)
                {
                    inTacticalView = !inTacticalView;
                    if (inTacticalView && CurrentLeader)
                    {
                        MainCamera.transform.position = CurrentLeader.transform.position;
                    }
                }

                if (inTacticalView)
                {
                    DrawBoard.SetActive(true);
                    /*
                    float mouseScroll = Input.mouseScrollDelta.y;
                    _orthoSize = Mathf.Clamp(_orthoSize * (mouseScroll > 0 ? 1f / (1f + mouseScroll * 0.2f) : (1f - mouseScroll * 0.2f)), 50, 2000);

                    if (ResetView.IsPressed)
                    {
                        MainCamera.transform.position = transform.position;
                    }

                    // move camera
                    if (ViewMove.IsHeld)
                    {
                        float mouseX = Input.GetAxis("Mouse X");
                        float mouseY = Input.GetAxis("Mouse Y");
                        Vector3 moveDir = (mouseX * -Vector3.right + mouseY * -Vector3.forward);
                        moveDir.y = 0;
                        MainCamera.transform.position += _orthoSize * moveDir * 0.05f * ViewSensitivity.Value;
                    }*/


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
                                    if (StatMaster.isClient)
                                    {
                                        ModNetworking.SendToHost(AircraftControllerMsgReceiver.MouseRouteMsg.CreateMessage(
                                                                    CurrentLeader.Group.Value, worldPosition, (int)0, false));
                                    }
                                }
                                else if (Attack.IsHeld)
                                {
                                    // set ruler
                                    Ruler.SetActive(true);
                                    Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                    Ruler.transform.position = new Vector3(worldPosition.x, 20.5f, worldPosition.z);
                                    Vector3 prePosition = Vector3.zero;
                                    Vector2 direction = Vector2.zero;
                                    
                                    try
                                    {
                                        if (Routes[CurrentLeader.Group.Value].Count > 0)
                                        {
                                            prePosition = Routes[CurrentLeader.Group.Value].LastOrDefault().Position;
                                        }
                                        else
                                        {
                                            prePosition = CurrentLeader.transform.position;
                                        }
                                    }
                                    catch
                                    {
                                        prePosition = CurrentLeader.transform.position;
                                    }
                                    

                                    direction = MathTool.Get2DCoordinate(new Vector3(worldPosition.x, Constants.CruiseHeight + Constants.SeaHeight, worldPosition.z) - prePosition).normalized;
                                    RulerDir.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);

                                    float RouteDist = 0;
                                    RouteDist += MathTool.Get2DDistance(worldPosition, prePosition);
                                    RouteDist += CalculateRouteDist(CurrentLeader);
                                    float AircraftVel = 1000f;
                                    switch (CurrentLeader.Type.Value)
                                    {
                                        case 0:
                                            AircraftVel = 77f;
                                            break;
                                        case 1:
                                            AircraftVel = 59f;
                                            break;
                                        case 2:
                                            AircraftVel = 67f;
                                            break;
                                        case 3:
                                            AircraftVel = 67f;
                                            break;
                                        default:
                                            break;
                                    }
                                    float needTime = (RouteDist-200) / AircraftVel + 200f/40f;
                                    RulerSize.transform.localScale = Vector3.one / 250f * needTime * 5 * 0.514f * 2;
                                }
                                else if (Attack.IsReleased)
                                {
                                    Ruler.SetActive(false);
                                    Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                    AddRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    if (StatMaster.isClient)
                                    {
                                        ModNetworking.SendToHost(AircraftControllerMsgReceiver.MouseRouteMsg.CreateMessage(
                                                                    CurrentLeader.Group.Value, worldPosition, CurrentLeader.Type.Value, false));
                                    }
                                }
                            }
                            else
                            {
                                if (Input.GetMouseButtonDown(1))
                                {
                                    Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                    ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), 0);
                                    if (StatMaster.isClient)
                                    {
                                        ModNetworking.SendToHost(AircraftControllerMsgReceiver.MouseRouteMsg.CreateMessage(
                                                                    CurrentLeader.Group.Value, worldPosition, (int)0, true));
                                    }
                                }
                                else if (Attack.IsHeld)
                                {
                                    // set ruler
                                    Ruler.SetActive(true);
                                    Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                    Ruler.transform.position = new Vector3(worldPosition.x, 20.5f, worldPosition.z);
                                    Vector3 prePosition = Vector3.zero;
                                    Vector2 direction = Vector2.zero;
                                    prePosition = CurrentLeader.transform.position;
                                    
                                    direction = MathTool.Get2DCoordinate(new Vector3(worldPosition.x, Constants.CruiseHeight + Constants.SeaHeight, worldPosition.z) - prePosition).normalized;
                                    RulerDir.rotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.y), Vector3.up);

                                    float RouteDist = 0;
                                    RouteDist += MathTool.Get2DDistance(worldPosition, prePosition);
                                    float AircraftVel = 1000f;
                                    switch (CurrentLeader.Type.Value)
                                    {
                                        case 0:
                                            AircraftVel = 77f;
                                            break;
                                        case 1:
                                            AircraftVel = 59f;
                                            break;
                                        case 2:
                                            AircraftVel = 67f;
                                            break;
                                        case 3:
                                            AircraftVel = 67f;
                                            break;
                                        default:
                                            break;
                                    }
                                    float needTime = (RouteDist - 200) / AircraftVel + 200f / 40f;
                                    RulerSize.transform.localScale = Vector3.one / 250f * needTime * 5 * 0.514f * 2;
                                }
                                else if (Attack.IsReleased)
                                {
                                    Ruler.SetActive(false);
                                    Vector2 screenPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
                                    Vector3 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
                                    ResetRoutePoint(CurrentLeader.Group.Value, new Vector2(worldPosition.x, worldPosition.z), CurrentLeader.Type.Value);
                                    if (StatMaster.isClient)
                                    {
                                        ModNetworking.SendToHost(AircraftControllerMsgReceiver.MouseRouteMsg.CreateMessage(
                                                                    CurrentLeader.Group.Value, worldPosition, CurrentLeader.Type.Value, true));
                                    }
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

                                if (StatMaster.isClient)
                                {
                                    ModNetworking.SendToHost(AircraftControllerMsgReceiver.ReturnMsg.CreateMessage(CurrentLeader.Group.Value));
                                }
                            }
                            else
                            {
                                MyLogger.Instance.Log("[" + CurrentLeader.Group.Value + "] cannot return because not all aircraft in cruise", myPlayerID);
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


            }
            else if (!StatMaster.isClient) // host solve the msg from client
            {
                while(AircraftControllerMsgReceiver.Instance.mouseRouteInfo[myPlayerID].Count>0)
                {
                    AircraftControllerMsgReceiver.MouseRouteInfo info = AircraftControllerMsgReceiver.Instance.mouseRouteInfo[myPlayerID].Dequeue();
                    switch (info.reset)
                    {
                        case false:
                            AddRoutePoint(info.group, new Vector2(info.worldPos.x, info.worldPos.z), info.type);
                            break;
                        case true:
                            ResetRoutePoint(info.group, new Vector2(info.worldPos.x, info.worldPos.z), info.type);
                            break;
                        default:
                            break;
                    }
                }
                if (AircraftControllerMsgReceiver.Instance.returnPressed[myPlayerID].valid)
                {
                    AircraftControllerMsgReceiver.Instance.returnPressed[myPlayerID].valid = false;
                    string group = AircraftControllerMsgReceiver.Instance.returnPressed[myPlayerID].group;

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
                    MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] on Carrier", myPlayerID);
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
                    MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] in hangar", myPlayerID);
                }
                else
                {
                    if (CurrentLeader.myGroup.Count + FlightDataBase.Instance.Decks[myPlayerID].Occupied_num > FlightDataBase.Instance.Decks[myPlayerID].Total_num)
                    {
                        MyLogger.Instance.Log("Not enough space on deck for [" + CurrentLeader.Group.Value + "]", myPlayerID);
                        MyLogger.Instance.Log("Need: " + CurrentLeader.myGroup.Count.ToString() + ", Occupy: " +
                                                FlightDataBase.Instance.Decks[myPlayerID].Occupied_num.ToString() + "/" +
                                                FlightDataBase.Instance.Decks[myPlayerID].Total_num.ToString(), myPlayerID);
                    }
                    else
                    {
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
                        MyLogger.Instance.Log("Not all aircrafts of [" + CurrentLeader.Group.Value + "] are on board", myPlayerID);
                    }
                    else
                    {
                        if (CurrentLeader.isSeaplane)
                        {
                            if (Catapults.catapults.Count == 0)
                            {
                                MyLogger.Instance.Log("No available catapult, please install at least one!!");
                            }
                            else
                            {
                                foreach (var member in CurrentLeader.myGroup)
                                {
                                    Catapults.AddAircraft(member.Value);
                                }
                            }
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
            FlightDataBase.Instance.ClearDeckHangarLifter(myPlayerID);
            try
            {
                inTacticalView = false;
            }
            catch { }
            if (DrawBoard)
            {
                Destroy(DrawBoard);
            }
        }
        public void OnDestroy()
        {
            FlightDataBase.Instance.ClearDeckHangarLifter(myPlayerID);
            try
            {
                inTacticalView = false;
            }
            catch { }
            if (DrawBoard)
            {
                Destroy(DrawBoard);
            }
        }

        public void OnGUI()
        {
            try
            {
                if (StatMaster.hudHidden)
                {
                    return;
                }
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId != myPlayerID)
                    {
                        return;
                    }
                }
                if (CurrentLeader)
                {
                    GUI.Box(new Rect(100, 200, 200, 30), CurrentLeader.Group.Value.ToString() + " " + CurrentLeader.status.ToString());
                    //GUI.Box(new Rect(100, 300, 200, 30), FlightDataBase.Instance.Decks[myPlayerID].Occupied_num.ToString() + "/" + FlightDataBase.Instance.Decks[myPlayerID].Total_num.ToString());
                }
            }
            catch { }
        }

    }
}
