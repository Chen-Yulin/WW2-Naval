using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding;
using Modding.Common;
using UnityEngine;
using static TutorialStepPrerequisite;

namespace WW2NavalAssembly
{
    public class ByTankMsgManager : SingleInstance<ByTankMsgManager>
    {
        public override string Name { get; } = "ByTank Message Manager";
        public static MessageType BreakAreaMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single);
        public Dictionary<int, float>[] BreakArea = new Dictionary<int, float>[16];

        public ByTankMsgManager()
        {
            for (int i = 0; i < 16; i++)
            {
                BreakArea[i] = new Dictionary<int, float>();
            }
        }
        public void SendBreakMsg(int playerID, int guid, float area)
        {
            Player p = Player.From((ushort)playerID);
            ModNetworking.SendTo(p, BreakAreaMsg.CreateMessage(playerID, guid, area));
        }
        public void BreakMsgReceiver(Message msg)
        {
            int pid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            float breakarea = (float)msg.GetData(2);
            if (BreakArea[pid].ContainsKey(guid))
            {
                BreakArea[pid][guid] = breakarea;
            }
            else
            {
                BreakArea[pid].Add(guid, breakarea);
            }
        }
    }
    class Bytank : MonoBehaviour
    {
        BlockBehaviour BB;
        MToggle AsWaterTank;
        MKey DrainKey;
        MKey FloodKey;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public Rigidbody rb;

        public bool pre_asWatertank = false;


        public Transform Center;

        public float MaxBy = 0;
        public float _byScale = 1f;
        public float ByScale
        {
            get
            {
                return _byScale; 
            }
            set
            {
                _byScale = Mathf.Clamp01(value);
                WaterInUI.size = Mathf.Sqrt((1 -_byScale)) * iconSize;
            }
        }

        FollowerUI WaterOutUI;
        FollowerUI WaterInUI;
        int iconSize = 30;

        Texture WaterOutTex;
        Texture WaterInTex;

        public bool breakNeedUpdate = false;
        private float _breakSpace = 0;
        public float BreakSpace
        {
            get { return _breakSpace; }
            set
            {
                if (_breakSpace != value)
                {
                    _breakSpace = value;
                    breakNeedUpdate = true;
                }
            }
        }


        public bool isSelf
        {
            get
            {
                return StatMaster.isMP ? myPlayerID == PlayerData.localPlayer.networkId : true;
            }
        }

        public void SafeAwake()
        {
            AsWaterTank = BB.AddToggle("As WaterTank", "AsWatertank", false);
            DrainKey = BB.AddKey("Drain", "Drain", KeyCode.Minus);
            FloodKey = BB.AddKey("Flood", "Flood", KeyCode.Equals);
        }

        public void Awake()
        {
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;

            myseed = (int)(UnityEngine.Random.value * 39);
            BB = GetComponent<BlockBehaviour>();
            try
            {
                myGuid = BB.BuildingBlock.Guid.GetHashCode();
            }
            catch
            {
                myGuid = BB.Guid.GetHashCode();
            }

            SafeAwake();
            pre_asWatertank = AsWaterTank.isDefaultValue;

            if (BB.isSimulating) { return; }
        }

        public void Start()
        {
            if (BB.isSimulating) // on simulate start
            {
                if (AsWaterTank.isDefaultValue)
                {
                    Destroy(gameObject.GetComponent<Bytank>());
                    return;
                }
                else
                {
                    rb = BB.Rigidbody;
                    Center = transform.Find("WoodenArmourVis");
                }
                MaxBy = Mathf.Clamp(Mathf.Abs(Center.lossyScale.x * Center.lossyScale.y * Center.lossyScale.z) * 15f, 1f, 99999f);
                if (isSelf)
                {
                    WaterOutTex = ModResource.GetTexture("WaterTankOut Texture").Texture;
                    WaterInTex = ModResource.GetTexture("WaterTankIn Texture").Texture;
                    WaterOutUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, WaterOutTex);
                    WaterInUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, WaterInTex);
                    WaterInUI.size = 0;
                }
                ByTankMsgManager.Instance.BreakArea[myPlayerID].Clear();
            }
        }

        public void SimulateFixedUpdateHost()
        {
            rb.AddForce(Mathf.Pow(Mathf.Clamp((Constants.SeaHeight - Center.position.y), 0, 1f),2) * Vector3.up * 60f * ByScale * MaxBy);
            if (StatMaster.isMP && !StatMaster.isClient && breakNeedUpdate && myseed == ModController.Instance.state)
            {
                breakNeedUpdate = false;
                ByTankMsgManager.Instance.SendBreakMsg(myPlayerID, myGuid, BreakSpace);
            }
        }

        public void FixedUpdate()
        {
            if (BB.isSimulating)
            {
                if (StatMaster.isMP)
                {
                    if (!StatMaster.isClient)
                    {
                        SimulateFixedUpdateHost();
                    }
                }
                else
                {
                    SimulateFixedUpdateHost();
                }
                
            }
        }

        public void SimulateUpdateHost()
        {
            ByScale -= BreakSpace * Time.deltaTime * 0.00001f;
            if (!AsWaterTank.isDefaultValue)
            {
                if (DrainKey.IsHeld && BreakSpace == 0)
                {
                    ByScale += 0.2f * Time.deltaTime;
                }
                if (FloodKey.IsHeld)
                {
                    ByScale -= 0.4f * Time.deltaTime;
                }
            }
        }

        public void SimulateUpdateClient()
        {
            if (isSelf)
            {
                if (ByTankMsgManager.Instance.BreakArea[myPlayerID].ContainsKey(myGuid))
                {
                    BreakSpace = ByTankMsgManager.Instance.BreakArea[myPlayerID][myGuid];
                    ByTankMsgManager.Instance.BreakArea[myPlayerID].Remove(myGuid);
                }
            }
        }

        public void Update()
        {
            if (BB.isSimulating) // simulate update
            {
                if (StatMaster.isMP)
                {
                    if (StatMaster.isClient)
                    {
                        SimulateUpdateClient();
                    }
                    else
                    {
                        SimulateUpdateHost();
                    }
                }
                else
                {
                    SimulateUpdateHost();
                }
            }
            else // build update
            {
                if (pre_asWatertank == AsWaterTank.isDefaultValue)
                {
                    pre_asWatertank = !AsWaterTank.isDefaultValue;
                    if (pre_asWatertank)
                    {
                        DrainKey.DisplayInMapper = true;
                        FloodKey.DisplayInMapper = true;
                    }
                    else
                    {
                        DrainKey.DisplayInMapper = false;
                        FloodKey.DisplayInMapper = false;
                    }
                }
            }
        }

    }
}
