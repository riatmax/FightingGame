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
    ///     Keeps the health bars up to date and shows who won when the round ends.
    ///     Any key after that restarts the game from the main menu
    ///
    /// </summary>



    [Header("--- References ---")]
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private GameLoop gameLoop;
    [SerializeField] private Slider[] healthBars;

    [Header("--- End Screen ---")]
    [SerializeField] private GameObject endScreenRoot;
    [SerializeField] private Image resultBanner;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text loserText;
    [SerializeField] private TMP_Text playAgainText;
    [SerializeField] private Color drawColor = Color.gray;
    [Tooltip("How long before a key press restarts the game, so a player still mashing doesn't skip the screen")]
    [SerializeField] private float playAgainDelay = 1.5f;

    private IDisposable _playAgainListener;


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

    private void OnPlayerJoined(int playerIndex)
    {
        Slider healthBar = healthBars[playerIndex];
        healthBar.fillRect.GetComponent<Image>().color = playerSpawner.GetPlayerColor(playerIndex);

        playerSpawner.GetFighter(playerIndex).Health.HealthChanged += healthLeft => healthBar.value = healthLeft;
    }

    private void OnRoundEnded(Fighter winner)
    {
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
        StartCoroutine(PlayAgainRoutine());
    }

    private IEnumerator PlayAgainRoutine()
    {
        yield return new WaitForSeconds(playAgainDelay);

        playAgainText.gameObject.SetActive(true);
        _playAgainListener = InputSystem.onAnyButtonPress.CallOnce(_ => Restart());
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
