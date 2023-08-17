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

namespace WW2NavalAssembly
{
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
            Aircraft a = UpQueue.Count>0? UpQueue.Dequeue():null;
            if (a && a.status == Aircraft.Status.InHangar)
            {
                a.FindDeck();
                Debug.Log("elevator lift aircraft: " + a.myGuid + "...");
                yield return new WaitForSeconds(delayTime);
                a.SwitchToOnBoard();
                //Debug.Log("Finish");
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
                Debug.Log("elevator drop aircraft: " + a.myGuid + "...");
                yield return new WaitForSeconds(delayTime);
                a.SwitchToInHangar();
                //Debug.Log("Finish");
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
        public float delayTime = 0.3f;

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
                Debug.Log("move aircraft: " + a.myGuid + " to take off spot...");
                yield return new WaitForSeconds(delayTime);
                a.ChangeDeckSpot(FlightDataBase.Instance.DeckObjects[a.myPlayerID].transform.Find("TakeOff").GetChild(0), true);
                Debug.Log("taking off ...");
                yield return new WaitForSeconds(delayTime);
                a.SwitchToTakingOff();
                Debug.Log("Finish take off");
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

    class AircraftController : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey ReturnKey;
        public MKey TakeOffKey;
        public MKey ElevatorUp;
        public MKey ElevatorDown;

        public GameObject DeckVis;
        public GameObject HangarVis;

        public bool hasDeck = false;
        public bool hasHangar = false;

        public Aircraft CurrentLeader;
        public AircraftElevator Elevator;
        public AircraftRunway Runway;

        public override void SafeAwake()
        {
            gameObject.name = "Aircraft Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);


            ReturnKey = AddKey("Aircraft Return", "ReturnKey", KeyCode.Backspace);
            TakeOffKey = AddKey("Aircraft Take Off", "TakeOffKey", KeyCode.Q);
            ElevatorUp = AddKey("Aircraft Elevator Up", "ElevatorUp", KeyCode.UpArrow);
            ElevatorDown = AddKey("Aircraft Elevator Down", "ElevatorDown", KeyCode.DownArrow);
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


            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
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
                    DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                    HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
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
                DeckVis = FlightDataBase.Instance.GenerateDeckOnStart(myPlayerID, transform.parent);
                HangarVis = FlightDataBase.Instance.GenerateHangarOnStart(myPlayerID, transform.parent);
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
        }

        public override void SimulateFixedUpdateAlways()
        {
            if (hasDeck)
            {
                // local frequency
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
                // local frequency
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
            if(hasDeck)
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
                
                foreach (var aircraft in CurrentLeader.myGroup)
                {
                    Elevator.AddDownQueue(aircraft.Value);
                }
            }

            if (ElevatorUp.IsPressed && CurrentLeader)
            {
                if (CurrentLeader.myGroup.Count + FlightDataBase.Instance.Decks[myPlayerID].Occupied_num > FlightDataBase.Instance.Decks[myPlayerID].Total_num)
                {
                    Debug.Log("Not enough space on deck for the entire group");
                }
                else
                {
                    FlightDataBase.Instance.Decks[myPlayerID].Occupied_num += CurrentLeader.myGroup.Count;
                    foreach (var aircraft in CurrentLeader.myGroup)
                    {
                        Elevator.AddUpQueue(aircraft.Value);
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
                        Debug.Log("Not all aircrafts are on board");
                    }
                    else
                    {
                        foreach (var member in CurrentLeader.myGroup.Reverse())
                        {
                            Runway.AddAircraft(member.Value);
                        }
                    }
                }
            }

        }


        public override void OnSimulateStop()
        {
            foreach (var hangar in FlightDataBase.Instance.Hangars[myPlayerID])
            {
                hangar.Value.Occupied_num = 0;
            }
            FlightDataBase.Instance.Decks[myPlayerID].Occupied_num = 0;
        }

        public void OnGUI()
        {
            if (CurrentLeader)
            {
                GUI.Box(new Rect(100, 200, 200, 30), CurrentLeader.Group.Value.ToString());
                GUI.Box(new Rect(100, 250, 200, 30), CurrentLeader.myGroup.Count.ToString());
            }
            //GUI.Box(new Rect(100, 300, 200, 30), FlightDataBase.Instance.TakeOffPosition[myPlayerID].ToString());


        }

    }
}
