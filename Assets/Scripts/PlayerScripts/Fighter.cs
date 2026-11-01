using UnityEngine;
using UnityEngine.InputSystem;

public class Fighter : MonoBehaviour
{
    [SerializeField] private Renderer[] hatRenderers;

    private PlayerAttack _playerAttack;
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;

    public PlayerHealth Health { get; private set; }
    public int PlayerIndex => _playerInput.playerIndex;

    private void Awake()
    {
        _playerAttack = GetComponent<PlayerAttack>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();
        Health = GetComponent<PlayerHealth>();
    }

    public void Tick()
    {
        _playerAttack.Tick();
        _playerMovement.Tick();
    }

    public void TryHit(Fighter target)
    {
        if (_playerAttack.TryHit(target.transform.position, out int damage))
            target.Health.TakeDamage(damage);
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        if (inputEnabled) _playerInput.ActivateInput();
        else _playerInput.DeactivateInput();
    }

    public void SetHatColor(Color color)
    {
        foreach (Renderer hatRenderer in hatRenderers)
            hatRenderer.material.color = color;
    }
}
