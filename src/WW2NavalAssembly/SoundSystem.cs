using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Modding;
using System.Security.Cryptography;

namespace WW2NavalAssembly
{
    public class SoundSystem : SingleInstance<SoundSystem>
    {
        public override string Name { get; } = "Sound System";
        public float[] SoundTrackResult = new float[360];
        
        public SoundSystem()
        {
        }

        public int AngleDiff(int a1, int a2)
        {
            return Mathf.Min(Mathf.Abs(a1 - a2), Mathf.Abs(a1 - 360 - a2));
        }

        public void AddSound(int playerID , int angle, float magnitude, float error) // 0-360, ~, 0-10
        {
            for (int i = 0; i < 360; i++)
            {
                SoundTrackResult[i] += magnitude / (0.5f + 1f / (0.2f + error) * Mathf.Pow(AngleDiff(i, angle), 2));
            }
        }

        public void FixedUpdate()
        {
            for (int i = 0; i < 360; i++)
            {
                SoundTrackResult[i] *= 0.8f;
            }
        }

    }
}
