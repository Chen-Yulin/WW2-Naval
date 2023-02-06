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

namespace WW2NavalAssembly
{
    public class LockData
    {
        public Vector3 position;
        public Vector3 velocity;
        public bool valid;
        public LockData()
        {
            position = Vector3.zero;
            velocity = Vector3.zero;
            valid = false;
        }
    }
    public class CameraData
    {
        public Vector3 position;
        public Vector3 forward;
        public bool valid;
        public CameraData()
        {
            position = Vector3.zero;
            forward = Vector3.zero;
            valid = false;
        }
    }
    public class ControllerDataManager : SingleInstance<ControllerDataManager>
    {
        public override string Name { get; } = "LockDataManager";
        public static MessageType LockMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Vector3, DataType.Boolean);
        public static MessageType CameraMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Vector3);
        public static MessageType ControllerVelMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3);

        public LockData[] lockData = new LockData[16];
        public CameraData[] cameraData = new CameraData[16];

        public int[] SpotNum = new int[16];
        public Vector3[] ControllerVel = new Vector3[16];
        public Vector3[] ControllerPos = new Vector3[16];
        public GameObject[] ControllerObject = new GameObject[16];

        public Dictionary<float, Controller.FCResult>[] ControllerFCResult = new Dictionary<float, Controller.FCResult>[16];

        public ControllerDataManager()
        {
            for (int i = 0; i < 16; i++)
            {
                lockData[i] = new LockData();
                cameraData[i] = new CameraData();
                ControllerFCResult[i] = new Dictionary<float, Controller.FCResult>();
                ControllerObject[i] = new GameObject();
            }
        }
        public void LockDataReceiver(Message msg)
        {
            lockData[(int)msg.GetData(0)].position = (Vector3)msg.GetData(1);
            lockData[(int)msg.GetData(0)].velocity = (Vector3)msg.GetData(2);
            lockData[(int)msg.GetData(0)].valid = (bool)msg.GetData(3);
        }
        public void CameraDataReceiver(Message msg)
        {
            Debug.Log("Receive Camera");
            cameraData[(int)msg.GetData(0)].position = (Vector3)msg.GetData(1);
            cameraData[(int)msg.GetData(0)].forward = (Vector3)msg.GetData(2);
            cameraData[(int)msg.GetData(0)].valid = true;
        }
        public void ControllerVelReceiver(Message msg)
        {
            ControllerVel[(int)msg.GetData(0)] = (Vector3)msg.GetData(1);
        }
    }

    public class Controller : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey TrackCannon;
        public MKey SwitchCannnon;
        public MKey Lock;
        public MSlider FCPanelSize;
        public MSlider TurrentHeight;

        public bool TrackOn;
        public Camera _viewCamera;
        public bool FCInitialized = false;

        public GameObject TargetCannon;

        GameObject FCCanvas;
        public GameObject FireControlPanel;
        public GameObject FCPitch;
        public GameObject FCOrien;
        public Dictionary<int, GameObject> PitchAimIcon = new Dictionary<int, GameObject>();
        public Dictionary<int, GameObject> OrienAimIcon = new Dictionary<int, GameObject>();
        public Dictionary<float, GameObject> PitchPredIcon = new Dictionary<float, GameObject>();
        public Dictionary<float, GameObject> OrienPredIcon = new Dictionary<float, GameObject>();
        public Dictionary<int, GameObject> TorpedoPreIcon = new Dictionary<int, GameObject>();
        public Dictionary<int, GameObject> TorpedoAimIcon = new Dictionary<int, GameObject>();
        public Dictionary<float, FCResult> FCResults = new Dictionary<float, FCResult>();

        public bool Locking = false;
        public GameObject lockingObject;

        public int iconSize = 128;
        public Texture LockIconOnScreen;

        public Vector2 BaseRandomError;
        public float SpotModifier;
        public float ClosingModifier;
        public Vector2 RandomError;
        public float PreYRotation;
        public float RotationError;
        public Vector3 myVelocity;

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

        public class Dist2PitchResult
        {
            public bool hasResult;
            public float time;
            public float pitch;

            public Dist2PitchResult()
            {
                hasResult = false;
            }
            public Dist2PitchResult(float time, float pitch)
            {
                this.hasResult = true;
                this.time = time;
                this.pitch = pitch;
            }
        }
        public class FCResult
        {
            public float Orien;
            public float Pitch;
            public bool hasRes;
            public Vector2 predPosition;
            public FCResult(float orien)
            {
                Orien = orien;
                hasRes = false;
            }
            public FCResult(float orien, float pitch, Vector2 PredPosition)
            {
                Orien = orien;
                Pitch = pitch;
                hasRes = true;
                predPosition = PredPosition;
            }
            public void Set(float orien, float pitch, bool HasRes, Vector2 PredPosition)
            {
                Orien = orien;
                Pitch = pitch;
                hasRes = HasRes;
                predPosition = PredPosition;
            }
            public float getTurrentAngle(Vector2 forward, Vector2 turrentPos)
            {
                return MathTool.Instance.SignedAngle(forward, predPosition - turrentPos);
            }

        }
        public FCResult CalculateGunFCPara(Vector2 targetPosition, Vector2 velocity, float caliber)
        {
            Vector2 myPosition = new Vector2(transform.position.x, transform.position.z);
            float dist = (targetPosition - myPosition).magnitude;
            Dist2PitchResult pitchRes = CalculateGunPitchFromDist(dist, caliber);
            if (pitchRes.hasResult)
            {
                dist = (targetPosition + velocity*pitchRes.time - myPosition).magnitude;
                pitchRes = CalculateGunPitchFromDist(dist, caliber);
                if (pitchRes.hasResult) // valid result
                {
                    float Orien = MathTool.Instance.SignedAngle(GetForward(), targetPosition + velocity * pitchRes.time - myPosition);
                    return new FCResult(Orien, pitchRes.pitch, targetPosition + velocity * pitchRes.time);
                }
                else
                {
                    return new FCResult(MathTool.Instance.SignedAngle(GetForward(), targetPosition-myPosition));
                }
            }
            else
            {
                return new FCResult(MathTool.Instance.SignedAngle(GetForward(), targetPosition - myPosition));
            }
        }

        public Vector2 CalculateTorpedoFCPara(Vector2 targetPosition, Vector2 velocity, int type)
        {
            float tSpeed = (type == 0 ? 11 : 18);
            Vector2 myPosition = new Vector2(transform.position.x, transform.position.z);
            Vector2 predictPosition = targetPosition;
            float esTime;
            for (int i = 0; i < 10; i++)
            {
                esTime = (predictPosition - myPosition).magnitude / tSpeed;
                predictPosition = targetPosition + esTime * velocity;
            }
            return (predictPosition - myPosition);
        }
        public Dist2PitchResult CalculateGunPitchFromDist(float dist, float caliber)
        {
            //Debug.Log("Start Iterating");
            float initialSpeed = Mathf.Sqrt(caliber + 100) * 8.5f;
            float g = 32.4f;
            float vx;
            float vy;
            float esT = 0;
            float angle = 0;
            for (int i = 0; i < 6; i++)
            {
                vx = initialSpeed * Mathf.Cos(angle);
                esT = -1 / 0.02f * Mathf.Log(1 - dist * 0.02f / vx);
                vy = g * esT / (1 - Mathf.Exp(-0.02f * esT)) - g / 0.02f;
                if (vy/initialSpeed < 0.7f)
                {
                    angle = (float)Math.Asin(vy / initialSpeed);
                    //Debug.Log(angle);
                }
                else
                {
                    return new Dist2PitchResult();
                }
                
            }

            // calculate height offset
            float modifiedPosition = dist - TurrentHeight.Value / Mathf.Tan(angle + 0.02f);
            angle = 0;
            for (int i = 0; i < 8; i++)
            {
                vx = initialSpeed * Mathf.Cos(angle);
                esT = -1 / 0.02f * Mathf.Log(1 - dist * 0.02f / vx);
                vy = g * esT / (1 - Mathf.Exp(-0.02f * esT)) - g / 0.02f;
                if (vy / initialSpeed < 0.7f)
                {
                    angle = (float)Math.Asin(vy / initialSpeed);
                    //Debug.Log(angle);
                }
                else
                {
                    return new Dist2PitchResult();
                }

            }



            return new Dist2PitchResult(esT, angle * 180/Mathf.PI);
        }

        public void InitFireControlPanel()
        {
            try
            {
                Destroy(GameObject.Find("FCCanvas"));
            }
            catch{}
            FCCanvas = (GameObject)Instantiate(AssetManager.Instance.FireControl.FireControl);
            FCCanvas.name = "FCCanvas";
            FireControlPanel = FCCanvas.transform.Find("Panel").gameObject;
            FireControlPanel.SetActive(false);
            FCPitch = FireControlPanel.transform.Find("PitchController").gameObject;
            FCOrien = FireControlPanel.transform.Find("OrienController").gameObject;
        }
        public void AdjustFCPanel()
        {
            float height = FCCanvas.transform.localPosition.y * 2;
            float width = FCCanvas.transform.localPosition.x * 2;
            FireControlPanel.transform.localScale = height / 980 * Vector3.one * FCPanelSize.Value;
            FireControlPanel.transform.position = new Vector3(width - FireControlPanel.transform.localScale.x * 150, FireControlPanel.transform.localScale.y * 150, 0);
            FireControlPanel.SetActive(true);
        }// adjust size and visiable
        public void InitGunFireControl()
        {
            Debug.Log("InitTorpedoFireControl");
            
            GameObject newTorpedoPredIcon0 = (GameObject)Instantiate(FCOrien.transform.Find("TorPredPrefab").gameObject, FCOrien.transform);
            newTorpedoPredIcon0.name = "TorPred 0";
            newTorpedoPredIcon0.SetActive(false);
            if (TorpedoPreIcon.ContainsKey(0))
            {
                TorpedoPreIcon[0] = newTorpedoPredIcon0;
            }
            else
            {
                TorpedoPreIcon.Add(0, newTorpedoPredIcon0);
            }
            
            GameObject newTorpedoPredIcon1 = (GameObject)Instantiate(FCOrien.transform.Find("TorPredPrefab").gameObject, FCOrien.transform);
            newTorpedoPredIcon1.transform.localScale = new Vector3(1, 0.6f, 1);
            newTorpedoPredIcon1.name = "TorPred 1";
            newTorpedoPredIcon1.SetActive(false);
            if (TorpedoPreIcon.ContainsKey(1))
            {
                TorpedoPreIcon[1] = newTorpedoPredIcon1;
            }
            else
            {
                TorpedoPreIcon.Add(1, newTorpedoPredIcon1);
            }


            foreach (var typeGroup in FireControlManager.Instance.Torpedos[myPlayerID])
            {
                Debug.Log("Type" + typeGroup.Key.ToString());
                // determine whether the torpedoTypeGroup is valid
                {
                    if (typeGroup.Value.Count == 0)
                    {
                        continue;
                    }

                    int validTorpedo = 0;
                    foreach (var torpedo in typeGroup.Value)
                    {
                        try
                        {
                            if (torpedo.Value.GetComponent<TorpedoLauncher>().myPlayerID == myPlayerID)
                            {
                                validTorpedo++;
                            }
                        }
                        catch { }
                    }
                    if (validTorpedo == 0)
                    {
                        continue;
                    }
                }
                foreach (var TorpedoPair in typeGroup.Value)
                {
                    Debug.Log(TorpedoPair.Key);
                    GameObject newOrienIcon = (GameObject)Instantiate(FCOrien.transform.Find("TorAimPrefab").gameObject, FCOrien.transform);
                    newOrienIcon.name = "TorAim " + typeGroup.Key;
                    newOrienIcon.SetActive(true);
                    if (TorpedoPreIcon.ContainsKey(TorpedoPair.Key))
                    {
                        TorpedoAimIcon[TorpedoPair.Key] = newOrienIcon;
                    }
                    else
                    {
                        TorpedoAimIcon.Add(TorpedoPair.Key, newOrienIcon); // guid, icon
                    }
                    
                }
            }

            Debug.Log("InitGunFireControl");
            foreach (var calibergroup in FireControlManager.Instance.Guns[myPlayerID])
            {
                {// determine whether the calibergroup is valid
                    if (calibergroup.Value.Count == 0)
                    {
                        continue;
                    }

                    int validGun = 0;
                    foreach (var gun in calibergroup.Value)
                    {
                        try
                        {
                            if (gun.Value.GetComponent<Gun>().myPlayerID == myPlayerID)
                            {
                                validGun++;
                            }
                        }
                        catch { }
                    }
                    if (validGun == 0)
                    {
                        continue;
                    }
                }

                Debug.Log("Caliber:" + calibergroup.Key);

                if (!PitchPredIcon.ContainsKey(calibergroup.Key))
                {
                    GameObject newPitchPredIcon = (GameObject)Instantiate(FCPitch.transform.Find("PredPrefab").gameObject, FCPitch.transform);
                    newPitchPredIcon.name = "Pred " + calibergroup.Key;
                    newPitchPredIcon.SetActive(true);
                    PitchPredIcon.Add(calibergroup.Key, newPitchPredIcon);
                }
                if (!OrienPredIcon.ContainsKey(calibergroup.Key))
                {
                    GameObject newOrienPredIcon = (GameObject)Instantiate(FCOrien.transform.Find("PredPrefab").gameObject, FCOrien.transform);
                    newOrienPredIcon.name = "Pred " + calibergroup.Key;
                    newOrienPredIcon.SetActive(true);
                    OrienPredIcon.Add(calibergroup.Key, newOrienPredIcon);
                }
                if (!FCResults.ContainsKey(calibergroup.Key))
                {
                    FCResults.Add(calibergroup.Key, new FCResult(0));
                }

                foreach (var gunPair in calibergroup.Value)
                {
                    Debug.Log(gunPair.Key);
                    GameObject newPitchIcon = (GameObject)Instantiate(FCPitch.transform.Find("AimPrefab").gameObject, FCPitch.transform);
                    newPitchIcon.name = "Aim " + calibergroup.Key;
                    newPitchIcon.SetActive(true);
                    PitchAimIcon.Add(gunPair.Key, newPitchIcon);
                    GameObject newOrienIcon = (GameObject)Instantiate(FCOrien.transform.Find("AimPrefab").gameObject, FCOrien.transform);
                    newOrienIcon.name = "Aim " + calibergroup.Key;
                    newOrienIcon.SetActive(true);
                    OrienAimIcon.Add(gunPair.Key, newOrienIcon);
                }
            }
        }
        public void UpdateGunIcon()
        {
            if (StatMaster.hudHidden)
            {
                FCCanvas.SetActive(false);
            }
            else
            {
                FCCanvas.SetActive(true);
            }
            // Gun
            {
                if (ControllerDataManager.Instance.lockData[myPlayerID].valid)
                {
                    Vector2 targetPos = new Vector2(ControllerDataManager.Instance.lockData[myPlayerID].position.x, ControllerDataManager.Instance.lockData[myPlayerID].position.z) + RandomError;
                    Vector2 targetVel = new Vector2(ControllerDataManager.Instance.lockData[myPlayerID].velocity.x, ControllerDataManager.Instance.lockData[myPlayerID].velocity.z);
                    foreach (var fcRes in FCResults)
                    {
                        FCResult res = CalculateGunFCPara(targetPos, targetVel, fcRes.Key);
                        fcRes.Value.Set(res.Orien, res.Pitch, res.hasRes, res.predPosition);
                    }
                    // upload FCResult
                    ControllerDataManager.Instance.ControllerFCResult[myPlayerID] = FCResults;
                    foreach (var PitchPred in PitchPredIcon)
                    {
                        PitchPred.Value.SetActive(true);
                        if (FCResults[PitchPred.Key].hasRes)
                        {
                            PitchPred.Value.transform.localEulerAngles = new Vector3(0, 0, -FCResults[PitchPred.Key].Pitch);
                        }
                        else
                        {
                            PitchPred.Value.transform.localEulerAngles = new Vector3(0, 0, -44);
                        }

                        PitchPred.Value.transform.Find("PredIcon").transform.localPosition = new Vector3(-PitchPred.Key / 5 - 50, 0, 0);
                    }
                    foreach (var OrienPred in OrienPredIcon)
                    {
                        OrienPred.Value.SetActive(true);
                        OrienPred.Value.transform.localEulerAngles = new Vector3(0, 0, FCResults[OrienPred.Key].Orien - 90);

                        OrienPred.Value.transform.Find("PredIcon").transform.localPosition = new Vector3(-OrienPred.Key / 10 - 50, 0, 0);
                    }
                }
                else
                {
                    foreach (var PitchPred in PitchPredIcon)
                    {
                        PitchPred.Value.SetActive(false);
                    }
                    foreach (var OrienPred in OrienPredIcon)
                    {
                        OrienPred.Value.SetActive(false);
                    }
                }

                foreach (var GunIcon in PitchAimIcon)
                {
                    try
                    {
                        Gun tmpGun = FireControlManager.Instance.GetGun(myPlayerID, GunIcon.Key).GetComponent<Gun>();
                        GunIcon.Value.transform.eulerAngles = new Vector3(0, 0, -tmpGun.GetFCPitchPara());
                        GunIcon.Value.transform.Find("GunIcon").transform.localPosition = new Vector3(-tmpGun.Caliber.Value / 5 - 50, 0, 0);
                    }
                    catch { }
                }
                foreach (var GunIcon in OrienAimIcon)
                {
                    try
                    {
                        Gun tmpGun = FireControlManager.Instance.GetGun(myPlayerID, GunIcon.Key).GetComponent<Gun>();
                        float angle;
                        if (ControllerDataManager.Instance.lockData[myPlayerID].valid && FCResults[tmpGun.Caliber.Value].hasRes)
                        {
                            angle = MathTool.Instance.SignedAngle(GetForward(), tmpGun.GetFCOrienPara()) +
                                    FCResults[tmpGun.Caliber.Value].Orien -
                                    FCResults[tmpGun.Caliber.Value].getTurrentAngle(GetForward(), new Vector2(tmpGun.transform.position.x, tmpGun.transform.position.z));
                        }
                        else
                        {
                            angle = MathTool.Instance.SignedAngle(GetForward(), tmpGun.GetFCOrienPara());
                        }
                        GunIcon.Value.transform.localEulerAngles = new Vector3(0, 0, angle - 90);
                        GunIcon.Value.transform.Find("GunIcon").transform.localPosition = new Vector3(-tmpGun.Caliber.Value / 10 - 50, 0, 0);
                    }
                    catch { }
                }
            }
            // torpedo
            {
                // Update Aim
                foreach (var TorIcon in TorpedoAimIcon)
                {
                    try
                    {
                        TorpedoLauncher tmpTor = FireControlManager.Instance.GetTorpedo(myPlayerID, TorIcon.Key).GetComponent<TorpedoLauncher>();
                        float angle;
                        angle = MathTool.Instance.SignedAngle(GetForward(), tmpTor.GetFCOrienPara());
                        TorIcon.Value.transform.eulerAngles = new Vector3(0, 0, angle);
                        TorIcon.Value.transform.localScale = new Vector3(1, (tmpTor.TorpedoType == 0 ? 1 : 0.6f), 1);
                    }
                    catch { }
                }
                //Update Prediction
                if (ControllerDataManager.Instance.lockData[myPlayerID].valid)
                {
                    foreach (var typeGroup in FireControlManager.Instance.Torpedos[myPlayerID])
                    {
                        
                        // determine whether the torpedoTypeGroup is valid
                        {
                            if (typeGroup.Value.Count == 0)
                            {
                                TorpedoPreIcon[typeGroup.Key].SetActive(false);
                                continue;
                            }

                            int validTorpedo = 0;
                            foreach (var torpedo in typeGroup.Value)
                            {
                                try
                                {
                                    if (torpedo.Value.GetComponent<TorpedoLauncher>().myPlayerID == myPlayerID)
                                    {
                                        validTorpedo++;
                                    }
                                }
                                catch { }
                            }
                            if (validTorpedo == 0)
                            {
                                TorpedoPreIcon[typeGroup.Key].SetActive(false);
                                continue;
                            }
                        }
                        //Debug.Log(typeGroup.Value.Count + " in " + typeGroup.Key);
                        TorpedoPreIcon[typeGroup.Key].SetActive(true);
                        Vector2 preDirection = CalculateTorpedoFCPara(  new Vector2(ControllerDataManager.Instance.lockData[myPlayerID].position.x, ControllerDataManager.Instance.lockData[myPlayerID].position.z),
                                                                        new Vector2(ControllerDataManager.Instance.lockData[myPlayerID].velocity.x, ControllerDataManager.Instance.lockData[myPlayerID].velocity.z),
                                                                        typeGroup.Key);
                        float angle = MathTool.Instance.SignedAngle(GetForward(), preDirection);
                        TorpedoPreIcon[typeGroup.Key].transform.eulerAngles = new Vector3(0, 0, angle);
                    }
                }
                else
                {
                    foreach(var TorPreIcon in TorpedoPreIcon)
                    {
                        TorPreIcon.Value.SetActive(false);
                    }
                }
                
            }
        }
        public Vector2 GetForward()
        {
            if (StatMaster.isClient)
            {
                return new Vector2(-transform.up.x, -transform.up.z);
                //return new Vector2(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].x, BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].z);
            }
            else
            {
                return new Vector2(-transform.up.x, -transform.up.z);
            }
        }
        public void UpdateVelocity()
        {
            if (StatMaster.isMP)
            {
                if (StatMaster.isClient)
                {
                    myVelocity = Vector3.Lerp(myVelocity, ControllerDataManager.Instance.ControllerVel[myPlayerID], 0.2f);
                }
                else
                {
                    myVelocity = gameObject.GetComponent<Rigidbody>().velocity;
                }
            }
            else
            {
                myVelocity = gameObject.GetComponent<Rigidbody>().velocity;
            }
        }
        public void UpdateRotation()
        {
            float nowYRotation = MathTool.Instance.SignedAngle(GetForward(), new Vector2(Vector3.forward.x, Vector3.forward.z));
            float deltaRotation;
            if (PreYRotation > 175 && nowYRotation < -175)
            {
                deltaRotation = nowYRotation - PreYRotation + 360;
            }
            else if (PreYRotation < -175 && nowYRotation > 175)
            {
                deltaRotation = PreYRotation - nowYRotation + 360;
            }
            else
            {
                deltaRotation = Mathf.Abs(nowYRotation - PreYRotation);
            }
            //Debug.Log(deltaRotation);
            PreYRotation = nowYRotation;
            RotationError += deltaRotation * 5 - 0.3f;
            RotationError = Mathf.Clamp(RotationError, 0, 25);
        }
        public void UpdateSpotModifier()
        {
            if (StatMaster.isMP)
            {
                if (StatMaster.isClient)
                {
                    if (PlayerData.localPlayer.networkId == myPlayerID)
                    {
                        SpotModifier *= Mathf.Pow(0.93f, ControllerDataManager.Instance.SpotNum[myPlayerID]);
                        ControllerDataManager.Instance.SpotNum[myPlayerID] = 0;
                    }
                }
                else
                {
                    if (myPlayerID == 0)
                    {
                        SpotModifier *= Mathf.Pow(0.93f, ControllerDataManager.Instance.SpotNum[myPlayerID]);
                        ControllerDataManager.Instance.SpotNum[myPlayerID] = 0;
                    }
                }
            }
            else
            {
                SpotModifier *= Mathf.Pow(0.93f, ControllerDataManager.Instance.SpotNum[myPlayerID]);
                ControllerDataManager.Instance.SpotNum[myPlayerID] = 0;
            }
            
        }
        public void UpdateClosingModifier()
        {
            ClosingModifier = 1 + (ControllerDataManager.Instance.lockData[myPlayerID].velocity - myVelocity).magnitude/10;
        }
        public void ResetBaseRandomError()
        {
            BaseRandomError = new Vector2(UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f);
            BaseRandomError /= BaseRandomError.magnitude;
            SpotModifier = 1;
            ClosingModifier = 1;
            //Debug.Log(BaseRandomError);
            //BaseRandomError *= (transform.position - LockDataManager.Instance.lockData[myPlayerID].position).magnitude / 7;
            //Debug.Log((transform.position - LockDataManager.Instance.lockData[myPlayerID].position).magnitude);
        }
        public void UpdateRandomError()
        {
            UpdateClosingModifier();
            UpdateSpotModifier();// update soptModifier
            //modify baseError 
            Vector3 OrienVector3 = transform.position - ControllerDataManager.Instance.lockData[myPlayerID].position;
            Vector2 OrienVector2 = new Vector2(OrienVector3.x, OrienVector3.z);
            //Debug.Log(OrienVector2 / 10);
            float FakeRandom = Mathf.Sign(BaseRandomError.x) * (Mathf.Abs(BaseRandomError.x) % 0.01f) * 50;
            //Debug.Log(FakeRandom);
            RandomError =   (BaseRandomError * OrienVector2.magnitude / 30 + OrienVector2/15 * FakeRandom + OrienVector2 / 10) * SpotModifier + 
                            RotationError * new Vector2(UnityEngine.Random.value-0.5f, UnityEngine.Random.value - 0.5f) * OrienVector2.magnitude/500;
            RandomError *= ClosingModifier;
        }

        public override void SafeAwake()
        {
            gameObject.name = "Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            TrackCannon = AddKey("Track Cannon", "TrackCannon", KeyCode.T);
            SwitchCannnon = AddKey("Switch Tracking Cannon", "SwitchTrackingCannon", KeyCode.RightShift);
            FCPanelSize = AddSlider("Fire Control Size", "FCSize", 1f, 0.2f, 5f);
            TurrentHeight = AddSlider("TurrentHeight", "TurrentHeight", 0.5f, 0f, 2f);
            
            Lock = AddKey("Lock", "WW2Lock", KeyCode.X);
            mySeed = (int)(UnityEngine.Random.value * 10);
        }
        public override void OnSimulateStart()
        {
            
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            LockIconOnScreen = ModResource.GetTexture("LockIconScreen Texture").Texture;
            PreYRotation = MathTool.Instance.SignedAngle(GetForward(), new Vector2(Vector3.forward.x, Vector3.forward.z));
            ControllerDataManager.Instance.ControllerObject[myPlayerID] = gameObject;
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    InitFireControlPanel();
                    AdjustFCPanel(); // adjust position and size
                }
            }
            catch { }
            

        }
        public override void OnSimulateStop()
        {
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    try
                    {
                        Destroy(GameObject.Find("FCCanvas"));
                    }
                    catch
                    {
                    }
                    Destroy(FCCanvas);
                    
                    PitchAimIcon.Clear();
                    OrienAimIcon.Clear();
                    PitchPredIcon.Clear();
                    OrienPredIcon.Clear();
                    TorpedoAimIcon.Clear();
                    TorpedoPreIcon.Clear();
                    Debug.Log("Clear Finish");
                }
            }
            catch { }
            
            SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;

            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    Locking = false;
                    ModNetworking.SendToAll(ControllerDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                }
                else
                {
                    Locking = false;
                    ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
                }
            }
            else
            {
                Locking = false;
                ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
            }
        }
        public void OnDestroy()
        {
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    try
                    {
                        Destroy(GameObject.Find("FCCanvas"));
                    }
                    catch
                    {
                    }
                    Destroy(FCCanvas);
                    
                    PitchAimIcon.Clear();
                    OrienAimIcon.Clear();
                    PitchPredIcon.Clear();
                    OrienPredIcon.Clear();
                    TorpedoAimIcon.Clear();
                    TorpedoPreIcon.Clear();
                    Debug.Log("Clear Finish");
                }
            }
            catch { }
            SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    Locking = false;
                    ModNetworking.SendToAll(ControllerDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                }
                else
                {
                    Locking = false;
                    ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
                }
            }
            else
            {
                Locking = false;
                ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
            }
        }

        public override void BuildingFixedUpdate()
        {
            ControllerDataManager.Instance.ControllerPos[myPlayerID] = transform.position;
        }
        public override void SimulateFixedUpdateAlways()
        {
            ControllerDataManager.Instance.ControllerPos[myPlayerID] = transform.position;
            UpdateVelocity();
            if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
            {
                UpdateRotation();
                UpdateRandomError();
                if (StatMaster.isMP)
                {
                    if (PlayerData.localPlayer.networkId != myPlayerID)
                    {
                        return;
                    }
                }
                if (!FCInitialized)
                {
                    FCInitialized = true;
                    InitGunFireControl();
                }
                else
                {
                    UpdateGunIcon();
                }
            }

        }
        public override void SimulateUpdateHost()
        {
            if (Lock.IsPressed)
            {
                if (StatMaster.isMP)
                {
                    if (myPlayerID == 0)
                    {
                        ResetBaseRandomError();
                        ControllerDataManager.Instance.cameraData[myPlayerID].valid = true;
                        ControllerDataManager.Instance.cameraData[myPlayerID].position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
                        ControllerDataManager.Instance.cameraData[myPlayerID].forward = Camera.main.ScreenPointToRay(Input.mousePosition).direction;
                    }
                }
                else
                {
                    ResetBaseRandomError();
                    ControllerDataManager.Instance.cameraData[myPlayerID].valid = true;
                    ControllerDataManager.Instance.cameraData[myPlayerID].position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
                    ControllerDataManager.Instance.cameraData[myPlayerID].forward = Camera.main.ScreenPointToRay(Input.mousePosition).direction;
                }
            }
        }
        public override void SimulateUpdateClient()
        {
            if (Lock.IsPressed)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    ResetBaseRandomError();
                    Debug.Log("SendCamera");
                    ModNetworking.SendToAll(ControllerDataManager.CameraMsg.CreateMessage(myPlayerID, Camera.main.ScreenPointToRay(Input.mousePosition).origin,
                                                                                                Camera.main.ScreenPointToRay(Input.mousePosition).direction));
                }
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (StatMaster.isMP)
            {
                if (ModController.Instance.state % 10 == mySeed)
                {
                    ModNetworking.SendToAll(ControllerDataManager.ControllerVelMsg.CreateMessage(myPlayerID, gameObject.GetComponent<Rigidbody>().velocity));
                }
            }
            if (ControllerDataManager.Instance.cameraData[myPlayerID].valid)
            {
                
                ControllerDataManager.Instance.cameraData[myPlayerID].valid = false;
                Ray cameraRay = new Ray(ControllerDataManager.Instance.cameraData[myPlayerID].position, ControllerDataManager.Instance.cameraData[myPlayerID].forward);
                RaycastHit hit;
                if (Physics.Raycast(cameraRay,out hit,3000))
                {
                    Debug.Log("true");
                    Locking = true;
                    lockingObject = hit.collider.transform.parent.gameObject;
                }
                else
                {
                    Debug.Log("false");
                    Locking = false;
                }
            }
            if (Locking)
            {
                if (StatMaster.isMP)
                {
                    if (myPlayerID != 0)
                    {
                        try
                        {
                            ModNetworking.SendToAll(ControllerDataManager.LockMsg.CreateMessage(myPlayerID, lockingObject.transform.position, lockingObject.GetComponent<Rigidbody>().velocity, true));
                        }
                        catch
                        {
                            Locking = false;
                            ModNetworking.SendToAll(ControllerDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                        }
                    }
                    else
                    {
                        try
                        {
                            ControllerDataManager.Instance.lockData[myPlayerID].valid = true;
                            ControllerDataManager.Instance.lockData[myPlayerID].position = lockingObject.transform.position;
                            ControllerDataManager.Instance.lockData[myPlayerID].velocity = lockingObject.GetComponent<Rigidbody>().velocity;
                        }
                        catch {
                            Locking = false;
                            ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
                        }
                    }
                }
                else
                {
                    try
                    {
                        ControllerDataManager.Instance.lockData[myPlayerID].valid = true;
                        ControllerDataManager.Instance.lockData[myPlayerID].position = lockingObject.transform.position;
                        ControllerDataManager.Instance.lockData[myPlayerID].velocity = lockingObject.GetComponent<Rigidbody>().velocity;
                    }
                    catch
                    {
                        Locking = false;
                        ControllerDataManager.Instance.lockData[myPlayerID].valid = false;
                    }
                }
                
            }
            else
            {
                ModNetworking.SendToAll(ControllerDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
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



            if (TrackCannon.IsHeld)
            {
                if (!TrackOn)
                {
                    TargetCannon = CannonTrackManager.Instance.GetTrackCannon(myPlayerID);

                }
                TrackOn = true;
            }
            else
            {
                TrackOn = false;
            }
            if (TrackOn)
            {
                if (SwitchCannnon.IsPressed)
                {
                    TargetCannon = CannonTrackManager.Instance.SwitchTrackCannon(myPlayerID);
                }
                if (TargetCannon && 
                    !(TargetCannon.transform.position.y < 20) &&
                    !TargetCannon.GetComponent<BulletBehaviour>().exploded)
                {
                    SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = false;
                    MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, TargetCannon.transform.position - 30f * TargetCannon.transform.forward,0.2f);
                    MainCamera.transform.rotation = TargetCannon.transform.rotation;
                }
            }
            else
            {
                SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;
            }
        }

        private void OnGUI()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    return;
                }
            }
            //GUI.Box(new Rect(100, 200, 200, 50), LockDataManager.Instance.cameraData[0].valid.ToString());
            //GUI.Box(new Rect(100, 300, 200, 50), LockDataManager.Instance.cameraData[1].valid.ToString());

            if (ControllerDataManager.Instance.lockData[myPlayerID].valid)
            {
                GUI.color = Color.green;
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(ControllerDataManager.Instance.lockData[myPlayerID].position);
                if (onScreenPosition.z >= 0)
                    GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), LockIconOnScreen);
            }
        }
    }
}
