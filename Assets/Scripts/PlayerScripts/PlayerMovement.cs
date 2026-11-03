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

    private void FixedUpdate()
    {
        if (_frozen)
        {
            _playerRb.linearVelocity = Vector3.zero;
            return;
        }

        Vector2 input = _moveAction.IsPressed() ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 desiredVelocity = new Vector3(input.x * moveSpeed, 0f, 0f);

        if (_playerRb.position.x > maxMove && input.x > 0 || _playerRb.position.x < -maxMove && input.x < 0) desiredVelocity = Vector3.zero;
        // if (!_canMove) desiredVelocity = Vector3.zero;

        _playerRb.linearVelocity = desiredVelocity;

        UpdateFootsteps(desiredVelocity.x != 0f);
    }

    // Goes off the input, so backing into the edge of the stage still counts
    public bool IsWalkingBackward(int facing)
    {
        return _moveAction.ReadValue<Vector2>().x * facing < 0f;
    }

    public bool IsWalkingForward(int facing)
    {
        return _moveAction.ReadValue<Vector2>().x * facing > 0f;
    }

    private void UpdateFootsteps(bool isMoving)
    {
        if (!isMoving)
        {
            _footstepTimer = 0f;
            return;
        }

        _footstepTimer -= Time.fixedDeltaTime;
        if (_footstepTimer > 0f) return;

        _footstepTimer = footstepInterval;

        if (SFXManager.Instance != null)
            SFXManager.Instance.PlayFootstep();
    }

    // Need to implement at some point, this will likely handle our movement
    public void Tick()
    {

    }
}
