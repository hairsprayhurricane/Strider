using UnityEngine;
using TMPro;
using System;
[RequireComponent(typeof(TextMeshProUGUI))]
public class LogInterface : MonoBehaviour
{
    public static float elapsedTime;
    public static float maxWaitTime = 5f;
    public static TextMeshProUGUI text;
    public static String savedText;

    public void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        text.text = "";
        elapsedTime = 0f;
    }

    void Update()
    {
        if (text.text != "")
        {
            elapsedTime += Time.deltaTime;
        }
        if (elapsedTime >= maxWaitTime)
        {
            Clear();
        }
        //Debug.Log(elapsedTime);
    }

    public static void Add(String newText, Color color = default)
    {
        if (text)
        {
            if (color == default) color = Color.red;

            string hex = ColorUtility.ToHtmlStringRGB(color);
            string line = $"<color=#{hex}>{newText}</color>\n";
            text.text += line;
            elapsedTime = 0f;
        }
        else
        {
            Debug.LogError($"LogInterface is not initialized.");
        }
    }

    public static void Clear()
    {
        text.text = "";
        elapsedTime = 0f;
    }

    public static void HideText()
    {
        savedText = text.text;
        text.text = "";
    }
    public static void ReturnText()
    {
        text.text = savedText;
        savedText = "";
    }
    
}