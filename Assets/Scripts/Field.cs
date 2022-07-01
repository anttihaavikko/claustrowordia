using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AnttiStarterKit.Animations;
using AnttiStarterKit.Extensions;
using AnttiStarterKit.Managers;
using AnttiStarterKit.ScriptableObjects;
using AnttiStarterKit.Utils;
using AnttiStarterKit.Visuals;
using Leaderboards;
using UnityEngine;
using UnityEngine.UI;
using Wikipedia;
using Random = UnityEngine.Random;

public class Field : MonoBehaviour
{
    [SerializeField] private Card cardPrefab;
    [SerializeField] private WordDictionary wordDictionary;
    [SerializeField] private Color markColor;
    [SerializeField] private Score scoreDisplay;
    [SerializeField] private Appearer twistTitle, twistBanner;
    [SerializeField] private RectTransform twistHolder;
    [SerializeField] private TwistPanel twistPanelPrefab;
    [SerializeField] private Hand hand;
    [SerializeField] private Appearer showBoardButton;
    [SerializeField] private Transform twistHolderAndTitle;
    [SerializeField] private Appearer undoButton, undoArrow;
    [SerializeField] private SpeechBubble bubble;
    [SerializeField] private GameObject gameOverContainer;
    [SerializeField] private Mascot mascot;
    [SerializeField] private WordDefiner wordDefiner;
    [SerializeField] private Wikier wikier;
    [SerializeField] private SoundCollection notes;
    [SerializeField] private GameObject muteIndicator;

    private Arcade arcade;

    public bool CanAct { get; private set; }

    public bool Undoing => undoing;

    private readonly TileGrid<Card> grid = new(7, 7);
    private readonly List<WordMatch> words = new();
    
    private bool showingBoard;
    private Card lastMoved;
    private bool undoing;
    private int multiAddition = 1;

    private Tutorial<TutorialType> tutorial;
    private bool muted;

    private void Awake()
    {
        wordDictionary.Setup();
    }

    private void Start()
    {
        muted = PlayerPrefs.HasKey("WordGridMuted");
        SetVolumes();
        
        AudioManager.Instance.Lowpass(false);
        AudioManager.Instance.Chorus(false);
        AudioManager.Instance.TargetPitch = 1f;
        
        tutorial = new Tutorial<TutorialType>("WordGridTutorials");
        
        EffectCamera.Effect(0.1f);

        tutorial.onShow += ShowTutorial;

        Invoke(nameof(ShowIntro), 1.5f);
        
        var langIndex = PlayerPrefs.GetInt("WordGridLanguage", 0);
        var languages = new[] { "en", "fi", "fr", "de", "es", "nl" };
        wikier.lang = languages[langIndex];
        
        wikier.onLoaded += ShowWiki;
    }

    public void Setup(Arcade a)
    {
        arcade = a;
    }

    private void ShowWiki(WikiArticle article)
    {
        if (!string.IsNullOrEmpty(article.excerpt))
        {
            bubble.Show($"({article.title}), {article.excerpt.ToLower()}");   
        }
    }

    private void Update()
    {
        if (Application.isEditor && Input.GetKeyDown(KeyCode.D))
        {
            tutorial.Clear();
        }
    }

    private void ShowIntro()
    {
        tutorial.Show(TutorialType.Intro);
    }

    private void ShowTutorial(TutorialType message)
    {
        bubble.Show(GetTutorialMessage(message));
    }

    private string GetTutorialMessage(TutorialType message)
    {
        return message switch
        {
            TutorialType.Intro => "Drag (letters) to the board and (create words) in any direction.",
            TutorialType.Undo => "You can (undo) your last move if you (slip) but you must replay the (same letter) again afterwards.",
            TutorialType.FullMatch => "When (every letter) on the board is part of a word, you get (ten times) the scores.",
            TutorialType.Reverse => "As you can see, (words) can even be formed (backwards)!",
            TutorialType.End => "The game (ends) when the (board is full). So try to find ways to (prolong) the game so you can score more.",
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null)
        };
    }

    public void ToggleBoardOrTwists()
    {
        showingBoard = !showingBoard;
        twistHolderAndTitle.gameObject.SetActive(!showingBoard);
        showBoardButton.text.text = showingBoard ? "BACK TO TWISTS" : "SHOW BOARD";
    }

    public void PlaceCard(Vector3 pos, string letter)
    {
        var card = Instantiate(cardPrefab, pos, Quaternion.identity);
        card.Setup(letter);
        card.draggable.DropLocked = true;
        card.hoverer.enabled = false;
        AddCard(card);
    }

    public void AddCard(Card card, bool check = true, bool fromHand = false)
    {
        if (fromHand)
        {
            AudioManager.Instance.PlayEffectFromCollection(1, card.transform.position, 0.7f);
        }
        
        lastMoved = fromHand ? card : null;
        var p = card.draggable.GetRoundedPos();
        var x = Mathf.RoundToInt(p.x + 3);
        var y = Mathf.RoundToInt(-p.y + 3);
        grid.Set(card, x, y);

        if (arcade && arcade.isActiveAndEnabled)
        {
            arcade.PlaceLetter(card.Letter, x, y, check);   
        }

        undoButton.Hide();

        if (!check) return;
        StartCoroutine(Check(x, y));
    }
    
    IEnumerator Check(int x, int y)
    {
        hand.SetState(false);
        
        var rowCards = grid.GetRow(y).ToList();
        var colCards = grid.GetColumn(x).ToList();
        
        var rowsReversed = rowCards.ToList();
        rowsReversed.Reverse();
        var colsReversed = colCards.ToList();
        colsReversed.Reverse();
        
        var rowLetters = rowCards.Select(c => c ? c.Letter : " ").ToList();
        var colLetters = colCards.Select(c => c ? c.Letter : " ").ToList();
        
        var row = string.Join(string.Empty, rowLetters);
        var col = string.Join(string.Empty, colLetters);
        
        words.Clear();

        yield return CheckString(row, x, rowCards);
        yield return CheckString(col, y, colCards);
        yield return CheckString(Reverse(row), 6 - x, rowsReversed, true);
        yield return CheckString(Reverse(col), 6 - y, colsReversed, true);

        var multi = 1;

        foreach (var w in words.OrderBy(w => w.word.Length))
        {
            StartCoroutine(Announce(w, multi));

            var i = 0;
            
            foreach (var c in w.cards.ToList())
            {
                var p = c.transform.position;
                var sound = notes.At(i);
                i++;
                AudioManager.Instance.PlayEffectAt(sound, p, 0.3f, false);
                AudioManager.Instance.PlayEffectFromCollection(1, p, 0.5f);
                EffectManager.Instance.AddEffect(0, p);
                c.Colorize(markColor);
                c.Shake(0.1f);
                yield return new WaitForSeconds(0.075f);
            }
            
            multi += multiAddition;

            yield return new WaitForSeconds(0.5f);
        }

        ShowWordDefinition();

        undoing = false;
    }

    public void ShowUndo()
    {
        undoButton.Show();
    }

    private void ShowWordDefinition()
    {
        if (words.Any())
        {
            var word = words.Random().word;
            var langIndex = PlayerPrefs.GetInt("WordGridLanguage", 0);

            if (langIndex == 0)
            {
                wordDefiner.DefineWord(word);
                return;
            }
            
            wikier.Load(word);
        }
    }

    public void GameOver()
    {
        AudioManager.Instance.PlayEffectAt(0, Vector3.zero, 1f, false);
        gameOverContainer.SetActive(true);
        
        AudioManager.Instance.Lowpass();
        AudioManager.Instance.Chorus();
        AudioManager.Instance.TargetPitch = 0.8f;
    }

    public void ShowTwists(List<Twist> twists)
    {
        undoButton.Hide();
        StartCoroutine(DoTwist(twists));
    }

    private IEnumerator DoTwist(List<Twist> twists)
    {
        bubble.CanHide = false;

        foreach (Transform child in twistHolder)
        {
            Destroy(child.gameObject);
        }

        AudioManager.Instance.PlayEffectAt(1, Vector3.zero, 1f, false);
        AudioManager.Instance.Lowpass();
        AudioManager.Instance.Chorus();

        yield return new WaitForSeconds(0.25f);
        
        twistTitle.Show();
        twistBanner.Show();

        yield return new WaitForSeconds(1f);
        
        twistHolder.gameObject.SetActive(true);

        var index = 0;
        twists.ForEach(t =>
        {
            var twist = Instantiate(twistPanelPrefab, twistHolder);
            twist.Setup(t);
            twist.button.onClick.AddListener(() =>
            {
                arcade.PickTwist(t.Index);
                twist.button.onClick.RemoveAllListeners();
            });
            index++;
        });

        yield return new WaitForSeconds(0.5f);

        showBoardButton.Show();
    }

    public void ApplyTwist(TwistType twist, string first, string second)
    {
        showBoardButton.Hide();
        hand.SetState(true);
        
        // Debug.Log($"Applying twist {twist.Type} with letters {twist.FirstLetter} and {twist.SecondLetter}");
        twistHolder.gameObject.SetActive(false);
        twistTitle.Hide();
        twistBanner.Hide();

        switch (twist)
        {
            case TwistType.Replace:
                arcade.DestroyAll(first, second);
                StartCoroutine(DestroyAll(first, second));
                break;
            case TwistType.Destroy:
                arcade.DestroyAll(first, null);
                StartCoroutine(DestroyAll(first));
                break;
            case TwistType.AddCards:
                arcade.AddCards(3);
                break;
            case TwistType.SlideUp:
                arcade.SlideVertical(1);
                StartCoroutine(SlideVertical(1));
                break;
            case TwistType.SlideRight:
                arcade.SlideHorizontal(1);
                StartCoroutine(SlideHorizontal(1));
                break;
            case TwistType.SlideLeft:
                arcade.SlideHorizontal(-1);
                StartCoroutine(SlideHorizontal(-1));
                break;
            case TwistType.SlideDown:
                arcade.SlideVertical(-1);
                StartCoroutine(SlideVertical(-1));
                break;
            case TwistType.MoreMulti:
                arcade.MoreMulti();
                multiAddition++;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        tutorial.Show(TutorialType.End);
        bubble.CanHide = true;
        
        AudioManager.Instance.Lowpass(false);
        AudioManager.Instance.Chorus(false);
    }
    
    private IEnumerator SlideVertical(int diff)
    {
        hand.SetState(false);
        
        var index = diff > 0 ? 0 : 6;
        var edgeCards = grid.GetRow(index).Where(c => c).ToList();
        grid.Remove(edgeCards);
        
        foreach (var c in edgeCards)
        {
            c.Explode();
            yield return new WaitForSeconds(0.1f);
        }

        for (var x = 0; x < 7; x++)
        {
            for (var y = index; y is < 7 and >= 0; y += diff)
            {
                var card = grid.Get(x, y + diff);
                grid.Set(card, x, y);
                
                if (card)
                {
                    var t = card.transform;
                    var p = t.position;
                    Tweener.MoveToBounceOut(t, p + Vector3.up * diff, 0.3f);
                    RandomSlideSound(p);
                }
            }
        }

        hand.SetState(true);
    }

    private IEnumerator SlideHorizontal(int diff)
    {
        hand.SetState(false);
        
        var index = diff > 0 ? 6 : 0;
        var edgeCards = grid.GetColumn(index).Where(c => c).ToList();
        grid.Remove(edgeCards);
        
        foreach (var c in edgeCards)
        {
            c.Explode();
            yield return new WaitForSeconds(0.1f);
        }

        for (var y = 0; y < 7; y++)
        {
            for (var x = index; x is < 7 and >= 0; x -= diff)
            {
                var card = grid.Get(x - diff, y);
                grid.Set(card, x, y);
                if (card)
                {
                    var t = card.transform;
                    var p = t.position;
                    Tweener.MoveToBounceOut(t, p + Vector3.right * diff, 0.3f);
                    RandomSlideSound(p);
                }
            }
        }

        hand.SetState(true);
    }

    private static void RandomSlideSound(Vector3 position)
    {
        if (Random.value < 0.1f)
        {
            AudioManager.Instance.PlayEffectFromCollection(2, position, 0.7f);
        }
    }

    private IEnumerator DestroyAll(string letter, string replacement = null)
    {
        hand.SetState(false);
        
        yield return new WaitForSeconds(0.5f);
        
        var cards = grid.All().Where(c => c && c.Letter == letter).ToList();
        var positions = cards.Select(c => c.transform.position);
        grid.Remove(cards);
        foreach (var c in cards)
        {
            c.Explode();
            yield return new WaitForSeconds(0.1f);
        }

        if (replacement == null)
        {
            hand.SetState(true);
            yield break;
        }
        
        yield return new WaitForSeconds(0.4f);

        foreach (var p in positions)
        {
            var card = Instantiate(cardPrefab, p, Quaternion.identity);
            card.Setup(replacement);
            AddCard(card, false);

            EffectManager.AddEffect(0, p);
            
            var x = Mathf.RoundToInt(p.x + 3);
            var y = Mathf.RoundToInt(-p.y + 3);
            
            AudioManager.Instance.PlayEffectFromCollection(1, p, 0.8f);

            yield return Check(x, y);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void DoScoreAnims(int wordLength, int multiplier)
    {
        if (wordLength >= 6)
        {
            mascot.Jump();
            return;
        }

        if (multiplier >= 4)
        {
            mascot.Duck();
        }
    }

    public static int GetScore(string word)
    {
        return Mathf.RoundToInt(Mathf.Pow(word.Length, 2));
    }

    private IEnumerator Announce(WordMatch match, int multiplier)
    {
        if (match.reverse)
        {
            tutorial.Show(TutorialType.Reverse);    
        }

        DoScoreAnims(match.word.Length, multiplier);
        
        var x = match.cards.Average(c => c.transform.position.x);
        var y = match.cards.Average(c => c.transform.position.y);
        const float diff = 0.3f;
        var score = GetScore(match.word);

        var pos = new Vector3(x, y, 0) + Vector3.down;
        
        AudioManager.Instance.PlayEffectFromCollection(2, pos, 2f);
        
        EffectManager.AddTextPopup(match.word.ToUpper(), pos);
        
        EffectCamera.Effect(0.15f);
        
        if (grid.All().All(c => !c || c.Matched || match.cards.Contains(c)))
        {
            yield return new WaitForSeconds(0.05f);
            const string extraText = "<size=5>FULL MATCH BONUS</size>";
            pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
            EffectManager.AddTextPopup(extraText, pos);
            AudioManager.Instance.PlayEffectFromCollection(2, pos, 1.5f);
            multiplier *= 10;
            
            tutorial.Show(TutorialType.FullMatch);
        }
        
        yield return new WaitForSeconds(0.05f);
        
        yield return new WaitForSeconds(0.05f);
        var scoreText = $"<size=7>{score}</size><size=4> x {multiplier}</size>";
        pos += Vector3.down * 0.5f + new Vector3(Random.Range(-diff, diff), 0, 0);
        EffectManager.AddTextPopup(scoreText, pos);
        AudioManager.Instance.PlayEffectFromCollection(2, pos, 1f);

        // scoreDisplay.Add(score * multiplier);
    }

    public static string Reverse(string s)
    {
        var charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    IEnumerator CheckString(string text, int mustInclude, List<Card> cards, bool isReversed = false)
    {
        if (text.Length < 3) yield break;
        
        for (var len = text.Length; len > 2; len--)
        {
            for (var start = 0; start <= text.Length - len; start++)
            {
                var word = text.Substring(start, len);
                if (wordDictionary.IsWord(word) && mustInclude >= start && mustInclude < start + len)
                {
                    words.Add(new WordMatch
                    {
                        word = word,
                        cards = cards.GetRange(start, len),
                        reverse = isReversed
                    });
                }
            }

            yield return null;
        }
    }

    public void Undo()
    {
        undoButton.Hide();
        undoArrow.Show();

        if (lastMoved)
        {
            var p = lastMoved.draggable.GetRoundedPos();
            var x = Mathf.RoundToInt(p.x + 3);
            var y = Mathf.RoundToInt(-p.y + 3);
            hand.SetPickState(false);
            lastMoved.draggable.CanDrag = true;
            lastMoved.hoverer.enabled = true;
            lastMoved.draggable.SetSortOrder(50);
            var position = lastMoved.transform.position;
            var pos = hand.transform.position + (hand.Size + 2) * 0.5f * Vector3.right;
            undoArrow.transform.position = pos;
            grid.Set(null, x, y);
            arcade.RemoveLetter(x, y);
            AudioManager.Instance.PlayEffectFromCollection(2, position, 1f);
            Tweener.MoveToBounceOut(lastMoved.transform, pos, 0.3f);
            lastMoved.draggable.enabled = true;
            lastMoved = null;
            undoing = true;

            tutorial.Show(TutorialType.Undo);
        }
    }

    public void HideUndoArrow()
    {
        undoArrow.Hide();
    }

    public void ShowUndoArrow()
    {
        undoArrow.Show();
    }

    public void AddScore(int amount)
    {
        scoreDisplay.Add(amount);
    }

    public void ToggleMute()
    {
        muted = !muted;
        SetVolumes();
    }

    private void SetVolumes()
    {
        muteIndicator.SetActive(muted);
        
        var volume = muted ? 0 : 0.5f;
        AudioManager.Instance.ChangeMusicVolume(volume);
        AudioManager.Instance.ChangeSoundVolume(volume);

        if (muted)
        {
            PlayerPrefs.SetInt("WordGridMuted", 1);
            return;
        }
        
        PlayerPrefs.DeleteKey("WordGridMuted");
    }
}

internal struct WordMatch
{
    public string word;
    public IEnumerable<Card> cards;
    public bool reverse;
}

internal struct LetterMatch
{
    public string word;
    public IEnumerable<string> letters;
    public bool reverse;
}

public enum TutorialType
{
    Intro,
    Undo,
    FullMatch,
    Reverse,
    End
}