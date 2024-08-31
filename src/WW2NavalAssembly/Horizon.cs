using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace WW2NavalAssembly
{
    public class HorizonManager : SingleInstance<HorizonManager>
    {
        public override string Name { get; } = "HorizonManager";
        public bool[][] VisibleToController = new bool[16][];
        public bool[][] VisibleToAircraft = new bool[16][];

        public HorizonManager()
        {
            for (int i = 0; i < 16; i++)
            {
                VisibleToController[i] = new bool[16];
                VisibleToAircraft[i] = new bool[16];
            }
        }

        public bool isVisble(int watcher, int target)
        {
            return VisibleToController[watcher][target] || VisibleToAircraft[watcher][target] || ControllerDataManager.Instance.ControllerObject[watcher] == null;
        }

        public void SetVisibleToAll(int player)
        {
            for (int i = 0; i < 16; i++)
            {
                VisibleToController[i][player] = true;
            }
        }
        public void CanSeeAll(int player)
        {
            for (int i = 0; i < 16; i++)
            {
                VisibleToController[player][i] = true;
            }
        }

        public void ClearAircraftVisible(int player)
        {
            for (int i = 0; i < 16; i++)
            {
                VisibleToAircraft[player][i] = false;
            }
        }


    }
    public class Horizon : MonoBehaviour
    {
        public bool wood = false;
        public bool halfVis = false;
        public BlockBehaviour bb;
        bool Enabled = false;
        public bool _show = true;
        public bool Findinactive = false;

        public int myPlayerID;
        public int watcherID;
        public int myseed;

        float AircraftDist = 1000f;

        int frame = 0;

        public bool isAircraft;
        public Aircraft aircraft;


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

        
        void Awake()
        {
            

        }
        void Start()
        {
            
            bb = GetComponent<BlockBehaviour>();
            if (bb.BlockID == (int)BlockType.Grabber)
            {
                Destroy(transform.Find("Joint").GetComponent<MeshRenderer>());
            }
            myPlayerID = bb.ParentMachine.PlayerID;
            if (StatMaster.isMP)
            {
                watcherID = PlayerData.localPlayer.networkId;
            }
            else
            {
                watcherID = 0;
            }
            myseed = (int)(UnityEngine.Random.value * 39);
            _show = true;
            isAircraft = GetComponent<Aircraft>() != null;
            aircraft = GetComponent<Aircraft>();
            Enabled = bb.isSimulating && myPlayerID != watcherID;
            if (Enabled)
            {
                if (bb.BlockID == (int)BlockType.Log || bb.BlockID == (int)BlockType.DoubleWoodenBlock)
                {
                    wood = true;
                    halfVis = !transform.Find("Vis").GetComponent<MeshRenderer>().enabled;
                }
            }
        }
        void OnDestroy()
        {
        }
        void FixedUpdate()
        {
            if (Enabled)
            {
                if (myseed == ModController.Instance.state)
                {
                    if (isAircraft && aircraft.isFlying)
                    {
                        Show = MathTool.DistFromWatcherAircraft(watcherID, transform) < AircraftDist || MathTool.DistFromWatcher(watcherID, transform) < AircraftDist;
                    }
                    else
                    {
                        Show = HorizonManager.Instance.isVisble(watcherID, myPlayerID);
                    }
                }
            }
            else
            {
                Enabled = bb.isSimulating && myPlayerID != watcherID;
                if (Enabled)
                {
                    if (bb.BlockID == (int)BlockType.Log || bb.BlockID == (int)BlockType.DoubleWoodenBlock)
                    {
                        wood = true;
                        halfVis = !transform.Find("Vis").GetComponent<MeshRenderer>().enabled;
                    }
                }
            }
        }

    }
}
