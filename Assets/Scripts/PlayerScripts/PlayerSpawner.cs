using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class PlayerSpawner : MonoBehaviour
{
    /// <summary>
    ///
    ///     Spawns a player when one of their keys is pressed. Each control scheme (P1, P2, ...)
    ///     joins once, at the spawn point with the same index, all sharing the keyboard
    ///
    ///     PlayerInput.Instantiate is required here - PlayerInputs placed directly in the scene
    ///     each try to claim the keyboard, so only one gets it and the rest get no devices
    ///
    /// </summary>



    [Header("--- Spawn Settings ---")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Character and menu color for each player, in the same order as the control schemes")]
    [SerializeField] private Color[] playerColors;
    private InputActionAsset _controls;
    private PlayerInput[] _joinedPlayers;
    private IDisposable _joinListener;
    private int _joinedCount;

    [SerializeField] private GameLoop _gameLoop;

    private CinemachineTargetGroup _targetGroup;

    public event Action<int> PlayerJoined;
    public bool AllPlayersJoined => _joinedCount >= Mathf.Min(spawnPoints.Length, InputManager.controlSchemes.Length);


    private void Awake()
    {
        _controls = playerPrefab.GetComponent<PlayerInput>().actions;
        _joinedPlayers = new PlayerInput[InputManager.controlSchemes.Length];

        _targetGroup = FindFirstObjectByType<CinemachineTargetGroup>(); // changed this line because it found the wrong game object, two different objects were tagged the same 
    }

    private void OnEnable()
    {
        _joinListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPress);
    }

    private void OnDisable()
    {
        _joinListener?.Dispose();
        _joinListener = null;
    }

    private void OnAnyButtonPress(InputControl control)
    {
        int schemeIndex = FindSchemeForControl(control);
        if (schemeIndex < 0 || schemeIndex >= spawnPoints.Length || _joinedPlayers[schemeIndex] != null)
            return;

        Join(schemeIndex, control.device);
    }

    private int FindSchemeForControl(InputControl control)
    {
        for (int i = 0; i < InputManager.controlSchemes.Length; i++)
        {
            InputBinding schemeMask = InputBinding.MaskByGroup(InputManager.controlSchemes[i]);

            foreach (InputBinding binding in _controls.bindings)
            {
                if (!binding.isComposite && schemeMask.Matches(binding) && InputControlPath.Matches(binding.effectivePath, control))
                    return i;
            }
        }

        return -1;
    }

    private void Join(int schemeIndex, InputDevice device)
    {
        PlayerInput player = PlayerInput.Instantiate(playerPrefab, schemeIndex, InputManager.controlSchemes[schemeIndex], -1, device);
        player.name = "Player " + (schemeIndex + 1);
        player.transform.SetPositionAndRotation(spawnPoints[schemeIndex].position, spawnPoints[schemeIndex].rotation);

        // Rigidbody has to be moved too - its interpolation overwrites the transform with where it spawned
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        playerRb.position = spawnPoints[schemeIndex].position;
        playerRb.rotation = spawnPoints[schemeIndex].rotation;

        _joinedPlayers[schemeIndex] = player;
        
        _targetGroup.AddMember(_joinedPlayers[schemeIndex].transform, 0.5f, 0.5f);

        Fighter fighter = player.GetComponent<Fighter>();
        fighter.SetBodyColor(playerColors[schemeIndex]);
        fighter.PlayerAttack.SetTrailColor(playerColors[schemeIndex]);
        fighter.Facing = spawnPoints[schemeIndex].position.x > 0f ? -1 : 1; // Face the middle of the stage until the round starts tracking the other player
        _gameLoop.RegisterFighter(fighter);

        _joinedCount++;
        PlayerJoined?.Invoke(schemeIndex);
    }

    public Color GetPlayerColor(int playerIndex)
    {
        return playerColors[playerIndex];
    }

    public Fighter GetFighter(int playerIndex)
    {
        return _joinedPlayers[playerIndex].GetComponent<Fighter>();
    }
}
