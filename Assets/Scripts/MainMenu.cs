using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    ///
    ///     Shows who has joined, then counts down and starts the round once everyone is in
    ///
    /// </summary>



    [Serializable]
    private class PlayerSlot
    {
        public Image background;
        public TMP_Text label;
    }

    [Header("--- References ---")]
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private GameLoop gameLoop;
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Button quitButton;
    [SerializeField] private PlayerSlot[] playerSlots;

    [Header("--- Countdown Settings ---")]
    [SerializeField] private int countdownStart = 3;
    [Tooltip("How long the menu stays up after the last player joins, so their join can be seen")]
    [SerializeField] private float allJoinedDelay = 1f;
    [SerializeField] private float fightTextDuration = 1f;


    private void Awake()
    {
        quitButton.onClick.AddListener(Quit);
    }

    private void OnEnable()
    {
        playerSpawner.PlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        playerSpawner.PlayerJoined -= OnPlayerJoined;
    }

    private void OnPlayerJoined(int playerIndex)
    {
        PlayerSlot slot = playerSlots[playerIndex];
        slot.background.color = playerSpawner.GetPlayerColor(playerIndex);
        slot.label.text = "PLAYER " + (playerIndex + 1) + "\nJOINED!";

        if (playerSpawner.AllPlayersJoined) StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        yield return new WaitForSeconds(allJoinedDelay);

        menuRoot.SetActive(false);
        countdownText.gameObject.SetActive(true);

        for (int i = countdownStart; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        gameLoop.StartRound();

        countdownText.text = "FIGHT!";
        yield return new WaitForSeconds(fightTextDuration);

        countdownText.gameObject.SetActive(false);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
