using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;
using System.Runtime.InteropServices;

namespace WW2NavalAssembly
{
    class AABlock : BlockScript
    {
        public int myPlayerID;
        public int myseed = 0;
        public int myGuid;

        public MMenu Type;
        public int gunNum = 1;

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            Type = AddMenu("AAType", 0, new List<string>
            {
                "1x20mm",
                "3x25mm",
                "2x40mm",
                "4x40mm",
                "2x100mm",
                "2x127mm",
            });
        }

        public override void OnSimulateStart()
        {
            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            switch (Type.Value)
            {
                case 0:
                    gunNum = 1;
                    break;
                case 1:
                    gunNum = 3;
                    break;
                case 2:
                    gunNum = 2;
                    break;
                case 3:
                    gunNum = 4;
                    break;
                case 4:
                    gunNum = 2;
                    break;
                case 5:
                    gunNum = 2;
                    break;
                default:
                    gunNum = 1;
                    break;
            }
        }
    }
}
