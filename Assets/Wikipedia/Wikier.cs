using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AnttiStarterKit.Extensions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

namespace Wikipedia
{
    public class Wikier : MonoBehaviour
    {
        public Action<WikiArticle> onLoaded;
        public Action<string> onError;

        public string lang = "fi";

        private string ExtractURL =>
            $"https://{lang}.wiktionary.org/w/api.php?action=query&prop=extracts&exsentences=1&exlimit=1&explaintext=1&formatversion=2&format=json&titles=";

        private string WikiTextURL =>
            $"https://{lang}.wiktionary.org/w/api.php?action=parse&prop=wikitext&formatversion=2&format=json&page=";

        private string RandomURL =>
            $"https://{lang}.wiktionary.org/w/api.php?action=query&generator=random&prop=info%7Ccategories&formatversion=2&grnnamespace=0&grnlimit=10&format=json";

        private WikiCertificateHandler certHandler;

        private void Awake()
        {
            certHandler = new WikiCertificateHandler();
        }

        public void Load(string page)
        {
            StartCoroutine(DoLoad(page));
        }

        public void LoadRandom()
        {
            StartCoroutine(DoLoadRandom());
        }

        private IEnumerator DoLoadRandom()
        {
            var req = UnityWebRequest.Get(RandomURL);
            req.certificateHandler = certHandler;
            
            yield return req.SendWebRequest();
            
            if (!string.IsNullOrEmpty(req.error))
            {
                onError?.Invoke(req.error);
                yield break;
            }
            
            // Debug.Log(req.downloadHandler.text);
            var data = JsonUtility.FromJson<RandomData> (req.downloadHandler.text);
            var randoms = data.query.pages.OrderByDescending(p => p.length).ToList();

            yield return DoLoad(randoms.First().title);
        }

        private IEnumerator DoLoad(string page)
        {
            var extract = UnityWebRequest.Get(ExtractURL + page);
            var wikitext = UnityWebRequest.Get(WikiTextURL + page);
            
            extract.certificateHandler = certHandler;
            wikitext.certificateHandler = certHandler;

            yield return extract.SendWebRequest();
            yield return wikitext.SendWebRequest();

            if (!string.IsNullOrEmpty(extract.error))
            {
                onError?.Invoke(extract.error);
                yield break;
            }
            
            if (!string.IsNullOrEmpty(wikitext.error))
            {
                onError?.Invoke(wikitext.error);
                yield break;
            }
            
            // Debug.Log(extract.downloadHandler.text);
            var data = JsonUtility.FromJson<Data> (extract.downloadHandler.text);
            // Debug.Log(wikitext.downloadHandler.text);
            var fullData = JsonUtility.FromJson<ParseData> (wikitext.downloadHandler.text);

            var article = data.query.pages.First();
            onLoaded?.Invoke(new WikiArticle
            {
                title = article.title,
                text = article.extract,
                excerpt = FindExcerpt(fullData.parse.wikitext),
                images = FindImages(fullData.parse.wikitext),
                words = FindWords(fullData.parse.wikitext),
                categories = FindCategories(fullData.parse.wikitext)
            });
        }
        
        private static string FindExcerpt(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            const string key = "{{short description|";
            var start = text.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (start < 0) return FindSomeText(text);
            start += key.Length;
            var end = text.IndexOf("}}", start, StringComparison.Ordinal);
            return text.Substring(start, end - start);
        }

        private static string FindSomeText(string text)
        {
            if (string.IsNullOrEmpty(text) || text.ToLower().Contains("redirect")) return null;
            var lines = text.Split("\n");
            return lines.Select(Clean).Where(line => line.Length > 3 && !line.Contains("==") && !line.Contains("(")).ToList().FirstOrDefault();
        }

        private static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            var preCleaned = Regex.Replace(text, @"\[\[.*:.*\]\]", "");
            preCleaned = Regex.Replace(preCleaned, @"\[\[([^]]*\|)*", "");
            preCleaned = Regex.Replace(preCleaned, @"\[[\d]\]", "");
            preCleaned = Regex.Replace(preCleaned, @"\|.*\|", "");
            preCleaned = Regex.Replace(preCleaned, @"\{\{.*\}\}", "");
            preCleaned = Regex.Replace(preCleaned, @"<!.*>", "");
            // preCleaned = Regex.Replace(preCleaned, @"\(.*\)", "");
            preCleaned = Regex.Replace(preCleaned, @"<ref.*>.*<\/ref>", "");
            preCleaned = Regex.Replace(preCleaned, @"<ref.*>", "");

            var sb = new StringBuilder(preCleaned);
            sb.Replace("{", "");
            sb.Replace("}", "");
            sb.Replace("[", "");
            sb.Replace("]", "");
            sb.Replace("*", "");
            sb.Replace("#", "");
            sb.Replace(":", "");
            sb.Replace(";", "");
            sb.Replace("\"", "");
            sb.Replace("'", "");
            sb.Replace("|", "");
            
            sb.Replace("(", "[");
            sb.Replace(")", "]");

            return sb.ToString().Trim();
        }
        
        private static List<string> FindCategories(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            
            var words = new List<string>();
            const string key = "[[Category:";
            var pos = 0;
            var matches = 0;

            while (matches < 100 && pos < text.Length)
            {
                var start = text.IndexOf(key, pos, StringComparison.Ordinal);
                if (start < 0) break;
                start += key.Length;
                var end = text.IndexOf("]]", start, StringComparison.Ordinal);
                var word = text.Substring(start, end - start);
                pos = end;
                var parts = word.Split('|');
                words.Add(parts.First());
                matches++;
            }
            
            return words;
        }
        
        private static List<string> FindWords(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<string>();
            
            var words = new List<string>();
            const string key = "[[";
            var pos = 0;
            var matches = 0;

            while (matches < 100 && pos < text.Length)
            {
                var start = text.IndexOf(key, pos, StringComparison.Ordinal);
                if (start < 0) break;
                start += key.Length;
                var end = text.IndexOf("]]", start, StringComparison.Ordinal);
                var word = text.Substring(start, end - start);
                pos = end;
                if(word.Contains(":")) continue;
                var parts = word.Split('|');
                words.Add(parts.First());
                matches++;
            }
            
            return words;
        }
        
        private static List<WikiImage> FindImages(string text)
        {
            if (string.IsNullOrEmpty(text)) return new List<WikiImage>();
            
            var images = new List<WikiImage>();
            const string key = "File:";
            var pos = 0;
            var matches = 0;

            while (matches < 100 && pos < text.Length)
            {
                var start = text.IndexOf(key, pos, StringComparison.Ordinal);
                if (start < 0) break;
                start += key.Length;
                var end = text.IndexOf("\n", start, StringComparison.Ordinal);
                var img = text.Substring(start, end - start);
                var parts = img.Split('|');
                images.Add(new WikiImage
                {
                    url = GetImageUrl(parts.First()),
                    caption = parts.Last()
                });
                pos = end;
                matches++;
            }
            
            return images;
        }

        private static string GetImageUrl(string file)
        {
            var replaced = file.Replace(" ", "_");
            var md5 = CreateMD5(replaced).ToLower();
            var folders = $"{md5.Substring(0, 1)}/{md5.Substring(0, 2)}";
            return $"https://upload.wikimedia.org/wikipedia/commons/{folders}/{replaced}";
        }

        private static string CreateMD5(string input)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            
            var sb = new StringBuilder();
            foreach (var b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }
            return sb.ToString();
        }
    }

    public class WikiCertificateHandler : CertificateHandler
    {
        // Encoded RSAPublicKey
        private static readonly string PUB_KEY = "";


        /// <summary>
        /// Validate the Certificate Against the Amazon public Cert
        /// </summary>
        /// <param name="certificateData">Certifcate to validate</param>
        /// <returns></returns>
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }

    [Serializable]
    internal class Data
    {
        public Query query;
    }

    [Serializable]
    internal class Query
    {
        public List<Page> pages;
    }
    
    [Serializable]
    internal class Page
    {
        public string title;
        public string extract;
    }
    
    [Serializable]
    internal class ParseData
    {
        public Parse parse;
    }
    
    [Serializable]
    internal class Parse
    {
        public string title;
        public string wikitext;
    }

    [Serializable]
    internal class RandomData
    {
        public RandomQuery query;
    }
    
    [Serializable]
    internal class RandomQuery
    {
        public List<RandomEntry> pages;
    }
    
    [Serializable]
    internal class RandomEntry
    {
        public string title;
        public int length;
    }

    [Serializable]
    public class WikiArticle
    {
        public string title;
        public string excerpt;
        public string text;
        public List<WikiImage> images;
        public List<string> words;
        public List<string> categories;
    }

    [Serializable]
    public class WikiImage
    {
        public string url;
        public string caption;
    }
}
