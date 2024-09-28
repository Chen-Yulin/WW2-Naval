using Microsoft.Win32;
using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WW2NavalAssembly
{
    public class PowerSystem : SingleInstance<PowerSystem>
    {
        public override string Name { get; } = "Power System";

        public List<Battery>[] suppliers = new List<Battery>[16];

        public int[] useWhich = new int[16];

        public PowerSystem()
        {
            for (int i = 0; i < 16; i++)
            {
                suppliers[i] = new List<Battery>();
            }
        }

        public void ResetSuppliers(int playerID)
        {
            suppliers[playerID].Clear();
        }

        public bool ReleasePower(int playerID, float power)
        {
            if (suppliers[playerID].Count > 0)
            {
                useWhich[playerID]++;
                if (useWhich[playerID] >= suppliers[playerID].Count)
                {
                    useWhich[playerID] = 0;
                }
                return suppliers[playerID][useWhich[playerID]].Discharge(power);
            }
            else
            {
                return false;
            }
            
        }

        public void SupplyPower(int playerID, float power)
        {
            float count = (float)suppliers[playerID].Count;
            if (count > 0)
            {
                foreach (var supplier in suppliers[playerID])
                {
                    supplier.Charge(power / count);
                }
            }
            
        }

        public void AddSupplier(int playerID, Battery battery)
        {
            suppliers[playerID].Add(battery);
        }

        public void RemoveSupplier(int playerID, Battery battery)
        {
            if (suppliers[playerID].Contains(battery))
            {
                suppliers[playerID].Remove(battery);
            }
        }

    }

    public class Battery : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int myseed;

        public float MaxPower;

        private float _power = 0;
        public float Power
        {
            get
            {
                return _power;
            }
            set
            {
                if (value != _power)
                {
                    _power = Mathf.Clamp(value, 0, MaxPower);
                    int currIconSize = (int)(iconSize * Power / MaxPower);
                    CapInUI.size = currIconSize;
                }
            }
        }

        FollowerUI CapOutUI;
        FollowerUI CapInUI;
        int iconSize = 30;

        Texture CapOutTex;
        Texture CapInTex;

        public bool isSelf
        {
            get
            {
                return StatMaster.isMP ? myPlayerID == PlayerData.localPlayer.networkId : true;
            }
        }

        public void Charge(float power)
        {
            Power += power;
        }

        public bool Discharge(float power)
        {
            if (Power == 0)
            {
                return false;
            }
            else
            {
                Power -= power;
                return true;
            }
        }

        public void UpdateUI()
        {
            if (StatMaster.hudHidden)
            {
                CapOutUI.show = false;
                CapInUI.show = false;
            }
            else
            {
                CapOutUI.show = true;
                CapInUI.show = true;
            }
        }

        public override void SafeAwake()
        {
            gameObject.name = "Battery";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            myseed = (int)(UnityEngine.Random.value * 400);
        }

        public override void OnSimulateStart()
        {
            gameObject.name = "Battery";
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            MaxPower = Mathf.Abs(transform.lossyScale.x * transform.lossyScale.y * transform.lossyScale.z) * 10f;
            if (isSelf)
            {
                CapOutTex = ModResource.GetTexture("BatteryCapOut Texture").Texture;
                CapInTex = ModResource.GetTexture("BatteryCapIn Texture").Texture;
                CapOutUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, CapOutTex);
                CapInUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, CapInTex);
                CapInUI.size = 0;
            }
            PowerSystem.Instance.AddSupplier(myPlayerID, this);
        }

        public override void OnSimulateStop()
        {
            PowerSystem.Instance.RemoveSupplier(myPlayerID, this);
        }

        public override void SimulateUpdateAlways()
        {
            if (isSelf)
            {
                UpdateUI();
            }
        }
    }
}
