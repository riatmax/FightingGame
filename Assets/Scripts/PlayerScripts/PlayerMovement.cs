using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    /// <summary>
    ///
    ///     Handles the players movement
    /// 
    /// </summary>
    
    
    
    [Header("--- Movement Settings ---")] 
    [SerializeField] private float moveSpeed;
    [Tooltip("How far left or right the player is allowed to move")]
    [SerializeField] private float maxMove;
    [Tooltip("Seconds between footstep sounds while walking")]
    [SerializeField] private float footstepInterval = 0.3f;
    private PlayerInput _thisPlayerInput;
    private InputAction _moveAction;
    private bool _canMove = true;
    private Rigidbody _playerRb;
    private float _footstepTimer;
    private bool _frozen;
    private float _moveX;

    public float MaxMove => maxMove;

    [Header("--- Dash ---")]
    [SerializeField] private float forwardDashDistance = 1.5f;
    [SerializeField] private float backDashDistance = 1.2f;
    [Tooltip("Ticks the dash takes to cover its distance")]
    [SerializeField] private int dashTicks = 12;
    [Tooltip("Max ticks between the two taps of a double tap")]
    [SerializeField] private int doubleTapWindow = 12;

    private Fighter _fighter;
    private int _lastDir;
    private int _lastTapDir;
    private int _ticksSinceTap = 999;
    private float _dashStep;
    private int _dashTicksRemaining;

    public bool IsDashing => _dashTicksRemaining > 0;

    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();
        _playerRb = GetComponent<Rigidbody>();

        _moveAction = _thisPlayerInput.actions[InputManager.move];

        _fighter = GetComponent<Fighter>();
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     if (other.collider.TryGetComponent(out PlayerMovement movement)) _canMove = false;
    //     else _canMove = true;
    // }
    
    public void SetFrozen(bool frozen)
    {
        _frozen = frozen;
    }

    // Goes off the input, so backing into the edge of the stage still counts
    public bool IsWalkingBackward(int facing)
    {
        return _moveX * facing < 0f;
    }

    public bool IsWalkingForward(int facing)
    {
        return _moveX * facing > 0f;
    }

    private void UpdateFootsteps(bool isMoving)
    {
        if (!isMoving)
        {
            _footstepTimer = 0f;
            return;
        }

        _footstepTimer -= 1f / GameLoop.TicksPerSecond;
        if (_footstepTimer > 0f) return;

        _footstepTimer = footstepInterval;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayFootstep();
    }

    // Runs once per game tick, from Fighter.Tick
    public void Tick(bool canMove)
    {
        _moveX = _moveAction.IsPressed() ? _moveAction.ReadValue<Vector2>().x : 0f;

        // Taps are tracked every tick, but a dash only starts when the fighter is free
        if (DetectDoubleTap(out int tapDir) && canMove && !IsDashing)
            StartDash(tapDir);

        Vector3 position = transform.position;
        float startX = position.x;

        if (IsDashing)
        {
            position.x += _dashStep;
            _dashTicksRemaining--;
        }
        else if (canMove)
        {
            position.x += _moveX * moveSpeed / GameLoop.TicksPerSecond;
        }

        position.x = Mathf.Clamp(position.x, -maxMove, maxMove);
        transform.position = position;

        // Compared after the clamp, so pushing into the stage edge doesn't count as walking
        UpdateFootsteps(position.x != startX);
    }

    // A tap is the stick or key going into a direction it wasn't already in
    private bool DetectDoubleTap(out int tapDir)
    {
        int dir = _moveX > 0.5f ? 1 : (_moveX < -0.5f ? -1 : 0);
        bool tapped = dir != 0 && dir != _lastDir;
        _lastDir = dir;
        tapDir = dir;

        if (_ticksSinceTap < 999) _ticksSinceTap++;
        if (!tapped) return false;

        bool isDouble = dir == _lastTapDir && _ticksSinceTap <= doubleTapWindow;

        // A dash uses up both taps, so a triple tap doesn't dash twice
        _lastTapDir = isDouble ? 0 : dir;
        _ticksSinceTap = 0;
        return isDouble;
    }

    private void StartDash(int dir)
    {
        float distance = dir == _fighter.Facing ? forwardDashDistance : backDashDistance;
        _dashStep = dir * distance / dashTicks;
        _dashTicksRemaining = dashTicks;
    }

    public void CancelDash()
    {
        _dashTicksRemaining = 0;
    }
}
