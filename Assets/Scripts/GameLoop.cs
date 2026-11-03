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
    private int _hitstopTicks;

    public event Action<Fighter> RoundEnded; // Sends the winner, or null if it was a draw

    private readonly List<Hit> _hits = new List<Hit>();
    private struct Hit
    {
        public Fighter Attacker;
        public Fighter Defender;
        public AttackData Attack;
        public Vector3 Point;
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
        if (_hitstopTicks > 0)
        {
            _hitstopTicks--;
            if (_hitstopTicks == 0) SetFightersFrozen(false);
            return;
        }

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
        Rect hurtbox = defender.Hurtbox;
        if (!hitbox.Overlaps(hurtbox)) return;

        // Middle of where the boxes overlap, pulled towards the camera so effects aren't inside the body
        float pointX = (Mathf.Max(hitbox.xMin, hurtbox.xMin) + Mathf.Min(hitbox.xMax, hurtbox.xMax)) / 2f;
        float pointY = (Mathf.Max(hitbox.yMin, hurtbox.yMin) + Mathf.Min(hitbox.yMax, hurtbox.yMax)) / 2f;

        _hits.Add(new Hit
        {
            Attacker = attacker,
            Defender = defender,
            Attack = attacker.PlayerAttack.CurrentAttack,
            Point = new Vector3(pointX, pointY, defender.transform.position.z - 0.6f)
        });
    }

    private void SetFightersFrozen(bool frozen)
    {
        foreach (Fighter fighter in _fighters)
            fighter.SetFrozen(frozen);
    }

    private void ApplyHits()
    {
        foreach (Hit hit in _hits)
        {
            hit.Attacker.PlayerAttack.MarkHit();

            if (hit.Defender.IsBlocking && !hit.Attack.BreaksBlock)
            {
                if (SFXManager.Instance != null)
                    SFXManager.Instance.PlayDefend();

                if (HitEffects.Instance != null)
                    HitEffects.Instance.PlayBlock(hit.Point, hit.Defender.transform.position);

                Debug.Log(hit.Defender.name + " blocked " + hit.Attacker.name + "'s " + hit.Attack.name + ". Current Tick: " + CurrentTick);
                continue;
            }

            hit.Defender.Health.TakeDamage(hit.Attack.Damage);
            hit.Defender.FlashBody();

            if (SFXManager.Instance != null)
                SFXManager.Instance.PlayAttackHit(hit.Attack);

            if (HitEffects.Instance != null)
                HitEffects.Instance.PlayHit(hit.Attack, hit.Point, hit.Attacker.Facing);

            if (hit.Attack.HitstopFrames > _hitstopTicks)
            {
                _hitstopTicks = hit.Attack.HitstopFrames;
                SetFightersFrozen(true);
            }

            Debug.Log(hit.Attacker.name + " hit " + hit.Defender.name + " with " + hit.Attack.name + " for " + hit.Attack.Damage + ". Current Tick: " + CurrentTick);
        }
        _hits.Clear();

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
        _hitstopTicks = 0;
        SetFightersFrozen(false);

        foreach (Fighter fighter in _fighters)
            fighter.SetInputEnabled(false);

        if (HitEffects.Instance != null)
            HitEffects.Instance.PlayKO();

        RoundEnded?.Invoke(winner);
    }
}