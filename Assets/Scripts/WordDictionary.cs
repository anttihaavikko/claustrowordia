using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class WordDictionary : MonoBehaviour
{
    public TextAsset dictionaryFile;

	private Dictionary<string, string> words;
    private List<string> letterPool;

    private string next;

    // Start is called before the first frame update
    void Awake()
    {
        letterPool = new List<string>();
        Prep();
    }

    void Prep()
    {
        words = dictionaryFile.text.Split('\n').Select(w => {
            var word = w.Trim().ToLower();
            return word.Split('\t')[0];
        }).Distinct().ToDictionary(x => x, x => x);

        Debug.Log("Loaded dictionary of " + words.Count + " words.");

        var sample = RandomWord();
        Debug.Log("Random word sample: '" + sample + "'");
    }

    // void CheckTracks()
    // {
    //     foreach (var track in tracks.Where(t => t.NeedsCheck()))
    //     {
    //         track.Check();
    //     }
    // }

    public bool IsWord(string word)
    {
        return words.ContainsKey(word.ToLower());
    }

    public string GetRandomLetter(bool remove = true)
    {
        if (!letterPool.Any())
            PopulateLetterPool();

        var letter = letterPool[0];
        if(remove)
        {
            letterPool.RemoveAt(0);
            next = GetRandomLetter(false);
        }

        return letter;
    }

    public string RandomWord()
    {
        var key = words.Keys.ToArray()[Random.Range(0, words.Count)];
        var word = words[key];
        return word;
    }

    void PopulateLetterPool()
    {
        var word = RandomWord();
        //Debug.Log("Seeding random letters with word '" + word + "'");
        letterPool.AddRange(Regex.Split(word, string.Empty).Where(IsOk).OrderBy(l => Random.value));
    }

    private bool IsOk(string letter)
    {
        return !string.IsNullOrEmpty(letter) && char.IsLetter(letter[0]);
    }

    public string GetNext()
    {
        return next;
    }
}
