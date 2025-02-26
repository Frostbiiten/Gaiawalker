using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class QuickDebugDrawer : MonoBehaviour
{
    public GUIStyle style;
    public static QuickDebugDrawer instance;
    public string debugDivider;
    public int updateInterval = 2;
    StringBuilder b = new StringBuilder();

    public enum DebugType
    {
        Log,
        Warning,
        Error
    }

    [System.Serializable]
    public class DebugVar
    {
        public string name;
        public string text;
        public float timeLeft;
        public DebugType debugType;

        public DebugVar(string n, string tex, float t)
        {
            name = n;
            timeLeft = t;
            text = tex;

            debugType = DebugType.Log;
        }
        public DebugVar(string n, string tex, float t, DebugType debugTyp)
        {
            name = n;
            timeLeft = t;
            text = tex;

            debugType = debugTyp;
        }
    }

    public List<DebugVar> info = new List<DebugVar>();

    [TextArea(15, 20)]
    public string completeDebugInfo;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        if (Time.frameCount % updateInterval == 0)
        {
            for (int i = 0; i < info.Count; i++)
            {
                info[i].timeLeft -= Time.deltaTime * updateInterval;

                if (info[i].timeLeft <= 0f)
                {
                    info.RemoveAt(i);
                }
            }

            b.Clear();

            for (int i = 0; i < info.Count; i++)
            {
                if (info[i].debugType == DebugType.Error)
                {
                    b.Append("<color=red>");
                    b.Append(info[i].name);
                    b.Append("</color><color=white>");
                    b.Append(debugDivider);
                    b.Append(info[i].text);
                    b.Append("</color>");
                    b.Append(Environment.NewLine);
                }

                if (info[i].debugType == DebugType.Warning)
                {
                    b.Append("<color=yellow>");
                    b.Append(info[i].name);
                    b.Append("</color><color=white>");
                    b.Append(debugDivider);
                    b.Append(info[i].text);
                    b.Append("</color>");
                    b.Append(Environment.NewLine);
                }

                if (info[i].debugType == DebugType.Log)
                {
                    b.Append("<color=white>");
                    b.Append(info[i].name);
                    b.Append(debugDivider);
                    b.Append(info[i].text);
                    b.Append("</color>");
                    b.Append(Environment.NewLine);
                }

                completeDebugInfo = b.ToString();
            }
        }
    }

    public void UpdateVar(string name, string newText)
    {
        for (int i = 0; i < info.Count; i++)
        {
            if (info[i].name == name)
            {
                info[i] = new DebugVar(info[i].name, newText, 1f);
                return;
            }
        }

        info.Add(new DebugVar(name, newText, 1f));
    }

    public void UpdateVar(string name, string newText, float time)
    {
        for (int i = 0; i < info.Count; i++)
        {
            if(info[i].name == name)
            {
                info[i] = new DebugVar(info[i].name, newText, time);
                return;
            }
        }

        info.Add(new DebugVar(name, newText, time));
    }

    public void UpdateVar(string name, string newText, float time, DebugType d)
    {
        for (int i = 0; i < info.Count; i++)
        {
            if (info[i].name == name)
            {
                info[i] = new DebugVar(info[i].name, newText, time, d);
                return;
            }
        }

        info.Add(new DebugVar(name, newText, time, d));
    }

    public void OnGUI()
    {
        GUILayout.Label(completeDebugInfo, style);
    }
}
