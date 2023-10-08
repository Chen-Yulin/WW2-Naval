using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Modding;

namespace WW2NavalAssembly
{
    class AircraftAssetManager : SingleInstance<AircraftAssetManager>
    {
        public override string Name { get; } = "Aircraft Asset Manager";

        public bool Loaded = false;

        public Texture Destroyed_Tex;

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

        //========================Spitfire===================
        public Mesh Spitfire_0_Mesh;
        public Mesh Spitfire_05_Mesh;
        public Mesh Spitfire_1_Mesh;
        public Mesh Spitfire_2_Mesh;

        public Texture Spitfire_0_Tex;
        public Texture Spitfire_05_Tex;
        public Texture Spitfire_1_Tex;
        public Texture Spitfire_2_Tex;

        // =======================Fulmar======================
        public Mesh Fulmar_0_Mesh;
        public Mesh Fulmar_05_Mesh;
        public Mesh Fulmar_1_Mesh;
        public Mesh Fulmar_2_Mesh;

        public Texture Fulmar_0_Tex;
        public Texture Fulmar_05_Tex;
        public Texture Fulmar_1_Tex;
        public Texture Fulmar_2_Tex;

        //=======================Barracuda====================
        public Mesh Barracuda_0_Mesh;
        public Mesh Barracuda_05_Mesh;
        public Mesh Barracuda_1_Mesh;
        public Mesh Barracuda_2_Mesh;

        public Texture Barracuda_0_Tex;
        public Texture Barracuda_05_Tex;
        public Texture Barracuda_1_Tex;
        public Texture Barracuda_2_Tex;

        public float Zero_Prop_Offset = 1.8f;
        public float B7A2_Prop_Offset = 3.55f;
        public float R99_Prop_Offset = 2.95f;
        public float F4U_Prop_Offset = 3f;
        public float SBD_Prop_Offset = 2.1f;
        public float SB2C_Prop_Offset = 3.45f;
        public float Spitfire_Prop_Offset = 3.5f;
        public float Fulmar_Prop_Offset = 1.45f;
        public float Barracuda_Prop_Offset = 2.85f;

        public Vector3 Zero_Body_Offset = new Vector3(0, 0.75f, 0.02f);
        public Vector3 R99_Body_Offset = new Vector3(0, 0.75f, -0.15f);
        public Vector3 B7A2_Body_Offset = new Vector3(0, 0.43f, -0.23f);
        public Vector3 F4U_Body_Offset = new Vector3(0, 0.45f, -0.1f);
        public Vector3 SBD_Body_Offset = new Vector3(0, 0.78f, 0f);
        public Vector3 SB2C_Body_Offset = new Vector3(0, 0.53f, -0.2f);
        public Vector3 Spitfire_Body_Offset = new Vector3(0, 0.13f, -0.31f);
        public Vector3 Fulmar_Body_Offset = new Vector3(0, 0.4f, 0.18f);
        public Vector3 Barracuda_Body_Offset = new Vector3(0, 0.18f, -0.12f);

        public Vector3[] Cockpit_Offset = new Vector3[9];


        public void LoadAsset()
        {
            Destroyed_Tex = ModResource.GetTexture("Aircraft Destroyed Texture").Texture;

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

            Spitfire_0_Mesh = ModResource.GetMesh("A-Spitfire-0 Mesh").Mesh;
            Spitfire_05_Mesh = ModResource.GetMesh("A-Spitfire-0.5 Mesh").Mesh;
            Spitfire_1_Mesh = ModResource.GetMesh("A-Spitfire-1 Mesh").Mesh;
            Spitfire_2_Mesh = ModResource.GetMesh("A-Spitfire-2 Mesh").Mesh;
            Spitfire_0_Tex = ModResource.GetTexture("A-Spitfire-0 Texture").Texture;
            Spitfire_05_Tex = ModResource.GetTexture("A-Spitfire-0.5 Texture").Texture;
            Spitfire_1_Tex = ModResource.GetTexture("A-Spitfire-1 Texture").Texture;
            Spitfire_2_Tex = ModResource.GetTexture("A-Spitfire-2 Texture").Texture;

            Fulmar_0_Mesh = ModResource.GetMesh("A-Fulmar-0 Mesh").Mesh;
            Fulmar_05_Mesh = ModResource.GetMesh("A-Fulmar-0.5 Mesh").Mesh;
            Fulmar_1_Mesh = ModResource.GetMesh("A-Fulmar-1 Mesh").Mesh;
            Fulmar_2_Mesh = ModResource.GetMesh("A-Fulmar-2 Mesh").Mesh;
            Fulmar_0_Tex = ModResource.GetTexture("A-Fulmar-0 Texture").Texture;
            Fulmar_05_Tex = ModResource.GetTexture("A-Fulmar-0.5 Texture").Texture;
            Fulmar_1_Tex = ModResource.GetTexture("A-Fulmar-1 Texture").Texture;
            Fulmar_2_Tex = ModResource.GetTexture("A-Fulmar-2 Texture").Texture;

            Barracuda_0_Mesh = ModResource.GetMesh("A-Barracuda-0 Mesh").Mesh;
            Barracuda_05_Mesh = ModResource.GetMesh("A-Barracuda-0.5 Mesh").Mesh;
            Barracuda_1_Mesh = ModResource.GetMesh("A-Barracuda-1 Mesh").Mesh;
            Barracuda_2_Mesh = ModResource.GetMesh("A-Barracuda-2 Mesh").Mesh;
            Barracuda_0_Tex = ModResource.GetTexture("A-Barracuda-0 Texture").Texture;
            Barracuda_05_Tex = ModResource.GetTexture("A-Barracuda-0.5 Texture").Texture;
            Barracuda_1_Tex = ModResource.GetTexture("A-Barracuda-1 Texture").Texture;
            Barracuda_2_Tex = ModResource.GetTexture("A-Barracuda-2 Texture").Texture;

        }

        public AircraftAssetManager()
        {
            Cockpit_Offset[0] = new Vector3(0, 2.6f, 3.2f);//Zero
            Cockpit_Offset[3] = new Vector3(0, 4.6f, 2.2f);//B7A2
            Cockpit_Offset[2] = new Vector3(0, 3.9f, 3.2f);//99
            Cockpit_Offset[1] = new Vector3(0, 3.9f, 2.5f);//F4U
            Cockpit_Offset[4] = new Vector3(0, 3.2f, 3.5f);//SBD
            Cockpit_Offset[5] = new Vector3(0, 4.6f, 3.2f);//SB2C
            Cockpit_Offset[6] = new Vector3(0, 3.9f, 3.2f);//Spitfire
            Cockpit_Offset[7] = new Vector3(0, 3.9f, 3.2f);//Fulmar
            Cockpit_Offset[8] = new Vector3(0, 3.9f, 3.2f);//Barracuda
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
                case "Spitfire":
                    return Spitfire_0_Mesh;
                case "Fulmar":
                    return Fulmar_0_Mesh;
                case "Barracuda":
                    return Barracuda_0_Mesh;
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
                case "Spitfire":
                    return Spitfire_05_Mesh;
                case "Fulmar":
                    return Fulmar_05_Mesh;
                case "Barracuda":
                    return Barracuda_05_Mesh;
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
                case "Spitfire":
                    return Spitfire_1_Mesh;
                case "Fulmar":
                    return Fulmar_1_Mesh;
                case "Barracuda":
                    return Barracuda_1_Mesh;
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
                case "Spitfire":
                    return Spitfire_2_Mesh;
                case "Fulmar":
                    return Fulmar_2_Mesh;
                case "Barracuda":
                    return Barracuda_2_Mesh;
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
                case "Spitfire":
                    return Spitfire_0_Tex;
                case "Fulmar":
                    return Fulmar_0_Tex;
                case "Barracuda":
                    return Barracuda_0_Tex;
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
                case "Spitfire":
                    return Spitfire_05_Tex;
                case "Fulmar":
                    return Fulmar_05_Tex;
                case "Barracuda":
                    return Barracuda_05_Tex;
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
                case "Spitfire":
                    return Spitfire_1_Tex;
                case "Fulmar":
                    return Fulmar_1_Tex;
                case "Barracuda":
                    return Barracuda_1_Tex;
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
                case "Spitfire":
                    return Spitfire_2_Tex;
                case "Fulmar":
                    return Fulmar_2_Tex;
                case "Barracuda":
                    return Barracuda_2_Tex;
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
                case "Spitfire":
                    return Spitfire_Prop_Offset;
                case "Fulmar":
                    return Fulmar_Prop_Offset;
                case "Barracuda":
                    return Barracuda_Prop_Offset;
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
                case "Spitfire":
                    return Spitfire_Body_Offset;
                case "Fulmar":
                    return Fulmar_Body_Offset;
                case "Barracuda":
                    return Barracuda_Body_Offset;
                default:
                    return Zero_Body_Offset;
            }
        }

        public Vector3 GetCockpitOffset(string name)
        {
            switch (name)
            {
                case "Zero":
                    return Cockpit_Offset[0];
                case "F4U":
                    return Cockpit_Offset[1];
                case "99":
                    return Cockpit_Offset[2];
                case "B7A2":
                    return Cockpit_Offset[3];
                case "SBD":
                    return Cockpit_Offset[4];
                case "SB2C":
                    return Cockpit_Offset[5];
                case "Spitfire":
                    return Cockpit_Offset[6];
                case "Fulmar":
                    return Cockpit_Offset[7];
                case "Barracuda":
                    return Cockpit_Offset[8];
                default:
                    return Cockpit_Offset[0];
            }
        }

        public void Start()
        {
            
        }
        public void Update()
        {
            if (!Loaded &&!StatMaster.isMainMenu)
            {
                LoadAsset();
                Loaded = true;
                Debug.Log("WW2 Aircraft Asset Loaded");
            }
        }

    }
}
