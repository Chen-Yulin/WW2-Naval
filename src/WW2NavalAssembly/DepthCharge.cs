using Modding;
using Modding.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static KillingHandler;
using static WW2NavalAssembly.Aircraft;
using static WW2NavalAssembly.FlightDataBase;

namespace WW2NavalAssembly
{
    public class DepthChargeMsgManager : SingleInstance<DepthChargeMsgManager>
    {
        public override string Name { get; } = "DepthCharge Message Manager";
        public static MessageType DepthMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Integer, DataType.Single);
        public Dictionary<int, Dictionary<int, float>>[] Depth = new Dictionary<int, Dictionary<int, float>>[16];

        public DepthChargeMsgManager()
        {
            for (int i = 0; i < 16; i++)
            {
                Depth[i] = new Dictionary<int, Dictionary<int, float>>();
            }
        }
        public void SendDepthMsg(int playerID, int guid, int lid, float depth)
        {
            ModNetworking.SendToAll(DepthMsg.CreateMessage(playerID, guid, lid, depth));
        }
        public void DepthMsgReceiver(Message msg)
        {
            int pid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            int lid = (int)msg.GetData(2);
            float depth = (float)msg.GetData(3);
            if (Depth[pid].ContainsKey(guid))
            {
                if (Depth[pid][guid].ContainsKey(lid))
                {
                    Depth[pid][guid][lid] = depth;
                }
                else
                {
                    Depth[pid][guid].Add(lid, depth);
                }
            }
            else
            {
                Depth[pid].Add(guid, new Dictionary<int, float>());
                Depth[pid][guid].Add(lid, depth);
            }
        }

    }
    public class DepthBomb : MonoBehaviour
    {
        public float Pound = 0;
        public int myPlayerID = 0;
        public int myGuid = 0;
        public Vector3 randomForce = Vector3.zero;
        public Rigidbody rig;

        // storage of the bomb
        public Transform storer = null;
        public Vector3 PosOffset = Vector3.zero;
        public Quaternion RotOffset = Quaternion.identity;

        // hit water
        float preHeight = 0f;
        public bool inWater
        {
            get
            {
                return transform.position.y < Constants.SeaHeight;
            }
        }

        // faze
        public float exploDepth = 10f;
        public bool exploded = false;
        public int id = 0;

        public bool isSelf
        {
            get
            {
                return StatMaster.isMP ? myPlayerID == PlayerData.localPlayer.networkId : true;
            }
        }

        public bool DetectHitWater()
        {
            if (preHeight > 20 && transform.position.y <= 20)
            {
                preHeight = transform.position.y;
                return true;
            }
            else
            {
                preHeight = transform.position.y;
                return false;
            }
        }
        public void AddWaterHitSound(Transform t)
        {
            AudioSource AS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            AS.clip = ModResource.GetAudioClip("GunWaterHit Audio");
            AS.Play();
            AS.spatialBlend = 1.0f;
            AS.volume = Pound / 800;
            AS.rolloffMode = AudioRolloffMode.Linear;
            AS.maxDistance = 500;
            AS.SetSpatializerFloat(1, 1f);
            AS.SetSpatializerFloat(2, 0);
            AS.SetSpatializerFloat(3, 12);
            AS.SetSpatializerFloat(4, 1000f);
            AS.SetSpatializerFloat(5, 1f);

            int listenid = StatMaster.isMP ? PlayerData.localPlayer.networkId : 0;
            GameObject controller = ControllerDataManager.Instance.ControllerObject[listenid];
            if (controller)
            {
                Vector2 arrow = MathTool.Get2DCoordinate(transform.position - controller.transform.position);
                float signedAngle = MathTool.SignedAngle(MathTool.Get2DCoordinate(-controller.transform.up), arrow);
                if (signedAngle < 0)
                {
                    signedAngle += 360;
                }
                //Debug.Log(signedAngle);
                float mag = 1f / arrow.magnitude * Pound/2000 * 5f;
                float error = Mathf.Clamp(Mathf.Sqrt(arrow.magnitude) * 5f, 0, 90);
                SoundSystem.Instance.AddSound(myPlayerID, (int)signedAngle, mag, error);
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
        public void HurtBalloon(GameObject balloon, Vector3 pos, bool AP)
        {
            BalloonLife life = balloon.GetComponent<BalloonLife>();
            if (life)
            {
                life.CutLife(Pound, AP);
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
        private void ExploDestroy(Vector3 pos, bool AP = false)
        {
            float exploPenetration = Pound / 5f * (AP ? 1f : 1.5f);
            try
            {
                //Debug.Log(armourGuid);
                Collider[] ExploCol = Physics.OverlapSphere(pos, Mathf.Sqrt(Pound) / (AP ? 8f : 5f));
                foreach (Collider hitedCollider in ExploCol)
                {
                    try
                    {
                        // armour
                        try
                        {
                            WoodenArmour wa = hitedCollider.attachedRigidbody.GetComponent<WoodenArmour>();
                            if (wa)
                            {
                                float ArmourBetween = 0;
                                Ray Ray = new Ray(pos, hitedCollider.transform.position - pos);
                                RaycastHit[] hitList = Physics.RaycastAll(Ray, (hitedCollider.transform.position - pos).magnitude);
                                foreach (RaycastHit raycastHit in hitList)
                                {
                                    try
                                    {
                                        //Debug.Log(raycastHit.rigidbody.name);
                                        if (raycastHit.collider.attachedRigidbody.GetComponent<WoodenArmour>())
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
                                    wa.CannonExplo(Pound, (hitedCollider.transform.position - pos).magnitude, !AP);
                                }

                            }
                        }
                        catch { }
                        // bytank
                        try
                        {
                            Bytank bt = hitedCollider.attachedRigidbody.GetComponent<Bytank>();
                            if (bt)
                            {
                                bt.BreakSpace = Pound * Pound * (bt.transform.position.y < 20 ? 10 : 2);
                            }
                        }
                        catch { }
                        // balloon
                        if ((hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon"))
                        {
                            HurtBalloon(hitedCollider.transform.parent.gameObject, hitedCollider.transform.position, AP);

                        }
                        //force
                        else if (hitedCollider.transform.parent.GetComponent<Rigidbody>())
                        {
                            if (!(hitedCollider.transform.parent.name == "Balloon" || hitedCollider.transform.parent.name == "SqrBalloon"))
                            {
                                hitedCollider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce((AP ? 2f : 4f) * Pound, pos, Mathf.Sqrt(Pound) / (AP ? 8f : 5f));
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        public void PlayWaterHit()
        {
            GameObject waterhit;
            if (Pound >= 283)
            {
                waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit1, new Vector3(transform.position.x, Constants.SeaHeight, transform.position.z), Quaternion.identity);
            }
            else if (Pound >= 100)
            {
                waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit2, new Vector3(transform.position.x, Constants.SeaHeight, transform.position.z), Quaternion.identity);
            }
            else
            {
                waterhit = (GameObject)Instantiate(AssetManager.Instance.WaterHit.waterhit3, new Vector3(transform.position.x, Constants.SeaHeight, transform.position.z), Quaternion.identity);
            }
            waterhit.transform.localScale = Pound / 381 * Vector3.one * Mathf.Sqrt(rig.velocity.magnitude) * 0.1f;
            Destroy(waterhit, 3);
            AddWaterHitSound(waterhit.transform);
        }
        public void FollowStorer()
        {
            if (storer)
            {
                transform.position = storer.localToWorldMatrix.MultiplyPoint3x4(PosOffset);
                transform.rotation = storer.rotation * RotOffset;
                rig.isKinematic = true;
            }
        }
        public void Explo()
        {
            exploded = true;
            PlayExploEffect();
            if (!StatMaster.isClient)
            {
                ExploDestroy(transform.position);
            }
            Destroy(gameObject);
        }
        public void AddExploSound(Transform t)
        {
            AudioSource exploAS = t.gameObject.AddComponent<AudioSource>();
            //t.gameObject.AddComponent<MakeAudioSourceFixedPitch>();
            exploAS.clip = ModResource.GetAudioClip("GunExplo Audio");
            exploAS.Play();
            exploAS.spatialBlend = 1.0f;
            exploAS.volume = Pound / 100;
            exploAS.rolloffMode = AudioRolloffMode.Linear;
            exploAS.maxDistance = 300;
            exploAS.SetSpatializerFloat(1, 1f);
            exploAS.SetSpatializerFloat(2, 0);
            exploAS.SetSpatializerFloat(3, 12);
            exploAS.SetSpatializerFloat(4, 1000f);
            exploAS.SetSpatializerFloat(5, 1f);
            int listenid = StatMaster.isMP ? PlayerData.localPlayer.networkId : 0;
            GameObject controller = ControllerDataManager.Instance.ControllerObject[listenid];
            if (controller)
            {
                Vector2 arrow = MathTool.Get2DCoordinate(transform.position - controller.transform.position);
                float signedAngle = MathTool.SignedAngle(MathTool.Get2DCoordinate(-controller.transform.up), arrow);
                if (signedAngle < 0)
                {
                    signedAngle += 360;
                }
                //Debug.Log(signedAngle);
                float mag = 1f / arrow.magnitude * Pound / 2000 * 5f;
                float error = Mathf.Clamp(Mathf.Sqrt(arrow.magnitude) * 2f, 0, 90);
                SoundSystem.Instance.AddSound(myPlayerID, (int)signedAngle, mag, error);
            }
        }
        public void PlayExploEffect()
        {
            GameObject explo;
            explo = (GameObject)Instantiate(AssetManager.Instance.CannonHit.UnderWaterExplo, transform.position, Quaternion.identity);
            explo.name = "Explo Water";
            explo.SetActive(true);
            explo.transform.localScale = Pound / 400 * Vector3.one;
            Destroy(explo, 3);
            AddExploSound(explo.transform);
            GameObject hitEffect = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.TorpedoHit, new Vector3(transform.position.x, Constants.SeaHeight, transform.position.z), Quaternion.identity);
            hitEffect.transform.localScale = Pound / 400f * Vector3.one;
            Destroy(hitEffect, 5);
            AddWaterHitSound(hitEffect.transform);
        }

        public void Start()
        {
            preHeight = transform.position.y;
            if (isSelf)
            {
                exploDepth = ControllerDataManager.Instance.ControllerObject[myPlayerID].GetComponent<Controller>().GetDepth();
                DepthChargeMsgManager.Instance.SendDepthMsg(myPlayerID, myGuid, id, exploDepth);
            }
            else
            {
                exploDepth = -1f;
            }
            
        }

        public void LateUpdate()
        {
            FollowStorer();
        }

        public void Update()
        {
            if (exploDepth < 0)
            {
                try
                {
                    exploDepth = DepthChargeMsgManager.Instance.Depth[myPlayerID][myGuid][id];
                }
                catch { }
            }
        }

        public void FixedUpdate()
        {
            if (ModController.Instance.showSea && storer == null)
            {
                if (DetectHitWater())
                {
                    PlayWaterHit();
                }
                if (inWater)
                {
                    rig.drag = 10f;
                    if (transform.position.y < exploDepth)
                    {
                        if (!exploded)
                        {
                            Explo();
                        }
                    }
                }
                else
                {
                    rig.drag = 0.2f;
                }
            }
            rig.AddForce(randomForce);
        }

    }
    public class DepthCharge : Gun
    {
        public float Pound = 200;
        public MSlider LaunchForce;
        protected new int iconSize = 15;

        public int launchID = 0;

        public Vector3 GetRandomForce()
        {
            Vector3 randomForce = new Vector3(UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f, UnityEngine.Random.value - 0.5f) * 6 / Mathf.Sqrt(Pound);
            randomForce += new Vector3(0, UnityEngine.Random.value - 0.5f, 0) * 6 / Mathf.Sqrt(Pound);
            return randomForce * Mathf.Pow(UnityEngine.Random.value, 2);
        }
        public override void UpdateUI()
        {
            if (StatMaster.hudHidden)
            {
                ReloadAPOutUI.show = false;
                ReloadAPInUI.show = false;
            }
            else
            {
                if (NextCannonType == 0)
                {
                    ReloadAPOutUI.show = true;
                }
                if (CannonType == 0)
                {
                    int currIconSize = (int)(iconSize * currentReloadTime / reloadTime);
                    ReloadAPInUI.size = currIconSize;
                    ReloadAPInUI.show = true;
                }
            }

        }
        public override void InitCannon()
        {
            Transform PrefabParent = BlockBehaviour.ParentMachine.transform.Find("Simulation Machine");
            string PrefabName = "DepthCharge [" + myPlayerID + "](" + Pound + ")";
            CannonPrefab = new GameObject(PrefabName);
            CannonPrefab.transform.parent = PrefabParent;
            DepthBomb DBtmp = CannonPrefab.AddComponent<DepthBomb>();
            DBtmp.Pound = Pound;
            DBtmp.myPlayerID = myPlayerID;
            DBtmp.myGuid = myGuid;
            DBtmp.id = launchID;
            DBtmp.storer = transform;
            DBtmp.PosOffset = new Vector3(0, -0.105f*transform.localScale.y, 0.193f*transform.localScale.z);

            Rigidbody RBtmp = CannonPrefab.AddComponent<Rigidbody>();
            DBtmp.rig = RBtmp;
            RBtmp.interpolation = RigidbodyInterpolation.Extrapolate;
            RBtmp.mass = 0.2f;
            RBtmp.drag = 0.2f;
            RBtmp.useGravity = false;
            GameObject BombVis = new GameObject("BombVis");
            BombVis.transform.SetParent(CannonPrefab.transform);
            BombVis.transform.localPosition = Vector3.zero;
            BombVis.transform.localRotation = Quaternion.identity;
            BombVis.transform.localScale = Vector3.one;
            MeshFilter MFtmp = BombVis.AddComponent<MeshFilter>();
            MFtmp.sharedMesh = ModResource.GetMesh("DCB Mesh").Mesh;
            MeshRenderer MRtmp = BombVis.AddComponent<MeshRenderer>();
            MRtmp.material.mainTexture = ModResource.GetTexture("DCB Texture").Texture;

            GravityModifier gm = CannonPrefab.AddComponent<GravityModifier>();
            gm.gravityScale = Constants.BulletGravity / Constants.Gravity;

            CannonPrefab.SetActive(false);
        }

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            FireKey = AddKey(LanguageManager.Instance.CurrentLanguage.GunFire, "Fire", KeyCode.C);
            LaunchForce = AddSlider("Launch Force", "LaunchForce", 10f, 0f, 50f);
            ReloadHEOut = ModResource.GetTexture("ReloadHEOut Texture").Texture;
            ReloadHEIn = ModResource.GetTexture("ReloadHEIn Texture").Texture;
            ReloadAPOut = ModResource.GetTexture("ReloadAPOut Texture").Texture;
            ReloadAPIn = ModResource.GetTexture("ReloadAPIn Texture").Texture;
            myseed = (int)(UnityEngine.Random.value * 10);
        }
        public void Start()
        {
            name = "DepthCharge";
        }
        public override void BuildingUpdate()
        {
        }
        public override void OnSimulateStart()
        {
            VisTransform = transform.Find("Vis");
            Pound = MathTool.MinAxis(transform.localScale) * 200;
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            InitCannon();
            reloadTime = Pound >= 100 ? 0.4f * Mathf.Sqrt(Pound) - 3 :
                                                (2.8f * Mathf.Sin(Mathf.PI / 160f * (Mathf.Pow(Pound, 1.5f) / 10f - 80)) + 2.9f) / 4f;
            currentReloadTime = reloadTime;
            CannonType = 0;
            NextCannonType = CannonType;
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


            // add block UI
            if (isSelf)
            {
                ReloadAPOutUI = BlockUIManager.Instance.CreateFollowerUI(transform, iconSize, ReloadAPOut);
                ReloadAPInUI = BlockUIManager.Instance.CreateFollowerUI(transform, iconSize, ReloadAPIn);
            }
            reloadefficiency = 1f;
        }
        public override void OnSimulateStop()
        {
            WeaponMsgReceiver.Instance.Fire[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTime[myPlayerID].Remove(myGuid);
            WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID].Remove(myGuid);
        }
        public void OnDestroy()
        {
        }
        public override void SimulateUpdateHost()
        {
            if (currentReloadTime < reloadTime)
            {
                CannonPrefab.SetActive(false);
                currentReloadTime += Time.deltaTime * reloadefficiency;
                if (ModController.Instance.state == myseed)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.ReloadMsg.CreateMessage(myPlayerID, myGuid, currentReloadTime,
                                                                                        (CannonType == 1) ? true : false,
                                                                                        (NextCannonType == 1) ? true : false, 0));
                }
                return;
            }
            else
            {
                CannonPrefab.SetActive(true);
            }

            if (FireKey.IsPressed)
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                gameObject.GetComponent<Rigidbody>().AddForce(-(transform.forward - transform.up * 0.6f).normalized * LaunchForce.Value * 10);
                Vector3 randomForce = GetRandomForce() * 0.2f;

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, CannonPrefab.transform.position, CannonPrefab.transform.rotation);
                DepthBomb DB = Cannon.GetComponent<DepthBomb>();
                DB.randomForce = randomForce;
                DB.rig.isKinematic = false;
                DB.rig.velocity = (transform.forward - transform.up * 0.6f).normalized * LaunchForce.Value;
                DB.storer = null;
                DB.id = launchID;

                CannonType = NextCannonType;
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward, Vector3.zero, timeFaze));
                }
                launchID ++;
            }
        }
        public override void SimulateUpdateClient()
        {
            if (WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid])
            {
                WeaponMsgReceiver.Instance.reloadTimeUpdated[myPlayerID][myGuid] = false;
                currentReloadTime = WeaponMsgReceiver.Instance.reloadTime[myPlayerID][myGuid];
                CannonType = WeaponMsgReceiver.Instance.CannonType[myPlayerID][myGuid] ? 1 : 0;
                NextCannonType = WeaponMsgReceiver.Instance.NextCannonType[myPlayerID][myGuid] ? 1 : 0;
            }
            if (currentReloadTime < reloadTime)
            {
                CannonPrefab.SetActive(false);
                currentReloadTime += Time.deltaTime * reloadefficiency;
            }
            else
            {
                CannonPrefab.SetActive(true);
            }

            if (WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire)
            {
                currentReloadTime = 0;
                muzzleStage = 0;
                WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fire = false;
                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, CannonPrefab.transform.position, CannonPrefab.transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                DepthBomb DB = Cannon.GetComponent<DepthBomb>();
                DB.randomForce = WeaponMsgReceiver.Instance.Fire[myPlayerID][myGuid].fireForce;
                DB.rig.isKinematic = false;
                DB.rig.velocity = (transform.forward - transform.up * 0.6f).normalized * LaunchForce.Value;
                DB.storer = null;
                DB.id = launchID;
                CannonType = NextCannonType;
                launchID++;
            }
        }
        public override void SimulateFixedUpdateHost()
        {
            if (muzzleStage < 7)
            {
                muzzleStage++;
                VisTransform.localPosition = Vector3.Lerp(VisTransform.localPosition, -Pound / 1600 * Vector3.forward, Pound / 1600);
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
                gameObject.GetComponent<Rigidbody>().AddForce(-(transform.forward - transform.up * 0.6f).normalized * LaunchForce.Value);
                Vector3 randomForce = GetRandomForce();

                GameObject Cannon = (GameObject)Instantiate(CannonPrefab, CannonPrefab.transform.position, CannonPrefab.transform.rotation);
                Cannon.name = "NavalCannon" + myPlayerID.ToString();
                Cannon.SetActive(true);
                DepthBomb DB = Cannon.GetComponent<DepthBomb>();
                DB.randomForce = randomForce;
                DB.rig.isKinematic = false;
                DB.rig.velocity = (transform.forward - transform.up * 0.6f).normalized * LaunchForce.Value;
                DB.storer = null;
                DB.id = launchID;
                CannonType = NextCannonType;
                ModNetworking.SendToAll(WeaponMsgReceiver.FireMsg.CreateMessage(myPlayerID, myGuid, randomForce, transform.forward, Vector3.zero, timeFaze));
                launchID++;
            }
        }
        public override void MySimulateFixedUpdateClient()
        {
            if (muzzleStage < 7)
            {
                muzzleStage++;
                VisTransform.localPosition = Vector3.Lerp(VisTransform.localPosition, -Pound / 1600 * Vector3.forward, Pound / 1600);
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
