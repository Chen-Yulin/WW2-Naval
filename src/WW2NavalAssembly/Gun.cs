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
    public class GunMsgReceiver : SingleInstance<GunMsgReceiver>
    {
        public override string Name { get; } = "Gun Msg Receiver";

        public static MessageType FireMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Vector3);// playerID, guid, randomForce, forward
        public static MessageType ExploMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single, DataType.Integer);//PlayerID, position, Caliber
        public static MessageType WaterHitMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3, DataType.Single);//PlayerID, position

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

        public Dictionary<int,firePara>[] Fire = new Dictionary<int,firePara>[16];
        public Queue<exploInfo>[] ExploInfo = new Queue<exploInfo>[16];
        public Queue<waterhitInfo>[] waterHitInfo = new Queue<waterhitInfo>[16];

        public GunMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                Fire[i] = new Dictionary<int, firePara>();
                ExploInfo[i] = new Queue<exploInfo>();
                waterHitInfo[i] = new Queue<waterhitInfo>();
            }
        }

        public void exploMsgReceiver(Message msg)
        {
            ExploInfo[(int)msg.GetData(0)].Enqueue(new exploInfo((Vector3)msg.GetData(1), (float)msg.GetData(2), (int)msg.GetData(3)));
        }
        public void fireKeyMsgReceiver(Message msg)
        {
            Fire[(int)msg.GetData(0)][(int)msg.GetData(1)] = new firePara(true,(Vector3)msg.GetData(2), (Vector3)msg.GetData(3));
        }
        public void waterHitMsgReceiver(Message msg)
        {
            waterHitInfo[(int)msg.GetData(0)].Enqueue(new waterhitInfo((Vector3)msg.GetData(1), (float)msg.GetData(2)));
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

        public float Caliber;
        public bool fire = false;
        bool thrustOn = false;
        Rigidbody myRigid;
        public Vector3 randomForce;
        public bool hasHitWater = false;

        public float penetration;
        public float decay;


        Stack<int> pericedBlock = new Stack<int> ();

        public int timer = 0;
        public bool timerOn;

        public bool exploded = false;

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
            else
            {
                return true;
            }

            if (Thickness/Mathf.Cos(angle*Mathf.PI/180) > penetration)
            {
                return false;
            }
            else
            {
                float eqThick = Thickness / Mathf.Cos(angle * Mathf.PI / 180);
                myRigid.velocity *= 1 - eqThick * 0.8f / penetration;
                penetration -= eqThick;
                pericedBlock.Push(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());

                if (pericedBlock.Count == 1)
                {
                    GameObject hole = new GameObject("hole");
                    hole.transform.SetParent(hit.collider.transform.parent);
                    hole.transform.localPosition = Vector3.zero;
                    hole.transform.localRotation = Quaternion.identity;
                    hole.transform.localScale = Vector3.one;

                    BulletHole BH = hole.AddComponent<BulletHole>();
                    BH.hittedCaliber = Caliber;
                    BH.normal = hit.normal;
                    BH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
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
        public void DetectCollisionHost()
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
                    timerOn = true;

                    // pericing

                    if (Perice(hit))
                    {
                        GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, hit.point, Quaternion.identity);
                        pierceEffect.transform.localScale = Caliber / 400 * Vector3.one;
                        Destroy(pierceEffect, 1);
                        AddPierceSound(pierceEffect.transform);
                        ModNetworking.SendToAll(GunMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Caliber, 1));

                        if (hit.collider.transform.parent.name == "Balloon" || hit.collider.transform.parent.name == "SqrBalloon")
                        {
                            BreakBallon(hit.collider.transform.position);
                        }

                        try
                        {
                            hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 4, ForceMode.Force);
                        }
                        catch { }
                        continue;
                    }
                    if (!exploded)
                    {
                        PlayExploHit(hit);
                    }
                    
                    break;
                }
            }
        }
        public void DetectCollisionClient()
        {
            foreach (GunMsgReceiver.exploInfo exploInfo in GunMsgReceiver.Instance.ExploInfo[myPlayerID])
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
            GunMsgReceiver.Instance.ExploInfo[myPlayerID].Clear();
        }
        public void DetectWaterHost()
        {
            if (transform.position.y < 20f && pericedBlock.Count == 0 && Caliber >= 283)
            {
                myRigid.velocity = new Vector3(myRigid.velocity.x, myRigid.velocity.y / 1.5f, myRigid.velocity.z);
                myRigid.AddForce(myRigid.velocity * 0.8f - Vector3.up * 10);
                penetration *= 0.97f;
            }
            if (transform.position.y < 20f && !hasHitWater && myRigid.velocity.y<0)
            {
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
                ModNetworking.SendToAll(GunMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Caliber));
            }
        }
        public void DetectWaterClient()
        {
            foreach (GunMsgReceiver.waterhitInfo waterhitInfo in GunMsgReceiver.Instance.waterHitInfo[myPlayerID])
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
            GunMsgReceiver.Instance.waterHitInfo[myPlayerID].Clear();
        }
        public void PlayExploHit(RaycastHit hit)
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, hit.point - myRigid.velocity.normalized * Caliber / 800f, Quaternion.identity);
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            //send to client
            ModNetworking.SendToAll(GunMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Caliber, 0));

            try
            {
                hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Caliber / 3, ForceMode.Force);
            }
            catch { }

            Collider[] ExploCol = Physics.OverlapSphere(hit.point-myRigid.velocity.normalized* Caliber /800f, Caliber / 300f);
            foreach (Collider hitedCollider in ExploCol)
            {
                if (hitedCollider.GetComponent<Rigidbody>())
                {
                    hitedCollider.GetComponent<Rigidbody>().AddExplosionForce(50f * Caliber, hit.point, 5f);
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
        public void PlayExploInAir()
        {
            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
            explo.SetActive(true);
            explo.transform.localScale = Caliber / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            //send to client
            ModNetworking.SendToAll(GunMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Caliber, 0));

            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber / 300f);
            foreach (Collider hitedCollider in ExploCol)
            {
                if (hitedCollider.GetComponent<Rigidbody>())
                {
                    hitedCollider.GetComponent<Rigidbody>().AddExplosionForce(50f * Caliber, transform.position, 7f);
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
        public void Start()
        {
            myRigid = gameObject.GetComponent<Rigidbody>();
            penetration = Caliber * 2;
            decay = Mathf.Pow(0.5f, 1 / (Mathf.Sqrt(Caliber + 100) * 33f));
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
                    DetectCollisionHost();
                    DetectWaterHost();
                    if (timerOn)
                    {
                        timer++;
                    }
                    if (timer > 5f && !exploded)
                    {
                        PlayExploInAir();
                    }
                }
                else
                {
                    DetectCollisionClient();
                    DetectWaterClient();
                    
                }
                
            }

            penetration *= decay;
        }
        
    }
    public class Gun:BlockScript
    {
        public int myPlayerID;
        public int myGuid;

        public MKey FireKey;
        public MSlider Caliber;

        GameObject CannonPrefab;

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
            TRtmp.material.SetColor("_TintColor", Color.white-0.6f*Color.blue);

            TRtmp.enabled = true;
            TRtmp.time = 0.1f;


            CannonPrefab.SetActive(false);
        }

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            FireKey = AddKey("Fire", "Fire", KeyCode.C);
            Caliber = AddSlider("Caliber (mm)", "Caliber", 406, 100, 510);
        }
        public override void OnSimulateStart()
        {
            BlockBehaviour.blockJoint.breakForce = float.PositiveInfinity;
            InitCannon();
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            try
            {
                if (StatMaster.isClient)
                {
                    GunMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new GunMsgReceiver.firePara(false,Vector3.zero,Vector3.zero));
                }
            }
            catch { }
        }
        public override void OnSimulateStop()
        {
            DestroyImmediate(CannonPrefab);
            GunMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
        }
        public override void SimulateUpdateHost()
        {
            if (FireKey.IsPressed)
            {
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward*5);
                Vector3 randomForce = new Vector3(UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f) * 3 / Mathf.Sqrt(Caliber.Value);
                randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f,0) * 5 / Mathf.Sqrt(Caliber.Value);

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Destroy(Cannon, 10);

                ModNetworking.SendToAll(GunMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward));
            }
        }
        public override void SimulateUpdateClient()
        {
            if (GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward, 
                                                            Quaternion.LookRotation(GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].forward, Vector3.up));
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                Destroy(Cannon, 10);
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (FireKey.EmulationPressed())
            {
                gameObject.GetComponent<Rigidbody>().AddForce(-Caliber.Value * transform.forward * 5);
                Vector3 randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 3 / Mathf.Sqrt(Caliber.Value);
                randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f, 0) * 5 / Mathf.Sqrt(Caliber.Value);

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward * transform.localScale.z, transform.rotation);
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Destroy(Cannon, 10);

                ModNetworking.SendToAll(GunMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward));
            }
        }
    }
}
