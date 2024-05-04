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
using static WW2NavalAssembly.Aircraft;

namespace WW2NavalAssembly
{
    public class WeaponMsgReceiver : SingleInstance<WeaponMsgReceiver>
    {
        public override string Name { get; } = "Gun Msg Receiver";

        public static MessageType FireMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Vector3, DataType.Vector3, DataType.Single);// playerID, guid, randomForce, forward, vel, time
        public static MessageType ExploMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single, DataType.Integer);//PlayerID, position, Caliber
        public static MessageType WaterHitMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single);//PlayerID, position
        public static MessageType HitHoleMsg = ModNetworking.CreateMessageType
                                                    (DataType.Integer, DataType.Integer, DataType.Single, DataType.Vector3, DataType.Vector3, DataType.Integer);
                                                    //playerID, guid, caliber, position, forward, type(0=gun,1=torpedo)
        public static MessageType ReloadMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Single, DataType.Boolean, DataType.Boolean, DataType.Integer);

        public class firePara
        {
            public bool fire = false;
            public Vector3 fireForce = Vector3.zero;
            public Vector3 forward = Vector3.zero;
            public Vector3 vel = Vector3.zero;
            public float time = 20;
            public firePara(bool fire, Vector3 fireForce, Vector3 forward, Vector3 vel, float time)
            {
                this.fire = fire;
                this.fireForce = fireForce;
                this.forward = forward;
                this.vel = vel;
                this.time = time;
            }
        }

        public class exploInfo
        {
            public Vector3 position = Vector3.zero;
            public float Caliber = 0;
            public int type = 0;// 0 for explosion, 1 for pierce, 2 for large explosion, 3 for explosion with smoke, 4 for small explosion
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
        public Dictionary<int, bool>[] CannonType = new Dictionary<int, bool>[16];
        public Dictionary<int, bool>[] NextCannonType = new Dictionary<int, bool>[16];
        public Dictionary<int, int>[] CannonNum = new Dictionary<int, int>[16];

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
                CannonType[i] = new Dictionary<int, bool>();
                NextCannonType[i] = new Dictionary<int, bool>();
                CannonNum[i] = new Dictionary<int, int>();
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
            Fire[(int)msg.GetData(0)][(int)msg.GetData(1)] = new firePara(true,(Vector3)msg.GetData(2), (Vector3)msg.GetData(3), (Vector3)msg.GetData(4), (float)msg.GetData(5));
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
            CannonType[(int)msg.GetData(0)][(int)msg.GetData(1)] = (bool)msg.GetData(3);
            NextCannonType[(int)msg.GetData(0)][(int)msg.GetData(1)] = (bool)msg.GetData(4);
            CannonNum[(int)msg.GetData(0)][(int)msg.GetData(1)] = (int)msg.GetData(5);
        }

        // solve explo
        public void AddExploSound(Transform t, float size)
        {
            AudioSource exploAS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            exploAS.clip = ModResource.GetAudioClip("GunExplo Audio");
            exploAS.Play();
            exploAS.spatialBlend = 1.0f;
            exploAS.volume = size / 100;
            exploAS.rolloffMode = AudioRolloffMode.Linear;
            exploAS.maxDistance = 300;
            exploAS.SetSpatializerFloat(1, 1f);
            exploAS.SetSpatializerFloat(2, 0);
            exploAS.SetSpatializerFloat(3, 12);
            exploAS.SetSpatializerFloat(4, 1000f);
            exploAS.SetSpatializerFloat(5, 1f);
        }
        public void AddPierceSound(Transform t, float size)
        {
            AudioSource AS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            AS.clip = ModResource.GetAudioClip("GunPierce Audio");
            AS.Play();
            AS.spatialBlend = 1.0f;
            AS.volume = size / 1000;
            AS.rolloffMode = AudioRolloffMode.Linear;
            AS.maxDistance = 200;
            AS.SetSpatializerFloat(1, 1f);
            AS.SetSpatializerFloat(2, 0);
            AS.SetSpatializerFloat(3, 12);
            AS.SetSpatializerFloat(4, 1000f);
            AS.SetSpatializerFloat(5, 1f);
        }
        public void AddWaterHitSound(Transform t, float size)
        {
            AudioSource AS = t.gameObject.AddComponent<AudioSource>();
            AS.clip = ModResource.GetAudioClip("GunWaterHit Audio");
            AS.Play();
            AS.spatialBlend = 1.0f;
            AS.volume = size / 800;
            AS.rolloffMode = AudioRolloffMode.Linear;
            AS.maxDistance = 500;
            AS.SetSpatializerFloat(1, 1f);
            AS.SetSpatializerFloat(2, 0);
            AS.SetSpatializerFloat(3, 12);
            AS.SetSpatializerFloat(4, 1000f);
            AS.SetSpatializerFloat(5, 1f);
        }
        public void PlayExploOnClient(int playerID)
        {
            foreach (exploInfo exploInfo in ExploInfo[playerID])
            {
                Vector3 exploPosition = exploInfo.position;
                switch (exploInfo.type)
                {
                    case 0:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 800 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform, exploInfo.Caliber);
                            break;
                        }
                    case 1:
                        {
                            GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, exploPosition, Quaternion.identity);
                            pierceEffect.transform.localScale = exploInfo.Caliber / 400f * Vector3.one;
                            Destroy(pierceEffect, 1);
                            AddPierceSound(pierceEffect.transform, exploInfo.Caliber);
                            break;
                        }
                    case 2:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 400 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform, exploInfo.Caliber);
                            break;
                        }
                    case 3:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploWithSmoke, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 800f * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform, exploInfo.Caliber);
                            break;
                        }
                    case 4:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploSmall, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 800 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform, exploInfo.Caliber);
                            break;
                        }
                    default:
                        break;
                }
            }
            WeaponMsgReceiver.Instance.ExploInfo[playerID].Clear();
        }
        public void PlayWaterHitOnClient(int playerID)
        {
            foreach (var WaterhitInfo in waterHitInfo[playerID])
            {
                GameObject waterhit;
                if (WaterhitInfo.Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, WaterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = WaterhitInfo.Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else if (WaterhitInfo.Caliber >= 100)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, WaterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = WaterhitInfo.Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit3, WaterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = WaterhitInfo.Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }

                AddWaterHitSound(waterhit.transform, WaterhitInfo.Caliber);

            }
            waterHitInfo[playerID].Clear();
        }

        public void Update()
        {
            if (StatMaster.isClient)
            {
                for (int i = 0; i < 16; i++)
                {
                    PlayExploOnClient(i);
                    PlayWaterHitOnClient(i);
                }
            }
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
        Stack<int> damagedBallon = new Stack<int> ();

        public int timer = 0;
        public bool timerOn;

        public bool exploded = false;
        public bool spotted = false;

        public float timeFaze = 20f;
        float currentTime = 0f;

        public bool AA
        {
            get { return timeFaze != 20f; }
        }


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
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
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
            try
            {
                try
                {
                    if (hit.collider.name == "TurrentVis")
                    {
                        return true;
                    }
                }
                catch { }
                try
                {
                    if (!(hit.collider.transform.parent.parent.name == "Engine"))
                    {

                        if (!hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>())
                        {
                            Debug.Log("not a block");
                            return false;
                        }
                        else
                        {
                            if (pericedBlock.Contains(hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
                            {
                                return true;
                            }
                        }

                    }
                    else
                    {
                        if (pericedBlock.Contains(hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    return false;
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
                    if (UnityEngine.Random.value < (angle - 70) / 20)
                    {
                        return false;
                    }
                    angle -= 3;
                }

                float Thickness;
                if (hit.collider.attachedRigidbody.GetComponent<WoodenArmour>())
                {
                    Thickness = hit.collider.attachedRigidbody.GetComponent<WoodenArmour>().thickness;
                }
                else if (hit.collider.attachedRigidbody.GetComponent<DefaultArmour>() || hit.collider.transform.GetComponent<DefaultArmour>())
                {
                    Thickness = 20;
                }
                else if (hit.collider.attachedRigidbody.GetComponent<CannonWell>())
                {
                    Vector3 CylinderUp = hit.collider.transform.parent.GetComponent<CannonWell>().WellVis.transform.up;

                    //Debug.Log(CylinderUp);
                    //Debug.Log(hit.normal);
                    if (hit.collider.name == "TurrentVis")
                    {
                        Thickness = 1f;
                    }
                    else if ((hit.normal - CylinderUp).magnitude < 0.01f || (hit.normal - CylinderUp).magnitude > 1.99f)
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
                    pericedBlock.Push(hit.collider.attachedRigidbody.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());

                    try
                    {
                        Aircraft a = hit.collider.attachedRigidbody.GetComponent<Aircraft>();
                        float hittedPossi = 0.7f;
                        if (a.status == Status.InHangar || a.status == Status.OnBoard && UnityEngine.Random.value > hittedPossi)
                        {
                            a.BeginExplo(false);
                        }
                    }
                    catch { }


                    if (hit.collider.attachedRigidbody.name != "SpinningBlock")    // add waterIn behaviour
                    {
                        
                        GameObject waterinhole = new GameObject("waterInHole");
                        waterinhole.transform.SetParent(hit.collider.attachedRigidbody.transform);
                        waterinhole.transform.localPosition = Vector3.zero;
                        waterinhole.transform.localRotation = Quaternion.identity;
                        waterinhole.transform.localScale = Vector3.one;

                        WaterInHole WH = waterinhole.AddComponent<WaterInHole>();
                        WH.hittedCaliber = Caliber;
                        WH.position = hit.collider.attachedRigidbody.transform.InverseTransformPoint(hit.point);
                        if (pericedBlock.Count == 1)
                        {
                            WH.holeType = 0;
                        }
                        else
                        {
                            WH.holeType = 1;
                        }
                    }


                    if (Caliber >= 100)
                    {
                        string hittedname = hit.collider.attachedRigidbody.name;
                        if (hittedname == "DoubleWoodenBlock" || hittedname == "SingleWoodenBlock" || hittedname == "Log" || hittedname == "SpinningBlock")
                        {   // add hole projector
                            GameObject piercedhole = new GameObject("PiercedHole");
                            piercedhole.transform.SetParent(hit.collider.attachedRigidbody.transform);
                            piercedhole.transform.localPosition = Vector3.zero;
                            piercedhole.transform.localRotation = Quaternion.identity;
                            piercedhole.transform.localScale = Vector3.one;

                            PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                            PH.hittedCaliber = Caliber;
                            PH.position = hit.collider.attachedRigidbody.transform.InverseTransformPoint(hit.point);
                            PH.forward = myRigid.velocity.normalized;

                            if (StatMaster.isMP)
                            {
                                ModNetworking.SendToAll(WeaponMsgReceiver.HitHoleMsg.CreateMessage((int)hit.collider.attachedRigidbody.transform.GetComponent<BlockBehaviour>().ParentMachine.PlayerID,
                                                                                                    hit.collider.attachedRigidbody.transform.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode(),
                                                                                                    Caliber, PH.position, PH.forward, 0));
                            }
                        }
                    }
                    return true;
                }
            }
            catch { return false; }
            


        }
        public void PlayGunShot()
        {
            GameObject gunsmoke;
            if (Caliber>=283)
            {
                gunsmoke = (GameObject)Instantiate(AssetManager.Instance.GunSmoke.gunsmoke1, transform.position, transform.rotation);
                gunsmoke.transform.localScale = Caliber / 200 * Vector3.one;
                Destroy(gunsmoke, 3);
            }
            else if (Caliber >= 76)
            {
                gunsmoke = (GameObject)Instantiate(AssetManager.Instance.GunSmoke.gunsmoke2, transform.position, transform.rotation);
                gunsmoke.transform.localScale = Caliber / 200 * Vector3.one;
                Destroy(gunsmoke, 3);
            }
            else
            {
                gunsmoke = (GameObject)Instantiate(AssetManager.Instance.GunSmoke.gunsmoke3, transform.position, transform.rotation);
                gunsmoke.transform.localScale = Caliber / 200 * Vector3.one;
                Destroy(gunsmoke, 2);
            }
            AddFireSound(gunsmoke.transform);
        }
        public void HurtBalloon(GameObject balloon, Vector3 pos, bool AP)
        {
            BalloonLife life = balloon.GetComponent<BalloonLife>();
            if (life)
            {
                life.CutLife(Caliber, AP);
                if (!life.isAlive())
                {
                    BreakBalloon(pos);
                }
            }
            else
            {
                BreakBalloon(pos);
            }
        }
        public void BreakBalloon(Vector3 position)
        {
            GameObject damager = new GameObject("damager");
            damager.transform.position = position;
            damager.AddComponent<BoxCollider>().size = new Vector3(0.01f,0.01f,0.01f);
            
            damager.AddComponent<Rigidbody>().velocity = new Vector3(0, 20, 0);
            Destroy(damager, 0.01f);
        }
        public void CannonDetectCollisionHost(bool AP = true)
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
                    if (hit.collider.isTrigger && hit.collider.name != "AmmoVis" && hit.collider.name != "WellArmourVis" && hit.collider.name != "TurrentVis")
                    {
                        continue;
                    }
                    timerOn = true;

                    // add force
                    try
                    {
                        if (!(hit.collider.transform.parent.name == "Balloon" || hit.collider.transform.parent.name == "SqrBalloon"))
                        {
                            hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 30f, ForceMode.Force);
                        }
                    }
                    catch { }

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
                        if ((hit.collider.transform.parent.name == "Balloon" || hit.collider.transform.parent.name == "SqrBalloon")
                            && !damagedBallon.Contains(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
                        {
                            damagedBallon.Push(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());
                            HurtBalloon(hit.collider.transform.parent.gameObject, hit.collider.transform.position, AP);
                            //BreakBalloon(hit.collider.transform.position);
                        }

                        // well or ammo damage
                        if (hit.collider.transform.parent.name == "SpinningBlock")
                        {
                            CannonWell CW = hit.collider.transform.parent.GetComponent<CannonWell>();
                            if (CW.totalCaliber != 0)
                            {
                                float WellExploProb = Caliber / CW.myCaliber * CW.gunNum * 0.08f / Mathf.Pow(CW.TurretSize.Value, 1);
                                float WellPalsyProb = 2 * WellExploProb;
                                float AmmoExploProb = 3 * WellExploProb;
                                if (hit.collider.name == "WellArmourVis")
                                {
                                    //Debug.Log(WellPalsyProb);
                                    if (UnityEngine.Random.value < WellPalsyProb)
                                    {
                                        CW.Wellpalsy = true;
                                    }
                                    if (UnityEngine.Random.value < WellExploProb * CW.myCaliber/500)
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
                                if (hit.collider.name == "TurrentVis")
                                {
                                    if (UnityEngine.Random.value < AmmoExploProb * 2)
                                    {
                                        CW.TurrentPalsy = true;
                                    }
                                }
                            }
                        }

                        if (hit.collider.transform.parent.parent.name == "Engine")
                        {
                            hit.collider.transform.parent.parent.GetComponent<Engine>().CannonDamage(Caliber);
                        }

                        
                        continue;
                    }
                    if (!exploded)
                    {
                        PlayExploHit(hit, AP);
                        Destroy(gameObject);
                    }
                    
                    break;
                }
            }
        }
        public void CannonDetectWaterHost()
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
            }
            if (transform.position.y < 20f && pericedBlock.Count == 0)
            {
                if (!hasHitWater)
                {
                    penetration *= 0.7f;
                }
                myRigid.velocity = new Vector3(myRigid.velocity.x, myRigid.velocity.y / (1+Mathf.Sqrt(Caliber)/40), myRigid.velocity.z);
                myRigid.AddForce(myRigid.velocity * Constants.BulletUnderWaterForce - Vector3.up * Constants.BulletUnderWaterDrag);
                penetration *= 0.8f + Mathf.Clamp(Mathf.Sqrt(Caliber) / 200,0,0.15f);
                if (myRigid.velocity.magnitude <= 5f)
                {
                    Destroy(gameObject);
                }
            }
            if (transform.position.y < 20f && !hasHitWater && myRigid.velocity.y< 0 && pericedBlock.Count == 0)
            {
                
                myRigid.drag = 11f/Mathf.Sqrt(Caliber)*20;
                GameObject waterhit;
                if (Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else if (Caliber >= 100)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit3, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                    waterhit.transform.localScale = Caliber / 381 * Vector3.one;
                    Destroy(waterhit, 3);
                }



                AddWaterHitSound(waterhit.transform);
                hasHitWater = true;
                ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Caliber));
            }
        }
        public void CannonDetectWaterClient()
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

        }
        private void PlayExploHit(RaycastHit hit, bool AP = true)
        {
            try
            {
                GameObject explo;
                if (Caliber < 100)
                {
                    explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploSmall, transform.position, Quaternion.identity);
                }
                else
                {
                    explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
                }
                explo.name = "Explo Hit";
                explo.SetActive(true);
                explo.transform.localScale = Caliber / 800 * (AP ? 1 : 2) * Vector3.one;
                Destroy(explo, 3);
                AddExploSound(explo.transform);

                exploded = true;

                //send to client
                if (Caliber < 100)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, 4));
                }
                else
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, AP ? 0 : 2));
                }

                try
                {
                    hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 10, ForceMode.Force);
                }
                catch { }

                ExploDestroyBalloon(hit.point, AP);
            }
            catch { }
            if (transform.FindChild("CannonVis"))
            {
                transform.FindChild("CannonVis").gameObject.SetActive(false);
            }
            
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());

            
        }
        private void PlayExploInAir(bool AP = true)
        {
            GameObject explo;
            if (Caliber < 100)
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploSmall, transform.position, Quaternion.identity);
            }
            else
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity); 
            }
            explo.name = "Explo Air";
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 800 * (AP?1:2) * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            if (!AA)
            {
                if (Caliber < 100)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, 4));
                }
                else
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, AP ? 0 : 2));
                }
            }

            ExploDestroyBalloon(transform.position, AP);
            if (transform.FindChild("CannonVis"))
            {
                transform.FindChild("CannonVis").gameObject.SetActive(false);
            }
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());
        }
        private void PlayExploForAircraft()
        {
            GameObject explo;
            if (Caliber < 100)
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploSmall, transform.position, Quaternion.identity);
            }
            else
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
            }
            explo.name = "Explo Air";
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 600 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            ExploDestroyAircraft(transform.position);
            if (transform.FindChild("CannonVis"))
            {
                transform.FindChild("CannonVis").gameObject.SetActive(false);
            }
            Destroy(gameObject.GetComponent<Rigidbody>());
            Destroy(gameObject.GetComponent<BulletBehaviour>());
        }
        public void HEDetectWaterHost()
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
            }
            if (transform.position.y < 20f && !hasHitWater && myRigid.velocity.y < 0)
            {
                
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
                PlayExploInAir(false);
                Destroy(gameObject,0.1f);
                ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Caliber));
            }
        }
        private void ExploDestroyBalloon(Vector3 pos, bool AP = true)
        {
            float exploPenetration = Caliber / 20f * (AP ? 1f : 1.5f);
            try
            {
                //Debug.Log(armourGuid);
                Collider[] ExploCol = Physics.OverlapSphere(transform.position, Mathf.Sqrt(Caliber) / (AP?8f:5f));
                foreach (Collider hitedCollider in ExploCol)
                {
                    try
                    {
                        try
                        {
                            Aircraft a = hitedCollider.attachedRigidbody.GetComponent<Aircraft>();
                            if (a)
                            {
                                float ArmourBetween = 0;
                                Ray Ray = new Ray(pos, hitedCollider.transform.position - pos);
                                RaycastHit[] hitList = Physics.RaycastAll(Ray, (hitedCollider.transform.position - pos).magnitude);
                                foreach (RaycastHit raycastHit in hitList)
                                {
                                    try
                                    {
                                        //Debug.Log(raycastHit.rigidbody.name);
                                        if (!pericedBlock.Contains(raycastHit.collider.attachedRigidbody.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode())
                                            && raycastHit.collider.attachedRigidbody.GetComponent<WoodenArmour>())
                                        {
                                            //Debug.Log(raycastHit.collider.transform.parent.GetComponent<WoodenArmour>().thickness);
                                            ArmourBetween += raycastHit.collider.transform.parent.GetComponent<WoodenArmour>().thickness;
                                        }
                                    }
                                    catch { }

                                }
                                //Debug.Log(ArmourBetween + " VS "+exploPenetration);
                                if (ArmourBetween > exploPenetration)
                                {
                                }
                                else
                                {
                                    float hittedPossi = a.hasLoad ? 0.9f : 0.97f;
                                    if (a.status == Status.InHangar || a.status == Status.OnBoard && UnityEngine.Random.value > hittedPossi)
                                    {
                                        a.BeginExplo(false);
                                    }
                                }

                            }
                        }
                        catch { }
                        //Debug.Log(hitedCollider.transform.parent.name);
                        if ((hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon")
                            && damagedBallon.Count == 0)
                        {
                            float ArmourBetween = 0;
                            Ray Ray = new Ray(pos, hitedCollider.transform.position - pos);
                            RaycastHit[] hitList = Physics.RaycastAll(Ray, (hitedCollider.transform.position - pos).magnitude);
                            foreach (RaycastHit raycastHit in hitList)
                            {
                                //Debug.Log(raycastHit.rigidbody.name);
                                if (!pericedBlock.Contains(raycastHit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode())
                                    && raycastHit.collider.transform.parent.GetComponent<WoodenArmour>())
                                {
                                    //Debug.Log(raycastHit.collider.transform.parent.GetComponent<WoodenArmour>().thickness);
                                    ArmourBetween += raycastHit.collider.transform.parent.GetComponent<WoodenArmour>().thickness;
                                }
                            }
                            //Debug.Log(ArmourBetween + " VS "+exploPenetration);
                            if (ArmourBetween > exploPenetration)
                            {
                                continue;
                            }
                            else
                            {
                                damagedBallon.Push(hitedCollider.transform.parent.gameObject.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());
                                HurtBalloon(hitedCollider.transform.parent.gameObject, hitedCollider.transform.position, AP);
                                //BreakBalloon(hitedCollider.transform.position);
                            }

                        }
                        else if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                        {
                            if (!(hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon"))
                            {
                                hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce((AP ? 2f : 3f) * Caliber, pos, Mathf.Sqrt(Caliber) / (AP ? 8f : 5f));
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        private void ExploDestroyAircraft(Vector3 pos)
        {
            if (StatMaster.isClient)
            {
                return;
            }
            //Debug.Log(armourGuid);
            float radius = Caliber / 5f;
            Collider[] ExploCol = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider hitedCollider in ExploCol)
            {
                try
                {
                    Aircraft a = hitedCollider.attachedRigidbody.GetComponent<Aircraft>();
                    if (a)
                    {
                        float dist = Vector3.Distance(pos, a.transform.position);
                        float AAForce = 1;
                        try
                        {
                            AAForce = ModController.Instance.AAForce;
                        }
                        catch { Debug.Log("AAforce get wrong"); }
                        a.ReduceHP((int)(Caliber * Caliber / (dist * 750f)));
                        a.IncreaseAnxiety((Caliber / (dist *10f)));
                        a.StartCoroutine(a.DisturbedCoroutine(5, radius/dist));
                        a.myRigid.AddExplosionForce(Caliber, pos, radius);
                    }
                }
                catch { }
            }
        }
        public void Start()
        {
            name = "Bullet";
            myRigid = gameObject.GetComponent<Rigidbody>();
            if (CannonType == 0)
            {
                penetration = Caliber * 2;
            }
            else if (CannonType == 1)
            {
                penetration = Caliber * 0.5f;
            }
            
            decay = Mathf.Pow(0.5f, 1 / (Mathf.Sqrt(Caliber + 100) * 30f));
            
        }

        public void FixedUpdate()
        {
            
            if (fire)
            {
                if (!thrustOn)
                {
                    myRigid.velocity = transform.forward * MathTool.GetInitialVel(Caliber, AA);
                    thrustOn = true;
                    PlayGunShot();
                } // add initial speed
                transform.rotation = Quaternion.LookRotation(myRigid.velocity);
                myRigid.AddForce(randomForce * 2f);
                if (!AA)
                {
                    if (!StatMaster.isClient)
                    {
                        if (CannonType == 0) // for AP
                        {
                            CannonDetectCollisionHost();
                            if (ModController.Instance.showSea)
                            {
                                CannonDetectWaterHost();
                            }
                            if (timerOn)
                            {
                                timer++;
                            }
                            if (timer > Constants.BulletAPTimer && !exploded)
                            {
                                PlayExploInAir();
                            }
                        }
                        else
                        {
                            CannonDetectCollisionHost(false);
                            if (pericedBlock.Count == 0 && ModController.Instance.showSea)
                            {
                                HEDetectWaterHost();
                            }
                            if (timerOn)
                            {
                                timer++;
                            }
                            if (timer > Constants.BulletHETimer && !exploded)
                            {
                                PlayExploInAir(false);
                            }

                        }

                    }
                    else
                    {
                        CannonDetectWaterClient();
                    }
                }
            }
            penetration *= decay;
            if (transform.position.y < -1)
            {
                Destroy(this.gameObject);
            }

            if (timeFaze < currentTime)
            {
                PlayExploForAircraft();
            }

            currentTime += Time.fixedDeltaTime;
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
        public MMenu DefaultCannon;
        public bool triggeredByGunner;
        public float timeFaze = 20f;

        public bool isSelf
        {
            get
            {
                return StatMaster.isMP? myPlayerID == PlayerData.localPlayer.networkId: true;
            }
        }

        public int CannonType;
        public int NextCannonType;

        GameObject CannonPrefab;

        Transform VisTransform;

        int muzzleStage = 100;

        public float reloadTime;
        public float currentReloadTime = 0;
        public float reloadefficiency = 0;

        Texture ReloadHEOut;
        Texture ReloadHEIn;
        Texture ReloadAPOut;
        Texture ReloadAPIn;
        int iconSize = 30;

        //UGUI
        FollowerUI ReloadHEOutUI;
        FollowerUI ReloadHEInUI;
        FollowerUI ReloadAPOutUI;
        FollowerUI ReloadAPInUI;

        public Vector3 GetRandomForce()
        {
            Vector3 randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 6 / Mathf.Sqrt(Caliber.Value);
            randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f, 0) * 6 / Mathf.Sqrt(Caliber.Value);
            return randomForce * Mathf.Pow(UnityEngine.Random.value, 2);
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
                if (NextCannonType == 0)
                {
                    ReloadAPOutUI.show = true;
                    ReloadHEOutUI.show = false;
                }
                else
                {
                    ReloadHEOutUI.show = true;
                    ReloadAPOutUI.show = false;
                }
                if (CannonType == 0)
                {
                    int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                    ReloadAPInUI.size = currIconSize;
                    ReloadAPInUI.show = true;
                    ReloadHEInUI.show = false;
                }
                else
                {
                    int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                    ReloadHEInUI.size = currIconSize;
                    ReloadHEInUI.show = true;
                    ReloadAPInUI.show = false;
                }
            }
            
        }

        public float GetFCPitchPara()
        {
            if (StatMaster.isClient)
            {
                //float angle = Vector3.Angle(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid], Vector3.up);
                //return (Mathf.Clamp(90 - angle, 0, 45));
                float angle = Vector3.Angle(transform.forward, Vector3.up);
                return (Mathf.Clamp(90 - angle, -89, 89));
            }
            else
            {
                float angle = Vector3.Angle(transform.forward, Vector3.up);
                return (Mathf.Clamp(90 - angle, -89, 89));
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
            Transform PrefabParent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            string PrefabName = "NavalCannon [" + myPlayerID + "](" + Caliber.Value + ")";
            if (PrefabParent.Find(PrefabName))
            {
                CannonPrefab = PrefabParent.Find(PrefabName).gameObject;
            }
            else
            {
                CannonPrefab = new GameObject(PrefabName);
                CannonPrefab.transform.parent = PrefabParent;
                BulletBehaviour BBtmp = CannonPrefab.AddComponent<BulletBehaviour>();
                BBtmp.Caliber = Caliber.Value;
                BBtmp.myPlayerID = myPlayerID;
                Rigidbody RBtmp = CannonPrefab.AddComponent<Rigidbody>();
                RBtmp.interpolation = RigidbodyInterpolation.Extrapolate;
                RBtmp.mass = 0.2f;
                RBtmp.drag = Caliber.Value > 100 ? 5000f / (Caliber.Value * Caliber.Value) : 1 - Caliber.Value / 200f;
                RBtmp.useGravity = false;
                if (Caliber.Value >= 100)
                {
                    GameObject CannonVis = new GameObject("CannonVis");
                    CannonVis.transform.SetParent(CannonPrefab.transform);
                    CannonVis.transform.localPosition = Vector3.zero;
                    CannonVis.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    CannonVis.transform.localScale = Vector3.one * Caliber.Value / 120;
                    MeshFilter MFtmp = CannonVis.AddComponent<MeshFilter>();
                    MFtmp.sharedMesh = ModResource.GetMesh("Cannon Mesh").Mesh;
                    MeshRenderer MRtmp = CannonVis.AddComponent<MeshRenderer>();
                    MRtmp.material.mainTexture = ModResource.GetTexture("Cannon Texture").Texture;
                }

                GravityModifier gm = CannonPrefab.AddComponent<GravityModifier>();
                gm.gravityScale = Constants.BulletGravity/Constants.Gravity;
                CannonPrefab.SetActive(false);
            }

            
        }
        public bool DetectSelfHost()
        {
            bool res = false;
            Ray GunRay = new Ray(transform.position + 3 * transform.forward * transform.localScale.z, transform.forward);
            RaycastHit[] hitList = Physics.RaycastAll(GunRay, 20f);
            foreach (var hit in hitList)
            {
                try
                {
                    if (hit.collider.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID == myPlayerID)
                    {
                        res = true;
                        break;
                    }
                }
                catch { }
            }
            return res;
        }

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            FireKey = AddKey(LanguageManager.Instance.CurrentLanguage.GunFire, "Fire", KeyCode.C);
            SwitchKey = AddKey(LanguageManager.Instance.CurrentLanguage.SwitchAPHE, "SwitchCannonType", KeyCode.R);
            Caliber = AddSlider(LanguageManager.Instance.CurrentLanguage.Caliber, "Caliber", 406, 10, 510);
            TrackOn = AddToggle(LanguageManager.Instance.CurrentLanguage.AsTrackCannon, "TrackCannon", false);
            FireControl = AddToggle(LanguageManager.Instance.CurrentLanguage.FireControl, "FireControl", false);
            GunGroup = AddText(LanguageManager.Instance.CurrentLanguage.Group, "GunGroup", "g0");
            DefaultCannon = AddMenu("Default Cannon", 0, LanguageManager.Instance.CurrentLanguage.GunType);
            ReloadHEOut = ModResource.GetTexture("ReloadHEOut Texture").Texture;
            ReloadHEIn = ModResource.GetTexture("ReloadHEIn Texture").Texture;
            ReloadAPOut = ModResource.GetTexture("ReloadAPOut Texture").Texture;
            ReloadAPIn = ModResource.GetTexture("ReloadAPIn Texture").Texture;
            myseed = (int)(UnityEngine.Random.value * 10);
        }
        public void Start()
        {
            name = "Gun";
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
            reloadTime = Caliber.Value >= 100 ? 0.4f * Mathf.Sqrt(Caliber.Value) - 3 : 
                                                (2.8f * Mathf.Sin(Mathf.PI/160f * (Mathf.Pow(Caliber.Value,1.5f)/10f - 80))+2.9f)/4f;
            currentReloadTime = reloadTime;
            CannonType = DefaultCannon.Value;
            NextCannonType = CannonType;
            Grouper.Instance.AddGun(myPlayerID, GunGroup.Value, myGuid, gameObject);
            if ((StatMaster.isMP && !StatMaster.isClient) || !StatMaster.isMP)
            {
                GunnerDataBase.Instance.AddGun(myPlayerID, myGuid, BlockBehaviour);
            }
            
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.AddGun(myPlayerID, Caliber.Value, myGuid, gameObject);
            }
            try
            {
                if (StatMaster.isClient)
                {
                    WeaponMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new WeaponMsgReceiver.firePara(false,Vector3.zero,Vector3.zero,Vector3.zero, (float)20));
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

            // add block UI
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
            if ((StatMaster.isMP && !StatMaster.isClient) || !StatMaster.isMP)
            {
                GunnerDataBase.Instance.RemoveGun(myPlayerID, myGuid);
            }
            Grouper.Instance.AddGun(myPlayerID, "null", myGuid, gameObject);
            if (!FireControl.isDefaultValue)
            {
                FireControlManager.Instance.RemoveGun(myPlayerID, myGuid);
            }
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
                currentReloadTime += Time.deltaTime * reloadefficiency;
                if (ModController.Instance.state == myseed)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ReloadMsg.CreateMessage(  myPlayerID, myGuid, currentReloadTime, 
                                                                                        (CannonType == 1)?true:false, 
                                                                                        (NextCannonType == 1)?true:false, 0));
                }
                return;
            }

            if (FireKey.IsPressed || (triggeredByGunner && !DetectSelfHost()) )
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward*5);
                Vector3 randomForce = GetRandomForce();

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;

                if (timeFaze != 20)
                {
                    float randomFaze = timeFaze + 0.05f - 0.1f * UnityEngine.Random.value;
                    randomFaze = Mathf.Clamp(randomFaze, 0.1f, 19f);
                    timeFaze = randomFaze;
                }
                
                Cannon.GetComponent<BulletBehaviour>().timeFaze = timeFaze;
                Destroy(Cannon, timeFaze + 2f);

                CannonType = NextCannonType;
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward, Vector3.zero, timeFaze));
                }
                
                if (!TrackOn.isDefaultValue)
                {
                    try
                    {
                        CannonTrackManager.Instance.AddTrackedCannon(myPlayerID, Cannon);
                    }
                    catch { }
                }

                if (triggeredByGunner)
                {
                    triggeredByGunner = false;
                    timeFaze = 20;
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
                CannonType = WeaponMsgReceiver.Instance.CannonType[myPlayerID][myGuid] ? 1:0;
                NextCannonType = WeaponMsgReceiver.Instance.NextCannonType[myPlayerID][myGuid] ? 1:0;
            }
            if (currentReloadTime < reloadTime)
            {
                currentReloadTime += Time.deltaTime * reloadefficiency;
            }

            if (WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, 
                                                            Quaternion.LookRotation(WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].forward, Vector3.up));
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;
                Cannon.GetComponent<BulletBehaviour>().timeFaze = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].time;
                Destroy(Cannon, timeFaze + 1f);

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
                timeFaze = 20;
                currentReloadTime = 0;
                muzzleStage = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward * 5);
                Vector3 randomForce = GetRandomForce();

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Cannon.GetComponent<BulletBehaviour>().CannonType = CannonType;
                Cannon.GetComponent<BulletBehaviour>().timeFaze = 20f;
                Destroy(Cannon, timeFaze + 2f);
                CannonType = NextCannonType;

                ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward, Vector3.zero, timeFaze));

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
        public void MySimulateFixedUpdateClient()
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

        }
    }
}
