﻿using System.Collections.Generic;
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
    public class WaterCarbin:MonoBehaviour
    {
        public float size = 0;
        public float shellWaterIn;
        public float carbinWaterIn;
        public float torpedoWaterIn;
        public Rigidbody body;

        public WoodenArmour WA;

        private float _coeff = 50000;
        private float _drainRate = 0.999f;

        public void AddShellWater(float water)
        {
            shellWaterIn = Mathf.Clamp(shellWaterIn + water, 0, size - carbinWaterIn);
        }
        public void AddCarbinWater(float water)
        {
            carbinWaterIn = Mathf.Clamp(carbinWaterIn + water, 0, size - shellWaterIn);
        }
        public void AddTorpedoWater(float water)
        {
            torpedoWaterIn = Mathf.Clamp(torpedoWaterIn + water, 0, 8*size);

        }
        private void RemoveCarbinWater()
        {
            carbinWaterIn *= 1 - (1-_drainRate) * WA.CrewRate * CrewManager.Instance.GetEfficiency(WA.myPlayerID);
        }
        private void ApplyFore()
        {
            body.AddForce((shellWaterIn + carbinWaterIn + torpedoWaterIn) / 100f * Vector3.down);
        }

        public void Awake()
        {
            BlockBehaviour BB = GetComponent<BlockBehaviour>();
            body = GetComponent<Rigidbody>();

            size = MathTool.GetArea(transform.localScale);
            switch (BB.BlockID)
            {
                case (int)BlockType.DoubleWoodenBlock: size *=2; break;
                case (int)BlockType.Log: size *= 3; break;
                default: break;
            }
            size *= _coeff;
        }
        public void FixedUpdate()
        {
            try
            {

                body.drag = (shellWaterIn + carbinWaterIn + torpedoWaterIn) / 80000f;
                ApplyFore();
                RemoveCarbinWater();
            }
            catch { }
            
        }
    }

    public class WaterInHole : MonoBehaviour
    {
        public int holeType = 0; // 0 for shell, 1 for carbin
        public float hittedCaliber;
        public Vector3 position;
        public float waterIn = 0;
        public float DCTime = 0;
        public float DCTimeNeeded;
        public int type = 0; // 0 for cannon, 1 for torpedo

        public GameObject Hole;
        private WaterCarbin wc;

        float sqrCaliber;
        Rigidbody rigid;
        WoodenArmour hittedArmour;
        bool disabled = false;

        Bytank bytank;
        public void Awake()
        {
            
        }
        public void Start()
        {
            try
            {
                sqrCaliber = hittedCaliber * hittedCaliber;
                Hole = new GameObject("Hole");
                Hole.transform.SetParent(transform);
                Hole.transform.localPosition = position;
                Hole.transform.rotation = Quaternion.identity;
                Hole.transform.localScale = Vector3.one * hittedCaliber / 400;

                rigid = transform.parent.GetComponent<Rigidbody>();
                hittedArmour = transform.parent.GetComponent<WoodenArmour>();
                DCTimeNeeded = (int)(sqrCaliber * 50f / 20000f);
                if (type==1)
                {
                    DCTimeNeeded = (int)(sqrCaliber * 1000f / 20000f);
                }
                // add waterCarbin component
                wc = transform.parent.GetComponent<WaterCarbin>();

                if (!wc)
                {
                    wc = transform.parent.gameObject.AddComponent<WaterCarbin>();
                    wc.WA = hittedArmour;
                }

                bytank = transform.parent.GetComponent<Bytank>();
                if (bytank)
                {
                    bytank.BreakSpace += hittedCaliber * hittedCaliber * (Hole.transform.position.y < 20 ? 10 : 1);
                }
            }
            catch {
                disabled = true;
            }

        }
        public void FixedUpdate()
        {
            try
            {
                if (disabled)
                {
                    return;
                }
                if (DCTime < DCTimeNeeded && Hole.transform.position.y < 20 && Hole.transform.position.y > 15)
                {
                    DCTime += hittedArmour.CrewRate * 0.5f + CrewManager.Instance.GetEfficiency(hittedArmour.myPlayerID) * 0.5f;
                    if (holeType == 0)
                    {
                        wc.AddShellWater(sqrCaliber / 500f * (type == 0 ? 1 : 10));
                        if (type == 1)
                        {
                            wc.AddTorpedoWater(sqrCaliber * sqrCaliber / 160000 * 0.01f);
                            wc.AddCarbinWater(sqrCaliber / 400f * 10);
                        }
                    }
                    else
                    {
                        wc.AddCarbinWater(sqrCaliber / 500f);
                    }
                }
                else if (DCTime >= DCTimeNeeded)
                {
                    Destroy(transform.gameObject);
                }
            }
            catch { }
            
        }
    }

    public class PiercedHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 forward;
        public Vector3 position;
        public int DCTime = 0;
        public int DCTimeNeeded;
        public int type = 0;

        public GameObject HoleProjector;
        public Projector HP;

        float sqrCaliber;
        public void Awake()
        {

        }
        public void Start()
        {
            sqrCaliber = hittedCaliber * hittedCaliber;
            if (type == 0)
            {
                HoleProjector = Instantiate(AssetManager.Instance.Projector.BulletHole);
                Destroy(HoleProjector.GetComponent<Projector>(),15);
            }
            else if (type == 1)
            {
                HoleProjector = Instantiate(AssetManager.Instance.Projector.TorpedoHole);
            }
            
            HoleProjector.transform.SetParent(transform);
            HoleProjector.transform.localPosition = position;
            HoleProjector.transform.rotation = Quaternion.LookRotation(forward);
            HoleProjector.transform.localScale = Vector3.one;
            HP = HoleProjector.GetComponent<Projector>();
            if (type == 0)
            {
                HP.orthographicSize = hittedCaliber / 1400;
            }
            else if (type == 1)
            {
                HP.orthographicSize = hittedCaliber / 400;
            }
            
            HP.farClipPlane = 0.01f;
            HP.nearClipPlane = 0;

            if (transform.parent.GetComponent<WoodenArmour>())
            {
                DCTimeNeeded = (int)(sqrCaliber * Mathf.Clamp(transform.parent.GetComponent<WoodenArmour>().thickness, 40, 650) / 20000);
            }
            else
            {
                DCTimeNeeded = (int)(sqrCaliber * Mathf.Clamp(transform.parent.GetComponent<CannonWell>().thickness, 40, 650) / 20000);
            }
            
        }
        public void FixedUpdate()
        {
            if (type == 0)
            {
                if (DCTime < DCTimeNeeded && HoleProjector.transform.position.y < 20)
                {
                    DCTime++;
                }
                else if (DCTime >= DCTimeNeeded)
                {
                    Destroy(transform.gameObject);
                }
            }
            else
            {

            }
            
        }
    }
}