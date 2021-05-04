using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if NS_DG_TWEENING
using DG.Tweening;
#endif

namespace _0G.Legacy
{
    [ExecuteAlways]
    public class GraphicController : MonoBehaviour, IBodyComponent
    {
        // DELEGATES

        public delegate void AnimationEndHandler(GraphicController graphicController, bool isCompleted);

        // EVENTS

        public event AnimationEndHandler AnimationEnded;
        public event RasterAnimationState.StateHandler FrameSequenceStarted;
        public event RasterAnimationState.StateHandler FrameSequenceStopped;
        public event RasterAnimationState.StateHandler FrameSequencePlayLoopStarted;
        public event RasterAnimationState.StateHandler FrameSequencePlayLoopStopped;

        // SERIALIZED FIELDS

        [SerializeField, Tooltip("Optional priority standalone animation.")]
        private RasterAnimation m_StandaloneAnimation = default;

        [SerializeField, Tooltip("Override the default character sprite *within the Unity editor only*.")]
        private Texture2D m_EditorSpriteOverride = default;

        [SerializeField]
        private GameObjectBody m_Body = default;

        // PROTECTED FIELDS

        protected float m_SpeedMultiplier = 1;

        // PRIVATE FIELDS

        private AnimationEndHandler m_AnimationCallback;

        private AnimationContext m_AnimationContext;

        private int m_AnimationFrameListIndex;

        private float m_AnimationFrameTimeElapsed;

        private int m_AnimationImageCount;

        private int m_AnimationImageIndex;

        private TimeTrigger m_FlickerTimeTrigger;

        private Material m_Material;

        private ParticleSystem m_ParticleSystemRoot;

        private System.Action m_ParticleSystemStopCallback;

        private RasterAnimationLoopMode m_RasterAnimationLoopMode;

        private RasterAnimationState m_RasterAnimationState;

        private RawImage m_RawImage;

        private Renderer m_Renderer;

        private List<MonoBehaviour> m_RenderLocks;

        private TrailRenderer m_TrailRenderer;

        private float m_TrailRendererOrigTime;

        private bool m_WaitForAdvanceFrameSequence;

        // COMPOUND PROPERTIES

        private Color m_ImageColor = Color.white;

        public Color ImageColor
        {
            get => m_ImageColor;
            set
            {
                m_ImageColor = value;
                RefreshAnimationImage();
            }
        }

        // STORAGE PROPERTIES

        protected MeshSortingLayer MeshSortingLayer { get; private set; }

        protected RasterAnimation RasterAnimation { get; private set; }

        // STANDARD PROPERTIES

        /// <summary>
        /// Should not be set publicly. Use LockRender/UnlockRender instead.
        /// </summary>
        public bool IsRendered
        {
            get
            {
                if (m_Renderer != null)
                {
                    return m_Renderer.enabled;
                }
                return false;
            }
            set
            {
                if (m_Renderer != null)
                {
                    m_Renderer.enabled = value;
                }
            }
        }

        // SHORTCUT PROPERTIES

        public GameObjectBody Body => m_Body;

        public Texture2D EditorSprite
        {
            get
            {
                if (m_EditorSpriteOverride != null)
                {
                    return m_EditorSpriteOverride;
                }
                else if (CharacterDossier != null)
                {
                    return CharacterDossier.GraphicData.EditorSprite;
                }
                return null;
            }
        }

        public float FrameSequenceDuration => m_RasterAnimationState.FrameSequenceDurationFull / m_SpeedMultiplier;

        public bool IsAnimationPlaying => m_AnimationContext != AnimationContext.None;

        protected virtual GameObject GraphicGameObject => m_Body?.Refs.GraphicGameObject ?? gameObject;

        protected virtual bool IsTimePaused => TimeThread.isPaused;

        protected virtual TimeThread TimeThread => m_Body != null ? m_Body.TimeThread : G.time.GetTimeThread(TimeThreadInstance.Field);

        private Material BaseSharedMaterial => CharacterDossier?.GraphicData.BaseSharedMaterial;

        private CharacterDossier CharacterDossier => m_Body.CharacterDossier;

        private bool IsStandaloneCharacterAnimation => m_Body.gameObject.CompareTag(CharacterTag.Animation.ToString());

        // MONOBEHAVIOUR METHODS

        protected virtual void Start()
        {
            InitRenderer();
            InitMaterial();
            InitRawImage();
            InitMeshSort();
            InitStandaloneAnimation();
            if (m_Body != null)
            {
                if (m_Body.IsCharacter) InitCharacter();
                if (m_Body.IsVFX) InitVFX();
            }
            AddPauseAndUnpauseHandlers();
        }

        protected virtual void Update()
        {
            if (G.U.IsEditMode(this) || IsTimePaused) return;

            if (!RasterAnimation.IsSet() && m_Body.IsSet() && m_Body.IsCharacter &&
                G.obj.RasterAnimations.ContainsKey(CharacterDossier.IdleAnimationName))
            {
                SetAnimation(AnimationContext.Idle, CharacterDossier.IdleAnimationName);
            }

            if (!RasterAnimation.IsSet()) return;

            m_AnimationFrameTimeElapsed += TimeThread.deltaTime;
            while (m_AnimationImageCount > 0)
            {
                float frameTime = RasterAnimation.SecondsPerFrame / m_SpeedMultiplier;
                if (m_AnimationFrameTimeElapsed >= frameTime)
                {
                    m_AnimationFrameTimeElapsed -= frameTime;
                    AdvanceImageIndex();
                }
                else
                {
                    break;
                }
            }
            
            RefreshAnimationImage();
        }

        protected virtual void OnParticleSystemStopped()
        {
            m_ParticleSystemStopCallback?.Invoke();
            m_ParticleSystemStopCallback = null;
        }

        protected virtual void OnDestroy()
        {
            RemovePauseAndUnpauseHandlers();

            RemoveCharacterStateHandlers();

            UnloadRasterAnimationState();

            if (G.U.IsPlayMode(this))
            {
                DestroyImmediate(m_Material);
            }
        }

        // INITIALIZATION METHODS

        public void InitBody(GameObjectBody body)
        {
            m_Body = body;
        }

        private void InitRenderer()
        {
            m_Renderer = GraphicGameObject.GetComponent<Renderer>();
            if (m_Renderer == null || m_Body == null || BaseSharedMaterial == null) return;
            m_Renderer.sharedMaterial = BaseSharedMaterial;
        }

        private void InitMaterial()
        {
            if (m_Renderer == null) return;
            m_Material = G.U.IsEditMode(this) ? m_Renderer.sharedMaterial : m_Renderer.material; // instance
        }

        private void InitRawImage()
        {
            m_RawImage = GraphicGameObject.GetComponent<RawImage>();
        }

        private void InitMeshSort()
        {
            if (GraphicGameObject.GetComponent<MeshFilter>() == null) return;
            MeshSortingLayer = G.U.Guarantee<MeshSortingLayer>(GraphicGameObject);
        }

        private void InitStandaloneAnimation()
        {
            if (m_StandaloneAnimation == null) return;
            SetAnimation(AnimationContext.Priority, m_StandaloneAnimation);
        }

        private void InitCharacter()
        {
            if (RasterAnimation == null) SetTexture(EditorSprite);
            if (G.U.IsPlayMode(this)) AddCharacterStateHandlers();
        }

        private void InitVFX()
        {
            // the root system must be on this game object in order to invoke OnParticleSystemStopped
            m_ParticleSystemRoot = GetComponent<ParticleSystem>();
            m_TrailRenderer = GetComponentInChildren<TrailRenderer>();
            m_TrailRendererOrigTime = m_TrailRenderer != null ? m_TrailRenderer.time : 0;
        }

        // MAIN METHODS

        public void RefreshAnimationImage()
        {
            if (!RasterAnimation.IsSet() || RasterAnimation.Textures == null) return;
            m_AnimationImageIndex = Mathf.Min(m_AnimationImageIndex, m_AnimationImageCount - 1);
            SetTexture(RasterAnimation.Textures[m_AnimationImageIndex]);
        }

        private void SetTexture(Texture texture)
        {
            if (m_RawImage != null)
            {
                m_RawImage.texture = texture;
                m_RawImage.color = ImageColor;
            }
            else if (m_Material != null)
            {
                m_Material.mainTexture = texture;
                m_Material.color = ImageColor;
            }
        }

        public void SetAnimation(AnimationContext context, string animationName, AnimationEndHandler callback = null)
        {
            if (G.obj.RasterAnimations.ContainsKey(animationName))
            {
                SetAnimation(context, G.obj.RasterAnimations[animationName], callback);
            }
            else
            {
                Debug.LogErrorFormat("Unable to find animation {0}.", animationName);
            }
        }

        public void SetAnimation(AnimationContext context, RasterAnimation rasterAnimation, AnimationEndHandler callback = null)
        {
            // handle an interrupted animation
            if (m_AnimationContext != AnimationContext.None)
            {
                OnAnimationEnd(false, false);
            }

            OnAnimationClear();

            RasterAnimation = rasterAnimation;
            rasterAnimation.LoadTextures();

            m_AnimationCallback = callback;
            m_AnimationContext = context;
            m_AnimationFrameListIndex = 0;
            m_AnimationFrameTimeElapsed = 0;
            m_AnimationImageCount = rasterAnimation.Textures.Length;
            m_AnimationImageIndex = 0;
            m_WaitForAdvanceFrameSequence = false;

            if (G.U.IsPlayMode(this))
            {
                RasterAnimationOptions options = new RasterAnimationOptions
                {
                    FrameSequenceStartHandler = OnFrameSequenceStart,
                    FrameSequenceStopHandler = OnFrameSequenceStop,
                    FrameSequencePlayLoopStartHandler = OnFrameSequencePlayLoopStart,
                    FrameSequencePlayLoopStopHandler = OnFrameSequencePlayLoopStop,
                    InfiniteLoopReplacement = m_Body != null && m_Body.IsGalleryAnimation ? 3 : 0,
                };
                LoadNewRasterAnimationState(rasterAnimation, options);
                m_AnimationImageIndex = m_RasterAnimationState.frameSequenceFromFrame - 1; // 1-based -> 0-based
            }

            OnAnimationSet();

            RefreshAnimationImage();
        }

        public void RestartAnimation() => m_RasterAnimationState.Reset();

        public void EndAnimation(AnimationContext context)
        {
            // handle an interrupted animation
            if (m_AnimationContext == context)
            {
                OnAnimationEnd(false);
            }
            else
            {
                G.U.Err("Attempting to end animation context {0}, but current context is {1}.",
                    context, m_AnimationContext);
            }
        }

        public void SetSharedMaterial(Material sharedMaterial)
        {
            m_Renderer.sharedMaterial = sharedMaterial;
            m_Material = G.U.IsEditMode(this) ? m_Renderer.sharedMaterial : m_Renderer.material; // instance
            RefreshAnimationImage();
        }

        public void SetDamageColor(float seconds)
        {
#if NS_DG_TWEENING
            ImageColor = new Color(1, 0.2f, 0.2f);
            TimeThread.AddTween(DOTween
                .To(() => ImageColor, x => ImageColor = x, Color.white, seconds)
                .SetEase(Ease.OutSine)
            );
#else
            G.U.Err("This function requires DG.Tweening (DOTween).");
#endif
        }

        // RENDER METHODS

        public void SetFlicker(float flickerRate = 20)
        {
            if (m_FlickerTimeTrigger != null)
            {
                m_FlickerTimeTrigger.Dispose();
            }
            m_FlickerTimeTrigger = TimeThread.AddTrigger(1f / flickerRate, DoFlicker);
        }

        private void DoFlicker(TimeTrigger tt)
        {
            IsRendered = !IsRendered;
            tt.Proceed();
        }

        public void EndFlicker()
        {
            if (m_FlickerTimeTrigger != null)
            {
                m_FlickerTimeTrigger.Dispose();
                m_FlickerTimeTrigger = null;
            }
            IsRendered = true;
        }

        public void LockRender(MonoBehaviour monoBehaviour)
        {
            if (monoBehaviour == null)
            {
                G.U.Warn("Null argument. A locking MonoBehaviour must be supplied.");
                return;
            }
            if (m_RenderLocks == null)
            {
                m_RenderLocks = new List<MonoBehaviour>();
            }
            m_RenderLocks.Add(monoBehaviour);
            IsRendered = false;
        }

        public void UnlockRender(MonoBehaviour monoBehaviour)
        {
            m_RenderLocks.Remove(monoBehaviour);
            IsRendered = m_RenderLocks.Count == 0;
        }

        // IMAGE INDEX / FRAME SEQUENCE METHODS

        public void AdvanceImageIndex()
        {
            if (m_RasterAnimationState.AdvanceFrame(
                ref m_AnimationFrameListIndex, out int frameNumber))
            {
                // GraphicController uses zero-based image index (m_AnimationImageIndex)
                // RasterAnimation uses one-based frame number (frameNumber)
                m_AnimationImageIndex = frameNumber - 1;
                RefreshAnimationImage();
            }
            else
            {
                OnAnimationEnd(true);
            }
        }

        public void ReverseImageIndex()
        {
            if (m_RasterAnimationState.ReverseFrame(
                ref m_AnimationFrameListIndex, out int frameNumber))
            {
                // GraphicController uses zero-based image index (m_AnimationImageIndex)
                // RasterAnimation uses one-based frame number (frameNumber)
                m_AnimationImageIndex = frameNumber - 1;
                RefreshAnimationImage();
            }
        }

        public void AdvanceFrameSequence(bool isImmediate = true)
        {
            if (!isImmediate)
            {
                m_WaitForAdvanceFrameSequence = true;
            }
            else if (m_RasterAnimationState.AdvanceFrameSequence(
                ref m_AnimationFrameListIndex, out int frameNumber))
            {
                // GraphicController uses zero-based image index (m_AnimationImageIndex)
                // RasterAnimation uses one-based frame number (frameNumber)
                m_AnimationImageIndex = frameNumber - 1;
            }
            else
            {
                OnAnimationEnd(true);
            }
        }

        public void ReverseFrameSequence()
        {
            if (m_RasterAnimationState.ReverseFrameSequence(
                ref m_AnimationFrameListIndex, out int frameNumber))
            {
                // GraphicController uses zero-based image index (m_AnimationImageIndex)
                // RasterAnimation uses one-based frame number (frameNumber)
                m_AnimationImageIndex = frameNumber - 1;
            }
        }

        public void GoToFrameSequenceWithPreAction(int actionId)
        {
            if (m_RasterAnimationState.GoToFrameSequenceWithPreAction(
                actionId, ref m_AnimationFrameListIndex, out int frameNumber))
            {
                // GraphicController uses zero-based image index (m_AnimationImageIndex)
                // RasterAnimation uses one-based frame number (frameNumber)
                m_AnimationImageIndex = frameNumber - 1;
            }
        }

        public void SetLoopMode(RasterAnimationLoopMode loopMode)
        {
            m_RasterAnimationLoopMode = loopMode;
            m_RasterAnimationState?.SetLoopMode(loopMode);
        }

        private void OnFrameSequenceStart(RasterAnimationState state)
        {
            FrameSequenceStarted?.Invoke(state);
        }

        private void OnFrameSequenceStop(RasterAnimationState state)
        {
            FrameSequenceStopped?.Invoke(state);
        }

        private void OnFrameSequencePlayLoopStart(RasterAnimationState state)
        {
            if (m_WaitForAdvanceFrameSequence)
            {
                m_WaitForAdvanceFrameSequence = false;
                // advance only if not at the start of a new sequence
                if (state.frameSequencePlayIndex > 0)
                {
                    AdvanceFrameSequence(true);
                    return;
                }
            }
            FrameSequencePlayLoopStarted?.Invoke(state);
        }

        private void OnFrameSequencePlayLoopStop(RasterAnimationState state)
        {
            FrameSequencePlayLoopStopped?.Invoke(state);
        }

        private void OnFrameSequenceAudioTriggered(FrameSequence.AudioTrigger audioTrigger)
        {
            Vector3 position = m_Body.IsSet() ? m_Body.CenterTransform.position : transform.position;
            G.audio.PlaySFX(audioTrigger.AudioEvent, position);
        }

        protected virtual void OnFrameChanged(RasterAnimationState state, int frameListIndex) { }

        protected virtual void OnAnimationClear() { }

        protected virtual void OnAnimationSet() { }

        private void OnAnimationEnd(bool isCompleted, bool reassessState = true)
        {
            m_AnimationContext = AnimationContext.None;

            // invoke main callback and fire event as applicable
            m_AnimationCallback?.Invoke(this, isCompleted);
            m_AnimationCallback = null;
            AnimationEnded?.Invoke(this, isCompleted);

            // if no new animation set by callback/event, reassess state as applicable
            if (m_AnimationContext == AnimationContext.None && reassessState)
            {
                OnCharacterStateChange(0, false);
            }
        }

        // RASTER ANIMATION STATE METHODS

        private void LoadNewRasterAnimationState(RasterAnimation rasterAnimation, RasterAnimationOptions options)
        {
            UnloadRasterAnimationState();
            // assign the state reference first
            // then call reset, which will invoke events that may need the state reference
            m_RasterAnimationState = new RasterAnimationState(rasterAnimation, options);
            m_RasterAnimationState.Reset();
            m_RasterAnimationState.FrameSequenceAudioTriggered += OnFrameSequenceAudioTriggered;
            m_RasterAnimationState.FrameChanged += OnFrameChanged;
            m_RasterAnimationState.SetLoopMode(m_RasterAnimationLoopMode);
        }

        private void UnloadRasterAnimationState()
        {
            if (m_RasterAnimationState != null)
            {
                m_RasterAnimationState.FrameChanged -= OnFrameChanged;
                m_RasterAnimationState.FrameSequenceAudioTriggered -= OnFrameSequenceAudioTriggered;
                m_RasterAnimationState = null;
            }
        }

        // CHARACTER STATE METHODS

        private void AddCharacterStateHandlers()
        {
            if (m_Body == null || CharacterDossier == null || IsStandaloneCharacterAnimation) return;

            List<StateAnimation> stateAnimations = CharacterDossier.GraphicData.StateAnimations;

            for (int i = 0; i < stateAnimations.Count; ++i)
            {
                StateAnimation sa = stateAnimations[i];

                m_Body.Refs.StateOwner.AddStateHandler(sa.state, OnCharacterStateChange);
            }
        }

        private void RemoveCharacterStateHandlers()
        {
            if (G.U.IsEditMode(this)) return;

            if (m_Body == null || CharacterDossier == null || IsStandaloneCharacterAnimation) return;

            List<StateAnimation> stateAnimations = CharacterDossier.GraphicData.StateAnimations;

            for (int i = 0; i < stateAnimations.Count; ++i)
            {
                StateAnimation sa = stateAnimations[i];

                m_Body.Refs.StateOwner.RemoveStateHandler(sa.state, OnCharacterStateChange);
            }
        }

        protected virtual void OnCharacterStateChange(ulong state, bool value)
        {
            // ignore state change if currently playing a higher priority animation
            if (m_AnimationContext > AnimationContext.CharacterState) return;

            if (m_Body == null || CharacterDossier == null) return;

            List<StateAnimation> stateAnimations = CharacterDossier.GraphicData.StateAnimations;

            AnimationContext context = AnimationContext.Idle;
            string animationName = CharacterDossier.IdleAnimationName;

            for (int i = 0; i < stateAnimations.Count; ++i)
            {
                StateAnimation sa = stateAnimations[i];

                // state 0 is also idle
                if (sa.state != 0 && m_Body.Refs.StateOwner.HasState(sa.state) || (value && state == sa.state))
                {
                    context = AnimationContext.CharacterState;
                    animationName = sa.animationName;
                    break;
                }
            }

            // ReSharper disable once Unity.PerformanceCriticalCodeNullComparison
            if (RasterAnimation == null || animationName != RasterAnimation.name)
            {
                SetAnimation(context, animationName);
            }
        }

        // VFX CONTROL METHODS

        public void StopVFX(System.Action callback)
        {
            if (m_TrailRenderer != null)
            {
                m_TrailRenderer.emitting = false;
            }
            if (m_ParticleSystemRoot != null)
            {
                ParticleSystem.MainModule main = m_ParticleSystemRoot.main;
                main.stopAction = ParticleSystemStopAction.Callback;
                m_ParticleSystemStopCallback = callback;
                m_ParticleSystemRoot.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            else
            {
                callback?.Invoke();
            }
        }

        // PAUSE / UNPAUSE METHODS

        private void AddPauseAndUnpauseHandlers()
        {
            if (G.U.IsEditMode(this)) return;

            TimeThread.AddPauseHandler(OnPause);
            TimeThread.AddUnpauseHandler(OnUnpause);
        }

        private void RemovePauseAndUnpauseHandlers()
        {
            if (G.U.IsEditMode(this)) return;

            TimeThread.RemoveUnpauseHandler(OnUnpause);
            TimeThread.RemovePauseHandler(OnPause);
        }

        private void OnPause()
        {
            if (m_TrailRenderer != null)
            {
                m_TrailRenderer.time = float.MaxValue;
            }
            if (m_ParticleSystemRoot != null)
            {
                m_ParticleSystemRoot.Pause(true);
            }
        }

        private void OnUnpause()
        {
            if (m_TrailRenderer != null)
            {
                m_TrailRenderer.time = m_TrailRendererOrigTime;
            }
            if (m_ParticleSystemRoot != null)
            {
                m_ParticleSystemRoot.Play(true);
            }
        }

        // EDITOR METHODS

        /// <summary>
        /// Intended only for specialized editor use, such as an animation preview.
        /// </summary>
        public void ClearMaterialTexture()
        {
            m_Material.mainTexture = null;
        }

        /// <summary>
        /// Intended only for specialized editor use, such as an animation preview.
        /// </summary>
        public void ResetStandaloneAnimation(RasterAnimationOptions options)
        {
            if (m_StandaloneAnimation == null) return;
            SetAnimation(m_AnimationContext, m_StandaloneAnimation);
            LoadNewRasterAnimationState(m_StandaloneAnimation, options);
            m_AnimationImageIndex = m_RasterAnimationState.frameSequenceFromFrame - 1; // 1-based -> 0-based
        }

        /// <summary>
        /// Intended only for specialized editor use, such as save processing.
        /// </summary>
        public void UnloadRendererMaterials()
        {
            if (m_Renderer == null) return;
            m_Renderer.sharedMaterials = new Material[1] { null };
        }
    }
}