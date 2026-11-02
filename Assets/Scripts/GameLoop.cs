using UnityEngine;
using System;
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

    public event Action<Fighter> RoundEnded; // Sends the winner, or null if it was a draw

    private readonly List<Hit> _hits = new List<Hit>();
    private struct Hit
    {
        public Fighter Attacker;
        public Fighter Defender;
        public AttackData Attack;
    }

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
        while (_roundStarted && _accumulator >= TickDuration && ticksThisFrame < MaxTicksPerFrame)
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

        UpdateFacing();

        foreach (Fighter fighter in _fighters)
            fighter.Tick();

        DetectHits();
        ApplyHits();
    }

    private void UpdateFacing()
    {
        if (_fighters.Count == 2)
        {
            float dx = _fighters[1].transform.position.x - _fighters[0].transform.position.x;
            if (dx > 0)
            {
                _fighters[0].Facing = 1;
                _fighters[1].Facing = -1;
            }
            if (dx < 0)
            {
                _fighters[0].Facing = -1;
                _fighters[1].Facing = 1;
            }
        }
    }

    private void DetectHits()
    {
        if (_fighters.Count != 2) return;

        CheckHit(_fighters[0], _fighters[1]);
        CheckHit(_fighters[1], _fighters[0]);
    }

    private void CheckHit(Fighter attacker, Fighter defender)
    {
        if (!attacker.PlayerAttack.TryGetHitbox(out Rect hitbox)) return;
        if (!hitbox.Overlaps(defender.Hurtbox)) return;

        _hits.Add(new Hit
        {
            Attacker = attacker,
            Defender = defender,
            Attack = attacker.PlayerAttack.CurrentAttack
        });
    }
    private void ApplyHits()
    {
        foreach (Hit hit in _hits)
        {
            hit.Attacker.PlayerAttack.MarkHit();
            Debug.Log(hit.Attacker.name + " hit " + hit.Defender.name + " with " + hit.Attack.name + ". Current Tick: " + CurrentTick);
        }
        _hits.Clear();
        // After everyone has ticked, so hits on the same tick both land and neither fighter gets priority
        foreach (Fighter attacker in _fighters)
        {
            foreach (Fighter target in _fighters)
            {
                if (attacker != target) attacker.TryHit(target);
            }
        }

        CheckForRoundEnd();
    }

    private void CheckForRoundEnd()
    {
        Fighter winner = null;
        int fightersLeft = 0;

        foreach (Fighter fighter in _fighters)
        {
            if (fighter.Health.IsDead) continue;

            winner = fighter;
            fightersLeft++;
        }

        if (fightersLeft > 1) return;

        _roundStarted = false;

        foreach (Fighter fighter in _fighters)
            fighter.SetInputEnabled(false);

        RoundEnded?.Invoke(winner);
    }
}