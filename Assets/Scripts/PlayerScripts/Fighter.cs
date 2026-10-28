using UnityEngine;

public class Fighter : MonoBehaviour
{
    private PlayerAttack _playerAttack;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        _playerAttack = GetComponent<PlayerAttack>();
        _playerMovement = GetComponent<PlayerMovement>();
    }

    public void Tick()
    {
        _playerAttack.Tick();
        _playerMovement.Tick();
    }
}
