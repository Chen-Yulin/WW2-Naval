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

        //=======================Zero=======================
        public Mesh Zero_0_Mesh;
        public Mesh Zero_05_Mesh;
        public Mesh Zero_1_Mesh;
        public Mesh Zero_2_Mesh;

        public Texture Zero_0_Tex;
        public Texture Zero_05_Tex;
        public Texture Zero_1_Tex;
        public Texture Zero_2_Tex;

        //=======================R99=======================
        public Mesh R99_0_Mesh;
        public Mesh R99_05_Mesh;
        public Mesh R99_1_Mesh;
        public Mesh R99_2_Mesh;

        public Texture R99_0_Tex;
        public Texture R99_05_Tex;
        public Texture R99_1_Tex;
        public Texture R99_2_Tex;

        //=======================B7A2=======================
        public Mesh B7A2_0_Mesh;
        public Mesh B7A2_05_Mesh;
        public Mesh B7A2_1_Mesh;
        public Mesh B7A2_2_Mesh;

        public Texture B7A2_0_Tex;
        public Texture B7A2_05_Tex;
        public Texture B7A2_1_Tex;
        public Texture B7A2_2_Tex;

        //=======================F4U=======================
        public Mesh F4U_0_Mesh;
        public Mesh F4U_05_Mesh;
        public Mesh F4U_1_Mesh;
        public Mesh F4U_2_Mesh;

        public Texture F4U_0_Tex;
        public Texture F4U_05_Tex;
        public Texture F4U_1_Tex;
        public Texture F4U_2_Tex;

        //=======================SBD=======================
        public Mesh SBD_0_Mesh;
        public Mesh SBD_05_Mesh;
        public Mesh SBD_1_Mesh;
        public Mesh SBD_2_Mesh;

        public Texture SBD_0_Tex;
        public Texture SBD_05_Tex;
        public Texture SBD_1_Tex;
        public Texture SBD_2_Tex;

        //=======================SB2C=======================
        public Mesh SB2C_0_Mesh;
        public Mesh SB2C_05_Mesh;
        public Mesh SB2C_1_Mesh;
        public Mesh SB2C_2_Mesh;

        public Texture SB2C_0_Tex;
        public Texture SB2C_05_Tex;
        public Texture SB2C_1_Tex;
        public Texture SB2C_2_Tex;

        public float Zero_Prop_Offset = 1.35f;
        public float B7A2_Prop_Offset = 2.3f;
        public float R99_Prop_Offset = 2.08f;
        public float F4U_Prop_Offset = 2.38f;
        public float SBD_Prop_Offset = 1.35f;
        public float SB2C_Prop_Offset = 2.3f;

        public Vector3 Zero_Body_Offset = new Vector3(0, 0.85f, 0);
        public Vector3 R99_Body_Offset = new Vector3(0, 0.82f, -0.14f);
        public Vector3 B7A2_Body_Offset = new Vector3(0, 0.41f, -0.22f);
        public Vector3 F4U_Body_Offset = new Vector3(0, 0.55f, -0.14f);
        public Vector3 SBD_Body_Offset = new Vector3(0, 0.74f, 0f);
        public Vector3 SB2C_Body_Offset = new Vector3(0, 0.52f, -0.2f);


        public AircraftAssetManager()
        {
            Zero_0_Mesh = ModResource.GetMesh("A-Zero-0 Mesh").Mesh;
            Zero_05_Mesh = ModResource.GetMesh("A-Zero-0.5 Mesh").Mesh;
            Zero_1_Mesh = ModResource.GetMesh("A-Zero-1 Mesh").Mesh;
            Zero_2_Mesh = ModResource.GetMesh("A-Zero-2 Mesh").Mesh;
            Zero_0_Tex = ModResource.GetTexture("A-Zero-0 Texture").Texture;
            Zero_05_Tex = ModResource.GetTexture("A-Zero-0.5 Texture").Texture;
            Zero_1_Tex = ModResource.GetTexture("A-Zero-1 Texture").Texture;
            Zero_2_Tex = ModResource.GetTexture("A-Zero-2 Texture").Texture;

            R99_0_Mesh = ModResource.GetMesh("A-R99-0 Mesh").Mesh;
            R99_05_Mesh = ModResource.GetMesh("A-R99-0.5 Mesh").Mesh;
            R99_1_Mesh = ModResource.GetMesh("A-R99-1 Mesh").Mesh;
            R99_2_Mesh = ModResource.GetMesh("A-R99-2 Mesh").Mesh;
            R99_0_Tex = ModResource.GetTexture("A-R99-0 Texture").Texture;
            R99_05_Tex = ModResource.GetTexture("A-R99-0.5 Texture").Texture;
            R99_1_Tex = ModResource.GetTexture("A-R99-1 Texture").Texture;
            R99_2_Tex = ModResource.GetTexture("A-R99-2 Texture").Texture;

            B7A2_0_Mesh = ModResource.GetMesh("A-B7A2-0 Mesh").Mesh;
            B7A2_05_Mesh = ModResource.GetMesh("A-B7A2-0.5 Mesh").Mesh;
            B7A2_1_Mesh = ModResource.GetMesh("A-B7A2-1 Mesh").Mesh;
            B7A2_2_Mesh = ModResource.GetMesh("A-B7A2-2 Mesh").Mesh;
            B7A2_0_Tex = ModResource.GetTexture("A-B7A2-0 Texture").Texture;
            B7A2_05_Tex = ModResource.GetTexture("A-B7A2-0.5 Texture").Texture;
            B7A2_1_Tex = ModResource.GetTexture("A-B7A2-1 Texture").Texture;
            B7A2_2_Tex = ModResource.GetTexture("A-B7A2-2 Texture").Texture;

            F4U_0_Mesh = ModResource.GetMesh("A-F4U-0 Mesh").Mesh;
            F4U_05_Mesh = ModResource.GetMesh("A-F4U-0.5 Mesh").Mesh;
            F4U_1_Mesh = ModResource.GetMesh("A-F4U-1 Mesh").Mesh;
            F4U_2_Mesh = ModResource.GetMesh("A-F4U-2 Mesh").Mesh;
            F4U_0_Tex = ModResource.GetTexture("A-F4U-0 Texture").Texture;
            F4U_05_Tex = ModResource.GetTexture("A-F4U-0.5 Texture").Texture;
            F4U_1_Tex = ModResource.GetTexture("A-F4U-1 Texture").Texture;
            F4U_2_Tex = ModResource.GetTexture("A-F4U-2 Texture").Texture;

            SBD_0_Mesh = ModResource.GetMesh("A-SBD-0 Mesh").Mesh;
            SBD_05_Mesh = ModResource.GetMesh("A-SBD-0.5 Mesh").Mesh;
            SBD_1_Mesh = ModResource.GetMesh("A-SBD-1 Mesh").Mesh;
            SBD_2_Mesh = ModResource.GetMesh("A-SBD-2 Mesh").Mesh;
            SBD_0_Tex = ModResource.GetTexture("A-SBD-0 Texture").Texture;
            SBD_05_Tex = ModResource.GetTexture("A-SBD-0.5 Texture").Texture;
            SBD_1_Tex = ModResource.GetTexture("A-SBD-1 Texture").Texture;
            SBD_2_Tex = ModResource.GetTexture("A-SBD-2 Texture").Texture;

            SB2C_0_Mesh = ModResource.GetMesh("A-SB2C-0 Mesh").Mesh;
            SB2C_05_Mesh = ModResource.GetMesh("A-SB2C-0.5 Mesh").Mesh;
            SB2C_1_Mesh = ModResource.GetMesh("A-SB2C-1 Mesh").Mesh;
            SB2C_2_Mesh = ModResource.GetMesh("A-SB2C-2 Mesh").Mesh;
            SB2C_0_Tex = ModResource.GetTexture("A-SB2C-0 Texture").Texture;
            SB2C_05_Tex = ModResource.GetTexture("A-SB2C-0.5 Texture").Texture;
            SB2C_1_Tex = ModResource.GetTexture("A-SB2C-1 Texture").Texture;
            SB2C_2_Tex = ModResource.GetTexture("A-SB2C-2 Texture").Texture;
        }

        public Mesh GetMesh0(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_0_Mesh;
                case "F4U":
                    return F4U_0_Mesh;
                case "99":
                    return R99_0_Mesh;
                case "B7A2":
                    return B7A2_0_Mesh;
                case "SBD":
                    return SBD_0_Mesh;
                case "SB2C":
                    return SB2C_0_Mesh;
                default:
                    return Zero_0_Mesh;
            }
        }
        public Mesh GetMesh05(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_05_Mesh;
                case "F4U":
                    return F4U_05_Mesh;
                case "99":
                    return R99_05_Mesh;
                case "B7A2":
                    return B7A2_05_Mesh;
                case "SBD":
                    return SBD_05_Mesh;
                case "SB2C":
                    return SB2C_05_Mesh;
                default:
                    return Zero_05_Mesh;
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
                case "99":
                    return R99_1_Mesh;
                case "B7A2":
                    return B7A2_1_Mesh;
                case "SBD":
                    return SBD_1_Mesh;
                case "SB2C":
                    return SB2C_1_Mesh;
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
                case "99":
                    return R99_2_Mesh;
                case "B7A2":
                    return B7A2_2_Mesh;
                case "SBD":
                    return SBD_2_Mesh;
                case "SB2C":
                    return SB2C_2_Mesh;
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
                case "99":
                    return R99_0_Tex;
                case "B7A2":
                    return B7A2_0_Tex;
                case "SBD":
                    return SBD_0_Tex;
                case "SB2C":
                    return SB2C_0_Tex;
                default:
                    return Zero_0_Tex;
            }
        }
        public Texture GetTex05(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_05_Tex;
                case "F4U":
                    return F4U_05_Tex;
                case "99":
                    return R99_05_Tex;
                case "B7A2":
                    return B7A2_05_Tex;
                case "SBD":
                    return SBD_05_Tex;
                case "SB2C":
                    return SB2C_05_Tex;
                default:
                    return Zero_05_Tex;
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
                case "99":
                    return R99_1_Tex;
                case "B7A2":
                    return B7A2_1_Tex;
                case "SBD":
                    return SBD_1_Tex;
                case "SB2C":
                    return SB2C_1_Tex;
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
                case "99":
                    return R99_2_Tex;
                case "B7A2":
                    return B7A2_2_Tex;
                case "SBD":
                    return SBD_2_Tex;
                case "SB2C":
                    return SB2C_2_Tex;
                default:
                    return Zero_2_Tex;
            }
        }
        public float GetPropOffset(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_Prop_Offset;
                case "F4U":
                    return F4U_Prop_Offset;
                case "99":
                    return R99_Prop_Offset;
                case "B7A2":
                    return B7A2_Prop_Offset;
                case "SBD":
                    return SBD_Prop_Offset;
                case "SB2C":
                    return SB2C_Prop_Offset;
                default:
                    return Zero_Prop_Offset;
            }
        }
        public Vector3 GetBodyOffset(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Zero_Body_Offset;
                case "F4U":
                    return F4U_Body_Offset;
                case "99":
                    return R99_Body_Offset;
                case "B7A2":
                    return B7A2_Body_Offset;
                case "SBD":
                    return SBD_Body_Offset;
                case "SB2C":
                    return SB2C_Body_Offset;
                default:
                    return Zero_Body_Offset;
            }
        }


    }
}
