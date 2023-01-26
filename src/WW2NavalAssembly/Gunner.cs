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
    public class GunnerMsgReceiver : SingleInstance<GunnerMsgReceiver>
    {
        public override string Name { get; } = "GunnerMsgReceiver";
        public static MessageType EmulateMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Integer, DataType.Integer);
        public static MessageType TargetMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean, DataType.Single, DataType.Single, DataType.Single);

        public Dictionary<int, int>[] EmulatePitch = new Dictionary<int, int>[16];
        public Dictionary<int, int>[] EmulateOrien = new Dictionary<int, int>[16];

        public Dictionary<int, bool>[] hasTarget = new Dictionary<int, bool>[16];
        public Dictionary<int, Vector2>[] TargetPredPos = new Dictionary<int, Vector2>[16];
        public Dictionary<int, float>[] TargrtPitch = new Dictionary<int, float>[16];

        public GunnerMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                EmulatePitch[i] = new Dictionary<int, int>();
                EmulateOrien[i] = new Dictionary<int, int>();
                hasTarget[i] = new Dictionary<int, bool>();
                TargetPredPos[i] = new Dictionary<int, Vector2>();
                TargrtPitch[i] = new Dictionary<int, float>();
            }
        }

        public void EmulateReceiver(Message msg)
        {
            EmulatePitch[(int)msg.GetData(0)][(int)msg.GetData(1)] = (int)msg.GetData(2);
            EmulateOrien[(int)msg.GetData(0)][(int)msg.GetData(1)] = (int)msg.GetData(3);
        }
        public void TargetReceiver(Message msg)
        {
            hasTarget[(int)msg.GetData(0)][(int)msg.GetData(1)] = (bool)msg.GetData(2);
            TargetPredPos[(int)msg.GetData(0)][(int)msg.GetData(1)] = new Vector2((float)msg.GetData(3), (float)msg.GetData(4));
            TargrtPitch[(int)msg.GetData(0)][(int)msg.GetData(1)] = (float)msg.GetData(5);
        }

    }
    class Gunner : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public float bindedCaliber;
        public List<GameObject> bindedGuns = new List<GameObject>();

        public MKey LeftKey;
        public MKey RightKey;
        public MKey UpKey;
        public MKey DownKey;
        public MText GunGroup;

        public int leftEmulateStage = 0;
        public int rightEmulateStage = 0;
        public int upEmulateStage = 0;
        public int downEmulateStage = 0;

        public int PreOrienDiection = 0;
        public int PrePitchDiection = 0;

        GameObject[] GunLine = new GameObject[4];

        public float targetPitch;
        public Vector2 targetPos;
        public bool hasTarget;

        public bool initialized = false;

        public override bool EmulatesAnyKeys { get { return true; } }
        public void RejectSpecific()
        {
            
        }
        public void GetFCPara()
        {
            if (ControllerDataManager.Instance.lockData[myPlayerID].valid)
            {
                if (ControllerDataManager.Instance.ControllerFCResult[myPlayerID].ContainsKey(bindedCaliber))
                {
                    if (ControllerDataManager.Instance.ControllerFCResult[myPlayerID][bindedCaliber].hasRes)
                    {
                        targetPitch = ControllerDataManager.Instance.ControllerFCResult[myPlayerID][bindedCaliber].Pitch;
                        targetPos = ControllerDataManager.Instance.ControllerFCResult[myPlayerID][bindedCaliber].predPosition;
                        hasTarget = true;
                    }
                    else
                    {
                        hasTarget = false;
                    }

                }
                else
                {
                    hasTarget = false;
                }
            }
            else
            {
                hasTarget = false;
            }

            
        }

        public void SendEmulateControl()
        {
            if (mySeed == ModController.Instance.state % 10)
            {
                ModNetworking.SendToHost(GunnerMsgReceiver.TargetMsg.CreateMessage(myPlayerID,myGuid,hasTarget,targetPos.x,targetPos.y,targetPitch));
            }
        }
        public void ReceiveEmulateControl()
        {
            //Orien
            if (GunnerMsgReceiver.Instance.EmulateOrien[myPlayerID][myGuid] == 1)
            {
                if (leftEmulateStage == 0)
                {
                    EmulateKeys(new MKey[0], LeftKey, true);
                    leftEmulateStage++;
                }
            }
            else if (GunnerMsgReceiver.Instance.EmulateOrien[myPlayerID][myGuid] == -1)
            {
                if (rightEmulateStage == 0)
                {
                    EmulateKeys(new MKey[0], RightKey, true);
                    rightEmulateStage++;
                }
            }
            else
            {
                if (leftEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], LeftKey, false);
                    leftEmulateStage--;
                }
                if (rightEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], RightKey, false);
                    rightEmulateStage--;
                }
            }

            //Pitch
            if (GunnerMsgReceiver.Instance.EmulatePitch[myPlayerID][myGuid] == 1)
            {
                if (upEmulateStage == 0)
                {
                    EmulateKeys(new MKey[0], UpKey, true);
                    upEmulateStage++;
                }
            }
            else if (GunnerMsgReceiver.Instance.EmulatePitch[myPlayerID][myGuid] == -1)
            {
                if (downEmulateStage == 0)
                {
                    EmulateKeys(new MKey[0], DownKey, true);
                    downEmulateStage++;
                }
            }
            else
            {
                if (upEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], UpKey, false);
                    upEmulateStage--;
                }
                if (downEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], DownKey, false);
                    downEmulateStage--;
                }
            }

        }
        public void EmulateControlOnHost()
        {
            if (GunnerMsgReceiver.Instance.hasTarget[myPlayerID][myGuid] && bindedGuns[0])
            {
                Vector2 GunForward = bindedGuns[0].GetComponent<Gun>().GetFCOrienPara();
                Vector2 targetVector = GunnerMsgReceiver.Instance.TargetPredPos[myPlayerID][myGuid] - new Vector2(bindedGuns[0].transform.position.x, bindedGuns[0].transform.position.z);
                //Debug.Log(MathTool.Instance.SignedAngle(GunForward, targetVector));
                if (MathTool.Instance.SignedAngle(GunForward, targetVector) > 0.5f)
                {
                    if (leftEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], LeftKey, true);
                        leftEmulateStage++;
                    }
                }
                else
                {
                    if (leftEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], LeftKey, false);
                        leftEmulateStage--;
                    }
                }
                if (MathTool.Instance.SignedAngle(GunForward, targetVector) < -0.5f)
                {
                    if (rightEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], RightKey, true);
                        rightEmulateStage++;
                    }

                }
                else
                {
                    if (rightEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], RightKey, false);
                        rightEmulateStage--;
                    }
                }

                if (GunnerMsgReceiver.Instance.TargrtPitch[myPlayerID][myGuid] - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() > 0.25f)
                {
                    if (upEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], UpKey, true);
                        upEmulateStage++;
                    }
                }
                else
                {
                    if (upEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], UpKey, false);
                        upEmulateStage--;
                    }
                }

                if (GunnerMsgReceiver.Instance.TargrtPitch[myPlayerID][myGuid] - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() < -0.25f)
                {
                    if (downEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], DownKey, true);
                        downEmulateStage++;
                    }
                }
                else
                {
                    if (downEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], DownKey, false);
                        downEmulateStage--;
                    }
                }

            }
            else
            {
                if (leftEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], LeftKey, false);
                    leftEmulateStage--;
                }
                if (rightEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], RightKey, false);
                    rightEmulateStage--;
                }
                if (upEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], UpKey, false);
                    upEmulateStage--;
                }
                if (downEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], DownKey, false);
                    downEmulateStage--;
                }
            }
        }
        public void EmulateControl()
        {
            if (hasTarget && bindedGuns[0])
            {
                Vector2 GunForward = bindedGuns[0].GetComponent<Gun>().GetFCOrienPara();
                Vector2 targetVector = targetPos - new Vector2(bindedGuns[0].transform.position.x, bindedGuns[0].transform.position.z);
                //Debug.Log(MathTool.Instance.SignedAngle(GunForward, targetVector));
                if (MathTool.Instance.SignedAngle(GunForward, targetVector) > 0.5f)
                {
                    if (leftEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], LeftKey, true);
                        leftEmulateStage ++;
                    }
                }
                else
                {
                    if (leftEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], LeftKey, false);
                        leftEmulateStage --;
                    }
                }
                if (MathTool.Instance.SignedAngle(GunForward, targetVector) < -0.5f)
                {
                    if (rightEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], RightKey, true);
                        rightEmulateStage ++;
                    }
                    
                }
                else
                {
                    if (rightEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], RightKey, false);
                        rightEmulateStage --;
                    }
                }

                if (targetPitch - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() > 0.25f)
                {
                    if (upEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], UpKey, true);
                        upEmulateStage++;
                    }
                }
                else
                {
                    if (upEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], UpKey, false);
                        upEmulateStage--;
                    }
                }

                if (targetPitch - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() < -0.25f)
                {
                    if (downEmulateStage == 0)
                    {
                        EmulateKeys(new MKey[0], DownKey, true);
                        downEmulateStage++;
                    }
                }
                else
                {
                    if (downEmulateStage == 1)
                    {
                        EmulateKeys(new MKey[0], DownKey, false);
                        downEmulateStage--;
                    }
                }

            }
            else
            {
                if (leftEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], LeftKey, false);
                    leftEmulateStage--;
                }
                if (rightEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], RightKey, false);
                    rightEmulateStage--;
                }
                if (upEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], UpKey, false);
                    upEmulateStage--;
                }
                if (downEmulateStage == 1)
                {
                    EmulateKeys(new MKey[0], DownKey, false);
                    downEmulateStage--;
                }
            }
            
        }
        public void GetMyCaliber()
        {
            bindedGuns.Clear();
            try
            {
                Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                int num = 0;
                if (gunlist.Count != 0)
                {
                    foreach (var gunObject in gunlist)
                    {
                        if (num > 3)
                        {
                            break;
                        }
                        if (!gunObject.Value || !gunObject.Value.GetComponent<BlockBehaviour>().isSimulating)
                        {
                            continue;
                        }
                        bindedCaliber = gunObject.Value.GetComponent<Gun>().Caliber.Value;
                        bindedGuns.Add(gunObject.Value);
                        num++;
                    }
                }
            }
            catch { }
        }
        public void initLine()
        {
            if (transform.Find("line0"))
            {
                for (int i = 0; i < 4; i++)
                {
                    GunLine[i] = transform.Find("line" + i.ToString()).gameObject;
                    GunLine[i].SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    GunLine[i] = new GameObject("line" + i.ToString());
                    GunLine[i].transform.SetParent(gameObject.transform);
                    LineRenderer LR = GunLine[i].AddComponent<LineRenderer>();
                    LR.material = new Material(Shader.Find("Particles/Additive"));
                    LR.SetColors(Color.red, Color.yellow);
                    LR.SetWidth(0.1f, 0.05f);
                    GunLine[i].SetActive(false);
                }
            }

        }
        public void ShowGroupLine()
        {
            foreach (GameObject line in GunLine)
            {
                line.SetActive(false);
            }
            try
            {
                Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                int num = 0;
                if (gunlist.Count != 0)
                {
                    foreach (var gunObject in gunlist)
                    {
                        if (num > 3)
                        {
                            break;
                        }
                        if (!gunObject.Value)
                        {
                            continue;
                        }
                        GunLine[num].GetComponent<LineRenderer>().SetPosition(0, transform.position);
                        GunLine[num].GetComponent<LineRenderer>().SetPosition(1, gunObject.Value.transform.position);
                        GunLine[num].SetActive(true);
                        num++;
                    }
                }
            }
            catch { }
        }
        public override void SafeAwake()
        {
            LeftKey = AddEmulatorKey("Left Key", "LeftKey", KeyCode.G);
            RightKey = AddEmulatorKey("Right Key", "RightKey", KeyCode.J);
            UpKey = AddEmulatorKey("Up Key", "UpKey", KeyCode.Y);
            DownKey = AddEmulatorKey("Down Key", "DownKey", KeyCode.H);
            GunGroup = AddText("Gun Group", "GunGroup", "g0");
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);
        }
        public void Start()
        {
            initLine();
        }
        public void Update()
        {
            if (ModController.Instance.showArmour)
            {
                ShowGroupLine();
            }
            else
            {
                foreach (GameObject line in GunLine)
                {
                    line.SetActive(false);
                }
            }
        }
        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            try
            {
                GunnerMsgReceiver.Instance.EmulatePitch[myPlayerID].Add(myGuid,0);
            }
            catch { }
            try
            {
                GunnerMsgReceiver.Instance.EmulateOrien[myPlayerID].Add(myGuid, 0);
            }
            catch { }
            try
            {
                GunnerMsgReceiver.Instance.TargetPredPos[myPlayerID].Add(myGuid, Vector2.zero);
            }
            catch {
                GunnerMsgReceiver.Instance.TargetPredPos[myPlayerID][myGuid] = Vector2.zero;
            }
            try
            {
                GunnerMsgReceiver.Instance.hasTarget[myPlayerID].Add(myGuid, false);
            }
            catch {
                GunnerMsgReceiver.Instance.hasTarget[myPlayerID][myGuid] = false;
            }
            try
            {
                GunnerMsgReceiver.Instance.TargrtPitch[myPlayerID].Add(myGuid, 0);
            }
            catch {
                GunnerMsgReceiver.Instance.TargrtPitch[myPlayerID][myGuid] = 0;
            }

        }
        public override void OnSimulateStop()
        {
            if (StatMaster.isMP)
            {
                if (myPlayerID == 0)
                {
                }
                else
                {
                    
                }
            }
            else
            {
            }
        }
        public override void SimulateFixedUpdateAlways()
        {
            if (!initialized)
            {
                initialized = true;
                GetMyCaliber();
            }

            RejectSpecific();
            GetFCPara();
            
        }
        public override void SimulateFixedUpdateClient()
        {
            SendEmulateControl();
        }
        public override void SendKeyEmulationUpdateHost()
        {
            if (StatMaster.isMP)
            {
                if (myPlayerID == 0)
                {
                    EmulateControl();
                }
                else
                {
                    EmulateControlOnHost();
                }
            }
            else
            {
                EmulateControl();
            }
            
        }
    }
}
