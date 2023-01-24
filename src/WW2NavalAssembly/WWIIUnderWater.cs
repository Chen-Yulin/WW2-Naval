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
using Modding.Blocks;

namespace WW2NavalAssembly
{
    public class WWIIUnderWater : MonoBehaviour
    {
        public float InitialDrag;
        public float InitialAngularDrag;
        public float UnderWaterDrag = 15f;
        public float UnderWaterAngularDrag = 20f;
        public Rigidbody rigid;
        public BlockBehaviour BB;

        public void addDrag()
        {
            if (!rigid)
            {
                rigid = transform.GetComponent<Rigidbody>();
                InitialDrag = rigid.drag;
                InitialAngularDrag = rigid.angularDrag;
            }
            if (ModController.Instance.showSea)
            {
                if (transform.position.y < 20)
                {
                    rigid.drag = UnderWaterDrag;
                    rigid.angularDrag = UnderWaterAngularDrag;
                }
                else
                {
                    rigid.drag = InitialDrag;
                    rigid.angularDrag = InitialAngularDrag;
                }
            }
            else
            {
                rigid.drag = InitialDrag;
                rigid.angularDrag = InitialAngularDrag;
            }

        }
        public void Awake()
        {
            BB = GetComponent<BlockBehaviour>();
            
        }
        public void FixedUpdate()
        {
            
            if (!BB)
            {
                BB = GetComponent<BlockBehaviour>();
            }

            if (BB.isSimulating)
            {
                if (StatMaster.isMP)
                {
                    if (!StatMaster.isClient)
                    {
                        addDrag();
                    }
                }
                else
                {
                    addDrag();
                }
            }
            
        }
    }
}
