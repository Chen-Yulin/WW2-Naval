using Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using UnityEngine;

namespace WW2NavalAssembly
{
    public class Bomb : MonoBehaviour
    {
        public int myPlayerID;
        public int myGuid;
        public GameObject parent;

        public int BombType = 0;
        public float Weight = 570;

        Rigidbody myRigid;
        public Vector3 randomForce;

        public float penetration;

        Stack<int> pericedBlock = new Stack<int>();
        Stack<int> damagedBallon = new Stack<int>();

        public int timer = 0;
        public bool timerOn;

        public bool hasHitWater = false;
        public bool exploded = false;
        public void AddExploSound(Transform t)
        {
            AudioSource exploAS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            exploAS.clip = ModResource.GetAudioClip("GunExplo Audio");
            exploAS.Play();
            exploAS.spatialBlend = 1.0f;
            exploAS.volume = Weight / 100;
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
            AS.volume = Weight / 1000;
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
            AS.volume = Weight / 800;
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

                        if (!hit.collider.transform.parent.GetComponent<BlockBehaviour>() && !hit.collider.transform.GetComponent<BlockBehaviour>())
                        {
                            Debug.Log("not a block");
                            return false;
                        }
                        if (hit.collider.transform.parent.GetComponent<BlockBehaviour>())
                        {
                            if (pericedBlock.Contains(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
                            {
                                return true;
                            }
                        }
                        else if (hit.collider.transform.GetComponent<BlockBehaviour>())
                        {
                            if (pericedBlock.Contains(hit.collider.transform.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
                            {
                                return true;
                            }
                        }
                    }
                    else
                    {
                        if (pericedBlock.Contains(hit.collider.transform.parent.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode()))
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
                if (hit.collider.transform.parent.GetComponent<WoodenArmour>())
                {
                    Thickness = hit.collider.transform.parent.GetComponent<WoodenArmour>().thickness;
                }
                else if (hit.collider.transform.parent.GetComponent<DefaultArmour>() || hit.collider.transform.GetComponent<DefaultArmour>())
                {
                    Thickness = 20;
                }
                else if (hit.collider.transform.parent.GetComponent<CannonWell>())
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
                    if (hit.collider.transform.parent.parent.name == "Engine")
                    {
                        pericedBlock.Push(hit.collider.transform.parent.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());
                    }
                    else
                    {
                        pericedBlock.Push(hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode());
                    }


                    if (hit.collider.transform.parent.name != "SpinningBlock")    // add waterIn behaviour
                    {

                        GameObject waterinhole = new GameObject("waterInHole");
                        waterinhole.transform.SetParent(hit.collider.transform.parent);
                        waterinhole.transform.localPosition = Vector3.zero;
                        waterinhole.transform.localRotation = Quaternion.identity;
                        waterinhole.transform.localScale = Vector3.one;

                        WaterInHole WH = waterinhole.AddComponent<WaterInHole>();
                        WH.hittedCaliber = Weight;
                        WH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                        if (pericedBlock.Count == 1)
                        {
                            WH.holeType = 0;
                        }
                        else
                        {
                            WH.holeType = 1;
                        }
                    }


                    if (Weight >= 100)
                    {
                        string hittedname = hit.collider.transform.parent.name;
                        if (hittedname == "DoubleWoodenBlock" || hittedname == "SingleWoodenBlock" || hittedname == "Log" || hittedname == "SpinningBlock")
                        {   // add hole projector
                            GameObject piercedhole = new GameObject("PiercedHole");
                            piercedhole.transform.SetParent(hit.collider.transform.parent);
                            piercedhole.transform.localPosition = Vector3.zero;
                            piercedhole.transform.localRotation = Quaternion.identity;
                            piercedhole.transform.localScale = Vector3.one;

                            PiercedHole PH = piercedhole.AddComponent<PiercedHole>();
                            PH.hittedCaliber = Weight;
                            PH.position = hit.collider.transform.parent.InverseTransformPoint(hit.point);
                            PH.forward = myRigid.velocity.normalized;

                            if (StatMaster.isMP)
                            {
                                ModNetworking.SendToAll(WeaponMsgReceiver.HitHoleMsg.CreateMessage((int)hit.collider.transform.parent.GetComponent<BlockBehaviour>().ParentMachine.PlayerID,
                                                                                                    hit.collider.transform.parent.GetComponent<BlockBehaviour>().BuildingBlock.Guid.GetHashCode(),
                                                                                                    Weight, PH.position, PH.forward, 0));
                            }
                        }
                    }
                    return true;
                }
            }
            catch { return false; }



        }
        public void HurtBalloon(GameObject balloon, Vector3 pos, bool AP)
        {
            BalloonLife life = balloon.GetComponent<BalloonLife>();
            if (life)
            {
                life.CutLife(Weight, AP);
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
            damager.AddComponent<BoxCollider>().size = new Vector3(0.01f, 0.01f, 0.01f);

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
                    try
                    {
                        if (hit.collider.transform.parent.parent.name == "Aircraft")
                        {
                            continue;
                        }
                    }
                    catch { }
                    

                    timerOn = true;

                    // pericing

                    if (Perice(hit))
                    {
                        // particle and sound effect
                        GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, hit.point, Quaternion.identity);
                        pierceEffect.transform.localScale = Weight / 400 * Vector3.one;
                        Destroy(pierceEffect, 1);
                        AddPierceSound(pierceEffect.transform);
                        ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Weight, 1));

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
                                float WellExploProb = Weight / CW.myCaliber * CW.gunNum * 0.08f;
                                float WellPalsyProb = 2 * WellExploProb;
                                float AmmoExploProb = 3 * WellExploProb;
                                if (hit.collider.name == "WellArmourVis")
                                {
                                    //Debug.Log(WellPalsyProb);
                                    if (UnityEngine.Random.value < WellPalsyProb)
                                    {
                                        CW.Wellpalsy = true;
                                    }
                                    if (UnityEngine.Random.value < WellExploProb * CW.myCaliber / 500)
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
                            hit.collider.transform.parent.parent.GetComponent<Engine>().CannonDamage(Weight);
                        }

                        // add force
                        try
                        {
                            if (!(hit.collider.transform.parent.name == "Balloon" || hit.collider.transform.parent.name == "SqrBalloon"))
                            {
                                hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Weight / 4, ForceMode.Force);
                            }
                        }
                        catch { }
                        continue;
                    }
                    if (!exploded)
                    {
                        PlayExploHit(hit, AP);
                        gameObject.SetActive(false);
                        Destroy(gameObject, 0.1f);
                    }

                    break;
                }
            }
        }
        public void CannonDetectCollisionClient()
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
                            explo.transform.localScale = exploInfo.Caliber / 800 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform);
                            break;
                        }
                    case 1:
                        {
                            GameObject pierceEffect = (GameObject)Instantiate(AssetManager.Instance.Pierce.Pierce, exploPosition, Quaternion.identity);
                            pierceEffect.transform.localScale = Weight / 400f * Vector3.one;
                            Destroy(pierceEffect, 1);
                            AddPierceSound(pierceEffect.transform);
                            break;
                        }
                    case 2:
                        {
                            GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, exploPosition, Quaternion.identity);
                            explo.SetActive(true);
                            explo.transform.localScale = exploInfo.Caliber / 400 * Vector3.one;
                            Destroy(explo, 3);
                            AddExploSound(explo.transform);
                            break;
                        }
                    default:
                        break;
                }


            }
            WeaponMsgReceiver.Instance.ExploInfo[myPlayerID].Clear();
        }
        public void CannonDetectWaterHost()
        {
            if (transform.position.y < 20f && myRigid.velocity.y < 0 && !hasHitWater && pericedBlock.Count == 0)
            {
                hasHitWater = true;
                GameObject waterhit;
                waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, new Vector3(transform.position.x, 20, transform.position.z), Quaternion.identity);
                waterhit.transform.localScale = Weight / 2f / 381f * Vector3.one;
                Destroy(waterhit, 3);

                PlayExploInAir(false, false);

                AddWaterHitSound(waterhit.transform);
                ModNetworking.SendToAll(WeaponMsgReceiver.WaterHitMsg.CreateMessage(myPlayerID, new Vector3(transform.position.x, 20, transform.position.z), Weight/2f));
                Destroy(gameObject);
            }
        }
        public void CannonDetectWaterClient()
        {

            foreach (WeaponMsgReceiver.waterhitInfo waterhitInfo in WeaponMsgReceiver.Instance.waterHitInfo[myPlayerID])
            {
                GameObject waterhit;
                if (waterhitInfo.Caliber >= 283)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, waterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = waterhitInfo.Caliber / 381f * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else if (waterhitInfo.Caliber >= 100)
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, waterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = waterhitInfo.Caliber / 381f * Vector3.one;
                    Destroy(waterhit, 3);
                }
                else
                {
                    waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit3, waterhitInfo.position, Quaternion.identity);
                    waterhit.transform.localScale = Weight / 381f * Vector3.one;
                    Destroy(waterhit, 3);
                }

                AddWaterHitSound(waterhit.transform);

            }
            WeaponMsgReceiver.Instance.waterHitInfo[myPlayerID].Clear();
        }
        private void PlayExploHit(RaycastHit hit, bool AP = true)
        {
            try
            {
                GameObject explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploWithSmoke, hit.point - myRigid.velocity.normalized * Weight / 800f, Quaternion.identity);
                explo.SetActive(true);
                explo.transform.localScale = Weight / 800 * (AP ? 1 : 2) * Vector3.one;
                Destroy(explo, 3);
                AddExploSound(explo.transform);

                exploded = true;

                //send to client
                ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, hit.point, Weight, 3));

                try
                {
                    hit.collider.attachedRigidbody.AddForce(transform.forward * myRigid.velocity.magnitude * Weight / 3, ForceMode.Force);
                }
                catch { }

                ExploDestroyBalloon(hit.point, AP);
            }
            catch { }

            Destroy(gameObject);


        }
        private void PlayExploInAir(bool AP = true, bool hasSmoke = true)
        {
            GameObject explo = null;
            if (hasSmoke)
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.exploWithSmoke, transform.position, Quaternion.identity);
            }
            else
            {
                explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.explo, transform.position, Quaternion.identity);
            }

            explo.SetActive(true);
            explo.transform.localScale = Weight / 800f * (AP ? 1 : 2) * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);

            exploded = true;

            //send to client
            if (hasSmoke)
            {
                ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Weight, 3));
            }
            else
            {
                ModNetworking.SendToAll(WeaponMsgReceiver.ExploMsg.CreateMessage(myPlayerID, transform.position, Weight, 0));
            }

            ExploDestroyBalloon(transform.position, AP);
            Destroy(gameObject);
        }
        
        private void ExploDestroyBalloon(Vector3 pos, bool AP = true)
        {
            float exploPenetration = Weight / 20f * (AP ? 1f : 1.5f);
            try
            {
                //Debug.Log(armourGuid);
                Collider[] ExploCol = Physics.OverlapSphere(transform.position, Mathf.Sqrt(Weight) / (AP ? 8f : 5f));
                foreach (Collider hitedCollider in ExploCol)
                {
                    try
                    {

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
                                hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce((AP ? 5f : 8f) * Weight, pos, Mathf.Sqrt(Weight) / (AP ? 8f : 5f));
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        public void Start()
        {
            name = "Gun";
            myRigid = gameObject.GetComponent<Rigidbody>();
            if (BombType == 0)
            {
                penetration = Weight * 0.12f;
            }

            TrailRenderer TR = gameObject.GetComponent<TrailRenderer>();
            if (BombType == 0)
            {
                TR.material.SetColor("_TintColor", new Color(1,1,1,0.1f));
            }

        }
        public void FixedUpdate()
        {
            myRigid.AddForce(randomForce);
            
            if (!StatMaster.isClient)
            {
                if (BombType == 0 && !hasHitWater) // for AP
                {
                    CannonDetectCollisionHost(false);
                    if (ModController.Instance.showSea)
                    {
                        CannonDetectWaterHost();
                    }
                    if (timerOn)
                    {
                        timer++;
                    }
                    if (timer > 10f && !exploded)
                    {
                        PlayExploInAir(false);
                    }
                }

            }
            else
            {
                CannonDetectCollisionClient();
                CannonDetectWaterClient();

            }
            if (transform.position.y < -1)
            {
                Destroy(this.gameObject);
            }

        }
    }
}
