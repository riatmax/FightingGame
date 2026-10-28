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
    private PlayerInput _thisPlayerInput;
    private InputAction _moveAction;
    private InputAction _button1Action;
    private InputAction _button2Action;
    private InputAction _button3Action;
    private bool _canMove = true;
    private Rigidbody _playerRb;
    

    
    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();
        _playerRb = GetComponent<Rigidbody>();

        _moveAction = _thisPlayerInput.actions[InputManager.move];
        _button1Action = _thisPlayerInput.actions[InputManager.button1];
        _button2Action = _thisPlayerInput.actions[InputManager.button2];
        _button3Action = _thisPlayerInput.actions[InputManager.button3];
    }

    // private void OnCollisionEnter(Collision other)
    // {
    //     if (other.collider.TryGetComponent(out PlayerMovement movement)) _canMove = false;
    //     else _canMove = true;
    // }
    
    private void FixedUpdate()
    {
        Vector2 input = _moveAction.IsPressed() ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        Vector3 desiredVelocity = new Vector3(input.x * moveSpeed, 0f, 0f);

        if (_playerRb.position.x > maxMove && input.x > 0 || _playerRb.position.x < -maxMove && input.x < 0) desiredVelocity = Vector3.zero;
        // if (!_canMove) desiredVelocity = Vector3.zero;

        _playerRb.linearVelocity = desiredVelocity;
    }
}
