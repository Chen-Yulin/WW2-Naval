using System;
using UnityEngine;

namespace skpCustomModule
{
	// Token: 0x0200003A RID: 58
	public class AdActiveChaffExplosionComponent : MonoBehaviour, IExplosionEffect
	{
		// Token: 0x06000136 RID: 310 RVA: 0x00016CDC File Offset: 0x00014EDC
		public void Update()
		{
			bool flag = !StatMaster.levelSimulating;
			if (!flag)
			{
				bool flag2 = this.projectileScript != null;
				if (flag2)
				{
					this.projectileScript = base.gameObject.GetComponent<AdProjectileScript>();
				}
			}
		}

        public bool OnExplode(float power, float upPower, float torquePower, Vector3 explosionPos, float radius, int mask, bool inWater)
        {
            throw new NotImplementedException();
        }

        // Token: 0x04000290 RID: 656
        public AdProjectileScript projectileScript;
	}
}
