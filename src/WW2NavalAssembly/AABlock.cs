using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace WW2NavalAssembly
{
    class AABlock : BlockScript
    {
        public int myPlayerID;
        public int myseed = 0;
        public int myGuid;

        public MMenu Type;
        public int gunNum = 1;

        public int preType = -1;
        public string preAppearance;
        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject GunObject;
        public GameObject BaseObject;


        
        public void InitBaseGunObject()
        {
            if (!transform.Find("Vis").Find("BaseBase"))
            {
                GameObject BaseBaseObject = new GameObject("BaseBase");
                BaseBaseObject.transform.SetParent(transform.Find("Vis"));
                BaseBaseObject.transform.localScale = Vector3.one;
                BaseBaseObject.transform.localPosition = Vector3.zero;
                BaseBaseObject.transform.localEulerAngles = Vector3.zero;
                BaseObject = new GameObject("Base");
                BaseObject.transform.SetParent(BaseBaseObject.transform);
                BaseObject.transform.localScale = Vector3.one;
                BaseObject.transform.localPosition = Vector3.zero;
                BaseObject.transform.localEulerAngles = Vector3.zero;

                BaseObject.AddComponent<MeshFilter>();
                BaseObject.AddComponent<MeshRenderer>().material = transform.Find("Vis").GetComponent<MeshRenderer>().material;
            }
            else
            {
                BaseObject = transform.Find("Vis").Find("BaseBase").gameObject;
            }
            if (!BaseObject.transform.Find("GunBase"))
            {
                GameObject GunBaseObject = new GameObject("GunBase");
                GunBaseObject.transform.SetParent(BaseObject.transform.parent);
                GunBaseObject.transform.localScale = Vector3.one;
                GunBaseObject.transform.localPosition = Vector3.zero;
                GunBaseObject.transform.localEulerAngles = Vector3.zero;
                GunObject = new GameObject("Gun");
                GunObject.transform.SetParent(GunBaseObject.transform);
                GunObject.transform.localScale = Vector3.one;
                GunObject.transform.localPosition = Vector3.zero;
                GunObject.transform.localEulerAngles = Vector3.zero;

                GunObject.AddComponent<MeshFilter>();
                GunObject.AddComponent<MeshRenderer>().material = transform.Find("Vis").GetComponent<MeshRenderer>().material;
            }
            else
            {
                GunObject = BaseObject.transform.Find("GunBase").gameObject;
            }
        }

        public void UpdateAppearance(int type)
        {
            BaseObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 0);
            BaseObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 0);
            BaseObject.transform.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 0);

            GunObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 1);
            GunObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 1);
            GunObject.transform.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 1);
            GunObject.transform.parent.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 2);
        }

        public void HoldAppearance()
        {
            bool changed = false;
            if (preShowCluster != StatMaster.clusterCoded)
            {
                preShowCluster = StatMaster.clusterCoded;
                changed = true;
            }
            else if (preSkinEnabled != OptionsMaster.skinsEnabled)
            {
                preSkinEnabled = OptionsMaster.skinsEnabled;
                changed = true;
            }
            if (changed)
            {
                UpdateAppearance(Type.Value);
            }
            if (preType != Type.Value)
            {
                preType = Type.Value;
                UpdateAppearance(Type.Value);
            }
        }
        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            Type = AddMenu("AAType", 0, new List<string>
            {
                "1x20mm",
                "3x25mm",
                "2x40mm",
                "4x40mm",
                "2x100mm",
                "2x127mm",
            });
            InitBaseGunObject();
        }
        public void Update()
        {
            HoldAppearance();
        }
        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            switch (Type.Value)
            {
                case 0:
                    gunNum = 1;
                    break;
                case 1:
                    gunNum = 3;
                    break;
                case 2:
                    gunNum = 2;
                    break;
                case 3:
                    gunNum = 4;
                    break;
                case 4:
                    gunNum = 2;
                    break;
                case 5:
                    gunNum = 2;
                    break;
                default:
                    gunNum = 1;
                    break;
            }

            if (!StatMaster.isClient)
            {
            }
        }
        public override void OnSimulateStop()
        {
            BlockBehaviour.BuildingBlock.GetComponent<AABlock>().UpdateAppearance(Type.Value);
        }
        public override void SimulateFixedUpdateHost()
        {
        }
    }
}
