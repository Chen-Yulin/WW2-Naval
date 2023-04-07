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
    public class Chimney : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MToggle AsChimney;
        public GameObject Smoke;
        public ParticleSystem SmokePS;

        public virtual void SafeAwake()
        {
            AsChimney = BB.AddToggle("As Chimney", "AsChimney", false);
        }
        public void Awake()
        {
            BB = GetComponent<BlockBehaviour>();
            SafeAwake();
            if (BB.isSimulating) { return; }
        }
        public void Start()
        {
            Smoke = (GameObject)Instantiate(AssetManager.Instance.Chimney.ChimneySmoke, transform);
            Smoke.transform.localPosition = Vector3.zero;
            Smoke.transform.localRotation = Quaternion.Euler(90,0,0);
            Smoke.SetActive(false);
            SmokePS = Smoke.GetComponent<ParticleSystem>();
        }
        public void Update()
        {
            if (BB.isSimulating && !AsChimney.isDefaultValue)
            {
                Smoke.SetActive(true);
            }
            else
            {
                Smoke.SetActive(false);
            }

        }
    }
}
