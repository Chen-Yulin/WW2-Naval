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
using Modding.Blocks;

namespace WW2NavalAssembly
{
    internal class WaveMaker : MonoBehaviour
    {
        public BlockBehaviour BB { get; internal set; }
        public MToggle WaveToggle;
        public MSlider WaveSize;

        GameObject Wave;
        GameObject ShipWave;
        GameObject WaveParticle;

        public int frameCount;
        bool preUseWave;

        public Horizon horizon;

        public void UpdateWave()
        {
            if (!horizon)
            {
                horizon = GetComponent<Horizon>();
            }
            if (horizon.Show)
            {
                Vector3 position = Wave.transform.position;
                ShipWave.transform.position = new Vector3(position.x, 20.05f, position.z);
                ShipWave.transform.eulerAngles = Vector3.zero;
                if (!Wave.activeSelf)
                {
                    Wave.SetActive(true);
                }
            }
            else
            {
                if (Wave.activeSelf)
                {
                    Wave.SetActive(false);
                }
            }
            
        }
        public void InitWave(float size)
        {
            if (transform.Find("Wave"))
            {
                Wave = transform.Find("Wave").gameObject;
                ShipWave = Wave.transform.Find("shipWave").gameObject;
            }
            else
            {
                Wave = new GameObject("Wave");
                Wave.transform.SetParent(transform);
                switch (BB.BlockID)
                {
                    case (int)BlockType.SingleWoodenBlock:
                        Wave.transform.localPosition = Vector3.forward*0.5f;
                        break;
                    case (int)BlockType.DoubleWoodenBlock:
                        Wave.transform.localPosition = Vector3.forward * 1f;
                        break;
                    case (int)BlockType.Log:
                        Wave.transform.localPosition = Vector3.forward * 1.5f;
                        break;
                    default:
                        break;
                }
                
                Wave.transform.localEulerAngles = Vector3.zero;
                Wave.transform.localScale = new Vector3(1/transform.localScale.x, 1/transform.localScale.y, 1/transform.localScale.z);

                ShipWave = (GameObject)Instantiate(AssetManager.Instance.TorpedoTrail.ShipWave, Wave.transform);
                ShipWave.name = "shipWave";
                ShipWave.transform.localPosition = Vector3.zero;
                ShipWave.transform.localEulerAngles = Vector3.zero;
                ShipWave.transform.localScale = Vector3.one;
            }
            WaveParticle = ShipWave.transform.Find("particle").gameObject;
            WaveParticle.transform.localScale = Vector3.one * size;
        }

        public void UpdateMapper()
        {
            if (preUseWave == WaveToggle.isDefaultValue)
            {
                preUseWave = !preUseWave;
                if (preUseWave)
                {
                    WaveSize.DisplayInMapper = true;
                }
                else
                {
                    WaveSize.DisplayInMapper = false;
                }
            }
        }

        public void BuildUpdate()
        {
            UpdateMapper();
        }
        public void SimulateUpdate()
        {
            
            if (!WaveToggle.isDefaultValue)
            {
                if (frameCount < 5)
                {
                    
                    frameCount ++;
                }
                else if (frameCount == 5)
                {
                    WaveParticle.SetActive(true);
                    frameCount++;
                }
                UpdateWave();
            }
            
        }

        public virtual void SafeAwake()
        {
            WaveToggle = BB.AddToggle("Wave Toggle", "WW2WaveToggle", false);
            WaveSize = BB.AddSlider("Wave Size", "WaveSize", 3f, 2f, 6f);
        }
        public void Awake()
        {
            BB = GetComponent<BlockBehaviour>();
            SafeAwake();
            frameCount = 0;
            if (BB.isSimulating) { return; }
        }
        public void Start()
        {
            preUseWave = WaveToggle.isDefaultValue;
            if (!BB)
            {
                BB = GetComponent<BlockBehaviour>();
            }
            if (BB.isSimulating && !WaveToggle.isDefaultValue)
            {
                InitWave(WaveSize.Value);
                WaveParticle.SetActive(false);
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
