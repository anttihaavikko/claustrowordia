using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Core;
using Mirror;
using UnityEngine;
using UltimateArcade.Frontend;
using UltimateArcade.Server;
using Random = UnityEngine.Random;

public class Arcade : NetworkBehaviour
{
    [SerializeField] private WordDictionary wordDictionary;
    
    private UltimateArcadeGameServerAPI serverApi;
    private UltimateArcadeGameClientAPI clientApi;
    private string token;
    
    private readonly TileGrid<string> grid = new(7, 7);
    private readonly List<LetterMatch> words = new();
    private int score;
    private int multiAddition = 1;
    private List<Twist> twists;
    private int move;

    private bool IsGameOver => grid.All().Count(c => !string.IsNullOrEmpty(c)) >= 49;
    
    private void Awake()
    {
        wordDictionary.Setup();
    }
    
    private void Start()
    {
        if (isServer)
        {
            serverApi = new UltimateArcadeGameServerAPI();
            AutoConnect.OnServerReady += AutoConnect_OnServerReady;
        }

        if (isClient)
        {
            // var gameToken = ExternalScriptBehavior.Token();
            // clientApi = new UltimateArcadeGameClientAPI(gameToken, ExternalScriptBehavior.BaseApiServerName());
            InitPlayer("gameToken");
        }
    }

    private IEnumerator Check(int x, int y)
    {
        var rowCards = grid.GetRow(y).ToList();
        var colCards = grid.GetColumn(x).ToList();
        
        var rowsReversed = rowCards.ToList();
        rowsReversed.Reverse();
        var colsReversed = colCards.ToList();
        colsReversed.Reverse();
        
        var rowLetters = rowCards.Select(c => c ?? " ").ToList();
        var colLetters = colCards.Select(c => c ?? " ").ToList();
        
        var row = string.Join(string.Empty, rowLetters);
        var col = string.Join(string.Empty, colLetters);
        
        words.Clear();

        yield return CheckString(row, x, rowCards);
        yield return CheckString(col, y, colCards);
        yield return CheckString(Field.Reverse(row), 6 - x, rowsReversed, true);
        yield return CheckString(Field.Reverse(col), 6 - y, colsReversed, true);

        var multi = 1;

        foreach (var w in words.OrderBy(w => w.word.Length))
        {
            var amount = Field.GetScore(w.word) * multi;
            score += amount;
            AddScore(amount);
            multi += multiAddition;
            yield return new WaitForSeconds(0.5f);
        }

        var field = Hand.Instance.Field;

        if (IsGameOver)
        {
            field.GameOver();
            yield break;
        }

        if (move == 0 || move % 10 != 0)
        {
            Hand.Instance.SetState(true);
            yield break;
        }

        twists = GetTwists().OrderBy(_ => Random.value).Take(3).ToList();
        for (var i = 0; i < twists.Count; i++)
        {
            AddLettersTo(twists[i], i);
        }

        field.ShowTwists(twists);
    }
    
    private void AddLettersTo(Twist twist, int index)
    {
        var randomLetter = wordDictionary.GetRandomLetter();
        var fieldLetter = grid.All().Where(c => c != null && c != randomLetter).OrderBy(_ => Random.value).First();
        twist.SetLetters(fieldLetter, randomLetter);
        twist.Index = index;
    }

    [Command]
    public void PickTwist(int index)
    {
        var twist = twists[index];
        Hand.Instance.Field.ApplyTwist(twist);
    }
    
    private static IEnumerable<Twist> GetTwists()
    {
        return new[]
        {
            new Twist(TwistType.Replace, "Immigrant blues", "Replace all [1] tiles with [2] tiles."),
            new Twist(TwistType.Destroy, "Delay the inevitable", "Destroy all [1] tiles."),
            new Twist(TwistType.AddCards, "Extra housings", "Receive (3 extra) letter tiles."),
            new Twist(TwistType.SlideUp, "Southern refugees", "(Slide) the whole board (up) one tile."),
            new Twist(TwistType.SlideDown, "Northern refugees", "(Slide) the whole board (down) one tile."),
            new Twist(TwistType.SlideLeft, "Eastern refugees", "(Slide) the whole board (left) one tile."),
            new Twist(TwistType.SlideRight, "Western refugees", "(Slide) the whole board (right) one tile."),
            new Twist(TwistType.MoreMulti, "Population boom", "Gain (extra +1) on each (multiplier) increase.")
        };
    }
    
    IEnumerator CheckString(string text, int mustInclude, List<string> letters, bool isReversed = false)
    {
        if (text.Length < 3) yield break;
        
        for (var len = text.Length; len > 2; len--)
        {
            for (var start = 0; start <= text.Length - len; start++)
            {
                var word = text.Substring(start, len);
                if (wordDictionary.IsWord(word) && mustInclude >= start && mustInclude < start + len)
                {
                    words.Add(new LetterMatch
                    {
                        word = word,
                        letters = letters.GetRange(start, len),
                        reverse = isReversed
                    });
                }
            }

            yield return null;
        }
    }

    [TargetRpc]
    private void AddScore(int amount)
    {
        Hand.Instance.Field.AddScore(amount);
    }
    
    [Command]
    private void InitPlayer(string gameToken)
    {
        Random.InitState(123);
        PlayerReady();
        token = gameToken;
        serverApi.ActivatePlayer(gameToken, _ => { }, _ => { });
    }
    
    private void AutoConnect_OnServerReady(string seed)
    {
        Debug.Log($"Seed set to {seed}");
        Random.InitState(AutoConnect.RandomSeed.GetHashCode());
    }

    [TargetRpc]
    private void PlayerReady()
    {
        var hand = Hand.Instance;
        var field = hand.Field;
        Hand.Instance.ArcadeReady(this);
        
        field.PlaceCard(new Vector3(-1f, -1f, 0), wordDictionary.GetRandomLetter());
        field.PlaceCard(new Vector3(1f, -1f, 0), wordDictionary.GetRandomLetter());
        field.PlaceCard(new Vector3(-1f, 1f, 0), wordDictionary.GetRandomLetter());
        field.PlaceCard(new Vector3(1f, 1f, 0), wordDictionary.GetRandomLetter());
    }

    [Command]
    public void AddCards(int amount)
    {
        StartCoroutine(AddCardsCoroutine(amount));
    }

    private IEnumerator AddCardsCoroutine(int amount)
    {
        for (var i = 0; i < amount; i++)
        {
            Hand.Instance.AddCard(wordDictionary.GetRandomLetter());
            yield return new WaitForSeconds(0.1f);
        }
    }

    [Command]
    public void PlaceLetter(string letter, int x, int y)
    {
        grid.Set(letter, x, y);
        StartCoroutine(Check(x, y));
        move++;
    }
    
    [Command]
    public void RemoveLetter(int x, int y)
    {
        grid.Set(null, x, y);
        move--;
    }
    
    [Command]
    public void SlideVertical(int diff)
    {
        var index = diff > 0 ? 0 : 6;
        var edgeCards = grid.GetRow(index).Where(c => c != null).ToList();
        grid.Remove(edgeCards);

        for (var x = 0; x < 7; x++)
        {
            for (var y = index; y is < 7 and >= 0; y += diff)
            {
                var value = grid.Get(x, y + diff);
                grid.Set(value, x, y);
            }
        }
    }
    
    [Command]
    public void SlideHorizontal(int diff)
    {
        var index = diff > 0 ? 6 : 0;
        var edgeCards = grid.GetColumn(index).Where(c => c != null).ToList();
        grid.Remove(edgeCards);

        for (var y = 0; y < 7; y++)
        {
            for (var x = index; x is < 7 and >= 0; x -= diff)
            {
                var value = grid.Get(x - diff, y);
                grid.Set(value, x, y);
            }
        }
    }
    
    [Command]
    public void DestroyAll(string letter, string replacement)
    {
        var cards = grid.All().Where(c => c == letter).ToList();
        var positions = cards.Select(c => grid.GetPosition(c));
        grid.Remove(cards);

        if (replacement == null) return;

        foreach (var p in positions)
        {
            if (!p.HasValue) continue;
            var x = Mathf.RoundToInt(p.Value.x + 3);
            var y = Mathf.RoundToInt(-p.Value.y + 3);
            StartCoroutine(Check(x, y));
        }
    }

    [Command]
    public void SubmitScore()
    {
        StartCoroutine(serverApi.ReportPlayerScore(token, score,
            () =>
            {
                UADebug.Log("player score reported");
                ClientGameOver();
                StartCoroutine(serverApi.Shutdown(
                        () => UADebug.Log("Shutdown requested"),
                        err => UADebug.Log("couldn't request shutdown:" + err)
                    )
                );
            },
            err => UADebug.Log("ERROR player join." + err)));
    } 
    
    [TargetRpc]
    private void ClientGameOver()
    {
        ExternalScriptBehavior.CloseGame();
    }

    [Command]
    public void MoreMulti()
    {
        multiAddition++;
    }
}