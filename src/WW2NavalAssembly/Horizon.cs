using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;

namespace WW2NavalAssembly
{
    public class Horizon : MonoBehaviour
    {
        public bool wood = false;
        public bool halfVis = false;
        public BlockBehaviour bb;
        bool Enabled = false;
        bool _show = true;
        public bool Findinactive = false;

        float AircraftDist = 1000f;

        int i = 0;


        public bool Show
        {
            get { return _show; }
            set
            {
                if (_show != value)
                {
                    _show = value;
                    ChangeBlockVisible(bb);
                }
            }
        }
        public void ChangeBlockVisible2(BlockBehaviour BB)
        {
            foreach (var renderer in BB.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.name == "CubeColliders")
                {
                    continue;
                }
                if (wood)
                {
                    if (halfVis)
                    {
                        if (renderer.name == "HalVis")
                        {
                            renderer.enabled = _show;
                        }
                        else if (renderer.name == "Vis")
                        {
                            renderer.enabled = false;
                        }
                        else
                        {
                            renderer.enabled = _show;
                        }
                    }
                    else
                    {
                        if (renderer.name == "HalVis")
                        {
                            renderer.enabled = false;
                        }
                        else if (renderer.name == "Vis")
                        {
                            renderer.enabled = _show;
                        }
                        else
                        {
                            renderer.enabled = _show;
                        }
                    }
                }
                else
                {
                    renderer.enabled = _show;
                }


            }
        }
        public void ChangeBlockVisible(BlockBehaviour BB)
        {
            if (BB.GetComponent<WoodenArmour>())
            {
                BB.GetComponent<WoodenArmour>().UpdateVis(ModController.Instance.ShowArmour);
            }
            else if (BB.GetComponent<DefaultArmour>())
            {
                BB.GetComponent<DefaultArmour>().UpdateVis(ModController.Instance.ShowArmour);
            }
            ChangeBlockVisible2(BB);
        }

        public GameObject controller;

        public float GetHorizon()
        {
            float radius = 6710000f;
            float height = Mathf.Clamp((controller.transform.position.y - Constants.SeaHeight), 0, 100) * 5;
            float horizonDist = Mathf.Sqrt(Mathf.Pow(height + radius,2)- Mathf.Pow(radius, 2))/10f;
            horizonDist = Mathf.Clamp(horizonDist, 100f, 100000f);
            //Debug.Log(horizonDist);
            return horizonDist;
        }

        public float GetDist()
        {
            return MathTool.Get2DDistance(controller.transform.position, transform.position);
        }

        public float GetAircraftDist()
        {
            float dist = float.MaxValue;

            int playerID = 0;
            if (StatMaster.isMP)
            {
                playerID = PlayerData.localPlayer.networkId;
            }

            foreach (var a in Grouper.Instance.GetLeaders(playerID))
            {
                Aircraft aircraft = a.Value.Value;
                float d = MathTool.Get2DDistance(transform.position, aircraft.transform.position);
                if (d<dist)
                {
                    dist = d;
                }
            }
            return dist;
        }
        void Start()
        {
            
        }
        void Update()
        {
            if (i<10)
            {
                i++;
            }
            else if (i == 10)
            {
                Findinactive = bb.BlockID == (int)BlockType.SpinningBlock;
                Enabled = bb.isSimulating;
                if (Enabled)
                {
                    try
                    {
                        if (StatMaster.isMP)
                        {
                            controller = ControllerDataManager.Instance.ControllerObject[PlayerData.localPlayer.networkId];
                        }
                        else
                        {
                            controller = ControllerDataManager.Instance.ControllerObject[0];
                        }
                    }
                    catch
                    {
                        Enabled = false;
                    }
                    if (bb.BlockID == (int)BlockType.Log || bb.BlockID == (int)BlockType.DoubleWoodenBlock)
                    {
                        wood = true;
                        halfVis = !transform.Find("Vis").GetComponent<MeshRenderer>().enabled;
                    }
                }
                i++;
            }
            else
            {
                if (Enabled && controller)
                {
                    Show = GetHorizon() > GetDist();
                    if (!Show)
                    {
                        Show = AircraftDist > GetAircraftDist();
                    }
                }
                if (!controller)
                {
                    try
                    {
                        if (StatMaster.isMP)
                        {
                            controller = ControllerDataManager.Instance.ControllerObject[PlayerData.localPlayer.networkId];
                        }
                        else
                        {
                            controller = ControllerDataManager.Instance.ControllerObject[0];
                        }
                    }
                    catch
                    {
                    }
                }
            }
            
        }
    }
}
