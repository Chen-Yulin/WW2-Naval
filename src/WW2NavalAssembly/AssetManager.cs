using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Modding;

namespace WW2NavalAssembly
{
    public class Asset_TorpedoTrail
    {
        public GameObject TorpedoTrail;
        public GameObject TorpedoHit;
        public Asset_TorpedoTrail(ModAssetBundle modAssetBundle)
        {
            TorpedoTrail = modAssetBundle.LoadAsset<GameObject>("TorpedoTrail");
            TorpedoHit = modAssetBundle.LoadAsset<GameObject>("TorpedoHit");
        }
    }
    public class Asset_Sea
    {
        public GameObject Sea;
        public Asset_Sea(ModAssetBundle modAssetBundle)
        {
            Sea = modAssetBundle.LoadAsset<GameObject>("Sea");
        }
    }
    public class Asset_FireControl
    {
        public GameObject FireControl;
        public Asset_FireControl(ModAssetBundle modAssetBundle)
        {
            FireControl = modAssetBundle.LoadAsset<GameObject>("FireControlCanvas");
        }
    }
    public class Asset_WellEffect
    {
        public GameObject WellExplo;
        public GameObject AmmoExplo;
        public Asset_WellEffect(ModAssetBundle modAssetBundle)
        {
            WellExplo = modAssetBundle.LoadAsset<GameObject>("WellExplo");
            AmmoExplo = modAssetBundle.LoadAsset<GameObject>("AmmoExplo");
        }
    }
    public class Asset_Projector
    {
        public GameObject BulletHole;
        public GameObject TorpedoHole;
        public Asset_Projector(ModAssetBundle modAssetBundle)
        {
            BulletHole = modAssetBundle.LoadAsset<GameObject>("holeProjector");
            TorpedoHole = modAssetBundle.LoadAsset<GameObject>("torpedoHoleProjector");
        }
    }
    public class Asset_Chimney
    {
        public GameObject ChimneySmoke;
        public Asset_Chimney(ModAssetBundle modAssetBundle)
        {
            ChimneySmoke = modAssetBundle.LoadAsset<GameObject>("Smoke");
        }
    }
    public class Asset_Pierce
    {
        public GameObject Pierce;
        public Asset_Pierce(ModAssetBundle modAssetBundle)
        {
            Pierce = modAssetBundle.LoadAsset<GameObject>("Perice");
        }
    }
    public class Asset_ArmourVis
    {
        public GameObject SingleArmour;
        public GameObject WellArmour;
        public Asset_ArmourVis(ModAssetBundle modAssetBundle)
        {
            SingleArmour = modAssetBundle.LoadAsset<GameObject>("SingleVis");
            WellArmour = modAssetBundle.LoadAsset<GameObject>("WellVis");
        }
    }
    public class Asset_GunSmoke
    {
        public GameObject gunsmoke1;
        public GameObject gunsmoke2;
        public Asset_GunSmoke(ModAssetBundle modAssetBundle)
        {
            gunsmoke1 = modAssetBundle.LoadAsset<GameObject>("GunSmoke1");
            gunsmoke2 = modAssetBundle.LoadAsset<GameObject>("GunSmoke2");
        }
    }
    public class Asset_CannonHit
    {
        public GameObject explo;
        public Asset_CannonHit(ModAssetBundle modAssetBundle)
        {
            explo = modAssetBundle.LoadAsset<GameObject>("CannonHit");
        }
    }
    public class Asset_WaterHit
    {
        public GameObject waterhit1;
        public GameObject waterhit2;
        
        public Asset_WaterHit(ModAssetBundle modAssetBundle)
        {
            waterhit1 = modAssetBundle.LoadAsset<GameObject>("waterHit1");
            waterhit2 = modAssetBundle.LoadAsset<GameObject>("waterHit2");
        }
    }


    public class AssetManager : SingleInstance<AssetManager>
    {
        public override string Name { get; } = "Asset Manager";

        public Asset_CannonHit CannonHit { get; protected set; }
        public Asset_WaterHit WaterHit { get; protected set; }

        public Asset_GunSmoke GunSmoke { get; protected set; }

        public Asset_ArmourVis ArmourVis { get; protected set; }
        public Asset_Pierce Pierce { get; protected set; }
        public Asset_Chimney Chimney { get; protected set; }
        public Asset_Projector Projector { get; protected set; }
        public Asset_WellEffect WellEffect { get; protected set; }
        public Asset_FireControl FireControl { get; protected set; }
        public Asset_Sea Sea { get; protected set; }
        public Asset_TorpedoTrail TorpedoTrail { get; protected set; }

        protected void Awake()
        {
            WaterHit = new Asset_WaterHit(ModResource.GetAssetBundle("WaterHit AB"));
            CannonHit = new Asset_CannonHit(ModResource.GetAssetBundle("CannonHit AB"));
            GunSmoke = new Asset_GunSmoke(ModResource.GetAssetBundle("GunSmoke AB"));
            ArmourVis = new Asset_ArmourVis(ModResource.GetAssetBundle("ArmourVis AB"));
            Pierce = new Asset_Pierce(ModResource.GetAssetBundle("Pierce AB"));
            Chimney = new Asset_Chimney(ModResource.GetAssetBundle("Chimney AB"));
            Projector = new Asset_Projector(ModResource.GetAssetBundle("Projector AB"));
            WellEffect = new Asset_WellEffect(ModResource.GetAssetBundle("WellEffect AB"));
            FireControl = new Asset_FireControl(ModResource.GetAssetBundle("FireControl AB"));
            Sea = new Asset_Sea(ModResource.GetAssetBundle("Sea AB"));
            TorpedoTrail = new Asset_TorpedoTrail(ModResource.GetAssetBundle("TorpedoTrail AB"));
        }
    }
}