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
using Navalmod;

namespace WW2NavalAssembly
{

    class CustomBlockController : SingleInstance<CustomBlockController>
    {

        public override string Name { get; } = "Custom Block Controller";

        internal PlayerMachineInfo PMI;

        private void Awake()
        {

            //加载配置
            //Events.OnMachineLoaded += LoadConfiguration;
            Events.OnMachineLoaded += (pmi) => { PMI = pmi; };
            ////储存配置
            //Events.OnMachineSave += SaveConfiguration;
            //添加零件初始化事件委托
            Events.OnBlockInit += AddSliders;

        }
        private void AddSliders(Block block)
        {

            BlockBehaviour blockbehaviour = block.BuildingBlock.InternalObject;
            AddSliders(blockbehaviour);
        }
        private void AddSliders(BlockBehaviour block)
        {
            if (block.GetComponent<H3NetworkBlock>() == null) {
                block.gameObject.AddComponent<H3NetworkBlock>().blockBehaviour = block;
            }
            //if (StatMaster.isMP == StatMaster.IsLevelEditorOnly)
            switch (block.BlockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    {
                        if (block.gameObject.GetComponent(typeof(WoodenArmour)) == null)
                            block.gameObject.AddComponent(typeof(WoodenArmour));
                        if (block.gameObject.GetComponent(typeof(WWIIUnderWater)) == null)
                            block.gameObject.AddComponent(typeof(WWIIUnderWater));
                        if (block.gameObject.GetComponent(typeof(FlightDeck)) == null)
                            block.gameObject.AddComponent(typeof(FlightDeck));
                        if (block.gameObject.GetComponent(typeof(WaveMaker)) == null)
                            block.gameObject.AddComponent(typeof(WaveMaker));
                        break;
                    }
                case (int)BlockType.DoubleWoodenBlock:
                    {
                        if (block.gameObject.GetComponent(typeof(WoodenArmour)) == null)
                            block.gameObject.AddComponent(typeof(WoodenArmour));
                        if (block.gameObject.GetComponent(typeof(WWIIUnderWater)) == null)
                            block.gameObject.AddComponent(typeof(WWIIUnderWater));
                        if (block.gameObject.GetComponent(typeof(FlightDeck)) == null)
                            block.gameObject.AddComponent(typeof(FlightDeck));
                        if (block.gameObject.GetComponent(typeof(WaveMaker)) == null)
                            block.gameObject.AddComponent(typeof(WaveMaker));
                        break;
                    }
                case (int)BlockType.Log:
                    {
                        if (block.gameObject.GetComponent(typeof(WoodenArmour)) == null)
                            block.gameObject.AddComponent(typeof(WoodenArmour));
                        if (block.gameObject.GetComponent(typeof(WWIIUnderWater)) == null)
                            block.gameObject.AddComponent(typeof(WWIIUnderWater));
                        if (block.gameObject.GetComponent(typeof(FlightDeck)) == null)
                            block.gameObject.AddComponent(typeof(FlightDeck));
                        if (block.gameObject.GetComponent(typeof(WaveMaker)) == null)
                            block.gameObject.AddComponent(typeof(WaveMaker));
                        break;
                    }
                case (int)BlockType.Rocket:
                    {
                        if (block.gameObject.GetComponent(typeof(DefaultArmour)) == null)
                            block.gameObject.AddComponent(typeof(DefaultArmour));
                        if (block.gameObject.GetComponent(typeof(Chimney)) == null)
                            block.gameObject.AddComponent(typeof(Chimney));
                        break;
                    }
                case (int)BlockType.SteeringHinge:
                    {
                        if (block.gameObject.GetComponent(typeof(WW2Hinge)) == null)
                        {
                            block.gameObject.AddComponent(typeof(WW2Hinge));
                        }
                        break;
                    }
                case (int)BlockType.SpinningBlock:
                    {
                        if (block.gameObject.GetComponent(typeof(CannonWell)) == null)
                        {
                            block.gameObject.AddComponent(typeof(CannonWell));
                        }
                        break;
                    }
                case (int)BlockType.SqrBalloon:
                    {
                        if (block.gameObject.GetComponent(typeof(BalloonLife)) == null)
                        {
                            block.gameObject.AddComponent(typeof(BalloonLife));
                        }
                        break;
                    }
                case (int)BlockType.Brace:
                    {
                        if (block.gameObject.GetComponent(typeof(DefaultArmour)) == null)
                            block.gameObject.AddComponent(typeof(DefaultArmour));
                        if (block.gameObject.GetComponent(typeof(BraceOptimizer)) == null)
                            block.gameObject.AddComponent(typeof(BraceOptimizer));
                        break;
                    }
                case (int)BlockType.BuildNode:
                    {
                        break;
                    }
                case (int)BlockType.BuildEdge:
                    {
                        break;
                    }
                default:
                    {
                        if (block.gameObject.GetComponent(typeof(DefaultArmour)) == null)
                            block.gameObject.AddComponent(typeof(DefaultArmour));
                        break;
                    }
            }
        }
    }
}
