using System.Collections.Generic;
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
        public Rigidbody body;

        private float _coeff = 1000;
        private float _drainRate = 0.999f;

        public void AddShellWater(float water)
        {
            shellWaterIn = Mathf.Clamp(shellWaterIn + water, 0, size - carbinWaterIn);
        }
        public void AddCarbinWater(float water)
        {
            carbinWaterIn = Mathf.Clamp(carbinWaterIn + water, 0, size - shellWaterIn);
        }
        public void RemoveCarbinWater()
        {
            carbinWaterIn *= _drainRate;
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
            body.AddForce((shellWaterIn + carbinWaterIn) * Vector3.down);
        }
    }
    public class CarbinWaterInHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 position;
        public float waterIn = 0;
        public int DCTime = 0;
        public int DCTimeNeeded;
        public int type = 0;
        public float minWaterIn;

        public GameObject HoleVis;

        float sqrCaliber;
        Rigidbody rigid;
        WoodenArmour hittedArmour;
        bool disabled = false;
        public void Awake()
        {

        }
        public void Start()
        {
            if (type == 1)
            {
                minWaterIn = 600 * hittedCaliber;
                waterIn = minWaterIn;
            }
            try
            {
                sqrCaliber = hittedCaliber * hittedCaliber;
                HoleVis = new GameObject("RigidObject");
                HoleVis.transform.SetParent(transform);
                HoleVis.transform.localPosition = position;
                HoleVis.transform.rotation = Quaternion.identity;
                HoleVis.transform.localScale = Vector3.one * hittedCaliber / 400;

                rigid = transform.parent.GetComponent<Rigidbody>();
                hittedArmour = transform.parent.GetComponent<WoodenArmour>();

                DCTimeNeeded = (int)(sqrCaliber * Mathf.Clamp(hittedArmour.thickness, 40, 650) / 20000);
            }
            catch
            {
                disabled = true;
            }

        }
        public void FixedUpdate()
        {
            if (disabled)
            {
                return;
            }
            if (DCTime < DCTimeNeeded && HoleVis.transform.position.y < 20 && HoleVis.transform.position.y > 15)
            {
                DCTime++;
                waterIn += sqrCaliber / 400;
            }
            else if (DCTime >= DCTimeNeeded && HoleVis.transform.position.y > 15)
            {
                waterIn -= 1000;
                if (type == 0)
                {
                    if (waterIn < 0)
                    {
                        waterIn = 0;
                        Destroy(transform.gameObject);
                    }
                }
                else if (type == 1)
                {
                    waterIn = Mathf.Clamp(waterIn, minWaterIn, float.MaxValue);
                }

            }

            if (HoleVis.transform.position.y < 20 && DCTime < DCTimeNeeded)
            {
                rigid.AddForce(-rigid.velocity * 20);
            }
            if (HoleVis.transform.position.y < 20)
            {
                rigid.velocity = new Vector3(rigid.velocity.x, Mathf.Clamp(rigid.velocity.y, -0.5f, 0.5f), rigid.velocity.z);
            }
            if (type == 0)
            {
                rigid.AddForce(-Vector3.up * Mathf.Clamp(waterIn / 200, 0, Mathf.Clamp(hittedArmour.thickness, 40, 650) * 30));
            }
            else if (type == 1)
            {
                rigid.AddForce(-Vector3.up * waterIn / 20);
            }
        }
    }

    public class ShellWaterInHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 position;
        public float waterIn = 0;
        public int DCTime = 0;
        public int DCTimeNeeded;
        public int type = 0;
        public float minWaterIn;

        public GameObject HoleVis;

        float sqrCaliber;
        Rigidbody rigid;
        WoodenArmour hittedArmour;
        bool disabled = false;
        public void Awake()
        {
            
        }
        public void Start()
        {
            if (type == 1)
            {
                minWaterIn = 600 * hittedCaliber;
                waterIn = minWaterIn;
            }
            try
            {
                sqrCaliber = hittedCaliber * hittedCaliber;
                HoleVis = new GameObject("RigidObject");
                HoleVis.transform.SetParent(transform);
                HoleVis.transform.localPosition = position;
                HoleVis.transform.rotation = Quaternion.identity;
                HoleVis.transform.localScale = Vector3.one * hittedCaliber / 400;

                rigid = transform.parent.GetComponent<Rigidbody>();
                hittedArmour = transform.parent.GetComponent<WoodenArmour>();

                DCTimeNeeded = (int)(sqrCaliber * Mathf.Clamp(hittedArmour.thickness, 40, 650) / 20000);
            }
            catch {
                disabled = true;
            }

        }
        public void FixedUpdate()
        {
            if (disabled)
            {
                return;
            }   
            if (DCTime < DCTimeNeeded && HoleVis.transform.position.y < 20 && HoleVis.transform.position.y > 15)
            {
                DCTime++;
                waterIn += sqrCaliber / 400;
            }
            else if (DCTime >= DCTimeNeeded && HoleVis.transform.position.y > 15)
            {
                waterIn -= 1000;
                if (type == 0)
                {
                    if (waterIn < 0)
                    {
                        waterIn = 0;
                        Destroy(transform.gameObject);
                    }
                }
                else if (type == 1)
                {
                    waterIn = Mathf.Clamp(waterIn, minWaterIn, float.MaxValue);
                }

            }

            if (HoleVis.transform.position.y < 20 && DCTime < DCTimeNeeded)
            {
                rigid.AddForce(-rigid.velocity * 20);
            }
            if (HoleVis.transform.position.y < 20)
            {
                rigid.velocity = new Vector3(rigid.velocity.x, Mathf.Clamp(rigid.velocity.y,-0.5f,0.5f), rigid.velocity.z);
            }
            if (type == 0)
            {
                rigid.AddForce(-Vector3.up * Mathf.Clamp(waterIn / 200, 0, Mathf.Clamp(hittedArmour.thickness, 40, 650) * 30));
            }
            else if (type == 1)
            {
                rigid.AddForce(-Vector3.up *waterIn / 20);
            }
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