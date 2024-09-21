using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding;
using UnityEngine;
using static TutorialStepPrerequisite;

namespace WW2NavalAssembly
{
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

        private float _waterMass = 0;
        private float _initialMass = 0.5f;

        public float MaxWater = 999f;
        public float WaterMass
        {
            get
            {
                return _waterMass;
            }
            set
            {
                _waterMass = value;
                rb.mass = _waterMass + _initialMass;
            }
        }

        public float WaterScale
        {
            get
            {
                return WaterMass / MaxWater;
            }
        }

        public void SafeAwake()
        {
            AsWaterTank = BB.AddToggle("As WaterTank", "AsWatertank", false);
            DrainKey = BB.AddKey("Drain", "Drain", KeyCode.Minus);
            FloodKey = BB.AddKey("Flood", "Flood", KeyCode.Plus);
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
                rb = BB.Rigidbody;
                MaxWater = Mathf.Clamp(transform.Find("WoodenArmourVis").localScale.magnitude * 100f, 1f, 99999f);
            }
        }

        public void SimulateUpdateHost()
        {
            if (FloodKey.IsHeld)
            {
                WaterMass = Mathf.Clamp(WaterMass + Time.deltaTime * 0.5f * MaxWater, 0f, 99999f);
            }
            if (DrainKey.IsHeld)
            {
                WaterMass = Mathf.Clamp(WaterMass - Time.deltaTime * 0.2f * MaxWater, 0f, 99999f);
            }
        }

        public void SimulateUpdateClient()
        {

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
