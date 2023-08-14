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
    class FlightDataBase : SingleInstance<FlightDataBase>
    {
        public override string Name { get; } = "Flight Data Base";

        public Vector2[] DeckForward = new Vector2[16];
        public Vector2[] DeckRight = new Vector2[16];
        public Dictionary<int, FlightDeck>[] AvailableDeckWood = new Dictionary<int, FlightDeck>[16];
        public Dictionary<int, FlightDeck>[] AvailableHangarWood = new Dictionary<int, FlightDeck>[16];
        public Deck[] Decks = new Deck[16];
        public Dictionary<string, Deck>[] Hangars = new Dictionary<string, Deck>[16];

        public GameObject[] DeckObjects = new GameObject[16];
        public Dictionary<string, GameObject>[] HangarObjects = new Dictionary<string, GameObject>[16];

        public GameObject[,] DeckLine = new GameObject[5, 16];
        public Dictionary<string, GameObject[]>[] HangarLine = new Dictionary<string, GameObject[]>[16];

        public Texture GunnerAlertIcon;
        int iconSize = 30;

        float AIRCRAFT_WIDTH = 1.5f;
        float AIRCRAFT_LENGTH = 2.4f;

        public class Deck
        {
            public bool valid;
            public float Width;
            public float Length;
            public float height;
            public float RightMargin = 0.6f;

            public Vector2 Center;
            public Vector2 Forward;
            public Vector2 Right;
            public Vector2 Anchor;

            public int Length_num;
            public int Width_num;
            public int Total_num;

            //public Vector3[] Corner = new Vector3[4];
            public Deck()
            {
                valid = false;
            }
            public Deck(Vector2 center, float width, float length, Vector2 forward, Vector2 right, float height)
            {
                valid = true;
                this.Center = center;
                this.Width = width;
                this.Length = length;
                this.Forward = forward;
                this.Right = right;
                this.height = height;
                this.Anchor = Center - Forward * Length / 2 + right * width / 2;

                this.Width_num = (int)((Width- 1.2f) / 2f) + 1;
                this.Length_num = (int)((Length-10) / 3f);
                this.Total_num = Width_num * Length_num;

                this.RightMargin = (Width - (Width_num - 1) * 1.5f) / 2f;
            }
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
        }
        public void UpdateHangarTransform(int playerID)
        {
            foreach (var hangarKey in Hangars[playerID].Keys)
            {
                if (Hangars[playerID][hangarKey] == null || !Hangars[playerID][hangarKey].valid || HangarObjects[playerID][hangarKey] == null)
                {
                    continue;
                }
                //...
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

            GameObject Vis = new GameObject("Vis");
            Vis.transform.parent = DeckObjects[playerID].transform;
            Vis.transform.localPosition = Vector3.zero;
            Vis.transform.localEulerAngles = Vector3.zero;

            for (int i = 0; i < Decks[playerID].Total_num; i++)
            {
                GameObject parkingSpot = Instantiate(AssetManager.Instance.Aircraft.ParkingSpot);
                parkingSpot.name = "ParkingSpot-" + i.ToString();
                parkingSpot.transform.parent = Vis.transform;

                Vector3 anchor = new Vector3(0,0.3f,0);
                Vector3 right = Vector3.right;
                Vector3 forward = Vector3.forward;
                bool ForwardABit = (i % Decks[playerID].Width_num) % 2 == 1;
                Vector3 spotPos = anchor - right * Decks[playerID].RightMargin + forward * 5f
                                    - i % Decks[playerID].Width_num * AIRCRAFT_WIDTH * right
                                    + i / Decks[playerID].Width_num * AIRCRAFT_LENGTH * forward
                                    + (ForwardABit ? AIRCRAFT_LENGTH/3f : 0) * forward;

                parkingSpot.transform.localPosition = spotPos;

                parkingSpot.transform.localEulerAngles = Vector3.zero;
            }


            if (preObject != null)
            {
                Destroy(preObject);
            }

            return DeckObjects[playerID];
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

        public void ShowDeckHangarVis(int playerID)
        {
            if (ModController.Instance.showArmour)
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
                            if (i == 0)
                            {
                                DLLR.SetColors(Color.white, Color.white);
                            }
                            else if (i == 2)
                            {
                                DLLR.SetColors(Color.gray, Color.gray);
                            }
                            else
                            {
                                DLLR.SetColors(Color.white, Color.gray);
                            }

                            DLLR.SetWidth(0.5f, 0.5f);
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

        public Deck CalculateDeck(int playerID)
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
                float width = Mathf.Abs(deckCorners[1].x - deckCorners[3].x);
                float length = Mathf.Abs(deckCorners[0].y - deckCorners[2].y);
                Vector2 forwardLeft = new Vector2(deckCorners[3].x, deckCorners[0].y);
                Vector2 center = forwardLeft + new Vector2(width / 2, -length / 2);
                return new Deck(MathTool.PointRotate(Vector2.zero,center,-orien), width, length, DeckForward[playerID], DeckRight[playerID], height);
            }
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
                                            height));
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

                    DLLR.SetWidth(0.5f, 0.5f);
                    DeckLine[i, j].SetActive(false);
                }
            }
        }
        public FlightDataBase()
        {
            for (int i = 0; i < 16; i++)
            {
                DeckForward[i] = new Vector2(1, 0);
                DeckRight[i] = new Vector2(0, 1);
                AvailableDeckWood[i] = new Dictionary<int, FlightDeck>();
                AvailableHangarWood[i] = new Dictionary<int, FlightDeck>();
                HangarObjects[i] = new Dictionary<string, GameObject>();
                Hangars[i] = new Dictionary<string, Deck>();
                HangarLine[i] = new Dictionary<string, GameObject[]>();
                Decks[i] = new Deck();
            }
            InitLine();
            GunnerAlertIcon = ModResource.GetTexture("gunnerAlert Texture").Texture;
        }

        public void FixedUpdate()
        {
            if (StatMaster.isMP)
            {
                if (!StatMaster.isClient)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        Decks[i] = CalculateDeck(i);
                    }
                    for (int i = 0;i < 16; i++)
                    {
                        CalculateHangar(i);
                    }
                    ShowDeckHangarVis(0);
                }
                else
                {
                    Decks[PlayerData.localPlayer.networkId] = CalculateDeck(PlayerData.localPlayer.networkId);
                    CalculateHangar(PlayerData.localPlayer.networkId);
                    ShowDeckHangarVis(PlayerData.localPlayer.networkId);
                }
            }
            else
            {
                Decks[0] = CalculateDeck(0);
                CalculateHangar(0);
                ShowDeckHangarVis(0);
            }
        }

        public void OnGUI()
        {
            if (Decks[0].valid)
            {
                try
                {
                    GUI.Box(new Rect(100, 200, 250, 50), Hangars[0].Count.ToString());
                }
                catch { }
                
                //GUI.Box(new Rect(100, 300, 250, 50), AvailableDeckWood[0].Count.ToString());
            }
            //ShowDeckParkingSpotOnGUI(0);
            
        }

    }
}
