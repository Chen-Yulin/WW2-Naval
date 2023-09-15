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
        public Transform target;
        public float size;
        public Image image;

        public FollowerUI(Transform t, float size, Texture texture)
        {
            image = gameObject.AddComponent<Image>();

            Sprite mySprite = Sprite.Create(texture as Texture2D, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);

            image.sprite = mySprite;

            gameObject.transform.SetParent(BlockUIManager.Instance.Canvas.transform);
            rectTransform = GetComponent<RectTransform>();

            target = t;
            this.size = size;
        }

        public void Update()
        {
            // 将目标对象的世界坐标转换为屏幕坐标
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(target.transform.position);

            // 将屏幕坐标转换为Canvas的本地坐标
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, screenPosition, null, out localPosition);

            // 更新Image的位置
            rectTransform.localPosition = localPosition;
        }


    }
    class BlockUIManager : SingleInstance<BlockUIManager>
    {
        public override string Name { get; } = "BlockUIManager";

        public GameObject Canvas;
        
        public void Awake()
        {
            Canvas = new GameObject("WW2BlockUI");
            Canvas.transform.parent = GameObject.Find("Canvas").transform;
        }

    }
}
