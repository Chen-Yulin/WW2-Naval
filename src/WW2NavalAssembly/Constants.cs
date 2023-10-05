using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WW2NavalAssembly
{
    static class Constants
    {
        public const float Gravity = 32.4f;
        public const float BulletGravity = 49f;

        public const float LandHeight = 10f;
        public const float CruiseHeight = 120f;
        public const float TorpedoAttackHeight = 1f;
        public const float BombAttackHeight = 300f;
        public const float BombDropHeight = 100f;
        public const float SeaHeight = 20f;

        public const float SlowTorpedoTime = 180f;
        public const float FastTorpedoTime = 60f;

        public const float BulletUnderWaterForce = 0.3f;
        public const float BulletUnderWaterDrag = 12f;
        public const int BulletAPTimer = 3;
        public const int BulletHETimer = 2;

        public const float MaxCaptainDetectAircraftRange = 1000f;
    }
}
