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
    public class WaterInHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 position;
        public float waterIn = 0;
        public int DCTime = 0;
        public int DCTimeNeeded;

        public GameObject HoleVis;

        float sqrCaliber;
        Rigidbody rigid;
        WoodenArmour hittedArmour;
        public void Awake()
        {
            
        }
        public void Start()
        {
            sqrCaliber = hittedCaliber * hittedCaliber;
            HoleVis = new GameObject("RigidObject");
            HoleVis.transform.SetParent(transform);
            HoleVis.transform.localPosition = position;
            HoleVis.transform.rotation = Quaternion.identity;
            HoleVis.transform.localScale = Vector3.one * hittedCaliber/400;

            rigid = transform.parent.GetComponent<Rigidbody>();
            hittedArmour = transform.parent.GetComponent<WoodenArmour>();

            DCTimeNeeded = (int)( sqrCaliber * Mathf.Clamp(hittedArmour.thickness, 40, 650) / 20000);
        }
        public void FixedUpdate()
        {
            
            if (DCTime < DCTimeNeeded && HoleVis.transform.position.y < 20)
            {
                DCTime++;
                waterIn += sqrCaliber / 200;
            }
            else if (DCTime >= DCTimeNeeded)
            {
                waterIn -= 1000;
                if (waterIn < 0)
                {
                    waterIn = 0;
                    Destroy(transform.gameObject);
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

            rigid.AddForce(-Vector3.up * Mathf.Clamp(waterIn / 200,0, Mathf.Clamp(hittedArmour.thickness,40,650)*20));

        }
    }

    public class PiercedHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 forward;
        public Vector3 position;
        public int DCTime = 0;
        public int DCTimeNeeded;

        public GameObject HoleProjector;
        public Projector HP;

        float sqrCaliber;
        public void Awake()
        {

        }
        public void Start()
        {
            sqrCaliber = hittedCaliber * hittedCaliber;
            HoleProjector = Instantiate(AssetManager.Instance.Projector.BulletHole);
            HoleProjector.transform.SetParent(transform);
            HoleProjector.transform.localPosition = position;
            HoleProjector.transform.rotation = Quaternion.LookRotation(forward);
            HoleProjector.transform.localScale = Vector3.one;
            HP = HoleProjector.GetComponent<Projector>();
            HP.orthographicSize = hittedCaliber / 1400;
            HP.farClipPlane = 0.01f;
            HP.nearClipPlane = 0;

            DCTimeNeeded = (int)(sqrCaliber * Mathf.Clamp(transform.parent.GetComponent<WoodenArmour>().thickness,40,650) / 20000);
        }
        public void FixedUpdate()
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
    }
}