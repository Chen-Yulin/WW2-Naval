﻿using System;
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
    public class AABlockMsgReceiver : SingleInstance<GunnerMsgReceiver>
    {
        public override string Name { get; } = "AABlockMsgReceiver";
    }
    class AATurretController : MonoBehaviour
    {
        public bool AA_active;
        public float TargetPitch;
        public float TargetYaw;

        public float caliber = 20;
        public float gunWidth = 1;
        public float MinLimit;
        public float MaxLimit;
        public float speed = 1;
        public int type;

        public ParticleSystem ShootEffect;

        public Transform Base;
        public Transform Gun;

        private float _pitch;
        private float _yaw;

        private float _real_pitch;
        private float _real_yaw;

        public float Pitch
        {
            get { return _pitch; }
            set 
            { 
                _pitch = value;
                Gun.transform.localEulerAngles = new Vector3(-value, 0, 0);
            }
        }
        public float Yaw
        {
            get { return _yaw; }
            set
            {
                _yaw = value;
                Base.transform.localEulerAngles = new Vector3(0, value, 0);
            }
        }
        public bool Shoot
        {
            set
            {
                if (ShootEffect.isPlaying != value)
                {
                    if (value)
                    {
                        ShootEffect.Play();
                    }
                    else
                    {
                        ShootEffect.Stop();
                    }
                }
            }
        }
        public void Start()
        {
            if (ShootEffect)
            {
                ShootEffect.randomSeed = (uint)UnityEngine.Random.value * 1000;
                ShootEffect.startSpeed = MathTool.GetInitialVel(caliber, true);
                ShootEffect.gravityModifier = 1.5f;
                ShootEffect.startLifetime = 2f;
                ShootEffect.startSize = Mathf.Pow(caliber, 1.5f) / 2000f;
                var emission = ShootEffect.emission;
                emission.rate = AAAssetManager.Instance.GetSpeed(type);
                ParticleSystem.LimitVelocityOverLifetimeModule Limit = ShootEffect.limitVelocityOverLifetime;
                Limit.dampen = 0.1f - caliber / 1000f;
                ShootEffect.transform.GetChild(0).localScale = new Vector3(gunWidth, 0, 1);
            }
        }
        public void Update()
        {
            Pitch += (_real_pitch - Pitch) * 0.2f;
            Yaw += (_real_yaw - Yaw) * 0.2f;
        }
        public void FixedUpdate()
        {
            float equv_speed = speed;
            bool ok = AA_active;
            if (Mathf.Abs(Yaw - TargetYaw) < equv_speed * 8)
            {
                _real_yaw += (TargetYaw - _real_yaw) * 0.2f;
            }
            else 
            {
                _real_yaw += (_real_yaw > TargetYaw ? -1 : 1) * equv_speed;
                ok = false;
            }
            if (Mathf.Abs(Pitch - TargetPitch) < equv_speed * 8)
            {
                _real_pitch += (TargetPitch - _real_pitch) * 0.2f;
            }
            else
            {
                _real_pitch += (_real_pitch > TargetPitch ? -1 : 1) * equv_speed;
                ok = false;
            }
            _real_yaw = Mathf.Clamp(_real_yaw, -MinLimit, MaxLimit);
            _real_pitch = Mathf.Clamp(_real_pitch, -5, 90);
            Shoot = ok;
        }
    }
    class AABlock : BlockScript
    {
        public int myPlayerID;
        public int myseed = 0;
        public int myGuid;

        public MMenu Type;
        public MLimits YawLimit;
        public int gunNum = 1;
        public float caliber = 20;

        public int preType = -1;
        public string preAppearance;
        public bool preSkinEnabled;
        public bool preShowCluster;

        public GameObject GunObject;
        public GameObject BaseObject;
        public GameObject originVis;

        // for shoot
        public GameObject Small_AA;
        

        // turret control
        public AATurretController AAVC;


        // FC data
        public bool hasTarget = false;
        public float targetPitch = 0;
        public Vector2 targetPos = Vector3.zero;
        public float targetTime = 20;

        public void GetFCPara()
        {
            if (!ControllerDataManager.Instance.aaController[myPlayerID])
            {
                hasTarget = false;
                return;
            }
            if (ControllerDataManager.Instance.aaController[myPlayerID].hasTarget)
            {
                if (ControllerDataManager.Instance.AAControllerFCResult[myPlayerID].ContainsKey(caliber))
                {
                    if (ControllerDataManager.Instance.AAControllerFCResult[myPlayerID][caliber].hasRes)
                    {
                        targetPitch = ControllerDataManager.Instance.AAControllerFCResult[myPlayerID][caliber].Pitch;
                        targetPos = ControllerDataManager.Instance.AAControllerFCResult[myPlayerID][caliber].predPosition;
                        targetTime = ControllerDataManager.Instance.AAControllerFCResult[myPlayerID][caliber].timer;
                        hasTarget = true;
                    }
                    else
                    {
                        hasTarget = false;
                    }

                }
                else
                {
                    hasTarget = false;
                }
            }
            else
            {
                hasTarget = false;
            }
        }
        public void InitSmallShoot()
        {
            Small_AA = (GameObject)Instantiate(AssetManager.Instance.AA.Small_AA, GunObject.transform.parent);
            Small_AA.transform.localScale = Vector3.one;
            Small_AA.transform.localRotation = Quaternion.identity;
            Small_AA.transform.localPosition = new Vector3(0, 0, 7f);
            Small_AA.SetActive(true);
            Small_AA.GetComponent<ParticleSystem>().Stop();
        }
        public void InitBaseGunObjectBuild()
        {
            BaseObject = transform.Find("Vis").gameObject;


            if (!BaseObject.transform.Find("GunBase"))
            {
                GameObject GunBaseObject = new GameObject("GunBase");
                GunBaseObject.transform.SetParent(BaseObject.transform);
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
        public void InitBaseGunObjectSimulate()
        {
            if (!transform.Find("myVis"))
            {
                GameObject myVis = new GameObject("myVis");
                myVis.transform.parent = transform;
                myVis.transform.localScale = Vector3.one * 0.2f;
                myVis.transform.localPosition = Vector3.zero;
                myVis.transform.localEulerAngles = new Vector3(90,0,0);
            }
            if (!transform.Find("myVis").Find("BaseBase"))
            {
                GameObject BaseBaseObject = new GameObject("BaseBase");
                BaseBaseObject.transform.SetParent(transform.Find("myVis"));
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
                BaseObject = transform.Find("myVis").Find("BaseBase").gameObject;
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
            AAVC = gameObject.AddComponent<AATurretController>();
            AAVC.Base = BaseObject.transform.parent;
            AAVC.Gun = GunObject.transform.parent;
            AAVC.MinLimit = YawLimit.Min;
            AAVC.MaxLimit = YawLimit.Max;
            AAVC.caliber = caliber;
            AAVC.gunWidth = AAAssetManager.Instance.GetWidth(Type.Value);
            AAVC.type = Type.Value;

            InitSmallShoot();
            AAVC.ShootEffect = Small_AA.GetComponent<ParticleSystem>();

        }

        public void UpdateAppearance(int type, bool simulating)
        {
            if (simulating)
            {
                BaseObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 0);
                BaseObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 0);
                BaseObject.transform.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 0);

                GunObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 1);
                GunObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 1);
                GunObject.transform.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 1);
                GunObject.transform.parent.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 2);
            }
            else
            {
                BaseObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 0);
                BaseObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 0);
                Vector3 offset = AAAssetManager.Instance.GetOffset(Type.Value, 0) * 0.2f;
                BaseObject.transform.localPosition = new Vector3((float)offset.x, -(float)offset.z, (float)offset.y);

                GunObject.GetComponent<MeshFilter>().sharedMesh = AAAssetManager.Instance.GetMesh(Type.Value, 1);
                GunObject.GetComponent<MeshRenderer>().material.mainTexture = AAAssetManager.Instance.GetTexture(Type.Value, 1);
                GunObject.transform.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 1);
                GunObject.transform.parent.localPosition = AAAssetManager.Instance.GetOffset(Type.Value, 2) - offset * 5f;
            }
        }

        public void HoldAppearance(bool simulating)
        {
            // disable Vis
            if (simulating && originVis)
            {
                if (originVis.activeSelf)
                {
                    originVis.SetActive(false);
                }
            }
            else
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
                    UpdateAppearance(Type.Value, simulating);
                }
                if (preType != Type.Value)
                {
                    preType = Type.Value;
                    UpdateAppearance(Type.Value, simulating);
                }
            }
        }
        public override void SafeAwake()
        {
            gameObject.name = "AA Block";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            Type = AddMenu("AAType", 0, new List<string>
            {
                "1x20mm",
                "3x25mm",
                "2x40mm",
                "4x40mm",
                //"2x100mm",
                //"2x127mm",
            });
            YawLimit = AddLimits("Turret Orien Limit", "YawLimit", 90, 90, 180, new FauxTransform(new Vector3(0,-0.5f,-0.5f),Quaternion.Euler(-90,0,0), Vector3.one * 0.0001f));
            InitBaseGunObjectBuild();
        }
        public override void BuildingUpdate()
        {
            HoldAppearance(false);
        }
        public override void SimulateUpdateAlways()
        {
            HoldAppearance(true);
        }
        public override void OnSimulateStart()
        {
            gameObject.name = "AA Block";
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            switch (Type.Value)
            {
                case 0:
                    gunNum = 1;
                    caliber = 20;
                    break;
                case 1:
                    gunNum = 3;
                    caliber = 25;
                    break;
                case 2:
                    gunNum = 2;
                    caliber = 40;
                    break;
                case 3:
                    gunNum = 4;
                    caliber = 40;
                    break;
                case 4:
                    gunNum = 2;
                    caliber = 100;
                    break;
                case 5:
                    gunNum = 2;
                    caliber = 127;
                    break;
                default:
                    gunNum = 1;
                    caliber = 20;
                    break;
            }
            originVis = transform.Find("Vis").gameObject;
            InitBaseGunObjectSimulate();
            UpdateAppearance(Type.Value, true);

            FireControlManager.Instance.AddGun(myPlayerID, caliber, myGuid, gameObject);

            if (!StatMaster.isClient)
            {
            }
        }
        public override void OnSimulateStop()
        {
            BlockBehaviour.BuildingBlock.GetComponent<AABlock>().UpdateAppearance(Type.Value, false);
            FireControlManager.Instance.RemoveGun(myPlayerID, myGuid);
        }
        public void OnDestroy()
        {
            FireControlManager.Instance.RemoveGun(myPlayerID, myGuid);
        }
        public override void SimulateFixedUpdateHost()
        {
            GetFCPara();
            if (hasTarget)
            {
                float yaw = MathTool.SignedAngle(targetPos - MathTool.Get2DCoordinate(transform.position),
                                                MathTool.Get2DCoordinate(-transform.up));
                AAVC.TargetYaw = yaw;
                AAVC.TargetPitch = targetPitch;
            }
            AAVC.AA_active = hasTarget;
        }
        public void OnGUI()
        {
            //GUI.Box(new Rect(100, 400, 200, 50), hasTarget.ToString() + " " + targetPitch.ToString());
        }
    }
}