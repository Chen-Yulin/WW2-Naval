using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;

namespace WW2NavalAssembly
{
    public class DieselMsgReceiver : SingleInstance<DieselMsgReceiver>
    {
        public override string Name { get; } = "Diesel Msg Receiver";
        public static MessageType EnableDieselMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean);
        public Dictionary<int, bool>[] enabled = new Dictionary<int, bool>[16];

        public DieselMsgReceiver() {
            for (int i = 0; i < 16; i++)
            {
                enabled[i] = new Dictionary<int, bool>();
            }
        }

        public void SendEnableMsg(int playerID, int guid, bool enable)
        {
            ModNetworking.SendToAll(EnableDieselMsg.CreateMessage(playerID, guid, enable));
        }

        public void ReceiveEnableMsg(Message msg)
        {
            int pid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            bool enable = (bool)msg.GetData(2);
            if (enabled[pid].ContainsKey(guid))
            {
                enabled[pid][guid] = enable;
            }
            else
            {
                enabled[pid].Add(guid, enable);
            }
        }

        public bool IsEnabled(int pid, int guid)
        {
            if (enabled[pid].ContainsKey(guid))
            {
                return enabled[pid][guid];
            }
            else
            {
                return false;
            }
        }

    }

    public class Diesel : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int myseed;

        public MKey Enable;

        public GameObject RunningSound;
        public AudioClip RunningClip;
        public AudioSource RunningAS;

        public Transform Vis;

        public float count = 0;

        public bool _enable = false;
        public bool Enabled
        {
            get { return _enable; }
            set
            {
                if (value != _enable)
                {
                    _enable = value;
                    if (StatMaster.isMP)
                    {
                        if (!StatMaster.isClient)
                        {
                            DieselMsgReceiver.Instance.SendEnableMsg(myPlayerID, myGuid, _enable);
                        }
                    }
                }
            }
        }

        public float Power = 0;

        public void InitSound()
        {
            RunningClip = ModResource.GetAudioClip("Diesel Audio");
            RunningSound = new GameObject("running sound");
            RunningSound.transform.SetParent(transform);
            RunningAS = RunningSound.GetComponent<AudioSource>() ?? RunningSound.AddComponent<AudioSource>();
            RunningAS.clip = RunningClip;
            RunningAS.spatialBlend = 1.0f;
            RunningAS.volume = 0f;
            RunningAS.loop = true;
            RunningAS.SetSpatializerFloat(1, 1f);
            RunningAS.SetSpatializerFloat(2, 0);
            RunningAS.SetSpatializerFloat(3, 12);
            RunningAS.SetSpatializerFloat(4, 1000f);
            RunningAS.SetSpatializerFloat(5, 1f);
            RunningSound.SetActive(false);
        }

        public override void SafeAwake()
        {
            gameObject.name = "Diesel";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            myseed = (int)(UnityEngine.Random.value * 400);

            Enable = AddKey("Enable", "Enable", KeyCode.P);
            InitSound();
        }

        public override void OnSimulateStart()
        {
            gameObject.name = "Diesel";
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            RunningSound.SetActive(true);
            Vis = transform.Find("Vis");
            Power = Mathf.Abs(transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z);
            DieselMsgReceiver.Instance.enabled[myPlayerID].Clear();
        }

        public override void SimulateUpdateHost()
        {
            if (transform.position.y > 18.5f || !ModController.Instance.showSea)
            {
                if (Enable.IsPressed)
                {
                    Enabled = !Enabled;
                }
            }
            else
            {
                Enabled = false;
            }
            
        }

        public override void SimulateUpdateClient()
        {
            Enabled = DieselMsgReceiver.Instance.IsEnabled(myPlayerID, myGuid);
        }

        public override void SimulateUpdateAlways()
        {
            count += Time.deltaTime;
            RunningAS.volume = Mathf.Lerp(RunningAS.volume, Enabled ? 1 : 0, 0.2f);
            if (Enabled)
            {
                PowerSystem.Instance.SupplyPower(myPlayerID, Power * Time.deltaTime);
                Vis.localPosition = new Vector3(0, 0, Mathf.Sin(Time.time * 300f) * 0.02f);
                
                if (count > 0.05f)
                {
                    count = 0;
                    int listenid = StatMaster.isMP ? PlayerData.localPlayer.networkId : 0;
                    GameObject controller = ControllerDataManager.Instance.ControllerObject[listenid];
                    if (controller)
                    {
                        Vector2 arrow = MathTool.Get2DCoordinate(transform.position - controller.transform.position);
                        float signedAngle = MathTool.SignedAngle(MathTool.Get2DCoordinate(-controller.transform.up), arrow);
                        if (signedAngle < 0)
                        {
                            signedAngle += 360;
                        }
                        //Debug.Log(signedAngle);
                        float mag = 1f / Mathf.Sqrt(arrow.magnitude) * Power * 0.2f;
                        float error = Mathf.Clamp(Mathf.Sqrt(arrow.magnitude), 0, 90);
                        SoundSystem.Instance.AddSound(myPlayerID, (int)signedAngle, mag, error);
                    }

                }
            }
            else
            {
                Vis.localPosition = Vector3.zero;
            }

            

        }



    }
}
