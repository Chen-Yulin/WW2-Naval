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
        public WellMsgReceicer()
        {
            for (int i = 0; i < 16; i++)
            {
                WellExplo[i] = new Dictionary<int, bool>();
                AmmoExplo[i] = new Dictionary<int, bool>();
                WellPalsy[i] = new Dictionary<int, bool>();
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
            else
            {
                WellPalsy[(int)msg.GetData(0)][(int)msg.GetData(1)] = true;
            }
        }

    }
    public class CannonWell : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MSlider Thickness;
        public MSlider Depth;
        public MSlider Offset;
        public MText GunGroup;
        public float thickness;
        public GameObject WellVis;
        GameObject AmmoVis;
        MeshRenderer WellVisRender;
        MeshRenderer AmmoVisRender;

        public int myseed;
        public int myPlayerID;
        public int myGuid;

        public bool Wellpalsy;
        public bool AmmoExplo;
        public bool WellExplo;
        public float myCaliber = 0;
        public float totalCaliber = 0;
        public float gunNum = 0;
        public bool exploded = false;
        public bool disableGun = false;

        GameObject[] GunLine = new GameObject[4];

        GameObject WellExploEffect;
        GameObject AmmoExploEffect;

        public void AmmoExploforce()
        {
            Collider[] explo = Physics.OverlapSphere(AmmoVis.transform.position, Mathf.Sqrt(myCaliber)*2);
            foreach (Collider collider in explo)
            {
                try
                {
                    collider.transform.parent.GetComponent<Rigidbody>().AddExplosionForce(Mathf.Sqrt(myCaliber)*300,AmmoVis.transform.position, Mathf.Sqrt(myCaliber) * 2);
                }
                catch { }
            }
        }
        public void WellExploForce()
        {
            Collider[] turrent = Physics.OverlapSphere(transform.position + (transform.localScale.z+0.2f) * transform.forward, totalCaliber/800);
            foreach (Collider collider in turrent)
            {
                try
                {
                    collider.transform.parent.GetComponent<Rigidbody>().AddForce(transform.forward * totalCaliber * 30);
                }
                catch { }
            }
            gameObject.GetComponent<Rigidbody>().AddForce(-transform.forward * totalCaliber * 200);
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
            if (WellExplo)
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
                    AmmoExploEffect.transform.rotation = Quaternion.identity;
                    AmmoExploEffect.transform.localScale = Vector3.one * Mathf.Sqrt(myCaliber) / 20;
                    AmmoExploEffect.SetActive(true);
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
            if (WellMsgReceicer.Instance.WellExplo[myPlayerID][myGuid])
            {
                disableGun = true;
                WellMsgReceicer.Instance.WellExplo[myPlayerID][myGuid] = false;
                WellExploEffect.SetActive(true);
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
                    AmmoExploEffect.transform.rotation = Quaternion.identity;
                    AmmoExploEffect.transform.localScale = Vector3.one * Mathf.Sqrt(myCaliber) / 20;
                    AmmoExploEffect.SetActive(true);
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
            WellVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z-Offset.Value-Depth.Value/2) / transform.lossyScale.z);
            
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
                WellWidth = Mathf.Clamp(myCaliber * numCoeff / 5,0.3f,4);
            }
            else
            {
                WellWidth = 0;
            }
            

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
            AmmoVis.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value - Depth.Value - AmmoThickness/2) / transform.lossyScale.z);
            AmmoVis.transform.localScale = new Vector3(totalCaliber / (450 * transform.lossyScale.x)*1.1f, AmmoThickness / (2 * transform.lossyScale.z), totalCaliber / (450 * transform.lossyScale.y)*1.1f);


            
            WellExploEffect.transform.localPosition = new Vector3(0, 0, (transform.localScale.z - Offset.Value) / transform.lossyScale.z);
            WellExploEffect.transform.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.z, 1f / transform.lossyScale.x) * totalCaliber/400;
            WellExploEffect.transform.rotation = Quaternion.identity;
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
            }
            else
            {
                WellVis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.WellArmour, transform);
                WellVis.name = "WellArmourVis";
                AmmoVis = (GameObject)Instantiate(AssetManager.Instance.ArmourVis.WellArmour, transform);
                AmmoVis.name = "AmmoVis";
            }

            WellVis.transform.localPosition = new Vector3(0, 0, 0.5f);
            WellVis.transform.localScale = new Vector3(1f, 1f, 1f);
            WellVis.transform.localRotation = Quaternion.Euler(90,0,0);
            WellVisRender = WellVis.GetComponent<MeshRenderer>();
            WellVis.layer = 25;
            WellVis.SetActive(true);
            Color tmpColor1 = WellVisRender.material.color;
            WellVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);


            AmmoVis.transform.localPosition = new Vector3(0, 0, 0.5f);
            AmmoVis.transform.localScale = new Vector3(1f, 1f, 1f);
            AmmoVis.transform.localRotation = Quaternion.Euler(90, 0, 0);
            AmmoVisRender = AmmoVis.GetComponent<MeshRenderer>();
            AmmoVis.layer = 25;
            AmmoVis.SetActive(true);
            tmpColor1 = AmmoVisRender.material.color;
            AmmoVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);
        }
        public void InitEffect()
        {
            if (!transform.Find("WellExploEffect"))
            {
                WellExploEffect = (GameObject)Instantiate(AssetManager.Instance.WellEffect.WellExplo, transform);
                WellExploEffect.name = "WellExploEffect";
                WellExploEffect.transform.localPosition = Vector3.zero;
                WellExploEffect.transform.rotation = Quaternion.identity;
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
            Depth = BB.AddSlider("Cannon Well Depth", "WW2Depth", 3, 1, 6);
            Offset = BB.AddSlider("Cannon Well Offset", "WW2DepthOffset", 1f, 0f, 2f);
            GunGroup = BB.AddText("Gun Group", "GunGroup", "g0");
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

            //transform.Find("Shadow").gameObject.layer = 25;
        }
        public void Update()
        {
            if (ModController.Instance.showArmour)
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
                }

                thickness = Thickness.Value;
                Color tmpColor = Color.HSVToRGB(Mathf.Clamp(0.5f - thickness / 1000, 0, 0.5f), 1, 1);
                WellVisRender.material.color = new Color(tmpColor.r, tmpColor.g, tmpColor.b, 0.6f);
                tmpColor = Color.HSVToRGB(Mathf.Clamp(0.5f - 13 / 1000, 0, 0.5f), 1, 1);
                AmmoVisRender.material.color = new Color(tmpColor.r, tmpColor.g, tmpColor.b, 0.6f);

                if (ModController.Instance.showArmour)
                {
                    transform.Find("Vis").gameObject.SetActive(false);
                    Color tmpColor1 = WellVisRender.material.color;
                    WellVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0.6f);
                    tmpColor1 = AmmoVisRender.material.color;
                    AmmoVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0.6f);

                }
                else
                {
                    transform.Find("Vis").gameObject.SetActive(true);
                    Color tmpColor1 = WellVisRender.material.color;
                    WellVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);
                    tmpColor1 = AmmoVisRender.material.color;
                    AmmoVisRender.material.color = new Color(tmpColor1.r, tmpColor1.g, tmpColor1.b, 0f);
                }
                if (StatMaster.isClient && transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
                {
                    SyncBulletHole();
                }
                AdjustPara ();
            }
            if (transform.gameObject.GetComponent<BlockBehaviour>().isSimulating)
            {
                if (!StatMaster.isClient)
                {
                    DetectHitHost();
                }
                else
                {
                    DetectHitClient();
                }
                
            }



        }
    }
}
