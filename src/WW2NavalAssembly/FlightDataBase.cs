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
using Modding.Blocks;

namespace WW2NavalAssembly
{
    public class ParkingSpot : MonoBehaviour
    {
        public bool occupied = false;
    }

    class FlightDataBase : SingleInstance<FlightDataBase>
    {
        public override string Name { get; } = "Flight Data Base";

        public AircraftController[] aircraftController = new AircraftController[16];

        public Vector2[] DeckForward = new Vector2[16];
        public Vector2[] DeckRight = new Vector2[16];
        public Dictionary<int, FlightDeck>[] AvailableDeckWood = new Dictionary<int, FlightDeck>[16];
        public Dictionary<int, FlightDeck>[] AvailableHangarWood = new Dictionary<int, FlightDeck>[16];
        public Deck[] Decks = new Deck[16];
        public float[] TakeOffPosition = new float[16];
        public Dictionary<string, Deck>[] Hangars = new Dictionary<string, Deck>[16];

        public GameObject[] DeckObjects = new GameObject[16];
        public Dictionary<string, GameObject>[] HangarObjects = new Dictionary<string, GameObject>[16];

        public GameObject[,] DeckLine = new GameObject[5, 16];
        public Dictionary<string, GameObject[]>[] HangarLine = new Dictionary<string, GameObject[]>[16];

        public List<Engine>[] engines = new List<Engine>[16];

        public Queue<Aircraft>[] LandingQueue = new Queue<Aircraft>[16];

        public Dictionary<int, AircraftLifter>[] MasterLifters = new Dictionary<int, AircraftLifter>[16];
        public Dictionary<int, AircraftLifter>[] SlaveLifters = new Dictionary<int, AircraftLifter>[16];

        public void CheckLandingQueue(int player)
        {
            Queue<Aircraft> newQ = new Queue<Aircraft>();
            while (LandingQueue[player].Count > 0)
            {
                Aircraft a = LandingQueue[player].Dequeue();
                if (a != null && (a.status == Aircraft.Status.Landing || a.status == Aircraft.Status.Returning))
                {
                    newQ.Enqueue(a);
                }
            }
            LandingQueue[player] = newQ;
        }

        
        float AIRCRAFT_HANGAR_WIDTH = 1.0f;
        float AIRCRAFT_LENGTH_HANGAR = 1.6f;
        float AIRCRAFT_DECK_WIDTH = 1.6f;
        float AIRCRAFT_LENGTH_DECK = 3f;

        public class Deck
        {
            public float AIRCRAFT_WIDTH = 1.6f;
            public float AIRCRAFT_LENGTH = 3f;

            public bool valid;
            public float Width;
            public float Length;
            public float height;
            public float RightMargin = 0.8f;

            public Vector2 Center;
            public Vector2 Forward;
            public Vector2 Right;
            public Vector2 Anchor;

            public int Length_num;
            public int Width_num;
            public int Total_num;

            public int Occupied_num = 0;

            public int Skip_num = 0;

            

            //public Vector3[] Corner = new Vector3[4];
            public Deck()
            {
                valid = false;
            }
            //brand new
            
            public Deck(Vector2 center, float width, float length, Vector2 forward, Vector2 right, float height, bool isHangar = false, int occupied_num = 0, int skip_num = 0)
            {
                if (isHangar)
                {
                    AIRCRAFT_LENGTH = 1.6f;
                    AIRCRAFT_WIDTH = 1.0f;
                }
                else
                {
                    AIRCRAFT_LENGTH = 3f;
                    AIRCRAFT_WIDTH = 1.6f;
                }

                valid = true;
                this.Center = center;
                this.Width = width;
                this.Length = length;
                this.Forward = forward;
                this.Right = right;
                this.height = height;
                this.Anchor = Center - Forward * Length / 2 + right * width / 2;

                this.Width_num = (int)((Width- 2*RightMargin) / AIRCRAFT_WIDTH) + ((Width - 2 * RightMargin>0)?1:0);
                this.Length_num = (int)((Length-(isHangar?3:10)) / AIRCRAFT_LENGTH) + 1;
                this.Skip_num = skip_num;
                this.Total_num = Width_num * Length_num - skip_num;

                this.RightMargin = (Width - (Width_num - 1) * AIRCRAFT_WIDTH) / 2f;
                this.Occupied_num = occupied_num;
            }
            //inherit
            public Deck(Vector2 center, float width, float length, Vector2 forward, Vector2 right, float height, int width_num, int length_num, bool isHangar = false,  int occupied_num = 0, int skip_num = 0)
            {
                if (isHangar)
                {
                    AIRCRAFT_LENGTH = 1.6f;
                    AIRCRAFT_WIDTH = 1.0f;
                }
                else
                {
                    AIRCRAFT_LENGTH = 3f;
                    AIRCRAFT_WIDTH = 1.6f;
                }

                valid = true;
                this.Center = center;
                this.Width = width;
                this.Length = length;
                this.Forward = forward;
                this.Right = right;
                this.height = height;
                this.Anchor = Center - Forward * Length / 2 + right * width / 2;

                this.Width_num = width_num;
                this.Length_num = length_num;
                this.Skip_num = skip_num;
                this.Total_num = Width_num * Length_num - skip_num;

                this.RightMargin = (Width - (Width_num - 1) * AIRCRAFT_WIDTH) / 2f;
                this.Occupied_num = occupied_num;
            }
            
        }
        public void GetTakeOffPosition(int playerID)
        {
            float res = 0;
            foreach (Transform spot in DeckObjects[playerID].transform.Find("Vis"))
            {
                if (spot.GetComponent<ParkingSpot>().occupied)
                {
                    res = Mathf.Max(res, spot.transform.localPosition.z);
                }
            }
            res += AIRCRAFT_LENGTH_DECK;
            TakeOffPosition[playerID] = res;
        }

        public void UpdateDeckTransform(int playerID)
        {
            if (Decks[playerID] == null || !Decks[playerID].valid)
            {
                return;
            }
            if (DeckObjects[playerID] == null)
            {
                return;
            }
            DeckObjects[playerID].transform.position = new Vector3(Decks[playerID].Anchor.x, Decks[playerID].height, Decks[playerID].Anchor.y);
            DeckObjects[playerID].transform.rotation = Quaternion.LookRotation(new Vector3(Decks[playerID].Forward.x, 0, Decks[playerID].Forward.y));
            DeckObjects[playerID].transform.eulerAngles = new Vector3(0, DeckObjects[playerID].transform.eulerAngles.y, 0);

            // for take off
            DeckObjects[playerID].transform.GetChild(1).localPosition = new Vector3(-Decks[playerID].Width / 2f, 0f, TakeOffPosition[playerID]);

        }
        public void UpdateHangarTransform(int playerID)
        {
            foreach (var hangarKey in Hangars[playerID].Keys)
            {
                if (Hangars[playerID][hangarKey] == null || !Hangars[playerID][hangarKey].valid || HangarObjects[playerID][hangarKey] == null)
                {
                    continue;
                }
                HangarObjects[playerID][hangarKey].transform.position = new Vector3(Hangars[playerID][hangarKey].Anchor.x, Hangars[playerID][hangarKey].height, Hangars[playerID][hangarKey].Anchor.y);
                HangarObjects[playerID][hangarKey].transform.rotation = Quaternion.LookRotation(new Vector3(Hangars[playerID][hangarKey].Forward.x, 30, Hangars[playerID][hangarKey].Forward.y));
                HangarObjects[playerID][hangarKey].transform.eulerAngles = new Vector3(0, HangarObjects[playerID][hangarKey].transform.eulerAngles.y, 0);
            }
        }

        public GameObject GenerateDeckOnStart(int playerID, Transform t)
        {
            if (t == null || !Decks[playerID].valid)
            {
                return null;
            }
            GameObject preObject = DeckObjects[playerID];

            DeckObjects[playerID] = new GameObject("DeckObject");
            DeckObjects[playerID].transform.parent = t;
            DeckObjects[playerID].transform.position = new Vector3(Decks[playerID].Anchor.x, Decks[playerID].height, Decks[playerID].Anchor.y);
            DeckObjects[playerID].transform.rotation = Quaternion.LookRotation(new Vector3(Decks[playerID].Forward.x, 0, Decks[playerID].Forward.y));
            DeckObjects[playerID].transform.eulerAngles = new Vector3(0, DeckObjects[playerID].transform.eulerAngles.y, 0);

            // for parking spot
            GameObject Vis = new GameObject("Vis");
            Vis.transform.parent = DeckObjects[playerID].transform;
            Vis.transform.localPosition = Vector3.zero;
            Vis.transform.localEulerAngles = Vector3.zero;

            int skipNum = 0;
            for (int i = 0; i < Decks[playerID].Total_num; i++)
            {
                Vector3 anchor = new Vector3(0,0,0);
                Vector3 right = Vector3.right;
                Vector3 forward = Vector3.forward;
                bool ForwardABit = (i % Decks[playerID].Width_num) % 2 == 1;
                Vector3 spotPos = anchor - right * Decks[playerID].RightMargin + forward * 2f
                                    - i % Decks[playerID].Width_num * AIRCRAFT_DECK_WIDTH * right
                                    + i / Decks[playerID].Width_num * AIRCRAFT_LENGTH_DECK * forward
                                    + (ForwardABit ? AIRCRAFT_LENGTH_DECK/2f : 0) * forward;

                bool CollideWithLifter = false;
                foreach (var lifter in MasterLifters[playerID])
                {
                    if (!lifter.Value)
                    {
                        continue;
                    }
                    //Debug.Log("Point" + Vis.transform.TransformPoint(spotPos));
                    //Debug.Log("Get Lifter"+ lifter.Value.Pos2D+ lifter.Value.Right2D+ lifter.Value.Size2D);
                    if (MathTool.pointInBox(MathTool.Get2DCoordinate(Vis.transform.TransformPoint(spotPos)), lifter.Value.Pos2D, lifter.Value.Right2D, lifter.Value.Size2D + new Vector2(0.5f, 0.5f)))
                    {
                        CollideWithLifter = true;
                    }
                    if (MathTool.pointInBox(MathTool.Get2DCoordinate(Vis.transform.TransformPoint(spotPos) - DeckObjects[playerID].transform.forward), lifter.Value.Pos2D, lifter.Value.Right2D, lifter.Value.Size2D + new Vector2(0.5f, 0.5f)))
                    {
                        CollideWithLifter = true;
                    }
                }
                foreach (var lifter in SlaveLifters[playerID])
                {
                    if (!lifter.Value)
                    {
                        continue;
                    }
                    //Debug.Log("Point" + Vis.transform.TransformPoint(spotPos));
                    //Debug.Log("Get Lifter"+ lifter.Value.Pos2D+ lifter.Value.Right2D+ lifter.Value.Size2D);
                    if (MathTool.pointInBox(MathTool.Get2DCoordinate(Vis.transform.TransformPoint(spotPos)), lifter.Value.Pos2D, lifter.Value.Right2D, lifter.Value.Size2D + new Vector2(0.5f, 0.5f)))
                    {
                        CollideWithLifter = true;
                    }
                    if (MathTool.pointInBox(MathTool.Get2DCoordinate(Vis.transform.TransformPoint(spotPos) - DeckObjects[playerID].transform.forward), lifter.Value.Pos2D, lifter.Value.Right2D, lifter.Value.Size2D + new Vector2(0.5f, 0.5f)))
                    {
                        CollideWithLifter = true;
                    }
                }

                if (CollideWithLifter)
                {
                    skipNum++;
                    continue;
                }

                GameObject parkingSpot = Instantiate(AssetManager.Instance.Aircraft.ParkingSpot);
                parkingSpot.name = "ParkingSpot-" + (i-skipNum).ToString();
                parkingSpot.transform.parent = Vis.transform;
                parkingSpot.AddComponent<ParkingSpot>();

                parkingSpot.transform.localPosition = spotPos;

                parkingSpot.transform.localEulerAngles = Vector3.zero;
            }
            Decks[playerID].Skip_num = skipNum;

            // for take off spot
            GameObject TakeOff = new GameObject("TakeOff");
            TakeOff.transform.parent = DeckObjects[playerID].transform;
            TakeOff.transform.localPosition = Vector3.zero;
            TakeOff.transform.localEulerAngles = Vector3.zero;

            GameObject takeoffSpot = Instantiate(AssetManager.Instance.Aircraft.TakeOffSpot);
            takeoffSpot.name = "Take Off Spot";
            takeoffSpot.transform.parent = TakeOff.transform;
            takeoffSpot.transform.localPosition = Vector3.zero;
            takeoffSpot.transform.localEulerAngles = Vector3.zero;

            GetTakeOffPosition(playerID);

            if (preObject != null)
            {
                Destroy(preObject);
            }

            return DeckObjects[playerID];
        }

        
        public GameObject GenerateHangarOnStart(int playerID, Transform t)
        {
            if (t == null)
            {
                return null;
            }

            GameObject rootHangar = new GameObject("Hangar root");
            rootHangar.transform.parent = t;
            
            foreach (var hangar in Hangars[playerID])
            {
                if (!hangar.Value.valid)
                {
                    if (HangarLine[playerID].ContainsKey(hangar.Key))
                    {
                        HangarLine[playerID].Remove(hangar.Key);
                    }
                    continue;
                }

                GameObject preObject;
                if (HangarObjects[playerID].ContainsKey(hangar.Key))
                {
                    preObject = HangarObjects[playerID][hangar.Key];
                }
                else
                {
                    HangarObjects[playerID].Add(hangar.Key, new GameObject());
                    preObject = null;
                }


                HangarObjects[playerID][hangar.Key] = new GameObject("HangarObject (" + hangar.Key + ")");
                HangarObjects[playerID][hangar.Key].transform.parent = rootHangar.transform;
                HangarObjects[playerID][hangar.Key].transform.position = new Vector3(hangar.Value.Anchor.x, hangar.Value.height, hangar.Value.Anchor.y);
                HangarObjects[playerID][hangar.Key].transform.rotation = Quaternion.LookRotation(new Vector3(hangar.Value.Forward.x, 0, hangar.Value.Forward.y));
                HangarObjects[playerID][hangar.Key].transform.eulerAngles = new Vector3(0, HangarObjects[playerID][hangar.Key].transform.eulerAngles.y, 0);

                GameObject Vis = new GameObject("Vis");
                Vis.transform.parent = HangarObjects[playerID][hangar.Key].transform;
                Vis.transform.localPosition = Vector3.zero;
                Vis.transform.localEulerAngles = Vector3.zero;

                for (int i = 0; i < hangar.Value.Total_num; i++)
                {
                    GameObject parkingSpot = Instantiate(AssetManager.Instance.Aircraft.ParkingSpot);
                    parkingSpot.name = "ParkingSpot-" + i.ToString();
                    parkingSpot.transform.parent = Vis.transform;
                    parkingSpot.AddComponent<ParkingSpot>();

                    Vector3 anchor = new Vector3(0, 0f, 0);
                    Vector3 right = Vector3.right;
                    Vector3 forward = Vector3.forward;
                    bool ForwardABit = (i % hangar.Value.Width_num) % 2 == 1;
                    Vector3 spotPos = anchor - right * (hangar.Value.RightMargin - 0.2f) + forward * 2f
                                        - i % hangar.Value.Width_num * AIRCRAFT_HANGAR_WIDTH * right
                                        + i / hangar.Value.Width_num * AIRCRAFT_LENGTH_HANGAR * forward;

                    parkingSpot.transform.localPosition = spotPos;

                    parkingSpot.transform.localEulerAngles = new Vector3(0, 37, 0);
                }


                if (preObject != null)
                {
                    Destroy(preObject);
                }
            }

            return rootHangar;
        }

        public void AddDeck(int playerID, int guid, FlightDeck deck)
        {
            if (AvailableDeckWood[playerID].ContainsKey(guid))
            {
                AvailableDeckWood[playerID][guid] = deck;
            }
            else
            {
                AvailableDeckWood[playerID].Add(guid, deck);
            }
        }
        public void AddHangar(int playerID, int guid, FlightDeck deck)
        {
            if (AvailableHangarWood[playerID].ContainsKey(guid))
            {
                AvailableHangarWood[playerID][guid] = deck;
            }
            else
            {
                AvailableHangarWood[playerID].Add(guid, deck);
            }
        }

        public void AddLifter(int playerID, int guid, AircraftLifter lifter)
        {
            if (MasterLifters[playerID].ContainsKey(guid))
            {
                MasterLifters[playerID][guid] = lifter;
            }
            else
            {
                MasterLifters[playerID].Add(guid , lifter);
            }
        }

        public void AddSlaveLifter(int playerID, int guid, AircraftLifter lifter)
        {
            if (SlaveLifters[playerID].ContainsKey(guid))
            {
                SlaveLifters[playerID][guid] = lifter;
            }
            else
            {
                SlaveLifters[playerID].Add(guid, lifter);
            }
        }
        public void RemoveLifter(int playerID, int guid)
        {
            if (MasterLifters[playerID].ContainsKey(guid))
            {
                MasterLifters[playerID].Remove(guid);
            }
        }
        public void RemoveSlaveLifter(int playerID, int guid)
        {
            if (SlaveLifters[playerID].ContainsKey(guid))
            {
                SlaveLifters[playerID].Remove(guid);
            }
        }
        public void ShowDeckHangarVis(int playerID)
        {
            if (ModController.Instance.ShowArmour)
            {
                // ================================= flight deck =================================
                if (Decks[playerID].valid)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        DeckLine[i, playerID].SetActive(true);
                    }
                    {   
                        // horizental box
                        LineRenderer DLLR = DeckLine[0, playerID].GetComponent<LineRenderer>();
                        Vector2 forward = Decks[playerID].Forward;
                        Vector2 right = new Vector2(forward.y, -forward.x);
                        Vector2 corner2D = Decks[playerID].Center + forward * Decks[playerID].Length / 2 - right * Decks[playerID].Width / 2;
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));
                        corner2D = Decks[playerID].Center + forward * Decks[playerID].Length / 2 + right * Decks[playerID].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));

                        DLLR = DeckLine[1, playerID].GetComponent<LineRenderer>();
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));
                        corner2D = Decks[playerID].Center - forward * Decks[playerID].Length / 2 + right * Decks[playerID].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));

                        DLLR = DeckLine[2, playerID].GetComponent<LineRenderer>();
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));
                        corner2D = Decks[playerID].Center - forward * Decks[playerID].Length / 2 - right * Decks[playerID].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));

                        DLLR = DeckLine[3, playerID].GetComponent<LineRenderer>();
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));
                        corner2D = Decks[playerID].Center + forward * Decks[playerID].Length / 2 - right * Decks[playerID].Width / 2;
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Decks[playerID].height, corner2D.y));

                        // verticle line
                        DLLR = DeckLine[4, playerID].GetComponent<LineRenderer>();
                        DLLR.SetPosition(1, new Vector3(Decks[playerID].Anchor.x, Decks[playerID].height, Decks[playerID].Anchor.y));
                        DLLR.SetPosition(0, new Vector3(Decks[playerID].Anchor.x, Decks[playerID].height+3, Decks[playerID].Anchor.y));
                    }// set line position
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        DeckLine[i, playerID].SetActive(false);
                    }
                }

                // ================================= hangar =================================
                foreach (var hangarGroup in Hangars[playerID])
                {
                    if (!Hangars[playerID][hangarGroup.Key].valid)
                    {
                        continue;
                    }

                    // add hangar line object if key not exist
                    if (!HangarLine[playerID].ContainsKey(hangarGroup.Key))
                    {
                        HangarLine[playerID].Add(hangarGroup.Key, new GameObject[5]);
                        for (int i = 0; i < 5; i++)
                        {
                            HangarLine[playerID][hangarGroup.Key][i] = new GameObject("HangarLine-" + i.ToString() + " (" + hangarGroup.Key + ")");
                            HangarLine[playerID][hangarGroup.Key][i].transform.parent = transform;
                            LineRenderer DLLR = HangarLine[playerID][hangarGroup.Key][i].AddComponent<LineRenderer>();
                            DLLR.material = new Material(Shader.Find("Particles/Additive"));

                            DLLR.SetColors(Color.white, Color.gray);

                            DLLR.SetWidth(0.2f, 0.2f);
                            HangarLine[playerID][hangarGroup.Key][i].SetActive(false);
                        }

                    }

                    foreach (var hangarLine in HangarLine[playerID][hangarGroup.Key])
                    {
                        hangarLine.SetActive(true);
                    }// turn on hangar line

                    {
                        // horizental box
                        LineRenderer DLLR = HangarLine[playerID][hangarGroup.Key][0].GetComponent<LineRenderer>();
                        Vector2 forward = Hangars[playerID][hangarGroup.Key].Forward;
                        Vector2 right = new Vector2(forward.y, -forward.x);
                        Vector2 corner2D =  Hangars[playerID][hangarGroup.Key].Center 
                                            + forward * Hangars[playerID][hangarGroup.Key].Length / 2 
                                            - right * Hangars[playerID][hangarGroup.Key].Width / 2;
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));
                        corner2D = Hangars[playerID][hangarGroup.Key].Center + forward * Hangars[playerID][hangarGroup.Key].Length / 2 + right * Hangars[playerID][hangarGroup.Key].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));

                        DLLR = HangarLine[playerID][hangarGroup.Key][1].GetComponent<LineRenderer>();
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));
                        corner2D = Hangars[playerID][hangarGroup.Key].Center - forward * Hangars[playerID][hangarGroup.Key].Length / 2 + right * Hangars[playerID][hangarGroup.Key].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));

                        DLLR = HangarLine[playerID][hangarGroup.Key][2].GetComponent<LineRenderer>();
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));
                        corner2D = Hangars[playerID][hangarGroup.Key].Center - forward * Hangars[playerID][hangarGroup.Key].Length / 2 - right * Hangars[playerID][hangarGroup.Key].Width / 2;
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));

                        DLLR = HangarLine[playerID][hangarGroup.Key][3].GetComponent<LineRenderer>();
                        DLLR.SetPosition(1, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));
                        corner2D = Hangars[playerID][hangarGroup.Key].Center + forward * Hangars[playerID][hangarGroup.Key].Length / 2 - right * Hangars[playerID][hangarGroup.Key].Width / 2;
                        DLLR.SetPosition(0, new Vector3(corner2D.x, Hangars[playerID][hangarGroup.Key].height, corner2D.y));

                        // verticle line
                        DLLR = HangarLine[playerID][hangarGroup.Key][4].GetComponent<LineRenderer>();
                        DLLR.SetPosition(1, new Vector3(Hangars[playerID][hangarGroup.Key].Anchor.x, Hangars[playerID][hangarGroup.Key].height, Hangars[playerID][hangarGroup.Key].Anchor.y));
                        DLLR.SetPosition(0, new Vector3(Hangars[playerID][hangarGroup.Key].Anchor.x, Hangars[playerID][hangarGroup.Key].height + 1.5f, Hangars[playerID][hangarGroup.Key].Anchor.y));
                    }// set line position
                }
                // clear the unused line object
                Stack<string> removeKey = new Stack<string>();
                foreach (var hangarGroupLine in HangarLine[playerID])
                {
                    if (!Hangars[playerID].ContainsKey(hangarGroupLine.Key))
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Destroy(hangarGroupLine.Value[i]);
                        }
                        removeKey.Push(hangarGroupLine.Key);// cannot directly remove the element in the dictionary in the foreach loop
                    }
                }
                foreach (var key in removeKey)
                {
                    HangarLine[playerID].Remove(key);
                }


            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    DeckLine[i, playerID].SetActive(false);
                }// turn off deck line
                foreach (var hangarGroupLine in HangarLine[playerID])
                {
                    foreach (var hangarLine in hangarGroupLine.Value)
                    {
                        hangarLine.SetActive(false);
                    }
                }// turn off hangar line
            }
        }
        public Deck CalculateDeck(int playerID, bool inherit = false)
        {
            List<int> tobeRemoved = new List<int>();

            float orien = MathTool.SignedAngle(new Vector2(0, 1), DeckForward[playerID]);
            Vector2[] deckCorners = new Vector2[4];
            bool cornersInitialized = false;
            float height = float.MinValue;

            foreach (var deck in AvailableDeckWood[playerID])
            {
                try
                {
                    if (deck.Value != null && deck.Value.WoodType.Selection == "Flight Deck")
                    {
                        WoodenArmour WA = deck.Value.GetComponent<WoodenArmour>();
                        Vector3 scale = WA.VisRef.transform.lossyScale / 2;
                        Vector3 p = WA.VisRef.transform.position;
                        // calculation
                        Vector3 forward = deck.Value.transform.forward;
                        Vector3 up = deck.Value.transform.up;
                        Vector3 right = deck.Value.transform.right;

                        Vector2[] newPoints = new Vector2[8];
                        {
                            newPoints[0] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z + up * scale.y + right * scale.x), orien);
                            newPoints[1] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z + up * scale.y - right * scale.x), orien);
                            newPoints[2] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z - up * scale.y + right * scale.x), orien);
                            newPoints[3] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z - up * scale.y - right * scale.x), orien);
                            newPoints[4] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z + up * scale.y + right * scale.x), orien);
                            newPoints[5] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z + up * scale.y - right * scale.x), orien);
                            newPoints[6] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z - up * scale.y + right * scale.x), orien);
                            newPoints[7] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z - up * scale.y - right * scale.x), orien);
                        }

                        if (!cornersInitialized)
                        {
                            cornersInitialized = true;
                            for (int i = 0; i < 4; i++)
                            {
                                deckCorners[i] = newPoints[0];
                            }
                        }

                        if (p.y > height)
                        {
                            height = p.y;
                        }

                        for (int i = 0; i < 8; i++)
                        {
                            if (newPoints[i].x <= deckCorners[0].x
                                && newPoints[i].x <= deckCorners[1].x
                                && newPoints[i].x <= deckCorners[2].x
                                && newPoints[i].x <= deckCorners[3].x)
                            {
                                deckCorners[3] = newPoints[i];
                            }
                            if (newPoints[i].x >= deckCorners[0].x
                                && newPoints[i].x >= deckCorners[1].x
                                && newPoints[i].x >= deckCorners[2].x
                                && newPoints[i].x >= deckCorners[3].x)
                            {
                                deckCorners[1] = newPoints[i];
                            }
                            if (newPoints[i].y <= deckCorners[0].y
                                && newPoints[i].y <= deckCorners[1].y
                                && newPoints[i].y <= deckCorners[2].y
                                && newPoints[i].y <= deckCorners[3].y)
                            {
                                deckCorners[2] = newPoints[i];
                            }
                            if (newPoints[i].y >= deckCorners[0].y
                                && newPoints[i].y >= deckCorners[1].y
                                && newPoints[i].y >= deckCorners[2].y
                                && newPoints[i].y >= deckCorners[3].y)
                            {
                                deckCorners[0] = newPoints[i];
                            }
                        }
                    }
                    else
                    {
                        tobeRemoved.Add(deck.Key);
                    }
                }
                catch {
                }
                
            }

            // delete invalid deck
            foreach (var deck in tobeRemoved)
            {
                AvailableDeckWood[playerID].Remove(deck);
            }

            if (!cornersInitialized)
            {
                return new Deck();  // no deck wood detected
            }
            else
            {
                int occupied_num = Decks[playerID].Occupied_num;
                
                float width = Mathf.Abs(deckCorners[1].x - deckCorners[3].x);
                float length = Mathf.Abs(deckCorners[0].y - deckCorners[2].y);
                Vector2 forwardLeft = new Vector2(deckCorners[3].x, deckCorners[0].y);
                Vector2 center = forwardLeft + new Vector2(width / 2, -length / 2);
                if (inherit)
                {
                    int width_num = Decks[playerID].Width_num;
                    int length_num = Decks[playerID].Length_num;
                    int skipNum = Decks[playerID].Skip_num;
                    return new Deck(MathTool.PointRotate(Vector2.zero, center, -orien), width, length, DeckForward[playerID], DeckRight[playerID], height, width_num, length_num, false, occupied_num, skipNum);
                }
                else
                {
                    return new Deck(MathTool.PointRotate(Vector2.zero, center, -orien), width, length, DeckForward[playerID], DeckRight[playerID], height, false, occupied_num);
                }
                
            }
        }
        public void UpdateDeck(int playerID, bool inherit = false)
        {
            Decks[playerID] = CalculateDeck(playerID, inherit);
        }
        public void UpdateHangar(int playerID)
        {
            CalculateHangar(playerID);
        }
        public void ClearDeckHangarLifter(int playerID)
        {
            Decks[playerID] = new Deck();
            Hangars[playerID] = new Dictionary<string, Deck>();
            MasterLifters[playerID] = new Dictionary<int, AircraftLifter>();
        }
        public void CalculateHangar(int playerID)
        {
            List<int> tobeRemoved = new List<int>();

            Dictionary<string, List<FlightDeck>> hangarGroups = new Dictionary<string, List<FlightDeck>>(); // for different group

            // group into subgroups
            foreach (var hangar in AvailableHangarWood[playerID])
            {
                if (hangar.Value != null && hangar.Value.WoodType.Selection == "Hangar")
                {
                    if (hangarGroups.ContainsKey(hangar.Value.HangarGroup.Value))
                    {
                        hangarGroups[hangar.Value.HangarGroup.Value].Add(hangar.Value);
                    }
                    else
                    {
                        hangarGroups.Add(hangar.Value.HangarGroup.Value, new List<FlightDeck> { hangar.Value});
                    }
                }
                else
                {
                    tobeRemoved.Add(hangar.Key);
                }
            }
            // delete invalid hangar
            foreach (var hangarGuid in tobeRemoved)
            {
                AvailableHangarWood[playerID].Remove(hangarGuid);
            }

            // save occupied number of hangar
            Dictionary<string, int> hangarOccupied = new Dictionary<string, int>();
            foreach (var h in Hangars[playerID])
            {
                hangarOccupied.Add(h.Key, h.Value.Occupied_num);
            }

            // reset Hangars
            Hangars[playerID] = new Dictionary<string, Deck>();

            foreach (var hangarGroup in hangarGroups)
            {
                float orien = MathTool.SignedAngle(new Vector2(0, 1), DeckForward[playerID]);
                Vector2[] deckCorners = new Vector2[4];
                bool cornersInitialized = false;
                float height = float.MinValue;

                foreach (var hangar in hangarGroup.Value)
                {
                    try
                    {
                        WoodenArmour WA = hangar.GetComponent<WoodenArmour>();
                        Vector3 scale = WA.VisRef.transform.lossyScale / 2;
                        Vector3 p = WA.VisRef.transform.position;
                        // calculation
                        Vector3 forward = hangar.transform.forward;
                        Vector3 up = hangar.transform.up;
                        Vector3 right = hangar.transform.right;

                        Vector2[] newPoints = new Vector2[8];
                        {
                            newPoints[0] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z + up * scale.y + right * scale.x), orien);
                            newPoints[1] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z + up * scale.y - right * scale.x), orien);
                            newPoints[2] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z - up * scale.y + right * scale.x), orien);
                            newPoints[3] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p + forward * scale.z - up * scale.y - right * scale.x), orien);
                            newPoints[4] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z + up * scale.y + right * scale.x), orien);
                            newPoints[5] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z + up * scale.y - right * scale.x), orien);
                            newPoints[6] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z - up * scale.y + right * scale.x), orien);
                            newPoints[7] = MathTool.PointRotate(Vector2.zero, MathTool.Get2DCoordinate(p - forward * scale.z - up * scale.y - right * scale.x), orien);
                        }

                        if (!cornersInitialized)
                        {
                            cornersInitialized = true;
                            for (int i = 0; i < 4; i++)
                            {
                                deckCorners[i] = newPoints[0];
                            }
                        }

                        if (p.y > height)
                        {
                            height = p.y;
                        }

                        for (int i = 0; i < 8; i++)
                        {
                            if (newPoints[i].x <= deckCorners[0].x
                                && newPoints[i].x <= deckCorners[1].x
                                && newPoints[i].x <= deckCorners[2].x
                                && newPoints[i].x <= deckCorners[3].x)
                            {
                                deckCorners[3] = newPoints[i];
                            }
                            if (newPoints[i].x >= deckCorners[0].x
                                && newPoints[i].x >= deckCorners[1].x
                                && newPoints[i].x >= deckCorners[2].x
                                && newPoints[i].x >= deckCorners[3].x)
                            {
                                deckCorners[1] = newPoints[i];
                            }
                            if (newPoints[i].y <= deckCorners[0].y
                                && newPoints[i].y <= deckCorners[1].y
                                && newPoints[i].y <= deckCorners[2].y
                                && newPoints[i].y <= deckCorners[3].y)
                            {
                                deckCorners[2] = newPoints[i];
                            }
                            if (newPoints[i].y >= deckCorners[0].y
                                && newPoints[i].y >= deckCorners[1].y
                                && newPoints[i].y >= deckCorners[2].y
                                && newPoints[i].y >= deckCorners[3].y)
                            {
                                deckCorners[0] = newPoints[i];
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                if (!cornersInitialized)
                {
                    Hangars[playerID].Add(hangarGroup.Key, new Deck());
                }
                else
                {
                    float width = Mathf.Abs(deckCorners[1].x - deckCorners[3].x);
                    float length = Mathf.Abs(deckCorners[0].y - deckCorners[2].y);
                    Vector2 forwardLeft = new Vector2(deckCorners[3].x, deckCorners[0].y);
                    Vector2 center = forwardLeft + new Vector2(width / 2, -length / 2);
                    Hangars[playerID].Add(  hangarGroup.Key, 
                                            new Deck(MathTool.PointRotate(Vector2.zero, center, -orien), 
                                            width, 
                                            length, 
                                            DeckForward[playerID], 
                                            DeckRight[playerID], 
                                            height,
                                            true,
                                            (hangarOccupied.ContainsKey(hangarGroup.Key) ? hangarOccupied[hangarGroup.Key]:0))
                                            );
                }
            }



        }
        public void InitLine()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    DeckLine[i, j] = new GameObject("DeckLine-" + i.ToString() + " (" + j.ToString() + ")");
                    DeckLine[i, j].transform.parent = transform;
                    LineRenderer DLLR = DeckLine[i, j].AddComponent<LineRenderer>();
                    DLLR.material = new Material(Shader.Find("Particles/Additive"));
                    if (i == 0)
                    {
                        DLLR.SetColors(Color.yellow, Color.yellow);
                    }
                    else if (i == 2)
                    {
                        DLLR.SetColors(new Color(0.6f, 0, 0.6f), new Color(0.6f, 0, 0.6f));
                    }
                    else
                    {
                        DLLR.SetColors(Color.yellow, new Color(0.6f, 0, 0.6f));
                    }

                    DLLR.SetWidth(0.3f, 0.3f);
                    DeckLine[i, j].SetActive(false);
                }
            }
        }
        public FlightDataBase()
        {
            for (int i = 0; i < 16; i++)
            {
                aircraftController[i] = null;
                DeckForward[i] = new Vector2(1, 0);
                DeckRight[i] = new Vector2(0, 1);
                AvailableDeckWood[i] = new Dictionary<int, FlightDeck>();
                AvailableHangarWood[i] = new Dictionary<int, FlightDeck>();
                HangarObjects[i] = new Dictionary<string, GameObject>();
                Hangars[i] = new Dictionary<string, Deck>();
                HangarLine[i] = new Dictionary<string, GameObject[]>();
                Decks[i] = new Deck();
                engines[i] = new List<Engine>();
                LandingQueue[i] = new Queue<Aircraft>();
                MasterLifters[i] = new Dictionary<int, AircraftLifter>();
                SlaveLifters[i] = new Dictionary<int, AircraftLifter>();
            }
            InitLine();
        }

        public void FixedUpdate()
        {
            if (StatMaster.isMP)
            {
                if (!StatMaster.isClient)
                {
                    ShowDeckHangarVis(0);
                }
                else
                {
                    ShowDeckHangarVis(PlayerData.localPlayer.networkId);
                }
            }
            else
            {
                ShowDeckHangarVis(0);
            }
        }

        public void OnGUI()
        {
            if (Decks[0].valid)
            {
                //GUI.Box(new Rect(100, 300, 250, 50), AvailableDeckWood[0].Count.ToString());
            }
            //ShowDeckParkingSpotOnGUI(0);
            
        }

    }
}
