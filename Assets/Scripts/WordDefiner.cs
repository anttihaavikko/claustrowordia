using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using Leaderboards;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class WordDefiner : MonoBehaviour
{
    public SpeechBubble bubble;
    
    private CertificateHandler certHandler;

    private Dictionary<string, string> cache;
    private IEnumerator handle;

    private void Awake()
    {
        certHandler = new CustomCertificateHandler();
        cache = new Dictionary<string, string>();
    }

    public void DefineWord(string word, bool lowerPosition = false)
    {
        if (handle != null)
        {
            StopCoroutine(handle);
        }

        handle = GetDefinition(word);
        StartCoroutine(handle);

        var pos = Vector3.zero.WhereY(lowerPosition ? 0.312f : 0.695f);
        Tweener.MoveToBounceOut(transform, pos, 0.2f);
    }

    private IEnumerator GetDefinition(string word)
    {
        if (cache.ContainsKey(word))
        {
            ShowDefinition(cache[word]);
            yield break;
        }
        
        var www = UnityWebRequest.Get("https://api.dictionaryapi.dev/api/v2/entries/en_US/" + word);
        www.certificateHandler = certHandler;

        yield return www.SendWebRequest();

        if (!string.IsNullOrEmpty(www.error))
        {
            // bubble.Hide();
            yield break;
        }

        var json = "{\"words\":" + www.downloadHandler.text + "}";
        var def = JsonUtility.FromJson<DefinitionData>(json);

        if (def.words.Length == 0)
        {
            // appearer.Hide();
            yield break;
        }
        var w = def.words[Random.Range(0, def.words.Length)];
        if (w.meanings.Length == 0)
        {
            // appearer.Hide();
            yield break;
        }
        var meaning = w.meanings[Random.Range(0, w.meanings.Length)];
        if (meaning.definitions.Length == 0)
        {
            // appearer.Hide();
            yield break;
        }
        
        var str = "<m>" + w.word + "</m>, " + meaning.partOfSpeech + ", " + meaning.definitions[Random.Range(0, meaning.definitions.Length)].definition;
        var sb = new StringBuilder(str);
        sb.Replace("(", "[");
        sb.Replace(")", "]");
        sb.Replace("<m>", "(");
        sb.Replace("</m>", ")");
        str = sb.ToString();
        ShowDefinition(str);
        
        cache.Add(word, str);
    }

    private void ShowDefinition(string def)
    {
        bubble.Show(def);
    }
}

[Serializable]
public class DefinitionData
{
    public WordOption[] words;
}

[Serializable]
public class WordOption
{
    public string word;
    public WordMeaning[] meanings;
}

[Serializable]
public class WordMeaning
{
    public string partOfSpeech;
    public WordDefinition[] definitions;
}

[Serializable]
public class WordDefinition
{
    public string definition;
}