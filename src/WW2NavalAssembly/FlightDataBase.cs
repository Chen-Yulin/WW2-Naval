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
        public Deck[] Decks = new Deck[16];

        public GameObject[] DeckObjects = new GameObject[16];

        public GameObject[,] DeckLine = new GameObject[5, 16];

        public Texture GunnerAlertIcon;
        int iconSize = 30;

        public class Deck
        {
            public bool valid;
            public float Width;
            public float Length;
            public float height;
            public float RightMargin = 1;

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

                this.Width_num = (int)((Width-2) / 2f) + 1;
                this.Length_num = (int)((Length-10) / 3f);
                this.Total_num = Width_num * Length_num;

                this.RightMargin = (Width - (Width_num - 1) * 2) / 2f;
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
                                    - i % Decks[playerID].Width_num * 2 * right
                                    + i / Decks[playerID].Width_num * 3 * forward
                                    + (ForwardABit ? 1.5f : 0) * forward;

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

        public void InitLine()
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    DeckLine[i, j] = new GameObject("DeckLine-"+i.ToString()+" ("+j.ToString()+")");
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

        public void ShowDeckParkingSpotOnGUI(int playerID)
        {
            if (ModController.Instance.showArmour)
            {
                if (Decks[playerID].valid)
                {
                    for (int i = 0; i < Decks[playerID].Total_num; i++)
                    {
                        Vector3 anchor = new Vector3(Decks[playerID].Anchor.x, Decks[playerID].height, Decks[playerID].Anchor.y);
                        Vector3 right = new Vector3(Decks[playerID].Right.x, 0, Decks[playerID].Right.y);
                        Vector3 forward = new Vector3(Decks[playerID].Forward.x, 0, Decks[playerID].Forward.y);
                        bool ForwardABit = (i % Decks[playerID].Width_num) % 2 == 1;
                        Vector3 spotPos =   anchor - right * Decks[playerID].RightMargin + forward * 5f
                                            - i % Decks[playerID].Width_num * 2 * right 
                                            + i / Decks[playerID].Width_num * 3 * forward
                                            + (ForwardABit? 1.5f : 0) * forward;

                        Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(spotPos);
                        if (onScreenPosition.z >= 0)
                        {
                                GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), GunnerAlertIcon);
                        }
                    }
                }
            }
        }

        public void ShowDeckVis(int playerID)
        {
            if (ModController.Instance.showArmour)
            {
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
            }
            else
            {
                for (int i = 0; i < 5; i++)
                {
                    DeckLine[i, playerID].SetActive(false);
                }
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
                    if (deck.Value != null && !deck.Value.AsDeck.isDefaultValue)
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

        public FlightDataBase()
        {
            for (int i = 0; i < 16; i++)
            {
                DeckForward[i] = new Vector2(1, 0);
                DeckRight[i] = new Vector2(0, 1);
                AvailableDeckWood[i] = new Dictionary<int, FlightDeck>();
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
                    ShowDeckVis(0);
                }
                else
                {
                    Decks[PlayerData.localPlayer.networkId] = CalculateDeck(PlayerData.localPlayer.networkId);
                    ShowDeckVis(PlayerData.localPlayer.networkId);
                }
            }
            else
            {
                Decks[0] = CalculateDeck(0);
                ShowDeckVis(0);
            }
        }

        public void OnGUI()
        {
            if (Decks[0].valid)
            {
                //GUI.Box(new Rect(100, 200, 250, 50), Decks[0].Center.ToString() + " " + Decks[0].Width.ToString() + " " + Decks[0].Length.ToString());
                //GUI.Box(new Rect(100, 300, 250, 50), AvailableDeckWood[0].Count.ToString());
            }
            //ShowDeckParkingSpotOnGUI(0);
            
        }

    }
}
