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
    public class WeaponMsgReceiver : SingleInstance<WeaponMsgReceiver>
    {
        public override string Name { get; } = "Gun Msg Receiver";

        public static MessageType FireMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Vector3);// playerID, guid, randomForce, forward
        public static MessageType ExploMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single, DataType.Integer);//PlayerID, position, Caliber
        public static MessageType WaterHitMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single);//PlayerID, position
        public static MessageType HitHoleMsg = ModNetworking.CreateMessageType
                                                    (DataType.Integer, DataType.Integer, DataType.Single, DataType.Vector3, DataType.Vector3, DataType.Integer);
                                                    //playerID, guid, caliber, position, forward, type(0=gun,1=torpedo)
        public static MessageType ReloadMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single);

        public class firePara
        {
            public bool fire = false;
            public Vector3 fireForce = Vector3.zero;
            public Vector3 forward = Vector3.zero;
            public firePara(bool fire, Vector3 fireForce, Vector3 forward)
            {
                this.fire = fire;
                this.fireForce = fireForce;
                this.forward = forward;
            }
        }

        public class exploInfo
        {
            public Vector3 position = Vector3.zero;
            public float Caliber = 0;
            public int type = 0;// 0 for explosion, 1 for pierce, 2 for large explosion
            public exploInfo(Vector3 position, float caliber, int type)
            {
                this.position = position;
                Caliber = caliber;
                this.type = type;
            }
        }

        public class waterhitInfo
        {
            public Vector3 position = Vector3.zero;
            public float Caliber = 0;
            public waterhitInfo(Vector3 position, float caliber)
            {
                this.position = position;
                Caliber = caliber;
            }
        }

        public class hitHoleInfo
        {
            public float Caliber;
            public Vector3 position;
            public Vector3 forward;
            public int type;
            public hitHoleInfo(float caliber, Vector3 position, Vector3 forward, int type)
            {
                Caliber = caliber;
                this.position = position;
                this.forward = forward;
                this.type = type;
            }
        }

        public Dictionary<int,firePara>[] Fire = new Dictionary<int,firePara>[16];
        public Queue<exploInfo>[] ExploInfo = new Queue<exploInfo>[16];
        public Queue<waterhitInfo>[] waterHitInfo = new Queue<waterhitInfo>[16];
        public Dictionary<int, List<hitHoleInfo>>[] BulletHoleInfo = new Dictionary<int, List<hitHoleInfo>>[16];
        public Dictionary<int, bool>[] reloadTimeUpdated = new Dictionary<int,bool>[16];
        public Dictionary<int, float>[] reloadTime = new Dictionary<int, float>[16];

        public WeaponMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                Fire[i] = new Dictionary<int, firePara>();
                ExploInfo[i] = new Queue<exploInfo>();
                waterHitInfo[i] = new Queue<waterhitInfo>();
                BulletHoleInfo[i] = new Dictionary<int, List<hitHoleInfo>>();
                reloadTimeUpdated[i] = new Dictionary<int, bool>();
                reloadTime[i] = new Dictionary<int, float>();
            }
        }
        public void exploMsgReceiver(Message msg)
        {
            ExploInfo[(int)msg.GetData(0)].Enqueue(new exploInfo((Vector3)msg.GetData(1), (float)msg.GetData(2), (int)msg.GetData(3)));
        }
        public void fireKeyMsgReceiver(Message msg)
        {
            if (!StatMaster.isClient)
            {
                return;
            }
            Fire[(int)msg.GetData(0)][(int)msg.GetData(1)] = new firePara(true,(Vector3)msg.GetData(2), (Vector3)msg.GetData(3));
        }
        public void waterHitMsgReceiver(Message msg)
        {
            waterHitInfo[(int)msg.GetData(0)].Enqueue(new waterhitInfo((Vector3)msg.GetData(1), (float)msg.GetData(2)));
        }
        public void hitHoleMsgReceiver(Message msg)
        {
            if (!StatMaster.isClient)
            {
                return;
            }
            BulletHoleInfo[(int)msg.GetData(0)][(int)msg.GetData(1)].Add(new hitHoleInfo((float)msg.GetData(2), (Vector3)msg.GetData(3), (Vector3)msg.GetData(4), (int)msg.GetData(5)));
        }
        public void reloadTimeMsgReceiver(Message msg)
        {
            if (!StatMaster.isClient)
            {
                return;
            }
            reloadTimeUpdated[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            reloadTime[(int)msg.GetData(0)][(int)msg.GetData(1)] = (float)msg.GetData(2);
        }
    }

    public class BlockPoseReceiver : SingleInstance<BlockPoseReceiver>
    {
        public override string Name { get; } = "BlockPoseReceiver";

        public static MessageType forwardMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3);

        public Dictionary<int,Vector3>[] forward = new Dictionary<int, Vector3>[16];
        
        public BlockPoseReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                forward[i] = new Dictionary<int,Vector3>();
            }
        }
        public void forwardMsgReceiver(Message msg)
        {
            forward[(int)msg.GetData(0)][(int)msg.GetData(1)] = (Vector3)msg.GetData(2);
        }
    }

    public class MakeAudioSourceFixedPitch : MonoBehaviour
    {
        protected AudioSource FixedAS;
        protected void Start()
        {
            FixedAS = base.GetComponent<AudioSource>();
        }
        protected void Update()
        {
            FixedAS.pitch = Time.timeScale;
        }
    }
    public class BulletBehaviour : MonoBehaviour
    {
        public int myPlayerID;
        public int myGuid;
        

        public int CannonType = 0;

        public float Caliber;
        public bool fire = false;
        bool thrustOn = false;
        Rigidbody myRigid;
        public Vector3 randomForce;
        public bool hasHitWater = false;

        public float penetration;
        public float decay;


        Stack<int> pericedBlock = new Stack<int> ();

        public int APtimer = 0;
        public bool APtimerOn;

        public bool exploded = false;
        public bool spotted = false;

        public void AddFireSound(Transform t)
        {
            AudioSource fireAS = t.gameObject.AddComponent<AudioSource> ();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            fireAS.clip = ModResource.GetAudioClip("GunShot Audio");
            fireAS.Play ();
            fireAS.spatialBlend = 1.0f;
            fireAS.volume = Caliber/1000;
            fireAS.rolloffMode = AudioRolloffMode.Linear;
            fireAS.maxDistance = 500;
            fireAS.SetSpatializerFloat(1, 1f);
            fireAS.SetSpatializerFloat(2, 0);
            fireAS.SetSpatializerFloat(3, 12);
            fireAS.SetSpatializerFloat(4, 1000f);
            fireAS.SetSpatializerFloat(5, 1f);
        }
        public void AddExploSound(Transform t)
        {
            AudioSource exploAS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            exploAS.clip = ModResource.GetAudioClip("GunExplo Audio");
            exploAS.Play();
            exploAS.spatialBlend = 1.0f;
            exploAS.volume = Caliber / 100;
            exploAS.rolloffMode = AudioRolloffMode.Linear;
            exploAS.maxDistance = 300;
            exploAS.SetSpatializerFloat(1, 1f);
            exploAS.SetSpatializerFloat(2, 0);
            exploAS.SetSpatializerFloat(3, 12);
            exploAS.SetSpatializerFloat(4, 1000f);
            exploAS.SetSpatializerFloat(5, 1f);
        }
        public void AddPierceSound(Transform t)
        {
            AudioSource AS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            AS.clip = ModResource.GetAudioClip("GunPierce Audio");
            AS.Play();
            AS.spatialBlend = 1.0f;
            AS.volume = Caliber / 1000;
            AS.rolloffMode = AudioRolloffMode.Linear;
            AS.maxDistance = 200;
            AS.SetSpatializerFloat(1, 1f);
            AS.SetSpatializerFloat(2, 0);
            AS.SetSpatializerFloat(3, 12);
            AS.SetSpatializerFloat(4, 1000f);
            AS.SetSpatializerFloat(5, 1f);
        }
        public void AddWaterHitSound(Transform t)
        {
            AudioSource AS = t.gameObject.AddComponent<AudioSource>();
            t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            AS.clip = ModResource.GetAudioClip("GunWaterHit Audio");
            AS.Play();
            AS.spatialBlend = 1.0f;
            AS.volume = Caliber / 800;
            AS.rolloffMode = AudioRolloffMode.Linear;
            AS.maxDistance = 500;
            AS.SetSpatializerFloat(1, 1f);
            AS.SetSpatializerFloat(2, 0);
            AS.SetSpatializerFloat(3, 12);
            AS.SetSpatializerFloat(4, 1000f);
            AS.SetSpatializerFloat(5, 1f);
        }
        public bool Perice(RaycastHit hit)
        {
            if (!hit.collider.transform.parent.GetComponent<BlockBehaviour>())
            {
                return false;
            }

            if (pericedBlock.Contains(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
            {
                return true;
            }

            float angle = Vector3.Angle(hit.normal, -myRigid.velocity);
            if (angle > 10 && angle < 20)
            {
                angle -= (angle - 10) / 2;
            }
            else if (angle >= 20 && angle < 70)
            {
                angle -= 5;
            }
            else if (angle >= 70)
            {
                if (UnityEngine.Random.value<(angle-70)/20)
                {
                    return false;
                }
                angle -= 3;
            }

            float Thickness;
            if (hit.collider.transform.parent.GetComponent<WoodenArmour>())
            {
                Thickness = hit.collider.transform.parent.GetComponent<WoodenArmour>().thickness;
            }
            else if(hit.collider.transform.parent.GetComponent<DefaultArmour>())
            {
                Thickness = 20;
            }
            else if (hit.collider.transform.parent.GetComponent<CannonWell>())
            {
                Vector3 CylinderUp = hit.collider.transform.parent.GetComponent<CannonWell>().WellVis.transform.up;

                //Debug.Log(CylinderUp);
                //Debug.Log(hit.normal);
                if ((hit.normal - CylinderUp).magnitude < 0.01f || (hit.normal - CylinderUp).magnitude > 1.99f)
                {
                    Thickness = 20f;
                }
                else
                {
                    Thickness = hit.collider.transform.parent.GetComponent<CannonWell>().thickness;
                }

                

            }
            else
            {
                return true;
            }

            if (Thickness / Mathf.Cos(angle * Mathf.PI / 180) > penetration)
            {
                return false;
            }
            else
            {
                float eqThick = Thickness / Mathf.Cos(angle * Mathf.PI / 180);
                myRigid.velocity *= 1 - eqThick * 0.8f / penetration;
                penetration -= eqThick;
                pericedBlock.Push(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());

                if (pericedBlock.Count == 1 && hit.collider.transform.parent.name != "SpinningBlock")    // add waterIn behaviour
                {
                    GameObject waterinhole = new GameObject("waterInHole");
                    waterinhole.transform.SetParent(hit.collider.transform.parent);
                    waterinhole.transform.localPosition = Vector3.zero;
                    waterinhole.transform.localRotation = Quaternion.identity;
                    waterinhole.transform.localScale = Vector3.one;

                    WaterInHole WH = waterinhole.AddComponent<WaterInHole>();
                    WH.hittedCaliber = Caliber;
                    WH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                }

                string hittedname = hit.collider.transform.parent.name;
                if (hittedname == "DoubleWoodenBlock" || hittedname == "SingleWoodenBlock" || hittedname == "Log" || hittedname == "SpinningBlock")
                {   // add hole projector
                    GameObject piercedhole = new GameObject("PiercedHole");
                    piercedhole.transform.SetParent(hit.collider.transform.parent);
                    piercedhole.transform.localPosition = Vector3.zero;
                    piercedhole.transform.localRotation = Quaternion.identity;
                    piercedhole.transform.localScale = Vector3.one;

                    PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                    PH.hittedCaliber = Caliber;
                    PH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                    PH.forward = myRigid.velocity.normalized;

                    if (StatMaster.isMP)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.HitHoleMsg.CreateMessage( (int) hit.collider.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID,
                                                                                            hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode(),
                                                                                            Caliber, PH.position, PH.forward, 0));
                    }
                }


                return true;
            }


        }
        public void PlayGunShot()
        {
            GameObject gunsmoke;
            if (Caliber>=283)
            {
                gunsmoke = (GameObject)Instantiate(AssetManager.Instance.GunSmoke.gunsmoke1, transform.position, transform.rotation);
                gunsmoke.transform.localScale = Caliber / 381 * Vector3.one;
                Destroy(gunsmoke, 3);
            }
            else
            {
                gunsmoke = (GameObject)Instantiate(AssetManager.Instance.GunSmoke.gunsmoke2, transform.position, transform.rotation);
                gunsmoke.transform.localScale = Caliber / 170 * Vector3.one;
                Destroy(gunsmoke, 3);
            }
            AddFireSound(gunsmoke.transform);
        }
        public void BreakBallon(Vector3 position)
        {
            GameObject damager = new GameObject("damager");
            damager.transform.position = position;
            damager.AddComponent<BoxCollider>().size = new Vector3(0.01f,0.01f,0.01f);
            
            damager.AddComponent<Rigidbody>().velocity = new Vector3(0, 20, 0);
            Destroy(damager, 0.01f);
        }
        public void APDetectCollisionHost()
        {
            Ray CannonRay = new Ray(transform.position, myRigid.velocity);
            RaycastHit[] hitList = Physics.RaycastAll(CannonRay, myRigid.velocity.magnitude * Time.fixedDeltaTime);
            
            if (hitList.Length != 0)
            {
                List<RaycastHit> list = new List<RaycastHit>(hitList);
                list.Sort((RaycastHit a, RaycastHit b) => (base.gameObject.transform.position - a.point).magnitude.CompareTo((base.gameObject.transform.position - b.point).magnitude));
                hitList = list.ToArray();
                foreach (RaycastHit hit in hitList)
                {
                    if (hit.collider.isTrigger && hit.collider.name != "AmmoVis" && hit.collider.name != "WellArmourVis")
                    {
                        continue;
                    }
                    APtimerOn = true;

                    // pericing

                    if (Perice(hit))
                    {
                        // particle and sound effect
                        GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, hit.point, Quaternion.identity);
                        pierceEffect.transform.localScale = Caliber / 400 * Vector3.one;
                        Destroy(pierceEffect, 1);
                        AddPierceSound(pierceEffect.transform);
                        ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Caliber, 1));

                        // destroy balloon if directly hitted
                        if (hit.collider.transform.parent.name == "Balloon" || hit.collider.transform.parent.name == "SqrBalloon")
                        {
                            BreakBallon(hit.collider.transform.position);
                        }

                        // well or ammo damage
                        if (hit.collider.transform.parent.name == "SpinningBlock")
                        {
                            CannonWell CW = hit.collider.transform.parent.GetComponent<CannonWell>();
                            if (CW.totalCaliber != 0)
                            {
                                float WellExploProb = Caliber / CW.myCaliber * CW.gunNum * 0.08f;
                                float WellPalsyProb = 2 * WellExploProb;
                                float AmmoExploProb = 3 * WellExploProb;
                                if (hit.collider.name == "WellArmourVis")
                                {
                                    //Debug.Log(WellPalsyProb);
                                    if (UnityEngine.Random.value < WellPalsyProb)
                                    {
                                        CW.Wellpalsy = true;
                                    }
                                    if (UnityEngine.Random.value < WellExploProb)
                                    {
                                        CW.WellExplo = true;
                                    }
                                }
                                if (hit.collider.name == "AmmoVis")
                                {
                                    if (UnityEngine.Random.value < AmmoExploProb)
                                    {
                                        CW.AmmoExplo = true;
                                    }
                                }
                            }
                        }

                        // add force
                        try
                        {
                            hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 4, ForceMode.Force);
                        }
                        catch { }
                        continue;
                    }
                    if (!exploded)
                    {
                        APPlayExploHit(hit);
                        Destroy(gameObject);
                    }
                    
                    break;
                }
            }
        }
        public void APDetectCollisionClient()
        {
            foreach (WeaponMsgReceiver.exploInfo exploInfo in WeaponMsgReceiver.Instance.ExploInfo[myPlayerID])
            {
                Vector3 exploPosition = exploInfo.position;
                switch (exploInfo.type)
                {
                    case 0:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 400 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform);
                            break;
                        }
                    case 1:
                        {
                            GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, exploPosition, Quaternion.identity);
                            pierceEffect.transform.localScale = Caliber / 400 * Vector3.one;
                            Destroy(pierceEffect, 1);
                            AddPierceSound(pierceEffect.transform);
                            break;
                        }
                    default:
                        break;
                }
                
                
            }
            WeaponMsgReceiver.Instance.ExploInfo[myPlayerID].Clear();
        }
        public void APDetectWaterHost()
        {
            if (transform.position.y < 20f && pericedBlock.Count == 0 && Caliber >= 283)
            {
                if (!spotted)
                {
                    spotted = true;
                    if ((transform.position - ControllerDataManager.Instance.lockData[myPlayerID].position).magnitude < 200f)
                    {
                        if (StatMaster.isMP)
                        {
                            if (myPlayerID == 0)
                            {
                                ControllerDataManager.Instance.SpotNum[myPlayerID]++;
                            }
                        }
                        else
                        {
                            ControllerDataManager.Instance.SpotNum[myPlayerID]++;
                        }
                    }
                }
                

                myRigid.velocity = new Vector3(myRigid.velocity.x, myRigid.velocity.y / 1.5f, myRigid.velocity.z);
                myRigid.AddForce(myRigid.velocity * 0.8f - Vector3.up * 10);
                penetration *= 0.97f;
            }
            if (transform.position.y < 20f && !hasHitWater && myRigid.velocity.y<0)
            {
                
                myRigid.drag = 11f;
                GameObject waterhit;
                if (Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);

                    if (Vector3.Angle(Vector3.up, myRigid.velocity) > 100 && UnityEngine.Random.value > 0.5f) 
                    {
                        Destroy(gameObject, 0.5f);
                    }
                    else
                    {
                        Destroy(gameObject, 0.2f);
                    }

                    
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                    Destroy(gameObject, 0.4f);
                }
                AddWaterHitSound(waterhit.transform);
                hasHitWater = true;
                ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Caliber));
            }
        }
        public void APDetectWaterClient()
        {
            if (transform.position.y < 20f)
            {
                if (!spotted)
                {
                    spotted = true;
                    if ((transform.position - ControllerDataManager.Instance.lockData[myPlayerID].position).magnitude < 200f)
                    {
                        if (StatMaster.isMP)
                        {
                            if (myPlayerID == PlayerData.localPlayer.networkId)
                            {
                                ControllerDataManager.Instance.SpotNum[myPlayerID]++;
                            }
                        }
                    }
                }
            }

            foreach (WeaponMsgReceiver.waterhitInfo waterhitInfo in WeaponMsgReceiver.Instance.waterHitInfo[myPlayerID])
            {
                GameObject waterhit;
                if (waterhitInfo.Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, waterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = waterhitInfo.Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, waterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = waterhitInfo.Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }

                AddWaterHitSound(waterhit.transform);

            }
            WeaponMsgReceiver.Instance.waterHitInfo[myPlayerID].Clear();
        }
        public void APPlayExploHit(RaycastHit hit)
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, hit.point - myRigid.velocity.normalized * Caliber / 800f, Quaternion.identity);
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            //send to client
            ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Caliber, 0));

            try
            {
                hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 3, ForceMode.Force);
            }
            catch { }

            Collider[] ExploCol = Physics.OverlapSphere(hit.point-myRigid.velocity.normalized* Caliber /800f, Caliber / 300f);
            foreach (Collider hitedCollider in ExploCol)
            {
                if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                {
                    hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(5f * Caliber, hit.point, 5f);
                }

                if (pericedBlock.Count != 0)
                {
                    try
                    {
                        //Debug.Log(hitedCollider.transform.parent.name);
                        if (hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon")
                        {
                            BreakBallon(hitedCollider.transform.position);
                        }
                    }
                    catch { }
                }
            }
            transform.FindChild("CannonVis").gameObject.SetActive(false);
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());
        }
        public void APPlayExploInAir()
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            //send to client
            ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, 0));

            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber / 300f);
            foreach (Collider hitedCollider in ExploCol)
            {
                if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                {
                    hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(5f * Caliber, transform.position, 7f);
                }

                if (pericedBlock.Count != 0)
                {
                    try
                    {
                        //Debug.Log(hitedCollider.transform.parent.name);
                        if (hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon")
                        {
                            BreakBallon(hitedCollider.transform.position);
                        }
                    }
                    catch { }
                }
            }
            transform.FindChild("CannonVis").gameObject.SetActive(false);
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());
        }
        public void HEDetectWaterHost()
        {
            if (transform.position.y < 20f && !hasHitWater && myRigid.velocity.y < 0)
            {
                if (!spotted)
                {
                    spotted = true;
                    if ((transform.position - ControllerDataManager.Instance.lockData[myPlayerID].position).magnitude < 200f)
                    {
                        if (StatMaster.isMP)
                        {
                            if (myPlayerID == 0)
                            {
                                ControllerDataManager.Instance.SpotNum[myPlayerID]++;
                            }
                        }
                        else
                        {
                            ControllerDataManager.Instance.SpotNum[myPlayerID]++;
                        }
                    }
                }
                GameObject waterhit;
                if (Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                }
                Destroy(waterhit, 3);
                AddWaterHitSound(waterhit.transform);
                hasHitWater = true;
                HEDestroyBalloonWater(transform.position);
                HEPlayExplo(0);
                Destroy(gameObject,0.1f);
                ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Caliber));
            }
        }
        public void HEDetectCollisionHost()
        {
            Ray CannonRay = new Ray(transform.position, myRigid.velocity);
            RaycastHit[] hitList = Physics.RaycastAll(CannonRay, myRigid.velocity.magnitude * Time.fixedDeltaTime);

            if (hitList.Length != 0)
            {
                List<RaycastHit> list = new List<RaycastHit>(hitList);
                list.Sort((RaycastHit a, RaycastHit b) => (base.gameObject.transform.position - a.point).magnitude.CompareTo((base.gameObject.transform.position - b.point).magnitude));
                hitList = list.ToArray();
                foreach (RaycastHit hit in hitList)
                {
                    if (hit.collider.isTrigger)
                    {
                        continue;
                    }

                    if (!exploded)
                    {
                        transform.position = hit.point;
                        float tmpThickness;
                        try
                        {
                            tmpThickness = hit.collider.transform.parent.GetComponent<WoodenArmour>().thickness;
                        }catch
                        {
                            tmpThickness = 20;
                        }
                        HEDestroyBalloon(hit);
                        HEPlayExplo(tmpThickness);
                        Destroy(gameObject,0.1f);
                    }

                    break;
                }
            }
        }
        public void HEPlayExplo(float thickness)
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            //send to client
            ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, 0));

            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber /( 3 * Mathf.Clamp(thickness,Caliber/10,Caliber) ) );
            //Debug.Log(Caliber / (5 * Mathf.Clamp(thickness, 10, Caliber)));
            foreach (Collider hitedCollider in ExploCol)
            {
                try
                {
                    //Debug.Log(hitedCollider.gameObject.transform.parent.name);
                    if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                    {
                        hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(1000f * Caliber / (5 * Mathf.Clamp(thickness, Caliber / 10, Caliber)), transform.position, Caliber / (5 * Mathf.Clamp(thickness, Caliber / 10, Caliber)));
                    }
                }
                catch { }
            }
            transform.FindChild("CannonVis").gameObject.SetActive(false);
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());
        }
        public void HEDestroyBalloon(RaycastHit hit)
        {
            int armourGuid = hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode();
            //Debug.Log(armourGuid);
            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber / 100);
            foreach (Collider hitedCollider in ExploCol)
            {
                try
                {
                    //Debug.Log(hitedCollider.transform.parent.name);
                    if (hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon")
                    {
                        bool hasArmourBetween = false;
                        Ray Ray = new Ray(hit.point, hitedCollider.transform.position-hit.point);
                        RaycastHit[] hitList = Physics.RaycastAll(Ray, (hitedCollider.transform.position - hit.point).magnitude);
                        foreach (RaycastHit raycastHit in hitList)
                        {
                            if (raycastHit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode() != armourGuid 
                                && raycastHit.collider.transform.parent.GetComponent<WoodenArmour>())
                            {
                                hasArmourBetween = true;
                            }
                        }
                        //Debug.Log(hasArmourBetween);
                        if (hasArmourBetween)
                        {
                            continue;
                        }
                        
                        BreakBallon(hitedCollider.transform.position);
                    }
                }
                catch { }
            }
        }
        public void HEDestroyBalloonWater(Vector3 position)
        {
            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber / 100);
            foreach (Collider hitedCollider in ExploCol)
            {
                try
                {
                    //Debug.Log(hitedCollider.transform.parent.name);
                    if (hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon")
                    {
                        bool hasArmourBetween = false;
                        Ray Ray = new Ray(position, hitedCollider.transform.position - position);
                        RaycastHit[] hitList = Physics.RaycastAll(Ray, (hitedCollider.transform.position - position).magnitude);
                        foreach (RaycastHit raycastHit in hitList)
                        {
                            if (raycastHit.collider.transform.parent.GetComponent<WoodenArmour>())
                            {
                                hasArmourBetween = true;
                            }
                        }
                        //Debug.Log(hasArmourBetween);
                        if (hasArmourBetween)
                        {
                            continue;
                        }

                        BreakBallon(hitedCollider.transform.position);
                    }
                }
                catch { }
            }
        }
        public void Start()
        {
            myRigid = gameObject.GetComponent<Rigidbody>();
            penetration = Caliber * 2;
            decay = Mathf.Pow(0.5f, 1 / (Mathf.Sqrt(Caliber + 100) * 33f));
            
            TrailRenderer TR = gameObject.GetComponent<TrailRenderer>();
            if (CannonType == 0)
            {
                TR.material.SetColor("_TintColor", Color.white);
            }
            else
            {
                TR.material.SetColor("_TintColor", Color.white - 0.8f * Color.blue);
            }
            
        }
        public void FixedUpdate()
        {
            if (fire)
            {
                if (!thrustOn)
                {
                    myRigid.velocity = transform.forward * Mathf.Sqrt(Caliber+100)*8.5f;
                    thrustOn = true;
                    PlayGunShot();
                } // add initial speed
                transform.rotation = Quaternion.LookRotation(myRigid.velocity);
                myRigid.AddForce(randomForce);

                if (!StatMaster.isClient)
                {
                    if (CannonType == 0) // for AP
                    {
                        APDetectCollisionHost();
                        if (pericedBlock.Count == 0 && ModController.Instance.showSea)
                        {
                            APDetectWaterHost();
                        }
                        if (APtimerOn)
                        {
                            APtimer++;
                        }
                        if (APtimer > 5f && !exploded)
                        {
                            APPlayExploInAir();
                        }
                    }
                    else
                    {
                        HEDetectCollisionHost();
                        if (ModController.Instance.showSea)
                        {
                            HEDetectWaterHost();
                        }
                        
                    }
                    
                }
                else
                {
                    APDetectCollisionClient();
                    APDetectWaterClient();
                    
                }
                
            }

            penetration *= decay;
        }
    }
    public class Gun:BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int myseed;

        public MKey FireKey;
        public MKey SwitchKey;
        public MSlider Caliber;
        public MToggle TrackOn;
        public MToggle FireControl;
        public MText GunGroup;

        public int CannonType;
        public int NextCannonType;

        GameObject CannonPrefab;

        Transform VisTransform;

        int muzzleStage = 100;

        public float reloadTime;
        public float currentReloadTime = 0;

        Texture ReloadHEOut;
        Texture ReloadHEIn;
        Texture ReloadAPOut;
        Texture ReloadAPIn;
        int iconSize = 30;

        public float GetFCPitchPara()
        {
            if (StatMaster.isClient)
            {
                //float angle = Vector3.Angle(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid], Vector3.up);
                //return (Mathf.Clamp(90 - angle, 0, 45));
                float angle = Vector3.Angle(transform.forward, Vector3.up);
                return (Mathf.Clamp(90 - angle, 0, 45));
            }
            else
            {
                float angle = Vector3.Angle(transform.forward, Vector3.up);
                return (Mathf.Clamp(90 - angle, 0, 45));
            }
        }
        public Vector2 GetFCOrienPara()
        {
            if (StatMaster.isClient)
            {
                //return new Vector2(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].x, BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].z);
                return new Vector2(transform.forward.x, transform.forward.z);
            }
            else
            {
                return new Vector2(transform.forward.x, transform.forward.z);
            }
        }
        void InitCannon()
        {
            CannonPrefab = new GameObject("NavaCannon");
            BulletBehaviour BBtmp = CannonPrefab.AddComponent<BulletBehaviour>();
            BBtmp.Caliber = Caliber.Value;
            BBtmp.myPlayerID = myPlayerID;
            Rigidbody RBtmp = CannonPrefab.AddComponent<Rigidbody>();
            RBtmp.mass = 0.2f;
            RBtmp.drag = 0.02f;
            RBtmp.useGravity = true;
            GameObject CannonVis = new GameObject("CannonVis");
            CannonVis.transform.SetParent(CannonPrefab.transform);
            CannonVis.transform.localPosition = Vector3.zero;
            CannonVis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CannonVis.transform.localScale = Vector3.one;
            MeshFilter MFtmp = CannonVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("Cannon Mesh").Mesh;
            MeshRenderer MRtmp = CannonVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("Cannon Texture").Texture;

            TrailRenderer TRtmp = CannonPrefab.AddComponent<TrailRenderer>();
            TRtmp.autodestruct = false;

            TRtmp.receiveShadows = false;
            TRtmp.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            TRtmp.startWidth = 0.001f * Caliber.Value;
            TRtmp.endWidth = 0f;

            TRtmp.material = new Material(Shader.Find("Particles/Additive"));

            TRtmp.material.SetColor("_TintColor", Color.white - 0.8f * Color.blue);
            

            TRtmp.enabled = true;
            TRtmp.time = 0.1f;


            CannonPrefab.SetActive(false);
        }

        public void ClearCannon()
        {
            List<GameObject> tmps = new List<GameObject>();
            for (int i = 0; i < 20; i++)
            {
                GameObject tmp = GameObject.Find("NavalCannon" + myPlayerID.ToString());
                if (tmp)
                {
                    tmp.name = "NavalCannon-1";
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
            SwitchKey = AddKey("Switch AP/HE", "SwitchCannonType", KeyCode.R);
            Caliber = AddSlider("Caliber (mm)", "Caliber", 406, 100, 510);
            TrackOn = AddToggle("Track Cannon", "TrackCannon", false);
            FireControl = AddToggle("Fire Control", "FireControl", false);
            GunGroup = AddText("Gun Group", "GunGroup", "g0");
            ReloadHEOut = ModResource.GetTexture("ReloadHEOut Texture").Texture;
            ReloadHEIn = ModResource.GetTexture("ReloadHEIn Texture").Texture;
            ReloadAPOut = ModResource.GetTexture("ReloadAPOut Texture").Texture;
            ReloadAPIn = ModResource.GetTexture("ReloadAPIn Texture").Texture;
            myseed = (int)(UnityEngine.Random.value * 10);
        }
        public override void BuildingUpdate()
        {
            if (ModController.Instance.state % 10 == myseed)
            {
                Grouper.Instance.AddGun(myPlayerID, GunGroup.Value, BlockBehaviour.Guid.GetHashCode(), gameObject);
            }
        }
        public override void OnSimulateStart()
        {
            VisTransform = transform.Find("Vis");
            BlockBehaviour.blockJoint.breakForce = float.PositiveInfinity;
            BlockBehaviour.blockJoint.breakTorque = float.PositiveInfinity;
            InitCannon();
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            reloadTime = 0.4f * Mathf.Sqrt(Caliber.Value) - 3;
            currentReloadTime = reloadTime;
            Grouper.Instance.AddGun(myPlayerID, GunGroup.Value, myGuid, gameObject);
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.AddGun(myPlayerID, Caliber.Value, myGuid, gameObject);
            }
            try
            {
                if (StatMaster.isClient)
                {
                    WeaponMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new WeaponMsgReceiver.firePara(false,Vector3.zero,Vector3.zero));
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
            ClearCannon();
            DestroyImmediate(CannonPrefab);
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.RemoveGun(myPlayerID,myGuid);
            }
            WeaponMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTime[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID].Remove(myGuid);
        }
        public void OnDestroy()
        {
            Grouper.Instance.AddGun(myPlayerID, "null", myGuid, gameObject);
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.RemoveGun(myPlayerID, myGuid);
            }
        }
        public override void SimulateUpdateHost()
        {
            if (SwitchKey.IsPressed)
            {
                if (NextCannonType == 0)
                {
                    NextCannonType = 1;
                }
                else
                {
                    NextCannonType = 0;
                }
            }

            if (currentReloadTime < reloadTime)
            {
                currentReloadTime += Time.deltaTime;
                if (ModController.Instance.state == myseed)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ReloadMsg.CreateMessage(myPlayerID, myGuid, currentReloadTime));
                }
                return;
            }

            if (FireKey.IsPressed)
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward*5);
                Vector3 randomForce = new Vector3(UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f) * 3 / Mathf.Sqrt(Caliber.Value);
                randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f,0) * 5 / Mathf.Sqrt(Caliber.Value);

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;
                Destroy(Cannon, 10);

                CannonType = NextCannonType;
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward));
                }
                
                if (!TrackOn.isDefaultValue)
                {
                    try
                    {
                        CannonTrackManager.Instance.AddTrackedCannon(myPlayerID, Cannon);
                    }
                    catch { }
                }
                
            }
        }
        public override void SimulateUpdateClient()
        {
            if (SwitchKey.IsPressed)
            {
                if (NextCannonType == 0)
                {
                    NextCannonType = 1;
                }
                else
                {
                    NextCannonType = 0;
                }
            }

            if (WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid])
            {
                WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid] = false;
                currentReloadTime = WeaponMsgReceiver.Instance.reloadTime[myPlayerID][myGuid];
            }
            if (currentReloadTime < reloadTime)
            {
                currentReloadTime += Time.deltaTime;
            }

            if (WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward, 
                                                            Quaternion.LookRotation(WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].forward, Vector3.up));
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;
                Destroy(Cannon, 10);

                CannonType = NextCannonType;

                if (!TrackOn.isDefaultValue)
                {
                    try
                    {
                        CannonTrackManager.Instance.AddTrackedCannon(myPlayerID, Cannon);
                    }
                    catch { }
                    
                }
                
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (!FireControl.isDefaultValue && StatMaster.isMP)
            {
                //ModNetworking.SendToAll(BlockPoseReceiver.forwardMsg.CreateMessage(myPlayerID, myGuid, transform.forward));
            }

            if (muzzleStage < 7)
            {
                muzzleStage++;
                VisTransform.localPosition = Vector3.Lerp(VisTransform.localPosition, - Caliber.Value / 800 * Vector3.forward, Caliber.Value/800);
            }
            else
            {
                if (VisTransform.localPosition.z < 0)
                {
                    VisTransform.localPosition += new Vector3(0, 0, 0.04f);
                }
            }
            if (currentReloadTime < reloadTime)
            {
                return;
            }

            if (FireKey.EmulationPressed())
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward * 5);
                Vector3 randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 3 / Mathf.Sqrt(Caliber.Value);
                randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f, 0) * 5 / Mathf.Sqrt(Caliber.Value);

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;
                Destroy(Cannon, 10);
                CannonType = NextCannonType;

                ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward));

                if (!TrackOn.isDefaultValue)
                {
                    try
                    {
                        CannonTrackManager.Instance.AddTrackedCannon(myPlayerID, Cannon);
                    }
                    catch { }
                }
            }
        }
        public override void SimulateFixedUpdateClient()
        {
            if (muzzleStage < 7)
            {
                muzzleStage++;
                VisTransform.localPosition = Vector3.Lerp(VisTransform.localPosition, -Caliber.Value / 800 * Vector3.forward, Caliber.Value / 800);
            }
            else
            {
                if (VisTransform.localPosition.z < 0)
                {
                    VisTransform.localPosition += new Vector3(0, 0, 0.04f);
                }
            }
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
                    if (NextCannonType == 0)
                    {
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), ReloadAPOut);
                    }
                    else
                    {
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), ReloadHEOut);
                    }
                    if (CannonType == 0)
                    {
                        int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - currIconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - currIconSize / 2, currIconSize, currIconSize), ReloadAPIn);
                    }
                    else
                    {
                        int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                        GUI.DrawTexture(new Rect(onScreenPosition.x - currIconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - currIconSize / 2, currIconSize, currIconSize), ReloadHEIn);
                    }

                }
            }
        }
    }
}
