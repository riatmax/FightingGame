using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    /// <summary>
    ///
    ///     Shows who has joined, then counts down and starts the round once everyone is in.
    ///     Also handles quitting, which works at any point in the game
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
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private PopIn countdownPopIn;
    [SerializeField] private InputActionAsset controls;
    [SerializeField] private PlayerSlot[] playerSlots;

    [Header("--- Title ---")]
    [SerializeField] private RectTransform title;
    [Tooltip("Degrees the title tips each way")]
    [SerializeField] private float titleRockAngle = 4f;
    [SerializeField] private float titleRockSpeed = 2.5f;

    [Header("--- Join Prompt ---")]
    [SerializeField] private RectTransform joinPrompt;
    [Tooltip("How far above and below its normal size the prompt scales, 0.1 = 10%")]
    [SerializeField] private float joinPulseAmount = 0.1f;
    [SerializeField] private float joinPulseSpeed = 8f;

    [Header("--- Countdown Settings ---")]
    [SerializeField] private int countdownStart = 3;
    [Tooltip("How long the menu stays up after the last player joins, so their join can be seen")]
    [SerializeField] private float allJoinedDelay = 1f;
    [SerializeField] private float fightTextDuration = 1f;

    private InputActionAsset _quitControls;
    private InputAction _quitAction;


    private void Awake()
    {
        // Its own copy, the players' PlayerInputs put their control scheme masks on the original
        _quitControls = Instantiate(controls);
        _quitAction = _quitControls.FindAction(InputManager.quit);
    }

    private void OnEnable()
    {
        playerSpawner.PlayerJoined += OnPlayerJoined;

        _quitAction.performed += OnQuitPressed;
        _quitAction.Enable();
    }

    private void OnDisable()
    {
        playerSpawner.PlayerJoined -= OnPlayerJoined;

        _quitAction.performed -= OnQuitPressed;
        _quitAction.Disable();
    }

    private void OnDestroy()
    {
        Destroy(_quitControls);
    }

    private void Update()
    {
        joinPrompt.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * joinPulseSpeed) * joinPulseAmount);
        title.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * titleRockSpeed) * titleRockAngle);
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
        hudRoot.SetActive(true);
        countdownText.gameObject.SetActive(true);

        for (int i = countdownStart; i > 0; i--)
        {
            countdownText.text = i.ToString();
            countdownPopIn.Play();
            yield return new WaitForSeconds(1f);
        }

        gameLoop.StartRound();

        countdownText.text = "FIGHT!";
        countdownPopIn.Play();
        yield return new WaitForSeconds(fightTextDuration);

        countdownText.gameObject.SetActive(false);
    }

    private void OnQuitPressed(InputAction.CallbackContext context)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
