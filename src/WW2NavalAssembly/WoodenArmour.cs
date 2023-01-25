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
        public int myPlayerID;
        public int myGuid;

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
                        if (transform.Find("Joint").gameObject.activeSelf)
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 1.5f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 3);
                        }
                        else
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 1f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 2);
                        }
                        
                        break;
                    }
                default:
                    break;
            }
            Vis.name = "WoodenArmourVis";
            Vis.transform.localRotation = Quaternion.identity;
            VisRender = Vis.GetComponent<MeshRenderer>();
            Vis.layer = 25;
            Vis.SetActive(true);
            Color tmpColor1 = VisRender.material.color;
            VisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);
        }

        public void SyncBulletHole()
        {
            foreach (WeaponMsgReceiver.hitHoleInfo info in WeaponMsgReceiver.Instance.BulletHoleInfo[myPlayerID][myGuid])
            {
                if (info.type == 0)
                {
                    GameObject piercedhole = new GameObject("PiercedHole");
                    piercedhole.transform.SetParent(transform);
                    piercedhole.transform.localPosition = Vector3.zero;
                    piercedhole.transform.localRotation = Quaternion.identity;
                    piercedhole.transform.localScale = Vector3.one;

                    PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                    PH.hittedCaliber = info.Caliber;
                    PH.position = info.position;
                    PH.forward = info.forward;
                }
                else if(info.type == 1)
                {
                    GameObject piercedhole = new GameObject("TorpedodHole");
                    piercedhole.transform.SetParent(transform);
                    piercedhole.transform.localPosition = Vector3.zero;
                    piercedhole.transform.localRotation = Quaternion.identity;
                    piercedhole.transform.localScale = Vector3.one;

                    PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                    PH.hittedCaliber = info.Caliber;
                    PH.position = info.position;
                    PH.forward = info.forward;
                    PH.type = 1;
                }

            }
            WeaponMsgReceiver.Instance.BulletHoleInfo[myPlayerID][myGuid].Clear();
        }

        public virtual void SafeAwake()
        {
            Thickness = BB.AddSlider("WW2-Naval Thickness", "WW2Thickness", 20f, 10f, 650f);
        }
        public void Awake()
        {
            myPlayerID = transform.gameObject.GetComponent<BlockBehaviour>().ParentMachine.PlayerID;
            
            myseed = (int)(UnityEngine.Random.value * 39);
            BB = GetComponent<BlockBehaviour>();
            SafeAwake();
            
            if (BB.isSimulating) { return; }
        }
        public void Start()
        {
            InitVis();
            
            //transform.Find("Shadow").gameObject.layer = 25;
        }
        public void FixedUpdate()
        {
            if (myGuid == 0 && transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
            {
                myGuid = transform.gameObject.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode();
                try
                {
                    if (StatMaster.isClient)
                    {
                        WeaponMsgReceiver.Instance.BulletHoleInfo[myPlayerID].Add(myGuid, new List<WeaponMsgReceiver.hitHoleInfo>());
                    }
                }
                catch { }
            }
            if (ModController.Instance.state == myseed)
            {
                if (!Vis)
                {
                    Vis = transform.Find("WoodenArmourVis").gameObject;
                    VisRender = Vis.GetComponent<MeshRenderer>();
                }
                
                thickness = Thickness.Value;
                Color tmpColor = Color.HSVToRGB(Mathf.Clamp(0.5f-thickness / 1000,0,0.5f), 1, 1);
                VisRender.material.color = new Color(tmpColor.r,tmpColor.g,tmpColor.b, 0.6f);

                if (ModController.Instance.showArmour)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    Color tmpColor1 = VisRender.material.color;
                    VisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0.6f);
                }
                else
                {
                    transform.Find("Vis").gameObject.SetActive(true);
                    Color tmpColor1 = VisRender.material.color;
                    VisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);
                }

                if (StatMaster.isClient && transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
                {
                    SyncBulletHole();
                }
            }

            
            
        }
    }
}
