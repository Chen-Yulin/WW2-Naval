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

namespace WW2NavalAssembly
{
    public class MathTool : SingleInstance<MathTool>
    {
        public override string Name { get; } = "MathTool";
        public float SignedAngle(Vector2 v1, Vector2 v2)
        {
            if (v1.x * v2.y - v1.y * v2.x < 0)
            {
                return -Vector2.Angle(v1, v2);
            }
            else
            {
                return Vector2.Angle(v1, v2);
            }
        }
    }
}
