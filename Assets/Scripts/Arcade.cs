using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    
    private readonly TileGrid<TileLetter> grid = new(7, 7);
    private readonly List<LetterMatch> words = new();
    private int score;
    private int multiAddition = 1;
    private List<Twist> twists;
    private int moves;
    private int seed;

    private bool IsGameOver => grid.All().Count(c => c != null) >= 49;
    
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
            var gameToken = GetToken();
            InitPlayer(gameToken);
        }
    }

    private string GetToken()
    {
        if (Application.isEditor) return "DummyToken";
        
        var gameToken = ExternalScriptBehavior.Token();
        clientApi = new UltimateArcadeGameClientAPI(gameToken, ExternalScriptBehavior.BaseApiServerName());
        return gameToken;
    }

    private IEnumerator Check(int x, int y)
    {
        var rowCards = grid.GetRow(y).ToList();
        var colCards = grid.GetColumn(x).ToList();
        
        var rowsReversed = rowCards.ToList();
        rowsReversed.Reverse();
        var colsReversed = colCards.ToList();
        colsReversed.Reverse();
        
        var rowLetters = rowCards.Select(c => c?.letter ?? " ").ToList();
        var colLetters = colCards.Select(c => c?.letter ?? " ").ToList();
        
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
            w.letters.ToList().ForEach(l => l.used = true);
            var bonusMulti = grid.All().All(c => c == null || c.used) ? 10 : 1;
            var amount = Field.GetScore(w.word) * multi * bonusMulti;
            score += amount;
            AddScore(amount);
            multi += multiAddition;
            yield return new WaitForSeconds(0.5f);
        }

        if (IsGameOver)
        {
            GameOver();
            SubmitScore();
            yield break;
        }

        if (moves == 0 || moves % 10 != 0)
        {
            NextRound(!words.Any());
            yield break;
        }

        Random.InitState(GetSeed());
        twists = GetTwists().OrderBy(_ => Random.value).Take(3).ToList();
        for (var i = 0; i < twists.Count; i++)
        {
            AddLettersTo(twists[i], i);
        }

        ShowTwists(twists);
    }

    [TargetRpc]
    private void ShowTwists(List<Twist> t)
    {
        Hand.Instance.Field.ShowTwists(t);
    }

    [TargetRpc]
    private void GameOver()
    {
        Hand.Instance.Field.GameOver();
    }

    [TargetRpc]
    private void NextRound(bool canUndo)
    {
        var hand = Hand.Instance;
        hand.SetState(true);

        if (canUndo)
        {
            hand.Field.ShowUndo();   
        }
    }

    private void AddLettersTo(Twist twist, int index)
    {
        var randomLetter = wordDictionary.GetRandomLetter(GetSeed());
        var fieldLetter = grid.All().Where(c => c != null && c.letter != randomLetter).OrderBy(_ => Random.value).First();
        twist.SetLetters(fieldLetter.letter, randomLetter);
        twist.SetIndex(index);
    }

    [Command]
    public void PickTwist(int index)
    {
        var twist = twists[index];
        ApplyTwist(twist.Type, twist.FirstLetter, twist.SecondLetter);
    }

    [TargetRpc]
    private void ApplyTwist(TwistType twist, string first, string second)
    {
        Hand.Instance.Field.ApplyTwist(twist, first, second);
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
    
    IEnumerator CheckString(string text, int mustInclude, List<TileLetter> letters, bool isReversed = false)
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
        Random.InitState(GetSeed());
        token = gameToken;
        serverApi.ActivatePlayer(gameToken, _ => { }, _ => { });

        PlayerReady();
    }
    
    private void AutoConnect_OnServerReady(string initialSeed)
    {
        seed = initialSeed.GetHashCode();
    }

    private int GetSeed()
    {
        return seed + moves;
    }

    [TargetRpc]
    private void PlayerReady()
    {
        Hand.Instance.ArcadeReady(this);
    }

    [Command]
    public void PlaceStarterCards()
    {
        PlaceCard(new Vector3(-1f, -1f, 0), wordDictionary.GetRandomLetter(seed));
        PlaceCard(new Vector3(1f, -1f, 0), wordDictionary.GetRandomLetter(seed));
        PlaceCard(new Vector3(-1f, 1f, 0), wordDictionary.GetRandomLetter(seed));
        PlaceCard(new Vector3(1f, 1f, 0), wordDictionary.GetRandomLetter(seed));
    }

    [TargetRpc]
    private void PlaceCard(Vector3 pos, string letter)
    {
        Hand.Instance.Field.PlaceCard(pos, letter);
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
            AddCard(wordDictionary.GetRandomLetter(GetSeed()));
            yield return new WaitForSeconds(0.1f);
        }
    }

    [TargetRpc]
    private void AddCard(string letter)
    {
        Hand.Instance.AddCard(letter);
    }

    [Command]
    public void PlaceLetter(string letter, int x, int y, bool check)
    {
        grid.Set(new TileLetter(letter), x, y);
        if (!check) return;
        StartCoroutine(Check(x, y));
        moves++;
    }
    
    [Command]
    public void RemoveLetter(int x, int y)
    {
        grid.Set(null, x, y);
        moves--;
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
        var cards = grid.All().Where(c => c != null && c.letter == letter).ToList();
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
    private void SubmitScore()
    {
        StartCoroutine(serverApi.ReportPlayerScore(token, score,
            () =>
            {
                UADebug.Log($"Score reported {score}");
                Invoke(nameof(ClientGameOver), 2f);
                StartCoroutine(serverApi.Shutdown(() => { }, _ => { }));
            },
            _ => {}
            )
        );
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

public class TileLetter
{
    public string letter;
    public bool used;

    public TileLetter(string l)
    {
        letter = l;
    }
}