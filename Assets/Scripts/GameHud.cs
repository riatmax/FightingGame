using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameHud : MonoBehaviour
{
    /// <summary>
    ///
    ///     Keeps the health bars up to date, slides and shakes them when a player gets hit,
    ///     and shows who won when the round ends. Any key after that restarts from the main menu
    ///
    /// </summary>



    [Header("--- References ---")]
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private GameLoop gameLoop;
    [SerializeField] private Slider[] healthBars;

    [Header("--- Health Bar Feel ---")]
    [Tooltip("How quickly a bar slides down to the player's real health")]
    [SerializeField] private float healthLerpSpeed = 6f;
    [Tooltip("How far a bar jumps around when its player gets hit")]
    [SerializeField] private float jitterStrength = 14f;
    [SerializeField] private float jitterDuration = 0.3f;

    [Header("--- End Screen ---")]
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private Image resultBanner;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text loserText;
    [SerializeField] private TMP_Text playAgainText;
    [SerializeField] private Color drawColor = Color.gray;
    [Tooltip("How long before a key press restarts the game, so a player still mashing doesn't skip the screen")]
    [SerializeField] private float playAgainDelay = 1.5f;
    [Tooltip("Real seconds after the KO slow motion ends before the end screen shows")]
    [SerializeField] private float endScreenDelay = 0.4f;

    [Header("--- Knockout Text ---")]
    [Tooltip("Shakes and flashes from the KO until the end screen, at full speed even during the slow motion")]
    [SerializeField] private TMP_Text knockoutText;
    [SerializeField] private float knockoutShakeStrength = 18f;
    [Tooltip("Flashes per second")]
    [SerializeField] private float knockoutFlashSpeed = 8f;
    [SerializeField] private Color knockoutFlashColor = new Color(1f, 0.82f, 0.12f);

    private IDisposable _playAgainListener;
    private Vector2 _knockoutRestPosition;
    private float[] _targetHealth;
    private float[] _jitterTimeLeft;
    private Vector2[] _barRestPositions;


    private void Awake()
    {
        _targetHealth = new float[healthBars.Length];
        _jitterTimeLeft = new float[healthBars.Length];
        _barRestPositions = new Vector2[healthBars.Length];

        for (int i = 0; i < healthBars.Length; i++)
        {
            _targetHealth[i] = healthBars[i].value;
            _barRestPositions[i] = ((RectTransform)healthBars[i].transform).anchoredPosition;
        }

        _knockoutRestPosition = knockoutText.rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        playerSpawner.PlayerJoined += OnPlayerJoined;
        gameLoop.RoundEnded += OnRoundEnded;
    }

    private void OnDisable()
    {
        playerSpawner.PlayerJoined -= OnPlayerJoined;
        gameLoop.RoundEnded -= OnRoundEnded;

        _playAgainListener?.Dispose();
        _playAgainListener = null;
    }

    // Unscaled so the bars keep moving during the KO slow motion
    private void Update()
    {
        for (int i = 0; i < healthBars.Length; i++)
        {
            healthBars[i].value = Mathf.Lerp(healthBars[i].value, _targetHealth[i], healthLerpSpeed * Time.unscaledDeltaTime);

            _jitterTimeLeft[i] = Mathf.Max(_jitterTimeLeft[i] - Time.unscaledDeltaTime, 0f);
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * (jitterStrength * _jitterTimeLeft[i] / jitterDuration);
            ((RectTransform)healthBars[i].transform).anchoredPosition = _barRestPositions[i] + jitter;
        }

        if (knockoutText.gameObject.activeSelf)
        {
            knockoutText.rectTransform.anchoredPosition = _knockoutRestPosition + UnityEngine.Random.insideUnitCircle * knockoutShakeStrength;
            knockoutText.color = Mathf.Repeat(Time.unscaledTime * knockoutFlashSpeed, 1f) < 0.5f ? Color.white : knockoutFlashColor;
        }
    }

    private void OnPlayerJoined(int playerIndex)
    {
        healthBars[playerIndex].fillRect.GetComponent<Image>().color = playerSpawner.GetPlayerColor(playerIndex);

        playerSpawner.GetFighter(playerIndex).Health.HealthChanged += healthLeft =>
        {
            _targetHealth[playerIndex] = healthLeft;
            _jitterTimeLeft[playerIndex] = jitterDuration;
        };
    }

    private void OnRoundEnded(Fighter winner)
    {
        StartCoroutine(EndScreenRoutine(winner));
    }

    private IEnumerator EndScreenRoutine(Fighter winner)
    {
        knockoutText.gameObject.SetActive(true);

        // Wait out the KO slow motion, however long it was set up to last
        while (HitEffects.Instance != null && Time.unscaledTime < HitEffects.Instance.KOSequenceEndTime)
            yield return null;

        yield return new WaitForSecondsRealtime(endScreenDelay);

        knockoutText.gameObject.SetActive(false);

        if (winner == null)
        {
            resultBanner.color = drawColor;
            resultText.text = "DRAW!";
            loserText.text = "NOBODY WINS";
        }
        else
        {
            int winnerNumber = winner.PlayerIndex + 1;
            int loserNumber = winnerNumber == 1 ? 2 : 1;

            resultBanner.color = playerSpawner.GetPlayerColor(winner.PlayerIndex);
            resultText.text = "PLAYER " + winnerNumber + " WINS!";
            loserText.text = "PLAYER " + loserNumber + " LOSES";
        }

        endScreenRoot.SetActive(true);

        yield return new WaitForSecondsRealtime(playAgainDelay);

        playAgainText.gameObject.SetActive(true);
        _playAgainListener = InputSystem.onAnyButtonPress.CallOnce(_ => Restart());
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
