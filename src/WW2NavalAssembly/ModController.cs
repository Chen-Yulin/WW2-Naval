using System;
using System.Collections.Generic;
using System.Text;

using Modding;

using Modding.Common;
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
        public bool showArmour = false;
        public bool showSea = false;
        public bool useSkyBox = false;
        public int skyboxSelector = 0;
        public bool skychanged = false;

        public int state;

        public bool isFirstFrame = true;
        public GameObject skybox;
        public Material[] matArray = new Material[2];


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
                showArmour = GUILayout.Toggle(showArmour, "Show Armour Layout");
                showSea = GUILayout.Toggle(showSea, "Sea Toggle");
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
