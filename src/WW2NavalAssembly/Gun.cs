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

        public static MessageType FireMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3);// playerID, guid, randomForce
        public static MessageType ExploMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Vector3);//PlayerID, position

        public class firePara
        {
            public bool fire = false;
            public Vector3 fireForce = Vector3.zero;
            public firePara(bool fire, Vector3 fireForce)
            {
                this.fire = fire;
                this.fireForce = fireForce;
            }
        }

        public Dictionary<int,firePara>[] Fire = new Dictionary<int,firePara>[16];
        public Queue<Vector3>[] exploPosition = new Queue<Vector3>[16];

        public GunMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                Fire[i] = new Dictionary<int, firePara>();
                exploPosition[i] = new Queue<Vector3>();
            }
        }

        public void exploMsgReceiver(Message msg)
        {
            exploPosition[(int)msg.GetData(0)].Enqueue((Vector3)msg.GetData(1));
        }
        public void fireKeyMsgReceiver(Message msg)
        {
            Fire[(int)msg.GetData(0)][(int)msg.GetData(1)] = new firePara(true,(Vector3)msg.GetData(2));
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
        public void Start()
        {
            myRigid = gameObject.GetComponent<Rigidbody>();
            
        }
        public void DetectCollisionHost()
        {
            Ray CannonRay = new Ray(transform.position, myRigid.velocity);
            RaycastHit hit;
            if (Physics.Raycast(CannonRay, out hit, myRigid.velocity.magnitude * Time.fixedDeltaTime))
            {
                if (hit.collider.isTrigger)
                {
                    return;
                }
                GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, hit.point, Quaternion.identity);
                explo.SetActive(true);
                explo.transform.localScale = Caliber / 800 * Vector3.one;
                Destroy(explo, 1);

                //send to client
                ModNetworking.SendToAll(GunMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point));

                try
                {
                    hit.collider.attachedRigidbody.AddForce(transform.forward * 200f * Caliber, ForceMode.Force);
                }
                catch
                {
                }
                Collider[] ExploCol = Physics.OverlapSphere(hit.point, Caliber / 220);
                foreach (Collider hits in ExploCol)
                {
                    if (hits.GetComponent<Rigidbody>())
                    {
                        hits.GetComponent<Rigidbody>().AddExplosionForce(200f * Caliber, hit.point, 5f);
                    }
                }
                gameObject.SetActive(false);
            }
        }
        public void DetectCollisionClient()
        {
            foreach(Vector3 exploPosition in GunMsgReceiver.Instance.exploPosition[myPlayerID])
            {
                GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, exploPosition, Quaternion.identity);
                explo.SetActive(true);
                explo.transform.localScale = Caliber / 800 * Vector3.one;
                Destroy(explo, 1);
            }
            GunMsgReceiver.Instance.exploPosition[myPlayerID].Clear();
        }
        public void FixedUpdate()
        {
            if (fire)
            {
                if (!thrustOn)
                {
                    myRigid.velocity = transform.forward * Mathf.Sqrt(Caliber+100)*8.5f;
                    thrustOn = true;
                } // add initial speed
                transform.rotation = Quaternion.LookRotation(myRigid.velocity);
                myRigid.AddForce(randomForce);

                if (!StatMaster.isClient)
                {
                    DetectCollisionHost();
                }
                else
                {
                    DetectCollisionClient();
                }
                
            }
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
            RBtmp.mass = 0.1f;
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
            InitCannon();
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            try
            {
                if (StatMaster.isClient)
                {
                    GunMsgReceiver.Instance.Fire[myPlayerID].Add(myGuid, new GunMsgReceiver.firePara(false,Vector3.zero));
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
                Vector3 randomForce = new Vector3(UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f, UnityEngine.Random.value-0.5f) * 1 / Mathf.Sqrt(Caliber.Value);
                randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f,0) * 2 / Mathf.Sqrt(Caliber.Value);

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward, transform.rotation);
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = randomForce;
                Destroy(Cannon, 10);

                ModNetworking.SendToAll(GunMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce));
            }
        }
        public override void SimulateUpdateClient()
        {
            if (GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, transform.position + 3 * transform.forward, transform.rotation);
                Cannon.SetActive(true);
                Cannon.GetComponent<BulletBehaviour>().fire = true;
                Cannon.GetComponent<BulletBehaviour>().randomForce = GunMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                Destroy(Cannon, 10);
            }
        }
    }
}
