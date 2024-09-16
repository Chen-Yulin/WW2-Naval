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
using Modding.Common;

namespace WW2NavalAssembly
{
    public class WoodenArmour : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MSlider Thickness;
        public float thickness;
        public GameObject Vis;
        public MeshRenderer VisRender;

        public GameObject VisRef { get { return Vis; } }

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public int frameCount = 0;
        public bool optimized = false;

        public bool AsKeel = false;

        public float _virtualCrew = 0;
        public float CrewRate = 1;

        public float Crew
        {
            get
            {
                return _virtualCrew * CrewManager.Instance.CrewResize[myPlayerID] * CrewRate;
            }
            set
            {
                if (CrewRate != 0 && CrewManager.Instance.CrewResize[myPlayerID] != 0 && _virtualCrew != 0)
                {
                    CrewRate = value / (_virtualCrew * CrewManager.Instance.CrewResize[myPlayerID]);

                    if (StatMaster.isMP)
                    {
                        CrewManager.Instance.SendCrewRate(myPlayerID, myGuid, CrewRate);
                    }
                }
            }
        }

        public float fuel = 1f;
        public bool _torched = false;
        public float HandleFire = 0f;

        public bool Torched
        {
            get
            { return _torched; }
            set {
                HandleFire /= 2f;
                if (value != _torched)
                {
                    if (value)
                    {
                        if (transform.position.y < Constants.SeaHeight)
                        {
                            return;
                        }
                    }

                    _torched = value;
                    if (StatMaster.isMP && !StatMaster.isClient)
                    {
                        CrewManager.Instance.SendOnFire(myPlayerID, myGuid, _torched);
                    }

                    Transform smoke = transform.Find("FireSmoke");
                    if (_torched)
                    {
                        if (smoke)
                        {
                            smoke.GetComponent<ParticleSystem>().Play();
                        }
                        else
                        {
                            smoke = ((GameObject)Instantiate(AssetManager.Instance.Catapult.WoodFire)).transform;
                            smoke.name = "FireSmoke";
                            smoke.gameObject.SetActive(false);
                            smoke.rotation = transform.rotation;
                            smoke.position = transform.position;
                            smoke.parent = transform;
                            smoke.transform.localScale = Vector3.one * Mathf.Sqrt(MathTool.GetArea(transform.localScale));
                            smoke.transform.GetChild(0).localScale = smoke.transform.localScale;
                            smoke.transform.GetChild(0).GetChild(0).localScale = smoke.transform.localScale;
                            smoke.gameObject.SetActive(true);
                            smoke.GetComponent<ParticleSystem>().Play();
                        }
                    }
                    else
                    {
                        smoke.GetComponent<ParticleSystem>().Stop();
                    }
                    
                }
            }
        }

        IEnumerator ChangeVis()
        {
            yield return new WaitForFixedUpdate();
            ModController.Instance.ShowChanged = false;
            yield return new WaitForSeconds(0.01f * myseed);

            UpdateVis(ModController.Instance.ShowArmour, ModController.Instance.ShowCrew);

            yield break;
        }

        public void CannonExplo(float Caliber, float dist, bool he)
        {
            if (Crew > 0)
            {
                CrewManager.Instance.GetResize(myPlayerID);
                float reduce = Caliber * 0.0401f * Mathf.Clamp(CrewRate, 0.1f, 1f) / Mathf.Clamp(dist * 4, 1, 10f) * (he ? 2 : 1);
                reduce = Mathf.Min(reduce, Crew);
                //Debug.Log(myPlayerID + "Explo:" + Crew + "-" + reduce);
                Crew -= reduce;
                CrewManager.Instance.CrewNum[myPlayerID] -= reduce;
                UpdateVis(ModController.Instance.ShowArmour, ModController.Instance.ShowCrew);

                float random = UnityEngine.Random.value;
                //Debug.Log(random);
                //if (random < (0.1f * (he?2f:1f)))
                //{
                //    Torched = true;
                //}
            }
            else
            {
                //Debug.Log("No crew?"+ CrewRate+" "+ CrewManager.Instance.CrewResize[myPlayerID]+" "+ _virtualCrew);
            }
        }
        public void CannonPerice(float Caliber)
        {
            if (Crew > 0)
            {
                CrewManager.Instance.GetResize(myPlayerID);
                float reduce = Caliber * 0.00802f * Mathf.Clamp(CrewRate, 0.1f, 1f);
                reduce = Mathf.Min(reduce, Crew);
                //Debug.Log(myPlayerID + "Perice:" + Crew + "-" + reduce);
                Crew -= reduce;
                CrewManager.Instance.CrewNum[myPlayerID] -= reduce;
                UpdateVis(ModController.Instance.ShowArmour, ModController.Instance.ShowCrew);
            }
            else
            {
                //Debug.Log("No crew?" + CrewRate + " " + CrewManager.Instance.CrewResize[myPlayerID] + " " + _virtualCrew);
            }
        }
        public void BreakForceOptimize()
        {
            foreach (var joint in BB.iJointTo)
            {
                if (joint.breakForce != 0 && !joint.connectedBody.GetComponent<CannonWell>())
                {
                    joint.breakForce = Mathf.Clamp(joint.breakForce, 35000f, float.MaxValue);
                    joint.breakTorque = Mathf.Clamp(joint.breakTorque, 35000f, float.MaxValue);
                }
            }
            foreach (var joint in BB.jointsToMe)
            {
                if (joint.breakForce != 0 && !joint.GetComponent<CannonWell>())
                {
                    joint.breakForce = Mathf.Clamp(joint.breakForce, 35000f, float.MaxValue);
                    joint.breakTorque = Mathf.Clamp(joint.breakTorque, 35000f, float.MaxValue);
                }
            }
        }
        public void ColliderOptimize()
        {
            BoxCollider BC;
            try
            {
                BC = BB.transform.Find("Joint1").GetComponent<BoxCollider>();
            }
            catch
            {
                return;
            }

            foreach (var collider in GetComponentsInChildren<BoxCollider>())
            {
                collider.enabled = false;
            }
            BC.enabled = true;

            switch (BB.BlockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    {
                        BC.center = new Vector3(0, 0, 0);
                        BC.size = new Vector3(0.8f, 0.8f, 1);
                        break;
                    }
                case (int)BlockType.DoubleWoodenBlock:
                    {
                        if (!transform.Find("Joint").gameObject.activeSelf)
                        {
                            BC.center = new Vector3(0, 0, 0f);
                            BC.size = new Vector3(0.95f, 0.95f, 1);
                        }
                        else
                        {
                            BC.center = new Vector3(0, 0, 0.5f);
                            BC.size = new Vector3(0.95f, 0.95f, 2);
                        }

                        break;
                    }
                case (int)BlockType.Log:
                    {
                        if (!transform.Find("Joint").gameObject.activeSelf)
                        {
                            BC.center = new Vector3(0, 0, 0.5f);
                            BC.size = new Vector3(0.95f, 0.95f, 2);
                        }
                        else
                        {
                            BC.center = new Vector3(0, 0, 1.0f);
                            BC.size = new Vector3(0.95f, 0.95f, 3);
                        }
                        break;
                    }
                default:
                    break;
            }
        }
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
                        if (transform.Find("Vis").Find("HalfVis").GetComponent<MeshRenderer>().enabled)
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 0.5f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 1);
                        }
                        else
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 1f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 2);
                        }
                        
                        break;
                    }
                case (int)BlockType.Log:
                    {
                        if (transform.Find("Vis").Find("HalfVis").GetComponent<MeshRenderer>().enabled)
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 1f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 2);
                        }
                        else
                        {
                            Vis.transform.localPosition = new Vector3(0, 0, 1.5f);
                            Vis.transform.localScale = new Vector3(0.95f, 0.95f, 3);
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
            VisRender.material = AssetManager.Instance.TransparentMat;
        }

        public void AddShipVolumn()
        {
            ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position);
            switch (BB.BlockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    {
                        ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position + transform.forward * transform.localScale.z);
                        break;
                    }
                case (int)BlockType.DoubleWoodenBlock:
                    {
                        if (transform.Find("Vis").Find("HalfVis").GetComponent<MeshRenderer>().enabled)
                        {
                            ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position + transform.forward * transform.localScale.z);
                        }
                        else
                        {
                            ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position + transform.forward * transform.localScale.z * 2);
                        }

                        break;
                    }
                case (int)BlockType.Log:
                    {
                        if (transform.Find("Vis").Find("HalfVis").GetComponent<MeshRenderer>().enabled)
                        {
                            ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position + transform.forward * transform.localScale.z * 2);
                        }
                        else
                        {
                            ShipSizeManager.Instance.size[myPlayerID].AddDot(transform.position + transform.forward * transform.localScale.z * 3);
                        }
                        break;
                    }
                default:
                    break;
            }
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

        public void UpdateVis(bool showArmour, bool showCrew)
        {
            if (GetComponent<Horizon>().Show)
            {
                if (showArmour)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    VisRender.material = AssetManager.Instance.ArmorMat[Mathf.Clamp((int)(thickness / 10f), 0, 65)];
                }
                else if (showCrew)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    VisRender.material = AssetManager.Instance.CrewMat[Mathf.Clamp((int)(CrewRate * 20f + 0.999f), 0, 19)];
                }
                else
                {
                    transform.Find("Vis").gameObject.SetActive(true);
                    VisRender.material = AssetManager.Instance.TransparentMat;
                }
            }
                
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
            BB = GetComponent<BlockBehaviour>();
            frameCount = 0;
            InitVis();
            if (BB.isSimulating)
            {
                ColliderOptimize();
            }
            if (!Vis)
            {
                Vis = transform.Find("WoodenArmourVis").gameObject;
                VisRender = Vis.GetComponent<MeshRenderer>();
            }

            //transform.Find("Shadow").gameObject.layer = 25;
        }
        public void Update()
        {
            if (StatMaster.isClient)
            {
                if (CrewManager.Instance.CrewUpdateReceive[myPlayerID].ContainsKey(myGuid))
                {
                    CrewRate = CrewManager.Instance.CrewUpdateReceive[myPlayerID][myGuid];
                    CrewManager.Instance.CrewUpdateReceive[myPlayerID].Remove(myGuid);
                    UpdateVis(ModController.Instance.ShowArmour, ModController.Instance.ShowCrew);
                }
                if (CrewManager.Instance.OnFire[myPlayerID].ContainsKey(myGuid))
                {
                    Torched = CrewManager.Instance.OnFire[myPlayerID][myGuid];
                    CrewManager.Instance.OnFire[myPlayerID].Remove(myGuid);
                }
            }
            if ((StatMaster.isMP && !StatMaster.isClient) || !StatMaster.isMP)
            {
                if (Torched)
                {
                    if (HandleFire > 1 || fuel < 0)
                    {
                        Torched = false;
                    }
                    else
                    {
                        fuel -= 0.05f * Time.deltaTime;
                        Crew = Mathf.Clamp(Crew - 0.5f * Time.deltaTime, 0f, 999f);
                        HandleFire += 0.1f * Time.deltaTime * (CrewRate * 0.5f + CrewManager.Instance.GetEfficiency(myPlayerID) * 0.5f);
                    }
                }
            }
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

            

            thickness = Thickness.Value;

            if (ModController.Instance.state == myseed)
            {
                if (StatMaster.isClient && transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
                {
                    SyncBulletHole();
                }
            }

            if (ModController.Instance.ShowChanged)
            {
                StartCoroutine(ChangeVis());
            }

            if (frameCount <= 4 && BB.isSimulating)
            {
                frameCount++;
            }
            if (frameCount > 4 && !optimized)
            {
                optimized = true;
                try
                {
                    AddShipVolumn();
                    CrewManager.Instance.VirtualCrew[myPlayerID] = 0f;
                }
                catch { }
                frameCount++;
            }
            else if (frameCount == 6)
            {
                _virtualCrew = MathTool.GetArea(transform.localScale);
                fuel = _virtualCrew;
                CrewManager.Instance.AddVirtualCrew(myPlayerID, _virtualCrew);
                frameCount++;
            }




        }
        public void OnEnable()
        {
            StartCoroutine(ChangeVis());
        }
    }
}
