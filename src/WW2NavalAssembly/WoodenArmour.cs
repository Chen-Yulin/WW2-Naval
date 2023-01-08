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
    class WoodenArmour : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MSlider Thickness;
        public float thickness;
        GameObject Vis;
        MeshRenderer VisRender;

        public int myseed;

        public void InitVis()
        {
            if (transform.Find("WoodenArmourVis"))
            {
                return;
            }
            Vis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.SingleArmour,transform);
            switch (BB.BlockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    {
                        Vis.transform.localPosition = new Vector3(0,0,0.5f);
                        Vis.transform.localScale = new Vector3(0.8f,0.8f,1);
                        break;
                    }
                case (int)BlockType.DoubleWoodenBlock:
                    {
                        Vis.transform.localPosition = new Vector3(0, 0, 1f);
                        Vis.transform.localScale = new Vector3(0.95f, 0.95f, 2);
                        break;
                    }
                case (int)BlockType.Log:
                    {
                        Vis.transform.localPosition = new Vector3(0, 0, 1.5f);
                        Vis.transform.localScale = new Vector3(0.95f, 0.95f, 3);
                        break;
                    }
                default:
                    break;
            }
            Vis.name = "WoodenArmourVis";
            Vis.transform.localRotation = Quaternion.identity;
            VisRender = Vis.GetComponent<MeshRenderer>();
            Vis.SetActive(false);
        }



        public virtual void SafeAwake()
        {
            Thickness = BB.AddSlider("WW2-Naval Thickness", "WW2Thickness", 20f, 10f, 650f);
        }
        public void Awake()
        {
            myseed = (int)(UnityEngine.Random.value * 39);
            BB = GetComponent<BlockBehaviour>();
            SafeAwake();
            if (BB.isSimulating) { return; }
        }
        public void Start()
        {
            InitVis();
        }
        public void FixedUpdate()
        {
            if (ModController.Instance.state == myseed)
            {
                if (!Vis)
                {
                    Vis = transform.Find("WoodenArmourVis").gameObject;
                    VisRender = Vis.GetComponent<MeshRenderer>();
                }
                if (ModController.Instance.showArmour)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    Vis.SetActive(true);
                }
                else
                {
                    transform.Find("Vis").gameObject.SetActive(true);
                    Vis.SetActive(false);
                }
                thickness = Thickness.Value;
                Color tmpColor = Color.HSVToRGB(Mathf.Clamp(0.5f-thickness / 1000,0,0.5f), 1, 1);
                VisRender.material.color = new Color(tmpColor.r,tmpColor.g,tmpColor.b, 0.6f);

            }

            
            
        }
    }
}
