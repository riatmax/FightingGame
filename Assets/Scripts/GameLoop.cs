using UnityEngine;
using System.Collections.Generic;

public class GameLoop : MonoBehaviour
{
    /// <summary>
    ///
    ///     Owns the simulation clock. Calls Tick() at a fixed 60 ticks per second,
    ///     independent of the rendering frame rate
    ///
    /// </summary>

    public const int TicksPerSecond = 60;
    private const float TickDuration = 1f / TicksPerSecond;
    private const int MaxTicksPerFrame = 5;

    private float _accumulator;
    public int CurrentTick { get; private set; }

    private readonly List<Fighter> _fighters = new List<Fighter>();
    private bool _roundStarted;

    public void RegisterFighter(Fighter fighter)
    {
        _fighters.Add(fighter);
        fighter.SetInputEnabled(false);
    }

    public void StartRound()
    {
        _roundStarted = true;
        _accumulator = 0f;
        CurrentTick = 0;

        foreach (Fighter fighter in _fighters)
            fighter.SetInputEnabled(true);
    }

    private void Update()
    {
        if (!_roundStarted) return;

        _accumulator += Time.deltaTime;

        int ticksThisFrame = 0;
        while (_accumulator >= TickDuration && ticksThisFrame < MaxTicksPerFrame)
        {
            Tick();
            _accumulator -= TickDuration;
            ticksThisFrame++;
        }

        if (_accumulator >= TickDuration)
        {
            _accumulator = 0f;
        }
    }

    private void Tick()
    {
        CurrentTick++;

        foreach (Fighter fighter in _fighters)
            fighter.Tick();
    }
}