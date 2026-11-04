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

    [Header("--- Jump Settings ---")]
    [Tooltip("Upward speed when leaving the ground")]
    [SerializeField] private float jumpVelocity = 13f;
    [Tooltip("Sideways speed of a diagonal jump. It's locked in at takeoff, so it can't be steered in the air")]
    [SerializeField] private float jumpHorizontalSpeed = 5f;
    [SerializeField] private float gravity = 40f;
    [Tooltip("Height of the floor the fighters stand on")]
    [SerializeField] private float groundHeight = 0f;

    private PlayerInput _thisPlayerInput;
    private InputAction _moveAction;
    private bool _canMove = true;
    private Rigidbody _playerRb;
    private float _footstepTimer;
    private bool _frozen;
    private float _moveX;
    private bool _grounded = true;
    private float _verticalVelocity;
    private float _jumpDirectionX;

    public float MaxMove => maxMove;
    public bool IsGrounded => _grounded;
    
    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();
        _playerRb = GetComponent<Rigidbody>();

        _moveAction = _thisPlayerInput.actions[InputManager.move];
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

    public void Tick(bool canMove)
    {
        Vector2 input = _moveAction.IsPressed() ? _moveAction.ReadValue<Vector2>() : Vector2.zero;

        // Zero in the air, so a jumping player isn't blocking or playing a walk animation
        _moveX = _grounded ? input.x : 0f;

        Vector3 position = transform.position;
        float startX = position.x;

        if (_grounded && canMove && input.y > 0.5f)
        {
            _grounded = false;
            _verticalVelocity = jumpVelocity;
            _jumpDirectionX = input.x > 0.3f ? 1f : input.x < -0.3f ? -1f : 0f;
        }

        // The jump keeps going even when the player can't act, so being hit or attacking mid-air still falls
        if (!_grounded)
        {
            position.x += _jumpDirectionX * jumpHorizontalSpeed / GameLoop.TicksPerSecond;
            position.y += _verticalVelocity / GameLoop.TicksPerSecond;
            _verticalVelocity -= gravity / GameLoop.TicksPerSecond;

            if (position.y <= groundHeight)
            {
                position.y = groundHeight;
                _grounded = true;

                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayFootstep();
            }
        }
        else if (canMove)
        {
            position.x += _moveX * moveSpeed / GameLoop.TicksPerSecond;
        }

        position.x = Mathf.Clamp(position.x, -maxMove, maxMove);
        transform.position = position;

        UpdateFootsteps(_grounded && position.x != startX);
    }
}
