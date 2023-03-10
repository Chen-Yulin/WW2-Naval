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
namespace WW2NavalAssembly
{
    class TorpedoLauncher : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int myseed;

        public MKey FireKey;
        public MKey SwitchKey;
        public MSlider Caliber;
        public MSlider Depth0;
        public MSlider Depth1;
        //public MToggle TrackOn;
        public MToggle FireControl;
        //public MText GunGroup;

        public int TorpedoType;
        public int NumLeft = 2;

        GameObject TorpedoPrefab;
        GameObject StaticTorpedoPrefab;

        Transform VisTransform;

        //int muzzleStage = 100;

        public float reloadTime;
        public float currentReloadTime = 0;

        Texture ReloadHEOut;
        Texture ReloadHEIn;
        Texture ReloadAPOut;
        Texture ReloadAPIn;
        int iconSize = 30;

        public void UpdateSelfToFC()
        {
            if (ModController.Instance.state % 10 == myseed)
            {
                AddSelfToFC();
            }
        }
        public void AddSelfToFC()
        {
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.AddTorpedo(myPlayerID, TorpedoType, myGuid, gameObject);
            }
        }
        public void RemoveSelfFromFC()
        {
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.RemoveTorpedo(myPlayerID, myGuid);
            }
        }
        public Vector2 GetFCOrienPara()
        {
            if (StatMaster.isClient)
            {
                //return new Vector2(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].x, BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].z);
                return new Vector2(-transform.up.x, -transform.up.z);
            }
            else
            {
                return new Vector2(-transform.up.x, -transform.up.z);
            }
        }
        void InitTorpedo()
        {
            TorpedoPrefab = new GameObject("NavalTorpedo");
            TorpedoPrefab.transform.localScale = new Vector3(2, 1.5f, 2);
            TorpedoBehaviour TBtmp = TorpedoPrefab.AddComponent<TorpedoBehaviour>();
            TBtmp.Caliber = Caliber.Value;
            TBtmp.myPlayerID = myPlayerID;
            Rigidbody RBtmp = TorpedoPrefab.AddComponent<Rigidbody>();
            RBtmp.mass = 0.2f;
            RBtmp.drag = 0.1f;
            RBtmp.useGravity = true;
            GameObject TorpedoVis = new GameObject("TorpedoVis");
            TorpedoVis.transform.SetParent(TorpedoPrefab.transform);
            TorpedoVis.transform.localPosition = Vector3.zero;
            TorpedoVis.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            TorpedoVis.transform.localScale = Vector3.one;
            MeshFilter MFtmp = TorpedoVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("Torpedo Mesh").Mesh;
            MeshRenderer MRtmp = TorpedoVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("Torpedo Texture").Texture;

            GameObject Fan1 = new GameObject("Fan1");
            Fan1.transform.SetParent(TorpedoPrefab.transform);
            Fan1.transform.localPosition = Vector3.zero;
            Fan1.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            Fan1.transform.localScale = Vector3.one;
            Fan1.AddComponent<MeshFilter>().sharedMesh = ModResource.GetMesh("TorpedoFan1 Mesh").Mesh;
            Fan1.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("TorpedoFan1 Texture").Texture;
            Fan1.AddComponent<PropellerBehaviour>().Direction = false;

            GameObject Fan2 = new GameObject("Fan2");
            Fan2.transform.SetParent(TorpedoPrefab.transform);
            Fan2.transform.localPosition = Vector3.zero;
            Fan2.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            Fan2.transform.localScale = Vector3.one;
            Fan2.AddComponent<MeshFilter>().sharedMesh = ModResource.GetMesh("TorpedoFan2 Mesh").Mesh;
            Fan2.AddComponent<MeshRenderer>().material.mainTexture = ModResource.GetTexture("TorpedoFan2 Texture").Texture;
            Fan2.AddComponent<PropellerBehaviour>().Direction = true;

            TorpedoPrefab.SetActive(false);
        }

        void InitStaticTorpedo()
        {
            StaticTorpedoPrefab = new GameObject("StaticTorpedo");
            StaticTorpedoPrefab.transform.SetParent(transform);
            StaticTorpedoPrefab.transform.localPosition = new Vector3(0.005f, 0, 0.1f);
            StaticTorpedoPrefab.transform.localRotation = Quaternion.identity;
            StaticTorpedoPrefab.transform.localScale = new Vector3(2, 1.5f, 2);
            GameObject TorpedoVis = new GameObject("TorpedoVis");
            TorpedoVis.transform.SetParent(StaticTorpedoPrefab.transform);
            TorpedoVis.transform.localPosition = Vector3.zero;
            TorpedoVis.transform.localRotation = Quaternion.Euler(0f, 0f, 270f);
            TorpedoVis.transform.localScale = Vector3.one;
            MeshFilter MFtmp = TorpedoVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("Torpedo Mesh").Mesh;
            MeshRenderer MRtmp = TorpedoVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("Torpedo Texture").Texture;

            StaticTorpedoPrefab.SetActive(false);
        }

        public void ClearTorpedo()
        {
            List<GameObject> tmps = new List<GameObject>();
            for (int i = 0; i < 2; i++)
            {
                GameObject tmp = GameObject.Find("Torpedo" + myPlayerID.ToString());
                if (tmp)
                {
                    tmp.name = "Torpedo-1";
                    tmps.Add(tmp);
                }
                else
                {
                    break;
                }
            }
            foreach (var t in tmps)
            {
                Destroy(t);
            }
        }

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            FireKey = AddKey("Fire", "Fire", KeyCode.C);
            SwitchKey = AddKey("Switch Speed", "SwitchCannonType", KeyCode.R);
            Caliber = AddSlider("Caliber (mm)", "Caliber", 533, 400, 610);
            Depth0 = AddSlider("Depth 0", "Depth0", 0.5f, 0f, 5f);
            Depth1 = AddSlider("Depth 1", "Depth1", 1.5f, 0f, 5f);
            //TrackOn = AddToggle("Track Cannon", "TrackCannon", false);
            FireControl = AddToggle("Fire Control", "FireControl", false);
            //GunGroup = AddText("Gun Group", "GunGroup", "g0");
            ReloadHEOut = ModResource.GetTexture("ReloadHEOut Texture").Texture;
            ReloadHEIn = ModResource.GetTexture("ReloadHEIn Texture").Texture;
            ReloadAPOut = ModResource.GetTexture("ReloadAPOut Texture").Texture;
            ReloadAPIn = ModResource.GetTexture("ReloadAPIn Texture").Texture;
            myseed = (int)(UnityEngine.Random.value * 10);
        }
        public void Start()
        {
            name = "Torpedo Launcher";
        }
        public override void OnSimulateStart()
        {
            VisTransform = transform.Find("Vis");
            BlockBehaviour.blockJoint.breakForce = float.PositiveInfinity;
            BlockBehaviour.blockJoint.breakTorque = float.PositiveInfinity;

            
            
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            reloadTime = 7f * Mathf.Sqrt(Caliber.Value);
            currentReloadTime = reloadTime;

            InitStaticTorpedo();
            InitTorpedo();
            AddSelfToFC();
            try
            {
                if (StatMaster.isClient)
                {
                    WeaponMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new WeaponMsgReceiver.firePara(false, Vector3.zero, Vector3.zero));
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    WeaponMsgReceiver.Instance.reloadTime[myPlayerID].Add(myGuid, 0);
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID].Add(myGuid, false);
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    BlockPoseReceiver.Instance.forward[myPlayerID].Add(myGuid, Vector3.zero);
                }
            }
            catch { }
        }
        public override void OnSimulateStop()
        {
            RemoveSelfFromFC();
            ClearTorpedo();
            DestroyImmediate(TorpedoPrefab);

            WeaponMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTime[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID].Remove(myGuid);
            
        }
        public void OnDestroy()
        {
            RemoveSelfFromFC();
        }
        public override void SimulateUpdateHost()
        {
            if (SwitchKey.IsPressed)
            {
                if (TorpedoType == 1)
                {
                    TorpedoType = 0;
                }
                else
                {
                    TorpedoType = 1;
                }
            }

            if (currentReloadTime < reloadTime)
            {
                if (NumLeft != 0)
                {
                    currentReloadTime += Time.deltaTime;
                    if (ModController.Instance.state == myseed)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.ReloadMsg.CreateMessage(myPlayerID, myGuid, currentReloadTime, TorpedoType == 1, false, NumLeft));
                    }
                }
                return;
            }
            StaticTorpedoPrefab.SetActive(true);
            if (FireKey.IsPressed)
            {
                NumLeft--;
                StaticTorpedoPrefab.SetActive(false);
                currentReloadTime = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(Caliber.Value * transform.up);

                GameObject Cannon = (GameObject)Instantiate(TorpedoPrefab, transform.position + 0.1f * transform.forward, transform.rotation);
                Cannon.name = "Torpedo" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<TorpedoBehaviour>().fire = true;
                Cannon.GetComponent<TorpedoBehaviour>().mode = TorpedoType;
                Cannon.GetComponent<TorpedoBehaviour>().parentGuid = myGuid;
                if (TorpedoType == 0)
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth0.Value;
                }
                else
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth1.Value;
                }

                if (TorpedoType == 0)
                {
                    Destroy(Cannon, 90);
                }
                else
                {
                    Destroy(Cannon, 27.8f);
                }

                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Cannon.transform.eulerAngles));
                }
            }
        }
        public override void SimulateUpdateClient()
        {
            if (SwitchKey.IsPressed)
            {
                if (TorpedoType == 1)
                {
                    TorpedoType = 0;
                }
                else
                {
                    TorpedoType = 1;
                }
            }

            if (WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid])
            {
                WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid] = false;
                currentReloadTime = WeaponMsgReceiver.Instance.reloadTime[myPlayerID][myGuid];
                TorpedoType = WeaponMsgReceiver.Instance.CannonType[myPlayerID][myGuid]?1:0;
                NumLeft = WeaponMsgReceiver.Instance.CannonNum[myPlayerID][myGuid];
            }
            if (currentReloadTime < reloadTime)
            {
                if (NumLeft != 0)
                {
                    currentReloadTime += Time.deltaTime;
                }
                StaticTorpedoPrefab.SetActive(false);
            }
            else
            {
                StaticTorpedoPrefab.SetActive(true);
            }

            if (WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                NumLeft--;
                currentReloadTime = 0;
                WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(TorpedoPrefab, transform.position + 0.1f * transform.forward,
                                                            Quaternion.Euler(WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].forward));
                Cannon.name = "Torpedo" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<TorpedoBehaviour>().fire = true;
                Cannon.GetComponent<TorpedoBehaviour>().mode = TorpedoType;
                Cannon.GetComponent<TorpedoBehaviour>().parentGuid = myGuid;
                if (TorpedoType == 0)
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth0.Value;
                }
                else
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth1.Value;
                }

                if (TorpedoType == 0)
                {
                    Destroy(Cannon, 90);
                }
                else
                {
                    Destroy(Cannon, 27.8f);
                }
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            UpdateSelfToFC();

            if (currentReloadTime < reloadTime)
            {
                return;
            }
            StaticTorpedoPrefab.SetActive(true);

            if (FireKey.EmulationPressed())
            {
                NumLeft--;
                currentReloadTime = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(Caliber.Value * transform.up);

                GameObject Cannon = (GameObject)Instantiate(TorpedoPrefab, transform.position + 0.1f * transform.forward, transform.rotation);
                Cannon.name = "Torpedo" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<TorpedoBehaviour>().fire = true;
                Cannon.GetComponent<TorpedoBehaviour>().mode = TorpedoType;
                Cannon.GetComponent<TorpedoBehaviour>().parentGuid = myGuid;
                if (TorpedoType == 0)
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth0.Value;
                }
                else
                {
                    Cannon.GetComponent<TorpedoBehaviour>().depth = Depth1.Value;
                }
                if (TorpedoType == 0)
                {
                    Destroy(Cannon, 90);
                }
                else
                {
                    Destroy(Cannon, 27.8f);
                }

                ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Cannon.transform.eulerAngles));
            }
        }
        public override void SimulateFixedUpdateClient()
        {
            UpdateSelfToFC();
        }
        public void OnGUI()
        {
            if (StatMaster.hudHidden)
            {
                return;
            }
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    return;
                }
            }
            if ((Camera.main.transform.position - transform.position).magnitude < 30 && BlockBehaviour.isSimulating)
            {
                Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(transform.position);
                if (onScreenPosition.z >= 0)
                {
                    if (TorpedoType == 0)
                    {
                        int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), ReloadAPOut);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - currIconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - currIconSize / 2, currIconSize, currIconSize), ReloadAPIn);
                    }
                    else
                    {
                        int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), ReloadHEOut);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - currIconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - currIconSize / 2, currIconSize, currIconSize), ReloadHEIn);
                    }

                }
            }
        }
    }
}
