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
        FollowerUI ReloadHEOutUI;
        FollowerUI ReloadHEInUI;
        FollowerUI ReloadAPOutUI;
        FollowerUI ReloadAPInUI;
        int iconSize = 30;

        public bool isSelf
        {
            get
            {
                return StatMaster.isMP ? myPlayerID == PlayerData.localPlayer.networkId : true;
            }
        }

        public void UpdateUI()
        {
            if (StatMaster.hudHidden)
            {
                ReloadAPOutUI.show = false;
                ReloadHEOutUI.show = false;
                ReloadAPInUI.show = false;
                ReloadHEInUI.show = false;
            }
            else
            {
                if (TorpedoType == 0)
                {
                    ReloadAPOutUI.show = true;
                    ReloadHEOutUI.show = false;
                    int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                    ReloadAPInUI.size = currIconSize;
                    ReloadAPInUI.show = true;
                    ReloadHEInUI.show = false;
                }
                else
                {
                    ReloadHEOutUI.show = true;
                    ReloadAPOutUI.show = false;
                    int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                    ReloadHEInUI.size = currIconSize;
                    ReloadHEInUI.show = true;
                    ReloadAPInUI.show = false;
                }
            }

        }
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
            Transform PrefabParent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            string PrefabName = "NavalTorpedo [" + myPlayerID + "](" + Caliber.Value + ")";
            if (PrefabParent.Find(PrefabName))
            {
                TorpedoPrefab = PrefabParent.Find(PrefabName).gameObject;
            }
            else
            {
                TorpedoPrefab = new GameObject(PrefabName);
                TorpedoPrefab.transform.parent = PrefabParent;
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
            FireKey = AddKey(LanguageManager.Instance.CurrentLanguage.TorpedoFire, "Fire", KeyCode.C);
            SwitchKey = AddKey(LanguageManager.Instance.CurrentLanguage.SwitchTorpedo, "SwitchCannonType", KeyCode.R);
            Caliber = AddSlider(LanguageManager.Instance.CurrentLanguage.Caliber, "Caliber", 533, 400, 610);
            Depth0 = AddSlider(LanguageManager.Instance.CurrentLanguage.TorpedoType0Depth, "Depth0", 0.5f, 0f, 5f);
            Depth1 = AddSlider(LanguageManager.Instance.CurrentLanguage.TorpedoType1Depth, "Depth1", 1.5f, 0f, 5f);
            //TrackOn = AddToggle("Track Cannon", "TrackCannon", false);
            FireControl = AddToggle(LanguageManager.Instance.CurrentLanguage.FireControl, "FireControl", false);
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
                    WeaponMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new WeaponMsgReceiver.firePara(false, Vector3.zero, Vector3.zero, Vector3.zero, (float)20));
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

            if (isSelf)
            {
                ReloadAPOutUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, ReloadAPOut);
                ReloadHEOutUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, ReloadHEOut);
                ReloadAPInUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, ReloadAPIn);
                ReloadHEInUI = BlockUIManager.Instance.CreateFollowerUI(transform, 30, ReloadHEIn);
            }

        }
        public override void OnSimulateStop()
        {
            RemoveSelfFromFC();
            ClearTorpedo();
            Destroy(TorpedoPrefab);

            WeaponMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTime[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID].Remove(myGuid);
            
        }
        public void OnDestroy()
        {
            RemoveSelfFromFC();
        }
        public void FixedUpdate()
        {
            if (BlockBehaviour.isSimulating)
            {
                if (StatMaster.isClient)
                {
                    MySimulateFixedUpdateClient();
                }
            }
        }
        public override void SimulateUpdateAlways()
        {
            if (isSelf)
            {
                UpdateUI();
            }
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
                Cannon.GetComponent<Rigidbody>().velocity = Rigidbody.velocity;
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
                    Destroy(Cannon, Constants.FastTorpedoTime);
                }
                else
                {
                    Destroy(Cannon, Constants.FastTorpedoTime);
                }

                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Cannon.transform.eulerAngles, Rigidbody.velocity, (float)20));
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
                Cannon.GetComponent<Rigidbody>().velocity = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].vel;
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
                    Destroy(Cannon, Constants.FastTorpedoTime);
                }
                else
                {
                    Destroy(Cannon, Constants.FastTorpedoTime);
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
                Cannon.GetComponent<Rigidbody>().velocity = Rigidbody.velocity;
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
                    Destroy(Cannon, Constants.FastTorpedoTime);
                }
                else
                {
                    Destroy(Cannon, Constants.FastTorpedoTime);
                }

                ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, Vector3.zero, Cannon.transform.eulerAngles, Rigidbody.velocity, (float)20));
            }
        }
        public void MySimulateFixedUpdateClient()
        {
            UpdateSelfToFC();
        }
        public void OnGUI()
        {
        }
    }
}
