using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WW2NavalAssembly
{
    using UnityEngine;

    public class GravityModifier : MonoBehaviour
    {
        public float gravityScale = 1.0f; // 设置重力的缩放比例

        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
            
            if (rb == null)
            {
                Debug.LogError("Rigidbody component not found!");
            }
            else
            {
                rb.useGravity = false;
            }
        }

        void FixedUpdate()
        {
            // 修改物体的重力
            if (rb)
            {
                Vector3 newGravity = Physics.gravity * gravityScale;
                rb.AddForce(newGravity, ForceMode.Acceleration);
            }
        }
    }

}
