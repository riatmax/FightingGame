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
        _moveX = _moveAction.IsPressed() ? _moveAction.ReadValue<Vector2>().x : 0f;

        Vector3 position = transform.position;
        float startX = position.x;

        if (canMove)
        {
            position.x += _moveX * moveSpeed / GameLoop.TicksPerSecond;
            position.x = Mathf.Clamp(position.x, -maxMove, maxMove);
            transform.position = position;
        }

        UpdateFootsteps(position.x != startX);
    }
}
