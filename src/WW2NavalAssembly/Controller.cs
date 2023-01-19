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
    public class LockDataManager : SingleInstance<LockDataManager>
    {
        public override string Name { get; } = "LockDataManager";
        public static MessageType LockMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Vector3, DataType.Boolean);
        public static MessageType CameraMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Vector3);

        public LockData[] lockData = new LockData[16];
        public CameraData[] cameraData = new CameraData[16];
        
        public LockDataManager()
        {
            for (int i = 0; i < 16; i++)
            {
                lockData[i] = new LockData();
                cameraData[i] = new CameraData();
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
            cameraData[(int)msg.GetData(0)].position = (Vector3)msg.GetData(1);
            cameraData[(int)msg.GetData(0)].forward = (Vector3)msg.GetData(2);
            cameraData[(int)msg.GetData(0)].valid = true;
            //Debug.Log("Receive Camera");
        }
    }

    public class Controller : BlockScript
    {
        public int myPlayerID;
        public int myGuid;

        public MKey TrackCannon;
        public MKey SwitchCannnon;
        public MKey Lock;
        public MSlider FCPanelSize;

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
        public Dictionary<float, FCResult> FCResults = new Dictionary<float, FCResult>();

        public bool Locking = false;
        public GameObject lockingObject;

        public int iconSize = 128;
        public Texture LockIconOnScreen;

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
        public FCResult CalculateFCPara(Vector2 targetPosition, Vector2 velocity, float caliber)
        {
            Vector2 myPosition = new Vector2(transform.position.x, transform.position.z);
            float dist = (targetPosition - myPosition).magnitude;
            Dist2PitchResult pitchRes = CalculatePitchFromDist(dist, caliber);
            if (pitchRes.hasResult)
            {
                dist = (targetPosition + velocity*pitchRes.time - myPosition).magnitude;
                pitchRes = CalculatePitchFromDist(dist, caliber);
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
        public Dist2PitchResult CalculatePitchFromDist(float dist, float caliber)
        {
            //Debug.Log("Start Iterating");
            float initialSpeed = Mathf.Sqrt(caliber + 100) * 8.5f;
            float g = 32.4f;
            float vx;
            float vy;
            float esT = 0;
            float angle = 0;
            for (int i = 0; i < 8; i++)
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
            return new Dist2PitchResult(esT, angle * 180/Mathf.PI);
        }
        public void InitFireControlPanel()
        {
            try
            {
                Destroy(GameObject.Find("FCCanvas"));
            }
            catch
            {
            }
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
        }
        public void InitGunFireControl()
        {
            Debug.Log("InitGunFireControl");
            foreach (var calibergroup in FireControlManager.Instance.Guns[myPlayerID])
            {
                if (calibergroup.Value.Count == 0)
                {
                    continue;
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
            if (LockDataManager.Instance.lockData[myPlayerID].valid)
            {
                Vector2 targetPos = new Vector2(LockDataManager.Instance.lockData[myPlayerID].position.x, LockDataManager.Instance.lockData[myPlayerID].position.z);
                Vector2 targetVel = new Vector2(LockDataManager.Instance.lockData[myPlayerID].velocity.x, LockDataManager.Instance.lockData[myPlayerID].velocity.z);
                foreach (var fcRes in FCResults)
                {
                    FCResult res = CalculateFCPara(targetPos, targetVel, fcRes.Key);
                    fcRes.Value.Set(res.Orien, res.Pitch, res.hasRes, res.predPosition);
                }
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
                    if (LockDataManager.Instance.lockData[myPlayerID].valid && FCResults[tmpGun.Caliber.Value].hasRes)
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
        public Vector2 GetForward()
        {
            if (StatMaster.isClient)
            {
                return new Vector2(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].x, BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].z);
            }
            else
            {
                return new Vector2(-transform.up.x, -transform.up.z);
            }
        }

        public override void SafeAwake()
        {
            gameObject.name = "Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            TrackCannon = AddKey("Track Cannon", "TrackCannon", KeyCode.T);
            SwitchCannnon = AddKey("Switch Tracking Cannon", "SwitchTrackingCannon", KeyCode.RightShift);
            FCPanelSize = AddSlider("Fire Control Size", "FCSize", 1f, 0.2f, 5f);
            Lock = AddKey("Lock", "WW2Lock", KeyCode.X);
            
        }
        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            LockIconOnScreen = ModResource.GetTexture("LockIconScreen Texture").Texture;
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    InitFireControlPanel();
                    AdjustFCPanel(); // adjust position and size
                }
            }
            catch { }
            
            try
            {
                if (StatMaster.isClient)
                {
                    BlockPoseReceiver.Instance.forward[myPlayerID].Add(myGuid, Vector3.zero);
                }
            }
            catch { }

        }
        public override void OnSimulateStop()
        {
            try
            {
                Destroy(GameObject.Find("FCCanvas"));
            }
            catch
            {
            }
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    Destroy(FCCanvas);
                    
                    PitchAimIcon.Clear();
                    OrienAimIcon.Clear();
                    PitchPredIcon.Clear();
                    OrienPredIcon.Clear();
                }
            }
            catch { }
            
            SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;

            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    Locking = false;
                    ModNetworking.SendToAll(LockDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                }
                else
                {
                    Locking = false;
                    LockDataManager.Instance.lockData[myPlayerID].valid = false;
                }
            }
            else
            {
                Locking = false;
                LockDataManager.Instance.lockData[myPlayerID].valid = false;
            }
        }
        public void OnDestroy()
        {
            try
            {
                Destroy(GameObject.Find("FCCanvas"));
            }
            catch
            {
            }
            try
            {
                if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
                {
                    Destroy(FCCanvas);
                    
                    PitchAimIcon.Clear();
                    OrienAimIcon.Clear();
                    PitchPredIcon.Clear();
                    OrienPredIcon.Clear();
                }
            }
            catch { }
            SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    Locking = false;
                    ModNetworking.SendToAll(LockDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                }
                else
                {
                    Locking = false;
                    LockDataManager.Instance.lockData[myPlayerID].valid = false;
                }
            }
            else
            {
                Locking = false;
                LockDataManager.Instance.lockData[myPlayerID].valid = false;
            }
        }
        public override void SimulateFixedUpdateAlways()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    return;
                }
            }
            if (!StatMaster.isMP || PlayerData.localPlayer.networkId == myPlayerID)
            {
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
                        LockDataManager.Instance.cameraData[myPlayerID].valid = true;
                        LockDataManager.Instance.cameraData[myPlayerID].position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
                        LockDataManager.Instance.cameraData[myPlayerID].forward = Camera.main.ScreenPointToRay(Input.mousePosition).direction;
                    }
                }
                else
                {
                    LockDataManager.Instance.cameraData[myPlayerID].valid = true;
                    LockDataManager.Instance.cameraData[myPlayerID].position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
                    LockDataManager.Instance.cameraData[myPlayerID].forward = Camera.main.ScreenPointToRay(Input.mousePosition).direction;
                }
            }
        }
        public override void SimulateUpdateClient()
        {
            if (Lock.IsPressed)
            {
                if (PlayerData.localPlayer.networkId == myPlayerID)
                {
                    ModNetworking.SendToAll(LockDataManager.CameraMsg.CreateMessage(myPlayerID, Camera.main.ScreenPointToRay(Input.mousePosition).origin,
                                                                                                Camera.main.ScreenPointToRay(Input.mousePosition).direction));
                }
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (StatMaster.isMP)
            {
                ModNetworking.SendToAll(BlockPoseReceiver.forwardMsg.CreateMessage(myPlayerID, myGuid, -transform.up));
            }
            if (LockDataManager.Instance.cameraData[myPlayerID].valid)
            {
                LockDataManager.Instance.cameraData[myPlayerID].valid = false;
                Ray cameraRay = new Ray(LockDataManager.Instance.cameraData[myPlayerID].position, LockDataManager.Instance.cameraData[myPlayerID].forward);
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
                            ModNetworking.SendToAll(LockDataManager.LockMsg.CreateMessage(myPlayerID, lockingObject.transform.position, lockingObject.GetComponent<Rigidbody>().velocity, true));
                        }
                        catch
                        {
                            Locking = false;
                            ModNetworking.SendToAll(LockDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
                        }
                    }
                    else
                    {
                        try
                        {
                            LockDataManager.Instance.lockData[myPlayerID].valid = true;
                            LockDataManager.Instance.lockData[myPlayerID].position = lockingObject.transform.position;
                            LockDataManager.Instance.lockData[myPlayerID].velocity = lockingObject.GetComponent<Rigidbody>().velocity;
                        }
                        catch {
                            Locking = false;
                            LockDataManager.Instance.lockData[myPlayerID].valid = false;
                        }
                    }
                }
                else
                {
                    try
                    {
                        LockDataManager.Instance.lockData[myPlayerID].valid = true;
                        LockDataManager.Instance.lockData[myPlayerID].position = lockingObject.transform.position;
                        LockDataManager.Instance.lockData[myPlayerID].velocity = lockingObject.GetComponent<Rigidbody>().velocity;
                    }
                    catch
                    {
                        Locking = false;
                        LockDataManager.Instance.lockData[myPlayerID].valid = false;
                    }
                }
                
            }
            else
            {
                ModNetworking.SendToAll(LockDataManager.LockMsg.CreateMessage(myPlayerID, Vector3.zero, Vector3.zero, false));
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

            if (LockDataManager.Instance.lockData[myPlayerID].valid)
            {
                //GUI.Box(new Rect(100, 200, 200, 50), LockDataManager.Instance.lockData[myPlayerID].position.ToString());
                GUI.color = Color.green;
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(LockDataManager.Instance.lockData[myPlayerID].position);
                if (onScreenPosition.z >= 0)
                    GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), LockIconOnScreen);
            }
        }
    }
}
