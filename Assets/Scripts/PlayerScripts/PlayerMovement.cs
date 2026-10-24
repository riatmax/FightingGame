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

    
    
    private PlayerInput _thisPlayerInput;
    private InputAction _moveAction;
    private InputAction _button1Action;
    private InputAction _button2Action;
    private InputAction _button3Action;
    

    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();

        _moveAction = _thisPlayerInput.actions[InputManager.move];
        _button1Action = _thisPlayerInput.actions[InputManager.button1];
        _button2Action = _thisPlayerInput.actions[InputManager.button2];
        _button3Action = _thisPlayerInput.actions[InputManager.button3];
    }

    private void Update()
    {
        if (_moveAction.IsPressed())
        {
            Vector2 input = _moveAction.ReadValue<Vector2>();
            transform.position += new Vector3(input.x * (moveSpeed * Time.deltaTime), 0f, 0f);
        }
    }
}
