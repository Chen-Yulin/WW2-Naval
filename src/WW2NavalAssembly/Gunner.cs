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
        public static MessageType GunnerActiveMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean);

        public Dictionary<int, int>[] EmulatePitch = new Dictionary<int, int>[16];
        public Dictionary<int, int>[] EmulateOrien = new Dictionary<int, int>[16];

        public Dictionary<int, bool>[] hasTarget = new Dictionary<int, bool>[16];
        public Dictionary<int, Vector2>[] TargetPredPos = new Dictionary<int, Vector2>[16];
        public Dictionary<int, float>[] TargrtPitch = new Dictionary<int, float>[16];
        public Dictionary<int, bool>[] GunnerActive = new Dictionary<int, bool>[16];

        public GunnerMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                EmulatePitch[i] = new Dictionary<int, int>();
                EmulateOrien[i] = new Dictionary<int, int>();
                hasTarget[i] = new Dictionary<int, bool>();
                TargetPredPos[i] = new Dictionary<int, Vector2>();
                TargrtPitch[i] = new Dictionary<int, float>();
                GunnerActive[i] = new Dictionary<int, bool>();
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
        public void GunnerActiveReceiver(Message msg)
        {
            GunnerActive[(int)msg.GetData(0)][(int)msg.GetData(1)] = (bool)msg.GetData(2);
        }

    }

    public class GunnerDataBase : SingleInstance<GunnerDataBase>
    {
        public override string Name { get; } = "Gunner Data Base";

        public Dictionary<int, BlockBehaviour>[] HingeInfo = new Dictionary<int, BlockBehaviour>[16];
        public Dictionary<int, BlockBehaviour>[] GunInfo = new Dictionary<int, BlockBehaviour>[16];

        public GunnerDataBase()
        {
            for (int i = 0; i < 16; i++)
            {
                HingeInfo[i] = new Dictionary<int, BlockBehaviour>();
                GunInfo[i] = new Dictionary<int, BlockBehaviour>();
            }
        }
        public void AddHinge(int playerID, int Guid, BlockBehaviour info)
        {
            if (!HingeInfo[playerID].ContainsKey(Guid))
            {
                HingeInfo[playerID].Add(Guid, info);
            }
            else
            {
                HingeInfo[playerID][Guid] = info;
            }
        }
        public void RemoveHinge(int playerID, int Guid)
        {
            if (HingeInfo[playerID].ContainsKey(Guid))
            {
                HingeInfo[playerID].Remove(Guid);
            }
        }
        public void AddGun(int playerID, int Guid, BlockBehaviour info)
        {
            if (!GunInfo[playerID].ContainsKey(Guid))
            {
                GunInfo[playerID].Add(Guid, info);
            }
            else
            {
                GunInfo[playerID][Guid] = info;
            }
        }
        public void RemoveGun(int playerID, int Guid)
        {
            if (GunInfo[playerID].ContainsKey(Guid))
            {
                GunInfo[playerID].Remove(Guid);
            }
        }

        public void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 50), HingeInfo[0].Count.ToString());
        }
    }
    class Gunner : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public float bindedCaliber;
        public List<GameObject> bindedGuns = new List<GameObject>();
        public List<SteeringWheel> OrienHinge = new List<SteeringWheel>();
        public List<SteeringWheel> PitchHinge = new List<SteeringWheel>();
        public List<Gun> FireGun = new List<Gun>();

        public MKey ActiveSwitch;
        public MKey LeftKey;
        public MKey RightKey;
        public MKey UpKey;
        public MKey DownKey;
        public MKey FireKey;
        public MText GunGroup;
        public MSlider OrienFaultTolerance;
        public MSlider ElevationFaultTolerance;
        public MSlider TurningSpeed;
        public MLimits Limit;

        public int leftEmulateStage = 0;
        public int rightEmulateStage = 0;
        public int upEmulateStage = 0;
        public int downEmulateStage = 0;
        public int fireEmulateStage = 0;

        public int PreOrienDiection = 0;
        public int PrePitchDiection = 0;

        GameObject[] GunLine = new GameObject[4];
        GameObject AngleCenter;

        public float targetTime;
        public float targetPitch;
        public Vector2 targetPos;
        public bool hasTarget;
        public bool GunnerActive = true;
        public float OrienCenterAngle;
        public float OrienGunSpan;
        public bool OrienLimitValid;
        public float PitchCenterAngle;
        public float PitchGunSpan;

        public float turningSpeed = 1;

        public bool initialized = false;

        public Texture GunnerAlertIcon;
        int iconSize = 30;

        public override bool EmulatesAnyKeys { get { return true; } }

        public void LimitHinge(SteeringWheel sw)
        {
            float num4;
            float num5;
            if (sw.Flipped)
            {
                num4 = 0f - sw.LimitsSlider.Min;
                num5 = sw.LimitsSlider.Max;
            }
            else
            {
                num4 = 0f - sw.LimitsSlider.Max;
                num5 = sw.LimitsSlider.Min;
            }

            sw.AngleToBe = ((sw.AngleToBe < num4) ? num4 : ((!(sw.AngleToBe > num5)) ? sw.AngleToBe : num5));
        }
        public void SetWW2Hinge(bool flag)
        {
            foreach (var sw in OrienHinge)
            {
                sw.GetComponent<WW2Hinge>().gunnerControlling = flag;
            }
            foreach (var sw in PitchHinge)
            {
                sw.GetComponent<WW2Hinge>().gunnerControlling = flag;
            }
        }
        public void Fire(bool flag = true)
        {
            foreach (var gun in FireGun)
            {
                gun.triggeredByGunner = flag;
            }
        }
        public void TurnUp(float delta)
        {
            foreach (var sw in PitchHinge)
            {
                bool same = false;
                MKey swKey = sw.GetComponent<BlockBehaviour>().KeyList[0];
                if (swKey.useMessage)
                {
                    if (UpKey.message[0] == swKey.message[0])
                    {
                        same = true;
                    }
                }
                else
                {
                    for (int i = 0; i < swKey.KeysCount; i++)
                    {
                        if (UpKey.GetKey(0) == sw.GetComponent<BlockBehaviour>().KeyList[0].GetKey(i))
                        {
                            same = true;
                            break;
                        }
                    }
                }

                //Debug.Log(sw.LimitsSlider.Min + " " + sw.LimitsSlider.Max);
                float mySpeed;
                if (delta > 3)
                {
                    mySpeed = turningSpeed;
                }
                else
                {
                    mySpeed = Mathf.Clamp(delta, 0.1f, 3f) / 3 * turningSpeed;
                }
                if (same)
                {
                    sw.AngleToBe += (!sw.Flipped) ? mySpeed : -mySpeed;
                }
                else
                {
                    sw.AngleToBe += (sw.Flipped) ? mySpeed : -mySpeed;
                }

                LimitHinge(sw);

            }
        }
        public void TurnDown(float delta)
        {
            foreach (var sw in PitchHinge)
            {
                bool same = false;
                MKey swKey = sw.GetComponent<BlockBehaviour>().KeyList[0];
                if (swKey.useMessage)
                {
                    if (UpKey.message[0] == swKey.message[0])
                    {
                        same = true;
                    }
                }
                else
                {
                    for (int i = 0; i < swKey.KeysCount; i++)
                    {
                        if (UpKey.GetKey(0) == sw.GetComponent<BlockBehaviour>().KeyList[0].GetKey(i))
                        {
                            same = true;
                            break;
                        }
                    }
                }

                //Debug.Log(sw.LimitsSlider.Min + " " + sw.LimitsSlider.Max);
                float mySpeed;
                if (delta > 3)
                {
                    mySpeed = turningSpeed;
                }
                else
                {
                    mySpeed = Mathf.Clamp(delta, 0.1f, 3f) / 3 * turningSpeed;
                }
                if (same)
                {
                    sw.AngleToBe += (sw.Flipped) ? mySpeed : -mySpeed;
                }
                else
                {
                    sw.AngleToBe += (!sw.Flipped) ? mySpeed : -mySpeed;
                }
                
                LimitHinge(sw);
            }
        }
        public void TurnLeft(float delta, bool OutOfSpan)
        {
            foreach (var sw in OrienHinge)
            {
                bool same = false;
                MKey swKey = sw.GetComponent<BlockBehaviour>().KeyList[0];
                if (swKey.useMessage)
                {
                    if (LeftKey.message[0] == swKey.message[0])
                    {
                        same = true;
                    }
                }
                else
                {
                    for (int i = 0; i < swKey.KeysCount; i++)
                    {
                        if (LeftKey.GetKey(0) == sw.GetComponent<BlockBehaviour>().KeyList[0].GetKey(i))
                        {
                            same = true;
                            break;
                        }
                    }
                }
                //Debug.Log(sw.LimitsSlider.Min + " " + sw.LimitsSlider.Max);
                float mySpeed;
                if (OutOfSpan || delta > 3)
                {
                    mySpeed = turningSpeed;
                }
                else
                {
                    mySpeed = Mathf.Clamp(delta,0.1f,3f) / 3 * turningSpeed;
                }
                if (same)
                {
                    sw.AngleToBe += (!sw.Flipped) ? mySpeed : -mySpeed;
                }
                else
                {
                    sw.AngleToBe += (sw.Flipped) ? mySpeed : -mySpeed;
                }
                if (OrienLimitValid)
                {
                    float tmpCenter = OrienCenterAngle * (sw.transform.up.y > 0 ? 1 : -1) * (same ? 1 : -1);
                    sw.AngleToBe = Mathf.Clamp(sw.AngleToBe, tmpCenter - OrienGunSpan, tmpCenter + OrienGunSpan);
                }
                

            }
        }
        public void TurnRight(float delta, bool OutOfSpan)
        {
            foreach (var sw in OrienHinge)
            {
                bool same = false;
                MKey swKey = sw.GetComponent<BlockBehaviour>().KeyList[0];
                if (swKey.useMessage)
                {
                    if (LeftKey.message[0] == swKey.message[0])
                    {
                        same = true;
                    }
                }
                else
                {
                    for (int i = 0; i < swKey.KeysCount; i++)
                    {
                        if (LeftKey.GetKey(0) == sw.GetComponent<BlockBehaviour>().KeyList[0].GetKey(i))
                        {
                            same = true;
                            break;
                        }
                    }
                }

                float mySpeed;
                if (OutOfSpan || delta > 3)
                {
                    mySpeed = turningSpeed;
                }
                else
                {
                    mySpeed = Mathf.Clamp(delta, 0.1f, 3f) / 3 * turningSpeed;
                }
                if (same)
                {
                    sw.AngleToBe += (sw.Flipped) ? mySpeed : -mySpeed;
                }
                else
                {
                    sw.AngleToBe += (!sw.Flipped) ? mySpeed : -mySpeed;
                }
                if (OrienLimitValid)
                {
                    float tmpCenter = OrienCenterAngle * (sw.transform.up.y > 0 ? 1 : -1) * (same ? 1 : -1);
                    sw.AngleToBe = Mathf.Clamp(sw.AngleToBe, tmpCenter - OrienGunSpan, tmpCenter + OrienGunSpan);
                }
                

            }
        }
        public void FindHinge()
        {
            foreach (var Blockpair in GunnerDataBase.Instance.HingeInfo[myPlayerID])
            {
                foreach (var key in Blockpair.Value.KeyList)
                {
                    bool find = false;
                    if (LeftKey.useMessage && key.useMessage)
                    {
                        if (LeftKey.message[0] == key.message[0])
                        {
                            //Debug.Log("Detect Orien Hinge:" + Blockpair.Value.BuildingBlock.Guid.ToString());
                            OrienHinge.Add(Blockpair.Value.GetComponent<SteeringWheel>());
                            find = true;
                            break;
                        }
                    }
                    else if (!LeftKey.useMessage && !key.useMessage)
                    {
                        for (int i = 0; i < key.KeysCount; i++)
                        {
                            if (LeftKey.GetKey(0) == key.GetKey(i))
                            {
                                //Debug.Log("Detect Orien Hinge:" + Blockpair.Value.BuildingBlock.Guid.ToString());
                                OrienHinge.Add(Blockpair.Value.GetComponent<SteeringWheel>());
                                find = true;
                                break;
                            }
                        }
                    }
                    if (UpKey.useMessage && key.useMessage)
                    {
                        if (UpKey.message[0] == key.message[0])
                        {
                            //Debug.Log("Detect Pitch Hinge:" + Blockpair.Value.BuildingBlock.Guid.ToString());
                            PitchHinge.Add(Blockpair.Value.GetComponent<SteeringWheel>());
                            find = true;
                            break;
                        }
                    }else if (!UpKey.useMessage && !key.useMessage)
                    {
                        for (int i = 0; i < key.KeysCount; i++)
                        {
                            if (UpKey.GetKey(0) == key.GetKey(i))
                            {
                                //Debug.Log("Detect Pitch Hinge:" + Blockpair.Value.BuildingBlock.Guid.ToString());
                                PitchHinge.Add(Blockpair.Value.GetComponent<SteeringWheel>());
                                find = true;
                                break;
                            }
                        }
                    }
                    if (find)
                    {
                        break;
                    }
                }
            }
        }
        public void FindGun()
        {
            foreach (var GunPair in GunnerDataBase.Instance.GunInfo[myPlayerID])
            {
                foreach (var key in GunPair.Value.KeyList)
                {
                    bool find = false;
                    if (FireKey.useMessage && key.useMessage)
                    {
                        if (FireKey.message[0] == key.message[0])
                        {
                            //Debug.Log("Detect Fire Gun:" + GunPair.Value.BuildingBlock.Guid.ToString());
                            FireGun.Add(GunPair.Value.GetComponent<Gun>());
                            find = true;
                            break;
                        }
                    }
                    else if (!FireKey.useMessage && !key.useMessage)
                    {
                        for (int i = 0; i < key.KeysCount; i++)
                        {
                            if (FireKey.GetKey(0) == key.GetKey(i))
                            {
                                //Debug.Log("Detect Fire Gun:" + GunPair.Value.BuildingBlock.Guid.ToString());
                                FireGun.Add(GunPair.Value.GetComponent<Gun>());
                                find = true;
                                break;
                            }
                        }
                    }
                    if (find)
                    {
                        break;
                    }
                }
            }
        }
        public bool GenerateHingeCenter()
        {
            if (!bindedGuns[0])
            {
                return false;
            }



            try {
                SteeringWheel sw = OrienHinge[0];

                bool same = false;
                MKey swKey = sw.GetComponent<BlockBehaviour>().KeyList[0];
                if (swKey.useMessage)
                {
                    if (LeftKey.message[0] == swKey.message[0])
                    {
                        same = true;
                    }
                }
                else
                {
                    for (int i = 0; i < swKey.KeysCount; i++)
                    {
                        if (LeftKey.GetKey(0) == sw.GetComponent<BlockBehaviour>().KeyList[0].GetKey(i))
                        {
                            same = true;
                        }
                    }
                }
                AngleCenter = new GameObject("Angle Center");
                AngleCenter.transform.SetParent(ControllerDataManager.Instance.ControllerObject[myPlayerID].transform);
                AngleCenter.transform.localPosition = Vector3.zero;
                AngleCenter.transform.eulerAngles = bindedGuns[0].transform.eulerAngles + new Vector3(0, OrienCenterAngle * (same?1:-1), 0);
                return true;
            }
            catch {
                return false;
            }
            
            
        }
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
                        targetTime = ControllerDataManager.Instance.ControllerFCResult[myPlayerID][bindedCaliber].timer;
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

        public void SendTargetToHost()
        {
            if (mySeed == ModController.Instance.state % 10)
            {
                ModNetworking.SendToHost(GunnerMsgReceiver.TargetMsg.CreateMessage(myPlayerID,myGuid,hasTarget,targetPos.x,targetPos.y,targetPitch));
            }
        }
        //public void ReceiveEmulateControl()
        //{
        //    //Orien
        //    if (GunnerMsgReceiver.Instance.EmulateOrien[myPlayerID][myGuid] == 1)
        //    {
        //        if (leftEmulateStage == 0)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), LeftKey, true);
        //            leftEmulateStage++;
        //        }
        //    }
        //    else if (GunnerMsgReceiver.Instance.EmulateOrien[myPlayerID][myGuid] == -1)
        //    {
        //        if (rightEmulateStage == 0)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), RightKey, true);
        //            rightEmulateStage++;
        //        }
        //    }
        //    else
        //    {
        //        if (leftEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), LeftKey, false);
        //            leftEmulateStage--;
        //        }
        //        if (rightEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), RightKey, false);
        //            rightEmulateStage--;
        //        }
        //    }

        //    //Pitch
        //    if (GunnerMsgReceiver.Instance.EmulatePitch[myPlayerID][myGuid] == 1)
        //    {
        //        if (upEmulateStage == 0)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), UpKey, true);
        //            upEmulateStage++;
        //        }
        //    }
        //    else if (GunnerMsgReceiver.Instance.EmulatePitch[myPlayerID][myGuid] == -1)
        //    {
        //        if (downEmulateStage == 0)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), DownKey, true);
        //            downEmulateStage++;
        //        }
        //    }
        //    else
        //    {
        //        if (upEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), UpKey, false);
        //            upEmulateStage--;
        //        }
        //        if (downEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), DownKey, false);
        //            downEmulateStage--;
        //        }
        //    }

        //} // deprecated


        public void ControlTurrent(bool controlling, bool OutOfSpan = false, float OrienDelta = 0, float TargetOrien = 0, float PitchDelta = 0)
        {
            if (controlling)
            {
                bool GunReady = true;

                if ((!OutOfSpan && OrienDelta > 0) ||
                    (OutOfSpan && TargetOrien > 0))
                {
                    if ((!OutOfSpan && OrienDelta > OrienFaultTolerance.Value) || OutOfSpan)
                    {
                        GunReady = false;
                    }

                    TurnLeft(OrienDelta, OutOfSpan);
                }
                if ((!OutOfSpan && OrienDelta < 0) ||
                    (OutOfSpan && TargetOrien < 0))
                {
                    if ((!OutOfSpan && OrienDelta < -OrienFaultTolerance.Value) || OutOfSpan)
                    {
                        GunReady = false;
                    }
                    TurnRight(-OrienDelta, OutOfSpan);
                }

                if (PitchDelta > 0)
                {
                    if (PitchDelta > ElevationFaultTolerance.Value)
                    {
                        GunReady = false;
                    }
                    TurnUp(PitchDelta);
                }

                if (PitchDelta < 0)
                {
                    if (PitchDelta < -ElevationFaultTolerance.Value)
                    {
                        GunReady = false;
                    }
                    TurnDown(-PitchDelta);
                }

                if (GunReady)
                {
                    Fire();
                }
                else
                {
                    Fire(false);
                }
            }
            else
            {
                SetWW2Hinge(false);
                Fire(false);
            }
        }
        public void EmulateControlOnHost()
        {
            if (GunnerMsgReceiver.Instance.hasTarget[myPlayerID][myGuid] && bindedGuns[0])
            {
                Vector2 GunForward = bindedGuns[0].GetComponent<Gun>().GetFCOrienPara();

                Vector2 CenterForward = new Vector2(1,0);
                if (OrienLimitValid)
                {
                    CenterForward = new Vector2(AngleCenter.transform.forward.x, AngleCenter.transform.forward.z);
                }
                
                Vector2 targetVector = GunnerMsgReceiver.Instance.TargetPredPos[myPlayerID][myGuid] - new Vector2(bindedGuns[0].transform.position.x, bindedGuns[0].transform.position.z);
                bool OutOfSpan = OrienLimitValid ? (Vector2.Angle(targetVector, CenterForward) > OrienGunSpan ||
                    (Mathf.Sign(MathTool.SignedAngle(targetVector, -CenterForward)) == Mathf.Sign(MathTool.SignedAngle(targetVector, GunForward)) &&
                        Vector2.Angle(targetVector, -CenterForward) < Vector2.Angle(targetVector, GunForward)
                        )
                    ) : false;
                float OrienDelta = MathTool.SignedAngle(GunForward, targetVector);
                float PitchDelta = GunnerMsgReceiver.Instance.TargrtPitch[myPlayerID][myGuid] - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara();
                float TargetOrien = MathTool.SignedAngle(CenterForward, targetVector);

                ControlTurrent(true, OutOfSpan, OrienDelta, TargetOrien, PitchDelta);
            }
            else
            {
                ControlTurrent(false);
            }
        }
        //public void EmulateControl()
        //{
        //    if (hasTarget && bindedGuns[0])
        //    {
        //        bool GunReady = true;
        //        Vector2 GunForward = bindedGuns[0].GetComponent<Gun>().GetFCOrienPara();
        //        Vector2 CenterForward = new Vector2();
        //        if (limitValid)
        //        {
        //            CenterForward = new Vector2(AngleCenter.transform.forward.x, AngleCenter.transform.forward.z);
        //        }
        //        Vector2 targetVector = targetPos - new Vector2(bindedGuns[0].transform.position.x, bindedGuns[0].transform.position.z);
        //        bool OutOfSpan = limitValid ? (Vector2.Angle(targetVector, CenterForward) > GunSpan ||
        //            (   Mathf.Sign(MathTool.SignedAngle(targetVector,-CenterForward)) == Mathf.Sign(MathTool.SignedAngle(targetVector,GunForward)) && 
        //                Vector2.Angle(targetVector, -CenterForward) < Vector2.Angle(targetVector, GunForward)
        //                )
        //            ) : false ;
        //        //Debug.Log(OutOfSpan+" "+ MathTool.SignedAngle(CenterForward, targetVector));
        //        if ((!OutOfSpan && MathTool.SignedAngle(GunForward, targetVector) > OrienFaultTolerance.Value) ||
        //            (OutOfSpan && MathTool.SignedAngle(CenterForward, targetVector) > 0))
        //        {
        //            GunReady = false;
        //            if (leftEmulateStage == 0)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), LeftKey, true);
        //                leftEmulateStage ++;
        //            }
        //        }
        //        else
        //        {
        //            if (leftEmulateStage == 1)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), LeftKey, false);
        //                leftEmulateStage --;
        //            }
        //        }
        //        if ((!OutOfSpan && MathTool.SignedAngle(GunForward, targetVector) < -OrienFaultTolerance.Value) ||
        //            (OutOfSpan && MathTool.SignedAngle(CenterForward, targetVector) < 0))
        //        {
        //            GunReady = false;
        //            if (rightEmulateStage == 0)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), RightKey, true);
        //                rightEmulateStage ++;
        //            }
                    
        //        }
        //        else
        //        {
        //            if (rightEmulateStage == 1)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), RightKey, false);
        //                rightEmulateStage --;
        //            }
        //        }

        //        if (targetPitch - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() > ElevationFaultTolerance.Value)
        //        {
        //            GunReady = false;
        //            if (upEmulateStage == 0)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), UpKey, true);
        //                upEmulateStage++;
        //            }
        //        }
        //        else
        //        {
        //            if (upEmulateStage == 1)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), UpKey, false);
        //                upEmulateStage--;
        //            }
        //        }

        //        if (targetPitch - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara() < -ElevationFaultTolerance.Value)
        //        {
        //            GunReady = false;
        //            if (downEmulateStage == 0)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), DownKey, true);
        //                downEmulateStage++;
        //            }
        //        }
        //        else
        //        {
        //            if (downEmulateStage == 1)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), DownKey, false);
        //                downEmulateStage--;
        //            }
        //        }

        //        if (GunReady)
        //        {
        //            if (fireEmulateStage == 0)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), FireKey, true);
        //                fireEmulateStage++;
        //            }
        //            else
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), FireKey, false);
        //                fireEmulateStage--;
        //            }
        //        }
        //        else
        //        {
        //            if (fireEmulateStage == 1)
        //            {
        //                EmulateKeys(BlockBehaviour.KeyList.ToArray(), FireKey, false);
        //                fireEmulateStage--;
        //            }
        //        }

        //    }
        //    else
        //    {
        //        if (leftEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), LeftKey, false);
        //            leftEmulateStage--;
        //        }
        //        if (rightEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), RightKey, false);
        //            rightEmulateStage--;
        //        }
        //        if (upEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), UpKey, false);
        //            upEmulateStage--;
        //        }
        //        if (downEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), DownKey, false);
        //            downEmulateStage--;
        //        }
        //        if (fireEmulateStage == 1)
        //        {
        //            EmulateKeys(BlockBehaviour.KeyList.ToArray(), FireKey, false);
        //            fireEmulateStage--;
        //        }
        //    }
            
        //}
        public void EmulateControl()
        {
            if (hasTarget && bindedGuns[0])
            {
                SetWW2Hinge(true);
                
                Vector2 GunForward = bindedGuns[0].GetComponent<Gun>().GetFCOrienPara();
                Vector2 CenterForward = new Vector2(1,0);
                if (OrienLimitValid)
                {
                    CenterForward = new Vector2(AngleCenter.transform.forward.x, AngleCenter.transform.forward.z);
                }
                Vector2 targetVector = targetPos - new Vector2(bindedGuns[0].transform.position.x, bindedGuns[0].transform.position.z);
                bool OutOfSpan = OrienLimitValid ? (Vector2.Angle(targetVector, CenterForward) > OrienGunSpan ||
                    (Mathf.Sign(MathTool.SignedAngle(targetVector, -CenterForward)) == Mathf.Sign(MathTool.SignedAngle(targetVector, GunForward)) &&
                        Vector2.Angle(targetVector, -CenterForward) < Vector2.Angle(targetVector, GunForward)
                        )
                    ) : false;
                //Debug.Log(OutOfSpan+" "+ MathTool.SignedAngle(CenterForward, targetVector));
                float OrienDelta = MathTool.SignedAngle(GunForward, targetVector);
                float PitchDelta = targetPitch - bindedGuns[0].GetComponent<Gun>().GetFCPitchPara();
                float TargetOrien = MathTool.SignedAngle(CenterForward, targetVector);

                ControlTurrent(true, OutOfSpan, OrienDelta, TargetOrien, PitchDelta);
            }
            else
            {
                ControlTurrent(false);
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
            ActiveSwitch = AddKey("Switch Active", "ActiveGunner", KeyCode.None);
            FireKey = AddEmulatorKey("Fire Key", "FireKey", KeyCode.None);
            LeftKey = AddEmulatorKey("Left Key", "LeftKey", KeyCode.G);
            RightKey = AddEmulatorKey("Right Key", "RightKey", KeyCode.J);
            UpKey = AddEmulatorKey("Up Key", "UpKey", KeyCode.Y);
            DownKey = AddEmulatorKey("Down Key", "DownKey", KeyCode.H);
            OrienFaultTolerance = AddSlider("Orien Fault Tolerance", "OrienFaultTolerance", 1f, 0f, 5f);
            ElevationFaultTolerance = AddSlider("Elevation Fault Tolerance", "ElevationFaultTolerance", 0.3f, 0f, 2f);
            TurningSpeed = AddSlider("Speed", "TurningSpeed", 1f, 0.1f, 1f);
            GunGroup = AddText("Gun Group", "GunGroup", "g0");
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 10);
            GunnerAlertIcon = ModResource.GetTexture("gunnerAlert Texture").Texture;
            //FauxTransform iconInfo = new FauxTransform(new Vector3(0f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one * 0.25f);
            //Limit = AddLimits("Limits", "Limits", 90f, 90f, 180f,iconInfo);
        }
        public void Start()
        {
            name = "Gunner";
            initLine();
        }
        public void OnDestroy()
        {
            
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
            
            GunnerActive = true;
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
            try
            {
                GunnerMsgReceiver.Instance.GunnerActive[myPlayerID].Add(myGuid, true);
            }
            catch
            {
                GunnerMsgReceiver.Instance.GunnerActive[myPlayerID][myGuid] = true;
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
        public override void SimulateUpdateHost()
        {
            if (ActiveSwitch.IsPressed)
            {
                GunnerActive = !GunnerActive;
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(GunnerMsgReceiver.GunnerActiveMsg.CreateMessage(myPlayerID, myGuid, GunnerActive));
                }
            }
        }
        public override void SimulateUpdateClient()
        {
            GunnerActive = GunnerMsgReceiver.Instance.GunnerActive[myPlayerID][myGuid];
        }
        public override void SimulateFixedUpdateAlways()
        {
            if (!initialized)
            {
                initialized = true;
                GetMyCaliber();
                turningSpeed = 1 / Mathf.Clamp(bindedCaliber, 40, 510) * 50 * Mathf.Clamp(TurningSpeed.Value,0.1f,1f);
                FindHinge();
                FindGun();
                if (OrienHinge.Count != 0)
                {
                    if (OrienHinge[0].LimitsSlider.IsActive)
                    {
                        OrienLimitValid = true;

                        OrienCenterAngle = (-OrienHinge[0].LimitsSlider.Min + OrienHinge[0].LimitsSlider.Max) / 2;
                        OrienGunSpan = (OrienHinge[0].LimitsSlider.Min + OrienHinge[0].LimitsSlider.Max) / 2;
                        OrienLimitValid = GenerateHingeCenter();
                    }
                    else
                    {
                        Debug.Log("Slider not active");
                        OrienLimitValid = false;
                    }
                }
                else
                {
                    OrienLimitValid = false;
                }
                if (PitchHinge.Count != 0)
                {
                    if (PitchHinge[0].LimitsSlider.IsActive)
                    {
                        PitchCenterAngle = (-PitchHinge[0].LimitsSlider.Min + PitchHinge[0].LimitsSlider.Max) / 2;
                        PitchGunSpan = (PitchHinge[0].LimitsSlider.Min + PitchHinge[0].LimitsSlider.Max) / 2;
                    }
                }
            }

            RejectSpecific();
            GetFCPara();
            
        }
        public override void SimulateFixedUpdateHost()
        {
            if (!GunnerActive)
            {
                SetWW2Hinge(false);
                Fire(false);
                return;
            }
            if (StatMaster.isMP && initialized)
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
        public override void SimulateFixedUpdateClient()
        {
            if (!GunnerActive)
            {
                return;
            }
            if (myPlayerID == PlayerData.localPlayer.networkId)
            {
                SendTargetToHost();
            }
            
        }
        public override void SendKeyEmulationUpdateHost()
        {


            
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
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(transform.position + transform.forward * transform.localScale.z);
                if (onScreenPosition.z >= 0)
                {
                    if (GunnerActive)
                    {
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), GunnerAlertIcon);
                    }
                }
            }
        }
    }
}
