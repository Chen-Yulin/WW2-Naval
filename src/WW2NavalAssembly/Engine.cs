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
    public class EngineMsgReceiver : SingleInstance<EngineMsgReceiver>
    {
        public override string Name { get; } = "Engine Msg Receiver";
        // playerID, guid, HPPercent, TapPosition, TargetVelocity
        public static MessageType EngineStateMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single, DataType.Single, DataType.Single);

        public class EngineData
        {
            public bool valid;
            public float HPPercent;
            public float TapPosition;
            public float ThrustPercent;
            public EngineData(float HPPercent, float TapPosition, float TargetVel)
            {
                valid = true;
                this.HPPercent = HPPercent;
                this.TapPosition = TapPosition;
                this.ThrustPercent = TargetVel;
            }
            public EngineData()
            {
                valid = false;
            }
        }

        public Dictionary<int,EngineData>[] engineData = new Dictionary<int, EngineData>[16];

        public EngineMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                engineData[i] = new Dictionary<int, EngineData>();
            }
        }

        public void MsgReceiver(Message msg)
        {
            engineData[(int)msg.GetData(0)][(int)msg.GetData(1)] = new EngineData((float)msg.GetData(2), (float)msg.GetData(3), (float)msg.GetData(4));
        }

    }
    public class Engine : BlockScript
    {
        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public MKey ForwardKey;
        public MKey BackKey;
        public MSlider AxlePosX;
        public MSlider AxlePosY;
        public MSlider AxleLength;
        public MSlider AxlePitch;
        public MSlider ThrustValue;
        public MSlider PropellerSize;

        public Rigidbody myRigid;
        public float Mass;

        public GameObject MyVis;
        public GameObject MyVisAnchor;
        public GameObject Sleeve;
        public GameObject Sleeve2;
        public GameObject Axle;
        public GameObject Propeller;
        public GameObject Trail;
        public ParticleSystem TrailParticle;
        PropellerBehaviour PropellerPB;

        public GameObject StablePivot;

        public float InitialHP = 0;
        public float HP = 0;
        public float HPPercent = 1;
        public float DamageCoeff = 0.003f;

        public int frameCount = 0;

        public Material InitialMat;
        public Material ArmMat;

        public float TapPosition = 0;
        //public float TargetVelocity = 0;

        Texture Meter;
        Texture Indicator;
        Texture Speed;
        int iconSize = 64;

        public float preVel;
        public float Accel;
        public float forceModifier = 1;
        public float stableForce = 200000f;

        public float ThrustPercentage = 0;

        public Stack<Chimney> Chimneys = new Stack<Chimney>();

        public void FindChimneys()
        {
            foreach(var chimney in transform.parent.GetComponentsInChildren<Chimney>())
            {
                if (chimney.GetComponent<Chimney>() != null)
                {
                    Chimneys.Push(chimney.GetComponent<Chimney>());
                }
            }
        }

        public void SetChimney(int state)
        {
            foreach(var chimney in Chimneys)
            {
                chimney.SetSmokeState(state);
            }
        }

        public void InitTrail()
        {
            Trail = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.ShipWave, transform);
            Trail.name = "Trail";
            Trail.transform.localPosition = Vector3.zero;
            Trail.transform.localScale = new Vector3(3,3,3f);
            TrailParticle = Trail.transform.Find("particle").gameObject.GetComponent<ParticleSystem>();
            TrailParticle.startLifetime = 4;
            Trail.SetActive(false);
        }
        public void UpdateTrail()
        {
            if (ModController.Instance.showSea && frameCount >5)
            {
                Trail.transform.position = new Vector3(Propeller.transform.position.x, 20.05f, Propeller.transform.position.z);
                if (!Trail.activeSelf)
                {
                    Trail.SetActive(true);
                }
            }
            else
            {
                if (Trail.activeSelf)
                {
                    Trail.SetActive(false);
                }
            }

            if (Propeller.transform.position.y > 21 || Propeller.transform.position.y < 15f)
            {
                if (TrailParticle.isPlaying)
                {
                    TrailParticle.Stop();
                }
            }
            else
            {
                if (TrailParticle.isStopped)
                {
                    TrailParticle.Play();
                }
            }

        }
        public void CannonDamage(float caliber)
        {
            HP -= caliber * DamageCoeff;
            HP = Mathf.Clamp(HP, 0, float.MaxValue);
            HPPercent = HP / InitialHP;
            ArmMat.SetColor("_TintColor", new Color(HPPercent,HPPercent,HPPercent,1));
            SetStableForce();
            SendEngineData();
        }
        public void SetStableForce()
        {
            if (!StatMaster.isClient)
            {
                SoftJointLimitSpring SJLS = new SoftJointLimitSpring();
                SJLS.spring = Mathf.Clamp(stableForce * HPPercent, 1, stableForce);

                foreach (var joint in StablePivot.GetComponents<ConfigurableJoint>())
                {
                    
                    joint.angularXLimitSpring = SJLS;
                    joint.angularYZLimitSpring = SJLS;
                }
            }
        }
        public void CalculateThrustPercentage()
        {
            if (ThrustPercentage >= TapPosition / 4 + 0.008f)
            {
                if (frameCount>500)
                {
                    ThrustPercentage -= 0.0005f / PropellerSize.Value;
                }
                else
                {
                    ThrustPercentage = TapPosition / 4;
                }
                
            }
            else if (ThrustPercentage <= TapPosition / 4 - 0.005f)
            {
                if (frameCount > 500)
                {
                    ThrustPercentage += 0.0004f / PropellerSize.Value;
                }
                else
                {
                    ThrustPercentage = TapPosition / 4;
                }
            }
        }
        public void Thrust()
        {
            CalculateThrustPercentage();

            float PBSpeed = Mathf.Sign(ThrustPercentage*20) * Mathf.Sqrt(Mathf.Abs(ThrustPercentage *20 * HPPercent)) * 4;
            PropellerPB.Speed = (Mathf.Abs(PBSpeed)<1f?0:PBSpeed) * Vector3.up;
            myRigid.AddForceAtPosition(-MyVisAnchor.transform.up * ThrustPercentage*ThrustValue.Value*HPPercent*PropellerSize.Value, Sleeve.transform.position);
        }
        public void UpdateArmVis()
        {
            if (ModController.Instance.ShowChanged)
            {
                if (ModController.Instance.ShowArmour)
                {
                    transform.Find("Vis").GetComponent<MeshRenderer>().material = ArmMat;
                }
                else
                {
                    transform.Find("Vis").GetComponent<MeshRenderer>().material = InitialMat;
                }
            }
        }
        public void UpdateThrustRange()
        {
            Vector3 scale = transform.localScale;
            ThrustValue.SetRange(0, scale.x * scale.y * scale.z * 1000);
            ThrustValue.Value =  Mathf.Clamp(ThrustValue.Value,0,ThrustValue.Max);
        }
        public void UpdateAxlePosition()
        {
            MyVisAnchor.transform.localEulerAngles = new Vector3(AxlePitch.Value, 0, 0);
            MyVis.transform.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z);
            Sleeve.transform.localPosition = new Vector3(AxlePosX.Value, 0, AxlePosY.Value);
            Sleeve.transform.localScale = new Vector3(1, transform.lossyScale.y, 1);

            Sleeve2.transform.localPosition = new Vector3(AxlePosX.Value, AxleLength.Value, AxlePosY.Value);

            Axle.transform.localPosition = new Vector3(AxlePosX.Value, 0, AxlePosY.Value);
            Axle.transform.localScale = new Vector3(1, AxleLength.Value/5.8f, 1);

            Propeller.transform.localPosition = new Vector3(AxlePosX.Value, AxleLength.Value, AxlePosY.Value);
            Propeller.transform.localScale = Vector3.one * PropellerSize.Value;
        }
        public void InitMaterial()
        {
            InitialMat = transform.Find("Vis").GetComponent<MeshRenderer>().material;
            ArmMat = new Material(Shader.Find("Particles/Alpha Blended"));
        }
        public void InitVis()
        {
            if (!transform.Find("MyVis"))
            {
                MyVis = new GameObject("MyVis");
                MyVis.transform.SetParent(transform);
                MyVis.transform.localPosition = Vector3.zero;
                MyVis.transform.localRotation = Quaternion.identity;
                MyVis.transform.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z);

                if (!MyVis.transform.Find("Anchor"))
                {
                    MyVisAnchor = new GameObject("Anchor");
                    MyVisAnchor.transform.SetParent(MyVis.transform);
                    MyVisAnchor.transform.localPosition = Vector3.zero;
                    MyVisAnchor.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    MyVisAnchor = MyVis.transform.Find("Anchor").gameObject;
                }

                if (!MyVisAnchor.transform.Find("Sleeve"))
                {
                    Sleeve = new GameObject("Sleeve");
                    Sleeve.transform.SetParent(MyVisAnchor.transform);
                    Sleeve.transform.localPosition = Vector3.zero;
                    Sleeve.transform.localRotation = Quaternion.identity;

                    Sleeve.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Engine-Sleeve Mesh").Mesh;
                    Sleeve.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("Engine Texture").Texture;
                }
                else
                {
                    Sleeve = MyVisAnchor.transform.Find("Sleeve").gameObject;
                }
                if (!MyVisAnchor.transform.Find("Sleeve2"))
                {
                    Sleeve2 = new GameObject("Sleeve2");
                    Sleeve2.transform.SetParent(MyVisAnchor.transform);
                    Sleeve2.transform.localPosition = Vector3.zero;
                    Sleeve2.transform.localRotation = Quaternion.identity;

                    Sleeve2.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Engine-Sleeve2 Mesh").Mesh;
                    Sleeve2.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("Engine-Sleeve2 Texture").Texture;
                }
                else
                {
                    Sleeve2 = MyVisAnchor.transform.Find("Sleeve2").gameObject;
                }
                if (!MyVisAnchor.transform.Find("Axle"))
                {
                    Axle = new GameObject("Axle");
                    Axle.transform.SetParent(MyVisAnchor.transform);
                    Axle.transform.localPosition = Vector3.zero;
                    Axle.transform.localRotation = Quaternion.identity;
                    Axle.transform.localScale = Vector3.one;

                    Axle.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Engine-Axle Mesh").Mesh;
                    Axle.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("Engine-Axle Texture").Texture;
                }
                else
                {
                    Axle = MyVisAnchor.transform.Find("Axle").gameObject;
                }
                if (!MyVisAnchor.transform.Find("Propeller"))
                {
                    Propeller = new GameObject("Propeller");
                    Propeller.transform.SetParent(MyVisAnchor.transform);
                    Propeller.transform.localPosition = Vector3.zero;
                    Propeller.transform.localRotation = Quaternion.identity;
                    Propeller.transform.localScale = Vector3.one;

                    Propeller.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Engine-Prop Mesh").Mesh;
                    Propeller.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("Engine-Prop Texture").Texture;
                }
                else
                {
                    Propeller = MyVisAnchor.transform.Find("Propeller").gameObject;
                }

            }
            else
            {
                MyVis = transform.Find("MyVis").gameObject;
            }


        }

        public void InitStable()
        {
            InitStablePivot();
            GameObject keel;
            keel = gameObject.GetComponent<ConfigurableJoint>().connectedBody.gameObject;
            try
            {
                keel.GetComponent<Rigidbody>().mass += 4f;
            }
            catch { }
            

            SoftJointLimitSpring SJLS = new SoftJointLimitSpring();
            SJLS.spring = stableForce;
            SoftJointLimit HigherSJL = new SoftJointLimit();
            HigherSJL.limit = 0.01f;
            SoftJointLimit LowerSJL = new SoftJointLimit();
            LowerSJL.limit = -0.01f;

            ConfigurableJoint CJ0 = StablePivot.AddComponent<ConfigurableJoint>();
            CJ0.connectedBody = keel.GetComponent<Rigidbody>();
            CJ0.axis = new Vector3(0, 0, 1);
            CJ0.secondaryAxis = new Vector3(1, 0, 1);
            CJ0.xMotion = ConfigurableJointMotion.Free;
            CJ0.yMotion = ConfigurableJointMotion.Free;
            CJ0.zMotion = ConfigurableJointMotion.Free;
            CJ0.angularXMotion = ConfigurableJointMotion.Limited;
            CJ0.angularYMotion = ConfigurableJointMotion.Locked;
            CJ0.angularZMotion = ConfigurableJointMotion.Free;

            CJ0.highAngularXLimit = HigherSJL;
            CJ0.lowAngularXLimit = LowerSJL;
            CJ0.angularYLimit = HigherSJL;

            CJ0.angularXLimitSpring = SJLS;
            CJ0.angularYZLimitSpring = SJLS;

            try
            {
                foreach (ConfigurableJoint joint in keel.GetComponent<BlockBehaviour>().jointsToMe)
                {
                    try
                    {
                        if (joint.name == "Brace")
                        {
                            joint.breakForce = float.MaxValue;
                            joint.breakTorque = float.MaxValue;
                        }
                    }
                    catch { }


                }
            }
            catch { }

        }

        public void InitStablePivot()
        {
            GameObject keel;
            keel = gameObject.GetComponent<ConfigurableJoint>().connectedBody.gameObject;
            keel.GetComponent<Rigidbody>().angularDrag = 30f;
            StablePivot = new GameObject("Stable Pivot");
            StablePivot.transform.SetParent(transform.parent);
            StablePivot.transform.position = transform.position;
            float orien = MathTool.SignedAngle(new Vector2(1, 0), new Vector2(transform.right.x, transform.right.z));
            StablePivot.transform.eulerAngles = new Vector3(0, -orien, 0);
            StablePivot.transform.localScale = new Vector3(transform.localScale.x, transform.localScale.z, transform.localScale.y);
            Rigidbody SPRigid = StablePivot.AddComponent<Rigidbody>();
            //StablePivot.AddComponent<MeshFilter>().mesh = ModResource.GetMesh("Engine Mesh").Mesh;
            //StablePivot.AddComponent<MeshRenderer>();
            SPRigid.isKinematic = true;
        }

        public void KeepStable()
        {
            StablePivot.transform.position = transform.position;

            float orien = MathTool.SignedAngle(new Vector2(1,0), new Vector2(transform.right.x,transform.right.z));
            //Debug.Log(orien);
            StablePivot.transform.eulerAngles = new Vector3(0, -orien, 0);
        }
        public void SendEngineData()
        {
            ModNetworking.SendToAll(EngineMsgReceiver.EngineStateMsg.CreateMessage(myPlayerID, myGuid, HPPercent, TapPosition, ThrustPercentage));
        }

        public override void SafeAwake()
        {
            ForwardKey = AddKey(LanguageManager.Instance.CurrentLanguage.EngineUp, "Forward", KeyCode.UpArrow);
            BackKey = AddKey(LanguageManager.Instance.CurrentLanguage.EngineDown, "Backward", KeyCode.DownArrow);
            ThrustValue = AddSlider(LanguageManager.Instance.CurrentLanguage.EngineThrust, "EngineThrust", 1000f, 0f, 1000f);
            AxleLength = AddSlider(LanguageManager.Instance.CurrentLanguage.EngineAxisLength, "AxleLength", 15f, 5f, 50f);
            AxlePosX = AddSlider(LanguageManager.Instance.CurrentLanguage.EngineAxisXOffset, "AxlePosX", 0f, -1f, 1f);
            AxlePosY = AddSlider(LanguageManager.Instance.CurrentLanguage.EngineAxisYOffset, "Axle Position Y", 0.5f, 0f, 1f);
            AxlePitch = AddSlider(LanguageManager.Instance.CurrentLanguage.EngineAxisPitch, "Axle Pitch", 0f, -10f, 10f);
            PropellerSize = AddSlider(LanguageManager.Instance.CurrentLanguage.PropellerSize, "PropellerSize", 1f, 0.5f, 2f);
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            InitVis();
            InitMaterial();
            Meter = ModResource.GetTexture("Engine-Meter Texture").Texture;
            Indicator = ModResource.GetTexture("Engine-Indicator Texture").Texture;
            Speed = ModResource.GetTexture("Engine-Speed Texture").Texture;
        }
        public void Start()
        {
            gameObject.name = "Engine";
        }

        public override void BuildingUpdate()
        {
            UpdateAxlePosition();
            UpdateThrustRange();
            
        }
        public void Update()
        {
            UpdateArmVis();
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
        public override void OnSimulateStart()
        {
            preVel = 0;
            ConfigurableJoint myJoint = gameObject.GetComponent<ConfigurableJoint>();
            myJoint.angularXMotion = ConfigurableJointMotion.Locked;
            myJoint.angularYMotion = ConfigurableJointMotion.Locked;
            myJoint.angularZMotion = ConfigurableJointMotion.Locked;
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            myseed = 5;
            if (!StatMaster.isClient)
            {
                BlockBehaviour.blockJoint.breakForce = float.PositiveInfinity;
                BlockBehaviour.blockJoint.breakTorque = float.PositiveInfinity;

                myRigid = GetComponent<Rigidbody>();
                Mass = GetComponent<BlockBehaviour>().ParentMachine.Mass;
            }

            PropellerPB = Propeller.AddComponent<PropellerBehaviour>();
            PropellerPB.Direction = false;
            PropellerPB.Speed = Vector3.zero;

            HP = transform.localScale.x * transform.localScale.y * transform.localScale.z;
            InitialHP = transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z;
            HP = InitialHP;
            HPPercent = 1;
            if (StatMaster.isMP)
            {
                try
                {
                    EngineMsgReceiver.Instance.engineData[myPlayerID].Add(myGuid, new EngineMsgReceiver.EngineData());
                }
                catch { }
            }
            InitTrail();
            FindChimneys();
            FlightDataBase.Instance.engines[myPlayerID].Add(this);
        }
        public override void OnSimulateStop()
        {
            FlightDataBase.Instance.engines[myPlayerID].Clear();
        }
        public void OnDestroy()
        {
            FlightDataBase.Instance.engines[myPlayerID].Clear();
        }
        public override void SimulateUpdateHost()
        {
            if (ForwardKey.IsPressed)
            {
                TapPosition++;
                TapPosition = Mathf.Clamp(TapPosition, -1, 4);
                SendEngineData();
                SetChimney((int)Mathf.Abs(TapPosition));
            }else if (BackKey.IsPressed)
            {
                TapPosition--;
                TapPosition = Mathf.Clamp(TapPosition, -1, 4);
                SendEngineData();
                SetChimney((int)Mathf.Abs(TapPosition));
            }
            UpdateTrail();
        }
        public override void SimulateUpdateClient()
        {
            if (EngineMsgReceiver.Instance.engineData[myPlayerID][myGuid].valid)
            {
                EngineMsgReceiver.Instance.engineData[myPlayerID][myGuid].valid = false;
                HPPercent = EngineMsgReceiver.Instance.engineData[myPlayerID][myGuid].HPPercent;
                HP = HPPercent * InitialHP;
                ArmMat.SetColor("_TintColor", new Color(HPPercent, HPPercent, HPPercent, 1));
                ThrustPercentage = EngineMsgReceiver.Instance.engineData[myPlayerID][myGuid].ThrustPercent;

                float remoteTapPosition = EngineMsgReceiver.Instance.engineData[myPlayerID][myGuid].TapPosition;
                if (TapPosition != remoteTapPosition)
                {
                    TapPosition = remoteTapPosition;
                    SetChimney((int)Mathf.Abs(TapPosition));
                }
            }
            UpdateTrail();
        }
        public override void SimulateFixedUpdateHost()
        {
            Thrust();
            if (frameCount < 25)
            {
                frameCount++;
            }
            else if(frameCount == 25)
            {
                InitStable();
                frameCount++;
            }
            else
            {
                KeepStable();
                frameCount++;
            }

            
        }
        public void MySimulateFixedUpdateClient()
        {
            frameCount++;
            CalculateThrustPercentage();
            float PBSpeed = Mathf.Sign(ThrustPercentage * 20) * Mathf.Sqrt(Mathf.Abs(ThrustPercentage * 20 * HPPercent)) * 4;
            PropellerPB.Speed = (Mathf.Abs(PBSpeed) < 1f ? 0 : PBSpeed) * Vector3.up;
        }
        public void OnGUI()
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
            if ((Camera.main.transform.position - transform.position).magnitude < 30 && BlockBehaviour.isSimulating)
            {
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
                if (onScreenPosition.z >= 0)
                {
                    GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), Meter);
                    GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2 - iconSize/5*TapPosition, iconSize, iconSize), Indicator);
                    GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, 
                                    Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2 + 4*iconSize / 5, 
                                    iconSize, -iconSize * ThrustPercentage * HPPercent*1.6f), Speed);
                }
            }
        }

    }
}
