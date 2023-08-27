using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UI;
using UnityEngine;

namespace WW2NavalAssembly
{
    public class MyLogger : SingleInstance<MyLogger>
    {
        public override string Name { get; } = "WW2 Logger";
        public ModLogger logger;

        public void Log(string message)
        {
            if (logger != null)
            {
                logger.WriteLine(message);
            }
        }

    }
    public class ModLogger : MonoBehaviour
    {
        public Text outputText; // 输出文本
        public ScrollRect scrollRect; // 滚动区域
        public RectTransform Rtransform;
        public GameObject Window;

        public GameObject canvas;

        private Queue<string> lines = new Queue<string>(); // 存储输出行的队列

        void Start()
        {
            Window = canvas.transform.Find("logger").gameObject;
            scrollRect = Window.GetComponent<ScrollRect>();
            GameObject textObject = Window.transform.GetChild(0).gameObject;
            outputText = textObject.GetComponent<Text>();
            outputText.text = "Logger initialized :)";
            
        }
        void OnDestroy()
        {
            MyLogger.Instance.logger = null;
            Destroy(Window);
        }

        public void WriteLine(string line)
        {
            // 将新行添加到队列中
            lines.Enqueue(line);

            // 如果队列中的行数超过了最大值，则移除最早的一行
            if (lines.Count > 20)
            {
                lines.Dequeue();
            }

            // 更新输出文本
            outputText.text = string.Join("\n", lines.ToArray());

            // 滚动到底部
            scrollRect.verticalNormalizedPosition = 0;
        }
    }
}
