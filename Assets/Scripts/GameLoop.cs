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

        ResolvePushboxes();
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
                hit.Defender.ReceiveHit(hit.Attack, true);
                continue;
            }

            hit.Defender.Health.TakeDamage(hit.Attack.Damage);
            hit.Defender.ReceiveHit(hit.Attack, false);
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

    private void ResolvePushboxes()
    {
        if (_fighters.Count != 2) return;

        Fighter a = _fighters[0];
        Fighter b = _fighters[1];

        float aX = a.transform.position.x;
        float bX = b.transform.position.x;

        float dx = bX - aX;
        float minDistance = a.PushboxHalfWidth + b.PushboxHalfWidth;
        float overlap = minDistance - Mathf.Abs(dx);

        if (overlap <= 0f) return;   // Not touching, nothing to do

        // Which way A gets pushed: away from B. B always goes the opposite way
        float aDirection;
        if (dx > 0f) aDirection = -1f;          // B is to the right, so A goes left
        else if (dx < 0f) aDirection = 1f;      // B is to the left, so A goes right
        else aDirection = -a.Facing;            // Step 15: exactly on top of each other, back away from where A faces

        float half = overlap / 2f;
        float aTarget = aX + aDirection * half;
        float bTarget = bX - aDirection * half;

        // Step 14: whatever a fighter can't move because of the stage edge, the other one moves instead
        float aLimit = a.PlayerMovement.MaxMove;
        float bLimit = b.PlayerMovement.MaxMove;

        float aClamped = Mathf.Clamp(aTarget, -aLimit, aLimit);
        bTarget -= aTarget - aClamped;          // A's leftover goes to B, in B's direction

        float bClamped = Mathf.Clamp(bTarget, -bLimit, bLimit);
        aClamped -= bTarget - bClamped;         // And the other way round, if B hit the edge
        aClamped = Mathf.Clamp(aClamped, -aLimit, aLimit);

        SetFighterX(a, aClamped);
        SetFighterX(b, bClamped);
    }

    private void SetFighterX(Fighter fighter, float x)
    {
        Vector3 position = fighter.transform.position;
        position.x = x;
        fighter.transform.position = position;
    }
}