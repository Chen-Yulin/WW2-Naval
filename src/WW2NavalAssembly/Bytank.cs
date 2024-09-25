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

        public Transform Center;

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
                }
                else
                {
                    rb = BB.Rigidbody;
                    _initialMass = rb.mass;
                    Center = transform.Find("WoodenArmourVis");
                    MaxWater = Mathf.Clamp(Center.lossyScale.magnitude * 15f, 1f, 99999f);
                }
                
            }
        }

        public void SimulateFixedUpdate()
        {
            rb.AddForce(Mathf.Pow(Mathf.Clamp((Constants.SeaHeight - Center.position.y), 0, 1f),2) * Vector3.up * MaxWater * 60f);
        }

        public void FixedUpdate()
        {
            if (BB.isSimulating)
            {
                SimulateFixedUpdate();
            }
        }

        public void SimulateUpdateHost()
        {
            if (!AsWaterTank.isDefaultValue)
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
