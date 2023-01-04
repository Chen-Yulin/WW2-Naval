using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Modding;

namespace WW2NavalAssembly
{
    public class Asset_CannonHit
    {
        public GameObject explo;
        public Asset_CannonHit(ModAssetBundle modAssetBundle)
        {
            explo = modAssetBundle.LoadAsset<GameObject>("CannonHit");
        }
    }
    

    public class AssetManager : SingleInstance<AssetManager>
    {
        public override string Name { get; } = "Asset Manager";

        public Asset_CannonHit CannonHit { get; protected set; }



        protected void Awake()
        {
           
            CannonHit = new Asset_CannonHit(ModResource.GetAssetBundle("CannonHit AB"));
        }
    }
}