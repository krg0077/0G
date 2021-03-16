using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _0G.Legacy
{
    /// <summary>
    /// Attack: Attack
    /// 1.  Attack allows a game object, specifically in prefab form, to be used as an "attack".
    /// 2.  Attack is to be added to a game object as a script/component*. The following must also be done:
    ///   a.  Attack must have a derived class specifically for this project (e.g. AttackMyGame).
    ///   b.  Any game object with the Attack component must exist on an "Attack" layer.
    ///   c.  Any game object with the Attack component must have its box collider set as a trigger.
    /// 3.  Attack is a key component of the Attack system, and is used in conjunction with the following classes:
    ///     AttackAbility, AttackAbilityUse, Attacker, AttackString, AttackTarget, and KnockBackCalcMode.
    /// 4.*-Attacker is abstract and must have a per-project derived class created (as mentioned in 2a);
    ///     the derived class itself must be added to a game object as a script/component.
    /// </summary>
    public abstract class Attack : MonoBehaviour, IBodyComponent
    {
        // EVENTS

        public event System.Action<Attack> Destroyed;

        // SERIALIZED FIELDS

        [Header("Optional Standalone Attack Ability")]

        [SerializeField, FormerlySerializedAs("m_attackAbility")]
        AttackAbility _attackAbility = default;

        [Header("Body")]

        [SerializeField]
        private GameObjectBody m_Body = default;

        // PRIVATE FIELDS

        Attacker _attacker;
        List<AttackTarget> _attackTargets = new List<AttackTarget>();
        BoxCollider _boxCollider;
        bool _isInitialized;
        TimeThread _timeThread;

        // MAIN PROPERTIES

        public virtual AttackAbility attackAbility { get { return _attackAbility; } }

        public virtual Attacker attacker { get { return _attacker; } }

        public virtual DamageDealtHandler damageDealtHandler { get; set; }

        public bool IsCompleted { get; private set; }

        public bool IsDisposed { get; private set; }

        public bool IsMain => !_attackAbility.IsAuxiliaryAttack;

        public Vector3 Velocity { get; set; }

        // SHORTCUT PROPERTIES

        public GameObjectBody Body => m_Body;

        private Hitbox Hitbox => m_Body.Refs.Hitbox;

        private Hurtbox Hurtbox => m_Body.Refs.Hurtbox;

        // INIT METHOD

        public void InitBody(GameObjectBody body)
        {
            m_Body = body;
        }

        // MONOBEHAVIOUR METHODS

        protected virtual void Awake()
        {
            Hitbox.TriggerEntered += OnHitboxTriggerEnter;
            Hitbox.TriggerExited += OnHitboxTriggerExit;
        }

        protected virtual void Start()
        {
            //if the attack is spawned normally, through AttackAbility, Init should have already been called by now
            //however, if it hasn't, and there is an Optional Standalone Attack Ability assigned, use this ability
            if (!_isInitialized)
            {
                if (_attackAbility != null)
                {
                    InitInternal();
                }
                else
                {
                    G.U.Warn("If this Attack exists on its own, " +
                        "it should probably have a Standalone Attack Ability.");
                }
            }
        }

        protected virtual void Update()
        {
            if (!_timeThread.isPaused && Velocity != Vector3.zero)
            {
                transform.Translate(Velocity * _timeThread.deltaTime, Space.World);
            }
        }

        // MAIN METHODS

        private void OnHitboxTriggerEnter(Collider other)
        {
            Hurtbox otherHurtbox = other.GetComponent<Hurtbox>();
            if (otherHurtbox == null) return;
            DamageTaker target = otherHurtbox.DamageTaker;
            if (target == null) return;
            AttackTarget at = _attackTargets.Find(x => x.target == target);
            if (at == null)
            {
                G.U.Assert(damageDealtHandler != null, "The damageDealtHandler must be set before collision occurs.");
                at = new AttackTarget(this, target, () => damageDealtHandler(this, target));
                _attackTargets.Add(at);
            }
            Vector3 attackPositionCenter = m_Body.CenterTransform.position;
            Vector3 hitPositionCenter = other.ClosestPoint(attackPositionCenter);
            at.StartTakingDamage(attackPositionCenter, hitPositionCenter);
        }

        private void OnHitboxTriggerExit(Collider other)
        {
            Hurtbox otherHurtbox = other.GetComponent<Hurtbox>();
            if (otherHurtbox == null) return;
            DamageTaker target = otherHurtbox.DamageTaker;
            if (target == null) return;
            AttackTarget at = _attackTargets.Find(x => x.target == target);
            G.U.Assert(at != null, string.Format(
                "Target \"{0}\" exited the trigger of Attack \"{1}\", but was not found in m_attackTargets.",
                other.name, name));
            at.StopTakingDamage();
        }

        private void OnDrawGizmos() // runs in edit mode, so don't rely upon actions done in Awake
        {
            Gizmos.color = Color.red;
            Vector3 p = transform.position;
            KRGGizmos.DrawCrosshairXY(p, 0.25f);
            if (Hitbox != null)
            {
                if (!Hitbox.Enabled)
                {
                    Gizmos.color = new Color(1, 0.5f, 0);
                }
                Vector3 bcCenter = Hitbox.Center;
                if (transform.localEulerAngles.y.Ap(180)) // hacky, but necessary
                {
                    bcCenter = bcCenter.Multiply(x: -1);
                }
                Gizmos.DrawWireCube(p + bcCenter, Hitbox.Size);
            }
        }

        protected virtual void OnDestroy()
        {
            ForceOnTriggerExit();
            ForceReleaseTargets();

            // revert the hurtbox replacement done in InitInternal
            Hurtbox attackerHurtbox = _attacker.Body.Refs.Hurtbox;
            if (attackerHurtbox != null && attackerHurtbox.gameObject.activeSelf)
            {
                attackerHurtbox.Enabled = true;
            }

            if (Hitbox != null)
            {
                Hitbox.TriggerExited -= OnHitboxTriggerExit;
                Hitbox.TriggerEntered -= OnHitboxTriggerEnter;
            }

            Destroyed?.Invoke(this);
        }

        public void Init(AttackAbility attackAbility, Attacker attacker)
        {
            if (_isInitialized)
            {
                G.U.Err("Init has already been called.");
                return;
            }
            _attackAbility = attackAbility;
            _attacker = attacker;
            InitInternal();
        }

        protected virtual void InitInternal()
        {
            _isInitialized = true;

            _timeThread = _attackAbility.TimeThread;

            if (_attackAbility.hasAttackLifetime)
            {
                _timeThread.AddTrigger(_attackAbility.attackLifetime, OnLifetimeEnd);
            }

            m_Body.FacingDirection = _attacker.Body.FacingDirection;

            // if the attack has a hurtbox, use it as a temporary replacement for the attacker's hurtbox
            if (Hurtbox != null && Hurtbox.gameObject.activeSelf)
            {
                Hurtbox attackerHurtbox = _attacker.Body.Refs.Hurtbox;
                if (attackerHurtbox != null && attackerHurtbox.gameObject.activeSelf)
                {
                    attackerHurtbox.Enabled = false;
                }
                Hurtbox.DamageTakerOverride = _attacker.Body.Refs.DamageTaker;
            }

            float travelSpeed = _attackAbility.travelSpeed;
            if (!travelSpeed.Ap(0))
            {
                Vector3 dir = _attackAbility.travelDirection.normalized;
                float flipX = m_Body.FacingDirection == Direction.Left ? -1 : 1;
                Velocity = new Vector3(flipX * dir.x * travelSpeed, dir.y * travelSpeed, dir.z * travelSpeed);
            }

            float attackDelay = _attackAbility.attackDelay;
            if (attackDelay > 0)
            {
                _timeThread.AddTrigger(attackDelay, x =>
                {
                    gameObject.SetActive(true);
                    PlayAttackSFX();
                });
                gameObject.SetActive(false);
            }
            else
            {
                PlayAttackSFX();
            }
        }

        protected virtual void PlayAttackSFX()
        {
            string sfxFmodEvent = _attackAbility.sfxFmodEvent;
            if (!string.IsNullOrEmpty(sfxFmodEvent))
            {
                if (_attackAbility.sfxFollowsAttacker)
                {
                    G.audio.PlaySFX(sfxFmodEvent, transform);
                }
                else
                {
                    G.audio.PlaySFX(sfxFmodEvent, transform.position);
                }
            }
        }

        private void OnLifetimeEnd(TimeTrigger tt)
        {
            Dispose(true);
        }

        public void Dispose(bool isCompleted)
        {
            if (this != null && !IsDisposed)
            {
                IsCompleted = isCompleted;
                IsDisposed = true;
                m_Body.Dispose();
            }
        }

        /// <summary>
        /// OnTriggerExit is often _randomly_ not called when the attack is destroyed.
        /// This method is called before destroying to ensure OnTriggerExit is called.
        /// TODO: This sometimes does not work properly, and may be obsolete now that ForceReleaseTargets exists.
        /// </summary>
        void ForceOnTriggerExit()
        {
            transform.Translate(-1000, -1000, -1000);
        }

        /// <summary>
        /// If the PC and an enemy hit each other at same time, and the enemy's attack is cancelled -- thus calling the
        /// G.End(...) method on this attack -- all while in the same frame... ForceOnTriggerExit will not actually
        /// force OnTriggerExit to be called. This may be because OnTriggerEnter and OnTriggerExit can't both be called
        /// in the same frame. Whatever the case, this method ensures that all targets are released from further damage.
        /// NOTE: This must be called from OnDestroy in order to work properly.
        /// </summary>
        void ForceReleaseTargets()
        {
            AttackTarget at;
            for (int i = 0; i < _attackTargets.Count; i++)
            {
                at = _attackTargets[i];
                if (at != null && at.target != null)
                {
                    at.StopTakingDamage();
                }
            }
            _attackTargets.Clear();
        }
    }
}