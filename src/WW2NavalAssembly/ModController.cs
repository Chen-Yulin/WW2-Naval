﻿using Modding;
using Navalmod;
using System;
using UnityEngine;


namespace WW2NavalAssembly
{
    public class mySmoothFollow : MonoBehaviour
    {
        private void Start()
        {
            if (this.target == null)
            {
                this.target = Camera.main.transform;
            }
        }

        private void LateUpdate()
        {
            if (this.target == null)
            {
                return;
            }
            base.transform.position = Vector3.Lerp(base.transform.position, this.target.position, TimeSlider.Instance.deltaTime * this.smoothAmount);
        }

        public Transform target;

        public float smoothAmount = 100;
    }

    class ModController : SingleInstance<ModController>
    {
        public override string Name { get; } = "WW2NavalModController";

        private Rect windowRect = new Rect(15f, 100f, 280f, 50f);
        private readonly int windowID = ModUtility.GetWindowId();
        public bool windowHidden = false;
        bool _showArmour = false;
        bool _showCrew = false;
        public bool ShowChanged = false;
        public float seahigh = 20f;
        public bool newseaEffect = true;
        public float seaStrenght;
        public bool ShowArmour
        {
            get
            {
                return _showArmour;
            }
            set
            {
                if (_showArmour != value)
                {
                    _showArmour = value;
                    if (_showArmour)
                    {
                        _showCrew = false;
                    }
                    ShowChanged = true;
                }
            }
        }
        public bool ShowCrew
        {
            get
            {
                return _showCrew;
            }
            set
            {
                if (_showCrew != value)
                {
                    _showCrew = value;
                    if (_showCrew)
                    {
                        _showArmour = false;
                    }
                    ShowChanged = true;
                }
            }
        }



        public bool showSea = false;
        public bool deleteFog = false;
        public bool useSkyBox = false;
        public int skyboxSelector = 0;
        public bool skychanged = false;
        public bool preFog = false;
        public string s_AAForce = "1";

        public float AAForce
        {
            get
            {
                return float.Parse(s_AAForce);
            }
        }

        public int state;
        public int longerState;

        public bool isFirstFrame = true;
        public GameObject skybox;
        public Material[] matArray = new Material[2];

        public void SetFog(bool a)
        {
            GameObject.Find("Main Camera").transform.Find("Fog Volume").gameObject.SetActive(a);
        }
        private void Awake()
        {

        }

        public void Start()
        {
        }

        public void Update()
        {
            
            if (!isFirstFrame)
            {
                if (StatMaster.isMainMenu)
                {
                    isFirstFrame = true;
                    Destroy(skybox);
                }
            }
            else
            {
                if (!StatMaster.isMainMenu)
                {
                    isFirstFrame = false;
                    //orgskybox = (GameObject.Find("MULTIPLAYER LEVEL").transform.FindChild("Environments").FindChild("Barren").FindChild("AviamisAtmosphere").FindChild("STAR SPHERE").gameObject);

                    skybox = new GameObject("WWII Sky Box");

                    matArray[0] = new Material(Shader.Find("Instanced/Block Shader (GPUI off)"));
                    matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("BlueSky").Texture);
                    matArray[0].SetTexture("_MainTex", Texture2D.blackTexture);
                    matArray[0].SetColor("_Color", Color.black);
                    matArray[0].SetColor("_EmissCol", Color.white);
                    matArray[1] = new Material(Shader.Find("Particles/Additive"));
                    matArray[1].mainTexture = ModResource.GetTexture("BlueSky").Texture;
                    matArray[1].SetColor("_TintColor", new Color(0,0,0,1f));
                    skybox.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("SkyBall").Mesh;
                    skybox.AddComponent<MeshRenderer>().materials = matArray;
                    skybox.GetComponent<MeshRenderer>().sortingOrder = -32768;
                    skybox.transform.localScale = new Vector3(4000, 4000, 4000);
                    mySmoothFollow MSF = skybox.AddComponent<mySmoothFollow>();
                    MSF.target = Camera.main.transform;


                    //skybox.GetComponent<MeshRenderer>().material.SetColor("_Emission", Color.white);
                    //skybox.GetComponent<MeshRenderer>().material.renderQueue = 4000;

                    skybox.SetActive(false);
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;


                    
                }
                
            }
            if (!StatMaster.isMainMenu && !isFirstFrame)
            {
                if (useSkyBox)
                {
                    skybox.SetActive(true);
                }
                else
                {
                    skybox.SetActive(false);
                }
                if (skychanged)
                {
                    switch (skyboxSelector)
                    {
                        case 0:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("BlueSky").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("BlueSky").Texture;
                            break;
                        case 1:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("Night").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("Night").Texture;
                            break;
                        case 2:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("Sunset1").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("Sunset1").Texture;
                            break;
                        case 3:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("Sunset2").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("Sunset2").Texture;
                            break;
                        case 4:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("Sunrise1").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("Sunrise1").Texture;
                            break;
                        case 5:
                            matArray[0].SetTexture("_EmissMap", ModResource.GetTexture("Sunrise2").Texture);
                            matArray[1].mainTexture = ModResource.GetTexture("Sunrise2").Texture;
                            break;
                        default:
                            break;
                    }
                    skybox.GetComponent<MeshRenderer>().materials = matArray;
                    skychanged = false;
                }
            }

            if (preFog != deleteFog)
            {
                preFog = deleteFog;
                SetFog(!deleteFog);
            }


            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.W))
                {
                    windowHidden = !windowHidden;
                }

            }
        }
        public void FixedUpdate()
        {
            if (state == 40)
            {
                state = 0;
            }
            else
            {
                state++;
            }
            if (longerState == 400)
            {
                longerState = 0;
            }
            else
            {
                longerState++;
            }

        }
        private void ToggleIndent(string text, float w, ref bool flag, Action func)
        {
            flag = GUILayout.Toggle(flag, text, new GUILayoutOption[0]);
            bool flag2 = flag;
            if (flag2)
            {
                GUILayout.BeginHorizontal(new GUILayoutOption[0]);
                GUILayout.Label("", new GUILayoutOption[]
                {
                    GUILayout.Width(w)
                });
                GUILayout.BeginVertical(new GUILayoutOption[0]);
                func();
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        private void MACWindow(int windoID)
        {
            GUILayout.BeginVertical();
            {
                ShowArmour = GUILayout.Toggle(ShowArmour, "Show Armour Layout");
                ShowCrew = GUILayout.Toggle(ShowCrew, "Show Crew Layout");
                showSea = GUILayout.Toggle(showSea, "Sea Toggle");
                newseaEffect = GUILayout.Toggle(newseaEffect, "Advance Sea Toggle");
                seaStrenght = GUILayout.HorizontalSlider( seaStrenght, 0.0f, 0.5f);
                GUILayout.Label("Sea State:" + seaStrenght.ToString("0.00"));
                deleteFog = GUILayout.Toggle(deleteFog, "Delete Fog");
                ToggleIndent("Use SkyBox", 20, ref useSkyBox, delegate
                {
                    GUILayout.BeginVertical("box", new GUILayoutOption[0]);
                    if (GUILayout.Button("BlueSky", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 0;
                        skychanged = true;
                    }
                    if (GUILayout.Button("Night", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 1;
                        skychanged = true;
                    }
                    if (GUILayout.Button("Sunset1", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 2;
                        skychanged = true;
                    }
                    if (GUILayout.Button("Sunset2", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 3;
                        skychanged = true;
                    }
                    if (GUILayout.Button("Sunrise1", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 4;
                        skychanged = true;
                    }
                    if (GUILayout.Button("Sunrise2", new GUILayoutOption[0]))
                    {
                        skyboxSelector = 5;
                        skychanged = true;
                    }

                    GUILayout.EndVertical();
                });

                if( GUILayout.Button("Apply", new GUILayoutOption[0]))
                {
                    SingleInstance<SpiderFucker>.Instance.Apply();
                }
                SingleInstance<SpiderFucker>.Instance.FloorDeactiveSwitch = GUILayout.Toggle(SingleInstance<SpiderFucker>.Instance.FloorDeactiveSwitch, "FloorDeactive", new GUILayoutOption[0]);
                //SingleInstance<SpiderFucker>.Instance.ExpandFloorSwitch = GUILayout.Toggle(SingleInstance<SpiderFucker>.Instance.ExpandFloorSwitch, "空气墙扩大10倍", new GUILayoutOption[0]);
                if (SingleInstance<SpiderFucker>.Instance.ExpandFloorSwitch && SingleInstance<SpiderFucker>.Instance.ExExpandFloorSwitch)
                {
                    SingleInstance<SpiderFucker>.Instance.ExExpandFloorSwitch = false;
                }
                SingleInstance<SpiderFucker>.Instance.ExExpandFloorSwitch = GUILayout.Toggle(SingleInstance<SpiderFucker>.Instance.ExExpandFloorSwitch, "Customized Boundary (m)", new GUILayoutOption[0]);
                if (SingleInstance<SpiderFucker>.Instance.ExpandFloorSwitch && SingleInstance<SpiderFucker>.Instance.ExExpandFloorSwitch)
                {
                    SingleInstance<SpiderFucker>.Instance.ExpandFloorSwitch = false;
                }
                SingleInstance<SpiderFucker>.Instance.ExExpandScale = Convert.ToSingle(GUILayout.TextArea(SingleInstance<SpiderFucker>.Instance.ExExpandScale.ToString(), new GUILayoutOption[0]));
                if (GUILayout.Button("FixClient", new GUILayoutOption[0]))
                {
                    SingleInstance<H3NetworkManager>.Instance.FixedCluster();
                }
                //GUILayout.Label("AA Efficiency");
                //s_AAForce = GUILayout.TextField(s_AAForce, 15);

                GUILayout.Label("Press Ctrl+W to hide");
            }

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUI.DragWindow();

        }

        private void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 50), BoundaryOff.ToString());

            if (!windowHidden && !StatMaster.hudHidden)
            {

                //Rect windowRect = new Rect(15f, 100f, 280f, 50f);

                windowRect = GUILayout.Window(windowID, windowRect, new GUI.WindowFunction(MACWindow), "WW2-Naval Mod Setting");
            }
        }
        
    }
}
