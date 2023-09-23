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

namespace WW2NavalAssembly
{
    public class WellMsgReceicer : SingleInstance<WellMsgReceicer>
    {
        public override string Name { get; } = "Well Msg Receiver";
        public static MessageType hitMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Integer); // playerID, guid, type

        public Dictionary<int, bool>[] WellExplo = new Dictionary<int, bool>[16];
        public Dictionary<int, bool>[] AmmoExplo = new Dictionary<int, bool>[16];
        public Dictionary<int, bool>[] WellPalsy = new Dictionary<int, bool>[16];
        public Dictionary<int, bool>[] TurrentPalsy = new Dictionary<int, bool>[16];
        public WellMsgReceicer()
        {
            for (int i = 0; i < 16; i++)
            {
                WellExplo[i] = new Dictionary<int, bool>();
                AmmoExplo[i] = new Dictionary<int, bool>();
                WellPalsy[i] = new Dictionary<int, bool>();
                TurrentPalsy[i] = new Dictionary<int, bool>();
            }
        }

        public void ExploMsgReceiver(Message msg)
        {
            if ((int)msg.GetData(2) == 0)
            {
                WellExplo[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            }
            else if ((int)msg.GetData(2) == 1)
            {
                AmmoExplo[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            }
            else if ((int)msg.GetData(2) == 3)
            {
                TurrentPalsy[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            }
            else
            {
                WellPalsy[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            }
        }

    }
    public class CannonWell : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MToggle UseWell;
        public MSlider Thickness;
        public MSlider Depth;
        public MSlider Offset;
        public MSlider AmmoResize;
        public MSlider TurretSize;
        public MText GunGroup;
        public float thickness;
        public GameObject WellVis;
        public GameObject TurretVis;
        GameObject AmmoVis;
        MeshRenderer WellVisRender;
        MeshRenderer AmmoVisRender;
        MeshRenderer TurrentVisRender;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public bool Wellpalsy;
        public bool TurrentPalsy;
        public bool AmmoExplo;
        public bool WellExplo;

        public int TurrentPalsyCount = 0;
        public int TurrentHP = 1;

        public float myCaliber = 0;
        public float totalCaliber = 0;
        public float gunNum = 0;
        public bool exploded = false;
        public bool disableGun = false;

        GameObject[] GunLine = new GameObject[4];

        GameObject WellExploEffect;
        GameObject AmmoExploEffect;

        bool initialized = false;
        bool visInitialized = false;
        public bool isWooden(BlockBehaviour bb)
        {
            if (!bb)
            {
                return false;
            }
            int blockID = bb.BlockID;
            switch (blockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    return true;
                case (int)BlockType.DoubleWoodenBlock:
                    return true;
                case (int)BlockType.Log:
                    return true;
                default:
                    return false;
            }
        }
        public void SetReloadEfficiency(float percent, bool initial = false)
        {
            if (initial)
            {
                TurretSize.Value = Mathf.Clamp(TurretSize.Value, 0.8f, 1.5f);
            }
            else
            {
                Color tmpColor = TurrentVisRender.material.color;
                TurrentVisRender.material.color = new Color(tmpColor.r / 2, tmpColor.g / 2, tmpColor.b / 2, ModController.Instance.ShowArmour ? 1f : 0f);
            }
            try
            {
                Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                int num = 0;
                if (gunlist.Count != 0)
                {
                    foreach (var gunObject in gunlist)
                    {
                        if (num > 3)
                        {
                            break;
                        }
                        if (!gunObject.Value)
                        {
                            continue;
                        }
                        if (initial)
                        {
                            float efficiency = 1;
                            if (!UseWell.isDefaultValue)
                            {
                                efficiency = Mathf.Clamp(8 / Mathf.Sqrt(gunObject.Value.GetComponent<Gun>().Caliber.Value), 0, 1);
                            }
                            gunObject.Value.GetComponent<Gun>().reloadefficiency = efficiency * TurretSize.Value * TurretSize.Value;
                        }
                        else
                        {
                            gunObject.Value.GetComponent<Gun>().reloadefficiency *= percent;
                        }

                        num++;
                    }
                }
            }
            catch { }
        }
        public void AmmoExploforce()
        {
            Collider[] explo = Physics.OverlapSphere(AmmoVis.transform.position, Mathf.Sqrt(myCaliber) / 6f);
            foreach (Collider collider in explo)
            {
                try
                {
                    BlockBehaviour BB = collider.attachedRigidbody.GetComponent<BlockBehaviour>();
                    if (isWooden(BB))
                    {
                        foreach (var joint in BB.iJointTo)
                        {
                            try
                            {
                                if (!joint.connectedBody.GetComponent<Engine>() && !joint.connectedBody.GetComponent<CannonWell>())
                                {
                                    joint.breakForce = 500f;
                                    joint.breakTorque = 500f;
                                }
                            }
                            catch { }
                            
                        }
                        foreach (var joint in BB.jointsToMe)
                        {
                            try
                            {
                                if (!joint.connectedBody.GetComponent<Engine>() && !joint.GetComponent<CannonWell>())
                                {
                                    joint.breakForce = 500f;
                                    joint.breakTorque = 500f;
                                }
                            }
                            catch { }
                        }
                    }
                    
                }
                catch { }
                try
                {
                    collider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(Mathf.Sqrt(myCaliber) * 200, AmmoVis.transform.position, Mathf.Sqrt(myCaliber) * 2);
                }
                catch { }
            }
        }
        public void WellExploForce()
        {
            try
            {
                foreach (var joints in gameObject.GetComponent<BlockBehaviour>().jointsToMe)
                {
                    joints.breakForce = 1f;
                }
            }
            catch { }
            Collider[] turrent = Physics.OverlapSphere(transform.position + (transform.localScale.z + 0.2f) * transform.forward, Mathf.Clamp(totalCaliber / 800, 0.5f, 1.5f));
            foreach (Collider collider in turrent)
            {
                try
                {
                    collider.transform.parent.GetComponent<Rigidbody>().AddForce(transform.forward * totalCaliber * 30);
                }
                catch { }
            }
            gameObject.GetComponent<Rigidbody>().AddForce(-transform.forward * totalCaliber * 250);
        }
        public void DisableGun()
        {
            if (disableGun)
            {
                try
                {
                    Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                    int num = 0;
                    if (gunlist.Count != 0)
                    {
                        foreach (var gunObject in gunlist)
                        {
                            if (num > 3)
                            {
                                break;
                            }
                            if (!gunObject.Value)
                            {
                                continue;
                            }
                            gunObject.Value.GetComponent<Gun>().currentReloadTime = 0;
                            num++;
                        }
                    }
                }
                catch { }
            }
        }
        public void DetectHitHost()
        {
            if (TurrentPalsy)
            {
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WellMsgReceicer.hitMsg.CreateMessage(myPlayerID, myGuid, 3));
                }
                TurrentPalsy = false;
                TurrentHP /= 2;
                TurrentPalsyCount++;
                SetReloadEfficiency(0.5f);
            }
            if (Wellpalsy)
            {
                //Debug.Log("WellPalsy");
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WellMsgReceicer.hitMsg.CreateMessage(myPlayerID, myGuid, 2));
                }
                Wellpalsy = false;
                // reset gun load
                try
                {
                    Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                    int num = 0;
                    if (gunlist.Count != 0)
                    {
                        foreach (var gunObject in gunlist)
                        {
                            if (num > 3)
                            {
                                break;
                            }
                            if (!gunObject.Value)
                            {
                                continue;
                            }
                            gunObject.Value.GetComponent<Gun>().currentReloadTime = 0;
                            num++;
                        }
                    }
                }
                catch { }
            }
            if (WellExplo && WellExploEffect)
            {
                disableGun = true;
                //Debug.Log("WellExplo");
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WellMsgReceicer.hitMsg.CreateMessage(myPlayerID, myGuid, 0));
                }
                WellExplo = false;
                if (!WellExploEffect.activeSelf)
                {
                    WellExploEffect.SetActive(true);
                    Destroy(WellExploEffect, 5);
                    WellExploForce();
                }

            }
            if (AmmoExplo)
            {
                disableGun = true;
                //Debug.Log("AmmoExplo");
                if (StatMaster.isMP)
                {
                    ModNetworking.SendToAll(WellMsgReceicer.hitMsg.CreateMessage(myPlayerID, myGuid, 1));
                }

                AmmoExplo = false;
                if (!exploded)
                {
                    AmmoExploEffect = (GameObject)Instantiate(AssetManager.Instance.WellEffect.AmmoExplo);
                    AmmoExploEffect.name = "AmmoExploEffect";
                    AmmoExploEffect.transform.position = AmmoVis.transform.position;
                    AmmoExploEffect.transform.localScale = Vector3.one * Mathf.Sqrt(myCaliber) / 20;
                    AmmoExploEffect.SetActive(true);
                    Destroy(AmmoExploEffect, 3.3f);
                    AmmoExploforce();
                    exploded = true;
                }

            }
        }
        public void DetectHitClient()
        {
            if (WellMsgReceicer.Instance.WellPalsy[myPlayerID][myGuid])
            {
                WellMsgReceicer.Instance.WellPalsy[myPlayerID][myGuid] = false;
                try
                {
                    Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                    int num = 0;
                    if (gunlist.Count != 0)
                    {
                        foreach (var gunObject in gunlist)
                        {
                            if (num > 3)
                            {
                                break;
                            }
                            if (!gunObject.Value)
                            {
                                continue;
                            }
                            gunObject.Value.GetComponent<Gun>().currentReloadTime = 0;
                            num++;
                        }
                    }
                }
                catch { }
            }
            if (WellMsgReceicer.Instance.TurrentPalsy[myPlayerID][myGuid])
            {
                WellMsgReceicer.Instance.TurrentPalsy[myPlayerID][myGuid] = false;
                TurrentHP /= 2;
                TurrentPalsyCount++;
                SetReloadEfficiency(0.5f);
            }
            if (WellMsgReceicer.Instance.WellExplo[myPlayerID][myGuid])
            {
                disableGun = true;
                WellMsgReceicer.Instance.WellExplo[myPlayerID][myGuid] = false;
                WellExploEffect.SetActive(true);
                Destroy(WellExploEffect, 5);
            }
            if (WellMsgReceicer.Instance.AmmoExplo[myPlayerID][myGuid])
            {
                disableGun = true;
                WellMsgReceicer.Instance.AmmoExplo[myPlayerID][myGuid] = false;
                if (!exploded)
                {
                    exploded = true;
                    AmmoExploEffect = (GameObject)Instantiate(AssetManager.Instance.WellEffect.AmmoExplo);
                    AmmoExploEffect.name = "AmmoExploEffect";
                    AmmoExploEffect.transform.position = AmmoVis.transform.position;
                    AmmoExploEffect.transform.localScale = Vector3.one * myCaliber / 400;
                    AmmoExploEffect.SetActive(true);
                    Destroy(AmmoExploEffect, 3.3f);
                    AmmoExploforce();
                }

            }
        }
        public void ShowGroupLine()
        {
            foreach (GameObject line in GunLine)
            {
                line.SetActive(false);
            }
            try
            {
                Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                int num = 0;
                if (gunlist.Count != 0)
                {
                    foreach (var gunObject in gunlist)
                    {
                        if (num > 3)
                        {
                            break;
                        }
                        if (!gunObject.Value)
                        {
                            continue;
                        }
                        GunLine[num].GetComponent<LineRenderer>().SetPosition(0, AmmoVis.transform.position);
                        GunLine[num].GetComponent<LineRenderer>().SetPosition(1, gunObject.Value.transform.position);
                        GunLine[num].SetActive(true);
                        num++;
                    }
                }
            }
            catch { }
        }
        public void AdjustPara()
        {
            WellVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value - Depth.Value / 2) / transform.lossyScale.z);
            TurretSize.Value = Mathf.Clamp(TurretSize.Value, 0.8f, 1.5f);
            try
            {
                totalCaliber = 0;
                Dictionary<int, GameObject> gunlist = Grouper.Instance.GetGun(myPlayerID, GunGroup.Value);
                int num = 0;
                if (gunlist.Count != 0)
                {
                    foreach (var gunObject in gunlist)
                    {
                        if (num == 0)
                        {
                            try
                            {
                                myCaliber = gunObject.Value.GetComponent<Gun>().Caliber.Value;
                            }
                            catch { }
                        }
                        if (num > 3)
                        {
                            break;
                        }
                        try
                        {
                            totalCaliber += gunObject.Value.GetComponent<Gun>().Caliber.Value;
                            num++;
                        }
                        catch { }

                    }
                }
            }
            catch { }

            if (myCaliber == 0)
            {
                gunNum = 0;
            }
            else
            {
                gunNum = totalCaliber / myCaliber;
            }

            float numCoeff = 0;
            switch (gunNum)
            {
                case 0: numCoeff = 0; break;
                case 1: numCoeff = 0.01f; break;
                case 2: numCoeff = 0.021f; break;
                case 3: numCoeff = 0.032f; break;
                case 4: numCoeff = 0.037f; break;
                default: break;
            }
            float WellWidth;
            if (totalCaliber != 0)
            {
                WellWidth = Mathf.Clamp(myCaliber * numCoeff / 5, 0.3f, 4);
            }
            else
            {
                WellVis.SetActive(false);
                AmmoVis.SetActive(false);
                WellWidth = 0.01f;
            }
            WellWidth *= TurretSize.Value;

            WellVis.transform.localScale = new Vector3(WellWidth / (transform.lossyScale.x), Depth.Value / (2 * transform.lossyScale.z), WellWidth / (transform.lossyScale.y));

            float AmmoThickness;
            if (myCaliber > 283)
            {
                AmmoThickness = 1f;
            }
            else if (myCaliber <= 283 && myCaliber >= 203)
            {
                AmmoThickness = 0.75f;
            }
            else
            {
                AmmoThickness = 0.5f;
            }
            float resize = Mathf.Clamp(Mathf.Sqrt(AmmoResize.Value * AmmoResize.Value + 1) + AmmoResize.Value, 0.618f, 1.618f);
            AmmoVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value - Depth.Value - AmmoThickness * resize * resize / 2) / transform.lossyScale.z);
            
            AmmoVis.transform.localScale = new Vector3(Mathf.Clamp(totalCaliber / (450 * transform.lossyScale.x) * 1.1f / resize, 0.01f, 10f),
                                                        AmmoThickness * resize * resize / (2 * transform.lossyScale.z),
                                                        Mathf.Clamp(totalCaliber / (450 * transform.lossyScale.y) * 1.1f / resize, 0.01f, 10f));

            float TurretThickness = Mathf.Clamp(Mathf.Sqrt(myCaliber) / 20,0.01f,10);

            if (UseWell.isDefaultValue)
            {
                TurretVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value) / transform.lossyScale.z);
                TurretVis.transform.localScale = new Vector3(WellVis.transform.localScale.x / 2.5f, TurretThickness / (transform.lossyScale.z), WellVis.transform.localScale.z / 2.5f);
            }
            else
            {
                TurretThickness *= 1.5f;
                TurretVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value) / transform.lossyScale.z);
                TurretVis.transform.localScale = new Vector3(WellVis.transform.localScale.x / 1.2f, TurretThickness / (transform.lossyScale.z), WellVis.transform.localScale.z / 1.2f);
            }
            


            WellExploEffect.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value) / transform.lossyScale.z);
            WellExploEffect.transform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z) * Mathf.Clamp(totalCaliber / 800, 0.3f, 1.5f);
            WellExploEffect.transform.eulerAngles = new Vector3(-90, WellExploEffect.transform.localEulerAngles.y, 0);
        }
        public void initLine()
        {
            if (transform.Find("line0"))
            {
                for (int i = 0; i < 4; i++)
                {
                    GunLine[i] = transform.Find("line" + i.ToString()).gameObject;
                    GunLine[i].SetActive(false);
                }
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    GunLine[i] = new GameObject("line" + i.ToString());
                    GunLine[i].transform.SetParent(gameObject.transform);
                    LineRenderer LR = GunLine[i].AddComponent<LineRenderer>();
                    LR.material = new Material(Shader.Find("Particles/Additive"));
                    LR.SetColors(Color.red, Color.blue);
                    LR.SetWidth(0.1f, 0.05f);
                    GunLine[i].SetActive(false);
                }
            }

        }
        public void InitVis()
        {
            if (transform.Find("WellArmourVis"))
            {
                WellVis = transform.Find("WellArmourVis").gameObject;
                WellVis.SetActive(true);
                AmmoVis = transform.Find("AmmoVis").gameObject;
                AmmoVis.SetActive(true);
                TurretVis = transform.Find("TurrentVis").gameObject;
                TurretVis.SetActive(true);

            }
            else
            {
                WellVis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.WellArmour, transform);
                WellVis.name = "WellArmourVis";
                //Destroy(WellVis.GetComponent<MeshCollider>());
                AmmoVis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.WellArmour, transform);
                AmmoVis.name = "AmmoVis";
                //Destroy(WellVis.GetComponent<MeshCollider>());
                TurretVis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.TurrentArmour, transform);
                TurretVis.name = "TurrentVis";
            }

            WellVis.transform.localPosition = new Vector3(0, 0, 0.5f);
            WellVis.transform.localScale = new Vector3(1f, 1f, 1f);
            WellVis.transform.localRotation = Quaternion.Euler(90, 0, 0);
            WellVisRender = WellVis.GetComponent<MeshRenderer>();
            WellVis.layer = 25;
            WellVis.SetActive(true);
            WellVisRender.material = AssetManager.Instance.TransparentMat;


            AmmoVis.transform.localPosition = new Vector3(0, 0, 0.5f);
            AmmoVis.transform.localScale = new Vector3(1f, 1f, 1f);
            AmmoVis.transform.localRotation = Quaternion.Euler(90, 0, 0);
            AmmoVisRender = AmmoVis.GetComponent<MeshRenderer>();
            AmmoVis.layer = 25;
            AmmoVis.SetActive(true);
            AmmoVisRender.material = AssetManager.Instance.TransparentMat;

            TurretVis.transform.localPosition = new Vector3(0, 0, 0.5f);
            TurretVis.transform.localScale = new Vector3(1f, 1f, 1f);
            TurretVis.transform.localRotation = Quaternion.Euler(90, 0, 0);
            TurrentVisRender = TurretVis.GetComponent<MeshRenderer>();
            TurretVis.layer = 25;
            TurretVis.SetActive(true);
            TurrentVisRender.material = AssetManager.Instance.TransparentMat;
        }
        public void InitEffect()
        {
            if (!transform.Find("WellExploEffect"))
            {
                WellExploEffect = (GameObject)Instantiate(AssetManager.Instance.WellEffect.WellExplo, transform);
                WellExploEffect.name = "WellExploEffect";
                WellExploEffect.transform.localPosition = Vector3.zero;
                WellExploEffect.transform.localEulerAngles = new Vector3(-90, 0, 0);
                WellExploEffect.transform.localScale = Vector3.one;
                WellExploEffect.SetActive(false);
            }
            else
            {
                WellExploEffect = transform.Find("WellExploEffect").gameObject;
                WellExploEffect.SetActive(false);
            }
        }
        public void SyncBulletHole()
        {
            foreach (WeaponMsgReceiver.hitHoleInfo info in WeaponMsgReceiver.Instance.BulletHoleInfo[myPlayerID][myGuid])
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
            WeaponMsgReceiver.Instance.BulletHoleInfo[myPlayerID][myGuid].Clear();
        }

        public virtual void SafeAwake()
        {
            Thickness = BB.AddSlider("WW2-Naval Thickness", "WW2Thickness", 20f, 10f, 650f);
            TurretSize = BB.AddSlider("Turret Size", "WW2TurretSize", 1f, 0.8f, 1.5f);
            Depth = BB.AddSlider("Cannon Well Depth", "WW2Depth", 3, 1, 6);
            Offset = BB.AddSlider("Cannon Well Offset", "WW2DepthOffset", 1f, 0f, 2f);
            AmmoResize = BB.AddSlider("Ammo Resize", "AmmoResize", 0f, -0.5f, 0.5f);
            GunGroup = BB.AddText("Gun Group", "GunGroup", "g0");
            UseWell = BB.AddToggle("Use Well", "UseWell", true);
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
            initialized = false;
            visInitialized = false;
            InitVis();
            initLine();
            InitEffect();
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
                //AdjustDepth();
            }
            try
            {
                if (StatMaster.isClient)
                {
                    WellMsgReceicer.Instance.WellExplo[myPlayerID].Add(myGuid, false);
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    WellMsgReceicer.Instance.AmmoExplo[myPlayerID].Add(myGuid, false);
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    WellMsgReceicer.Instance.WellPalsy[myPlayerID].Add(myGuid, false);
                }
            }
            catch { }
            try
            {
                if (StatMaster.isClient)
                {
                    WellMsgReceicer.Instance.TurrentPalsy[myPlayerID].Add(myGuid, false);
                }
            }
            catch { }

            //transform.Find("Shadow").gameObject.layer = 25;
        }
        public void Update()
        {
            if (ModController.Instance.ShowArmour)
            {
                ShowGroupLine();
            }
            else
            {
                foreach (GameObject line in GunLine)
                {
                    line.SetActive(false);
                }
            }
        }
        public void FixedUpdate()
        {
            DisableGun();

            if (ModController.Instance.state == myseed)
            {
                if (!WellVis)
                {
                    WellVis = transform.Find("WellArmourVis").gameObject;
                    WellVisRender = WellVis.GetComponent<MeshRenderer>();
                    AmmoVis = transform.Find("AmmoVis").gameObject;
                    AmmoVisRender = AmmoVis.GetComponent<MeshRenderer>();
                    TurretVis = transform.Find("TurrentVis").gameObject;
                    TurrentVisRender = TurretVis.GetComponent<MeshRenderer>();
                }

                thickness = Thickness.Value;
                

                if (ModController.Instance.ShowArmour)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    WellVisRender.material = AssetManager.Instance.ArmorMat[Mathf.Clamp((int)(thickness / 10f),0,65)];
                    AmmoVisRender.material = AssetManager.Instance.ArmorMat[1];
                    TurrentVisRender.material = AssetManager.Instance.TurrentMat[Mathf.Clamp(TurrentPalsyCount,0,7)];

                }
                else
                {
                    transform.Find("Vis").gameObject.SetActive(true);
                    WellVisRender.material = AssetManager.Instance.TransparentMat;
                    AmmoVisRender.material = AssetManager.Instance.TransparentMat;
                    TurrentVisRender.material = AssetManager.Instance.TransparentMat;
                }
                if (StatMaster.isClient && transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
                {
                    SyncBulletHole();
                }
                try
                {
                    AdjustPara();
                }
                catch { }

            }
            if (transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
            {
                if (!initialized)
                {
                    initialized = true;
                    SetReloadEfficiency(1, true);
                }
                else if (!visInitialized && initialized)
                {
                    visInitialized = true;
                    if (!UseWell.isDefaultValue)
                    {
                        WellVis.SetActive(false);
                        AmmoVis.SetActive(false);
                    }
                    else
                    {
                        WellVis.SetActive(true);
                        AmmoVis.SetActive(true);
                    }
                    AdjustPara();
                    if (gunNum == 0)
                    {
                        WellVis.SetActive(false);
                        AmmoVis.SetActive(false);
                        TurretVis.SetActive(false);
                    }
                }
                

                if (!StatMaster.isClient)
                {
                    DetectHitHost();
                }
                else
                {
                    DetectHitClient();
                }

            }
            else
            {
                if (ModController.Instance.state == myseed)
                {
                    if (!UseWell.isDefaultValue || totalCaliber == 0)
                    {
                        WellVis.SetActive(false);
                        AmmoVis.SetActive(false);
                    }
                    else
                    {
                        WellVis.SetActive(true);
                        AmmoVis.SetActive(true);
                    }
                }

            }



        }
    }
}