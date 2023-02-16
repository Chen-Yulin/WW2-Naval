using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Modding;

namespace WW2NavalAssembly
{
    class AircraftAssetManager : SingleInstance<AircraftAssetManager>
    {
        public override string Name { get; } = "Asset Manager";

        public Mesh Zero_0_Mesh;
        public Mesh Zero_1_Mesh;
        public Mesh Zero_2_Mesh;

        public Texture Zero_0_Tex;
        public Texture Zero_1_Tex;
        public Texture Zero_2_Tex;

        public Mesh F4U_0_Mesh;
        public Mesh F4U_1_Mesh;
        public Mesh F4U_2_Mesh;

        public Texture F4U_0_Tex;
        public Texture F4U_1_Tex;
        public Texture F4U_2_Tex;

        public float Zero_Offset = 0.48f;
        public float F4U_Offset = 0.76f;

        public AircraftAssetManager()
        {
            Zero_0_Mesh = ModResource.GetMesh("A-Zero-0 Mesh").Mesh;
            Zero_1_Mesh = ModResource.GetMesh("A-Zero-1 Mesh").Mesh;
            Zero_2_Mesh = ModResource.GetMesh("A-Zero-2 Mesh").Mesh;
            Zero_0_Tex = ModResource.GetTexture("A-Zero-0 Texture").Texture;
            Zero_1_Tex = ModResource.GetTexture("A-Zero-1 Texture").Texture;
            Zero_2_Tex = ModResource.GetTexture("A-Zero-2 Texture").Texture;
            F4U_0_Mesh = ModResource.GetMesh("A-F4U-0 Mesh").Mesh;
            F4U_1_Mesh = ModResource.GetMesh("A-F4U-1 Mesh").Mesh;
            F4U_2_Mesh = ModResource.GetMesh("A-F4U-2 Mesh").Mesh;
            F4U_0_Tex = ModResource.GetTexture("A-F4U-0 Texture").Texture;
            F4U_1_Tex = ModResource.GetTexture("A-F4U-1 Texture").Texture;
            F4U_2_Tex = ModResource.GetTexture("A-F4U-2 Texture").Texture;
        }

        public Mesh GetMesh0(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_0_Mesh;
                case "F4U":
                    return F4U_0_Mesh;
                default:
                    return Zero_0_Mesh;
            }
        }
        public Mesh GetMesh1(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_1_Mesh;
                case "F4U":
                    return F4U_1_Mesh;
                default:
                    return Zero_1_Mesh;
            }
        }
        public Mesh GetMesh2(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_2_Mesh;
                case "F4U":
                    return F4U_2_Mesh;
                default:
                    return Zero_2_Mesh;
            }
        }
        public Texture GetTex0(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_0_Tex;
                case "F4U":
                    return F4U_0_Tex;
                default:
                    return Zero_0_Tex;
            }
        }
        public Texture GetTex1(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_1_Tex;
                case "F4U":
                    return F4U_1_Tex;
                default:
                    return Zero_1_Tex;
            }
        }
        public Texture GetTex2(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_2_Tex;
                case "F4U":
                    return F4U_2_Tex;
                default:
                    return Zero_2_Tex;
            }
        }
        public float GetOffset(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_Offset;
                case "F4U":
                    return F4U_Offset;
                default:
                    return Zero_Offset;
            }
        }


    }
}
