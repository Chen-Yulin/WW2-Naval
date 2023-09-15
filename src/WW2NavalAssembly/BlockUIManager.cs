using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;

namespace WW2NavalAssembly
{
    public class FollowerUI:MonoBehaviour
    {
        private RectTransform rectTransform;
        private RectTransform parentRect;
        public Transform target;
        public float size;
        public Image image;
        public bool show = true;
        public float DisplayDist = 30;
        public Vector2 offset = Vector2.zero;

        public void Initialize(Transform t, float size, Texture texture, float dist)
        {
            image = gameObject.AddComponent<Image>();

            Sprite mySprite = Sprite.Create(texture as Texture2D, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);

            image.sprite = mySprite;

            gameObject.transform.SetParent(BlockUIManager.Instance.Canvas.transform);
            rectTransform = GetComponent<RectTransform>();
            parentRect = transform.parent.gameObject.GetComponent<RectTransform>();

            target = t;
            this.size = size;
            this.DisplayDist = dist;
        }

        public void Update()
        {
            if (!target)
            {
                Destroy(this.gameObject);
            }
            else
            {

                if (show)
                {
                    // 将目标对象的世界坐标转换为屏幕坐标
                    Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);

                    if (screenPosition.z > 0 && screenPosition.z < DisplayDist)
                    {
                        if (image.enabled == false)
                        {
                            image.enabled = true;
                        }
                        // 将屏幕坐标转换为Canvas的本地坐标
                        Vector2 localPosition;
                        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, (Vector2)screenPosition + offset, null, out localPosition);

                        // 更新Image的位置
                        rectTransform.localPosition = localPosition;
                    }
                    else
                    {
                        if (image.enabled == true)
                        {
                            image.enabled = false;
                        }
                    }

                    rectTransform.localScale = Vector3.one * size / 100f;
                }
                else
                {
                    if (image.enabled == true)
                    {
                        image.enabled = false;
                    }
                }
            }
            
        }


    }
    class BlockUIManager : SingleInstance<BlockUIManager>
    {
        public override string Name { get; } = "BlockUIManager";

        public GameObject Canvas;

        public FollowerUI CreateFollowerUI(Transform t, float size, Texture texture, float dist = 30f)
        {
            GameObject UIObject = new GameObject("Follower");
            UIObject.transform.parent = Canvas.transform;
            FollowerUI follower = UIObject.AddComponent<FollowerUI>();
            follower.Initialize(t, size, texture, dist);
            return follower;
        }
        
        public void Awake()
        {
            Canvas = new GameObject("WW2BlockUI");
            Canvas.transform.parent = GameObject.Find("Canvas").transform;
            Canvas.AddComponent<RectTransform>();
        }

    }
}
