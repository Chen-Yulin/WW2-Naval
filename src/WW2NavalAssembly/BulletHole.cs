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
    public class BulletHole : MonoBehaviour
    {
        public float hittedCaliber;
        public Vector3 normal;
        public Vector3 position;
        public float waterIn = 0;
        public int DCTime = 0;
        public int DCTimeNeeded;

        public GameObject HoleVis;

        float sqrCaliber;
        Rigidbody rigid;
        public void Awake()
        {
            
        }
        public void Start()
        {
            sqrCaliber = hittedCaliber * hittedCaliber;
            HoleVis = new GameObject("RigidObject");
            HoleVis.transform.SetParent(transform);
            HoleVis.transform.localPosition = position;
            HoleVis.transform.rotation = Quaternion.LookRotation(normal);
            HoleVis.transform.localScale = Vector3.one * hittedCaliber/400;

            rigid = transform.parent.GetComponent<Rigidbody>();

            DCTimeNeeded = (int)( sqrCaliber / 100);
        }
        public void FixedUpdate()
        {
            
            if (DCTime < DCTimeNeeded && HoleVis.transform.position.y < 20)
            {
                DCTime++;
                waterIn += sqrCaliber / 100;
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

            rigid.AddForce(-Vector3.up * waterIn / 200);

        }
    }
}