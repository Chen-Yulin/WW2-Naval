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
    class TorpedoMsgReceiver : SingleInstance<TorpedoMsgReceiver>
    {
        public override string Name { get; } = "Torpedo Msg Receiver";

        public static MessageType TorpedoDataMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Vector3, DataType.Boolean);
        public static MessageType TorpedoGuidMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Integer);
        public class TorpedoData
        {
            public Vector3 position;
            public bool exploded = false;
            public bool updated = false;
            public TorpedoData(Vector3 position, bool exploded)
            {
                this.position = position;
                this.exploded = exploded;
                this.updated = true;
            }
            public TorpedoData()
            {
                updated = false;
            }
        }

        public Dictionary<int, TorpedoData>[] torpedoData = new Dictionary<int, TorpedoData>[16];
        public Dictionary<int, int>[] torpedoGuid = new Dictionary<int, int>[16];

        public TorpedoMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                torpedoData[i] = new Dictionary<int, TorpedoData>();
                torpedoGuid[i] = new Dictionary<int, int>();
            }
        }

        public void TorpedoDataMsgReceiver(Message msg)
        {
            if (torpedoData[(int)msg.GetData(0)].ContainsKey((int)msg.GetData(1)))
            {
                torpedoData[(int)msg.GetData(0)][(int)msg.GetData(1)] = new TorpedoData((Vector3)msg.GetData(2), (bool)msg.GetData(3));
            }
            else
            {
                torpedoData[(int)msg.GetData(0)].Add((int)msg.GetData(1), new TorpedoData((Vector3)msg.GetData(2), (bool)msg.GetData(3)));
            }
            
        }
        public void TorpedoGuidMsgReceiver(Message msg)
        {
            torpedoGuid[(int)msg.GetData(0)][(int)msg.GetData(1)] = (int)msg.GetData(2);
        }
        
    }

    class PropellerBehaviour : MonoBehaviour
    {
        public bool Direction;
        public Vector3 Speed = new Vector3(0,10,0);

        public void Update()
        {
            transform.localEulerAngles += Speed * (Direction ? 1 : -1);
        }
    }
    class TorpedoBehaviour : MonoBehaviour
    {
        public int myPlayerID;
        public int parentGuid;
        //public int myGuid;
        public int mySeed;

        public float Caliber;
        public bool fire = false;
        public float depth;
        public int mode;

        public Rigidbody myRigid;

        public bool launched = false;
        public float LaunchForce = 500f;

        GameObject Trail;
        public void UpdateVis()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    if ((transform.position - ControllerDataManager.Instance.ControllerPos[PlayerData.localPlayer.networkId]).magnitude < 100f &&
                        ControllerDataManager.Instance.ControllerPos[PlayerData.localPlayer.networkId] != Vector3.zero)
                    {
                        Trail.SetActive(true);
                        transform.localScale = Vector3.one;
                    }
                    else
                    {
                        Trail.SetActive(false);
                        transform.localScale = Vector3.zero;
                    }
                }
            }
            
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
        public void InitTrail()
        {
            Trail = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.TorpedoTrail, transform);
            Trail.name = "Trail";
            Trail.transform.localPosition = Vector3.zero;
            Trail.transform.localScale = new Vector3(1,1,1f);
            Trail.SetActive(false);
        }
        public void BreakBallon(Vector3 position)
        {
            GameObject damager = new GameObject("damager");
            damager.transform.position = position;
            damager.AddComponent<BoxCollider>().size = new Vector3(0.01f, 0.01f, 0.01f);

            damager.AddComponent<Rigidbody>().velocity = new Vector3(0, 20, 0);
            Destroy(damager, 0.01f);
        }
        public void TorpedoExploHost(RaycastHit hit)
        {
            Debug.Log("Torpedo hit");
            GameObject hitEffect = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.TorpedoHit, new Vector3(hit.point.x, 20, hit.point.z), Quaternion.identity);
            Destroy(hitEffect, 5);
            AddWaterHitSound(hitEffect.transform);
            AddExploSound(transform);
            Collider[] ExploCol = Physics.OverlapSphere(transform.position, Caliber / 100f);
            foreach (Collider hitedCollider in ExploCol)
            {
                if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                {
                    hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(20f * Caliber, transform.position, 10f);
                }
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
        public void TorpedoExploClient()
        {
            Debug.Log("Client Torpedo hit");
            GameObject hitEffect = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.TorpedoHit,
                                    new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
            Destroy(hitEffect, 5);
            AddWaterHitSound(hitEffect.transform);
            AddExploSound(transform);
        }
        public bool DetectCollisionHost()
        {
            Ray TorpedoRay = new Ray(transform.position - 0.5f * transform.up, -transform.up);
            RaycastHit hit;
            if (Physics.Raycast(TorpedoRay, out hit, 0.5f))
            {
                if (hit.collider.isTrigger)
                {
                    return false;
                }
                if (hit.collider.transform.parent.GetComponent<WoodenArmour>() && hit.collider.transform.parent.GetComponent<BlockBehaviour>().isSimulating)
                {
                    try
                    {
                        foreach (var joints in hit.collider.transform.parent.GetComponent<BlockBehaviour>().jointsToMe)
                        {
                            joints.breakForce = float.PositiveInfinity;
                            joints.breakTorque = float.PositiveInfinity;
                        }
                        foreach (var joints in hit.collider.transform.parent.GetComponent<BlockBehaviour>().iJointTo)
                        {
                            joints.breakForce = float.PositiveInfinity;
                            joints.breakTorque = float.PositiveInfinity;
                        }
                    }
                    catch
                    {
                    }
                    

                    TorpedoExploHost(hit);


                    GameObject waterinhole = new GameObject("waterInHole");
                    waterinhole.transform.SetParent(hit.collider.transform.parent);
                    waterinhole.transform.localPosition = Vector3.zero;
                    waterinhole.transform.localRotation = Quaternion.identity;
                    waterinhole.transform.localScale = Vector3.one;

                    WaterInHole WH = waterinhole.AddComponent<WaterInHole>();
                    WH.hittedCaliber = Caliber;
                    WH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                    WH.type = 1;


                    GameObject piercedhole = new GameObject("TorpedoHole");
                    piercedhole.transform.SetParent(hit.collider.transform.parent);
                    piercedhole.transform.localPosition = Vector3.zero;
                    piercedhole.transform.localRotation = Quaternion.identity;
                    piercedhole.transform.localScale = Vector3.one;

                    PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                    PH.hittedCaliber = Caliber;
                    PH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                    PH.forward = myRigid.velocity.normalized;
                    PH.type = 1;

                    

                    if (StatMaster.isMP)
                    {
                        ModNetworking.SendToAll(WeaponMsgReceiver.HitHoleMsg.CreateMessage((int)hit.collider.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID,
                                                                                            hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode(),
                                                                                            Caliber, PH.position, PH.forward, 1));
                    }
                    return true;
                }
            }
            return false;
        }

        public void Start()
        {
            mySeed = (int)(UnityEngine.Random.value * 240);
            
            InitTrail();
            myRigid = GetComponent<Rigidbody>();

            //myGuid = System.Guid.NewGuid().GetHashCode();
            if (!TorpedoMsgReceiver.Instance.torpedoData[myPlayerID].ContainsKey(parentGuid))
            {
                TorpedoMsgReceiver.Instance.torpedoData[myPlayerID].Add(parentGuid, new TorpedoMsgReceiver.TorpedoData(transform.position, false));
            }
            else
            {
            }

            
            
        }

        public void FixedUpdate()
        {


            if (fire && !launched)
            {
                launched = true;
                fire = false;
                myRigid.AddForce(-transform.up * LaunchForce);
                myRigid.angularDrag = 100;
                
            }
            if (launched)
            {
                
                if (transform.position.y < 20)
                {
                    UpdateVis();
                    if (transform.position.y > 20 - depth - 0.2f)
                    {
                        Trail.SetActive(true);
                        Trail.transform.position = new Vector3(Trail.transform.position.x, 20f, Trail.transform.position.z);
                    }
                    
                    myRigid.drag = 5;
                    transform.rotation = Quaternion.LookRotation(Vector3.up, transform.up);

                    if (mode == 0)
                    {
                        myRigid.AddForce(-transform.up * 12f + new Vector3(0, 20 - depth - transform.position.y + 6.5f, 0));
                    }
                    else
                    {
                        myRigid.AddForce(-transform.up * 19f + new Vector3(0, 20 - depth - transform.position.y + 6.5f, 0));
                    }

                    if (StatMaster.isMP)
                    {
                        if (!StatMaster.isClient)
                        {
                            if (DetectCollisionHost())
                            {
                                ModNetworking.SendToAll(TorpedoMsgReceiver.TorpedoDataMsg.CreateMessage(myPlayerID, parentGuid, transform.position, true));
                                Destroy(gameObject);
                            }
                            else if(mySeed == ModController.Instance.state) 
                            {
                                ModNetworking.SendToAll(TorpedoMsgReceiver.TorpedoDataMsg.CreateMessage(myPlayerID, parentGuid, transform.position, false));
                            }

                        }
                    }
                    else
                    {
                        if (DetectCollisionHost())
                        {
                            Destroy(gameObject);
                        }
                    }
                }
                if (StatMaster.isMP || StatMaster.isClient)
                {
                    if (TorpedoMsgReceiver.Instance.torpedoData[myPlayerID][parentGuid].updated)
                    {
                        TorpedoMsgReceiver.Instance.torpedoData[myPlayerID][parentGuid].updated = false;
                        //Debug.Log("Client Torpedo justify");
                        transform.position = TorpedoMsgReceiver.Instance.torpedoData[myPlayerID][parentGuid].position;
                        if (TorpedoMsgReceiver.Instance.torpedoData[myPlayerID][parentGuid].exploded)
                        {
                            TorpedoExploClient();
                            Destroy(gameObject);
                        }
                    }
                }
            }

        }
        public void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 50), transform.position.y.ToString());
            //GUI.Box(new Rect(100, 300, 200, 50), myRigid.velocity.magnitude.ToString());
        }
    }
}
