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

        public MSlider SmokeSize;
        public MSlider SmokeSpeed;
        public MSlider SmokeGravity;

        private bool preAsChimney;

        public float SizeCoeff = 1;
        public float SpeedCoeff = 1;

        public Horizon horizon;

        public void SetSmokeState(int state)
        {
            SizeCoeff = 1 + (state)*0.2f;
            SpeedCoeff = 1 + (state)*1f;

            SmokePS.startSpeed = SmokeSpeed.Value * SpeedCoeff;
            SmokePS.startSize = SmokeSize.Value * SizeCoeff;
            SmokePS.startColor = new Color(1, 1, 1, 0.05f + state * 0.1f);
        }


        public void ControlMapper()
        {
            if (preAsChimney != !AsChimney.isDefaultValue) {
                preAsChimney = !AsChimney.isDefaultValue;
                if (preAsChimney)
                {
                    SmokeSize.DisplayInMapper = true;
                    SmokeSpeed.DisplayInMapper= true;
                    SmokeGravity.DisplayInMapper = true;
                }
                else
                {
                    SmokeSize.DisplayInMapper = false;
                    SmokeSpeed.DisplayInMapper = false;
                    SmokeGravity.DisplayInMapper = false;
                }
            }
        }

        public void BuildUpdate()
        {
            ControlMapper();
        }

        public void SimulateUpdate()
        {
            if (!horizon)
            {
                horizon = GetComponent<Horizon>();
            }
            if (horizon.Show)
            {
                SmokePS.Play();
            }
            else
            {
                SmokePS.Stop();
            }
        }


        public virtual void SafeAwake()
        {
            AsChimney = BB.AddToggle("As Chimney", "AsChimney", false);
            SmokeSize = BB.AddSlider("Smoke Size", "WW2SmokeSize", 3f, 1f, 8f);
            SmokeSpeed = BB.AddSlider("Smoke Speed", "WW2SmokeSpeed", 3f, 0.2f, 5f);
            SmokeGravity = BB.AddSlider("Smoke Gravity", "WW2SmokeGravity", -0.05f, -0.1f, 0.1f);
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
            Smoke.SetActive(false);
            Smoke.transform.localPosition = Vector3.zero;
            Smoke.transform.localRotation = Quaternion.Euler(90,0,0);
            SmokePS = Smoke.GetComponent<ParticleSystem>();

            // simulate start
            if (BB.isSimulating && !AsChimney.isDefaultValue)
            {
                Smoke.SetActive(true);
                SmokePS.startSize = SmokeSize.Value;
                SmokePS.startSpeed = SmokeSpeed.Value;
                SmokePS.gravityModifier = SmokeGravity.Value;
                SmokePS.startColor = new Color(1, 1, 1, 0.05f);
            }
        }
        public void Update()
        {
            if (BB.isSimulating)
            {
                SimulateUpdate();
            }
            else
            {
                BuildUpdate();
            }
        }
    }
}
