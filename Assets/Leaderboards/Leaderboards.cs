using System;
using System.Linq;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Utils;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Leaderboards
{
    public class Leaderboards : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        
        public ScoreRow rowPrefab;

        private ScoreManager scoreManager;
        private int page;
        private int lang;
        
        private void Start()
        {
            scoreManager = GetComponent<ScoreManager>();
            scoreManager.onLoaded += ScoresLoaded;
            scoreManager.LoadLeaderBoards(page, -1);
        }

        private void Update()
        {
            DebugPagination();
        }

        private void DebugPagination()
        {
            if (!Application.isEditor) return;
            if (Input.GetKeyDown(KeyCode.A)) ChangePage(-1);
            if (Input.GetKeyDown(KeyCode.D)) ChangePage(1);
        }

        private void ScoresLoaded()
        {
            var data = scoreManager.GetData();

            for (var i = 0; i < transform.childCount; i++)
            {
                var go = transform.GetChild(i).gameObject;
                Destroy(go);
            }

            data.scores.ToList().ForEach(entry =>
            {
                var row = Instantiate(rowPrefab, transform);
                row.Setup(entry.position + ". " + entry.name, entry.score, entry.locale, entry.level);
            });
        }

        public void ChangePage(int direction)
        {
            if (page + direction < 0 || direction > 0 && scoreManager.EndReached) return;
            page = Mathf.Max(page + direction, 0);
            Reload();
        }
        
        public void ChangeMode(int direction)
        {
            var modes = new[] { "all", "en", "fi", "fr", "de", "es", "nl" };
            lang = (lang + direction).LoopAround(0, 7);
            title.text = $"LEADERBOARDS ({modes[lang]})";
            Reload();
        }

        private void Reload()
        {
            var filter = lang == 0 ? -1 : lang + 10;
            scoreManager.CancelLeaderboards();
            scoreManager.LoadLeaderBoards(page, filter);
        }
    }
}
