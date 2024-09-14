using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Modding;
using UnityEngine;


namespace WW2NavalAssembly
{
    public class ShipSizeManager : SingleInstance<ShipSizeManager>
    {
        public override string Name { get; } = "Ship Size Manager";
        public class ShipSize
        {
            public Vector3 sizePos;
            public Vector3 sizeNeg;

            public float Volumn
            {
                get
                {
                    return forward == Vector3.zero? 0 : Mathf.Clamp(sizePos.x - sizeNeg.x, 2f, 999f) * Mathf.Clamp(Mathf.Sqrt(sizePos.y - sizeNeg.y), 2f, 999f) * Mathf.Pow(Mathf.Clamp(sizePos.z - sizeNeg.z, 1f, 999f), 1.5f);
                }
            }

            public Vector3 origin;
            public Vector3 forward;
            float DistThreshold = 100f;
            public ShipSize(Vector3 o)
            {
                origin = o;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }
            public ShipSize()
            {
                origin = Vector3.zero;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }

            public void AddDot(Vector3 o)
            {
                if(forward == Vector3.zero)
                {
                    //Debug.LogError("No captain");
                    return;
                }
                Vector3 pointer = o - origin;
                if (pointer.magnitude < DistThreshold)
                {
                    float halfLen = Vector3.Dot(forward, pointer);
                    float halfHeight = Vector3.Dot(Vector3.up, pointer);
                    float halfWidth = Vector3.Dot(Vector3.Cross(forward, Vector3.up).normalized, pointer);
                    sizePos.x = Mathf.Max(halfWidth, sizePos.x);
                    sizePos.y = Mathf.Max(halfHeight, sizePos.y);
                    sizePos.z = Mathf.Max(halfLen, sizePos.z);
                    sizeNeg.x = Mathf.Min(halfWidth, sizeNeg.x);
                    sizeNeg.y = Mathf.Min(halfHeight, sizeNeg.y);
                    sizeNeg.z = Mathf.Min(halfLen, sizeNeg.z);
                }
                //Debug.LogError(sizePos.ToString() + sizeNeg.ToString());
            }

            public void Reset()
            {
                origin = Vector3.zero;
                forward = Vector3.zero;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }
        }
        public ShipSize[] size = new ShipSize[16];

        public ShipSizeManager()
        {
            for (int i = 0; i < 16; i++)
            {
                size[i] = new ShipSize();
            }
        }



    }
}
