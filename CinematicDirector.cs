using System.Collections;
using _0G.Legacy;
using UnityEngine;

namespace _0G
{
    public class CinematicDirector : MonoBehaviour
    {
        // STATIC MEMBERS
        
        public static CinematicDirector Instance { get; private set; }

        public static void Setup(GameObject anchor) => anchor.AddComponent<CinematicDirector>();

        // DELEGATES & EVENTS
        
        public delegate void CinematicSequenceHandler(CinematicSequence sequence);

        public event CinematicSequenceHandler CinematicSequenceStarted;
        public event CinematicSequenceHandler CinematicSequenceStopped;
        
        // PROPERTIES

        private static TimeThread TimeThread => G.time.GetTimeThread(TimeThreadInstance.Cinematic);

        // MONOBEHAVIOUR METHODS
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
        
        // SEQUENCE METHODS

        public void RunSequence(CinematicSequence sequence)
        {
            StartCoroutine(RunSequenceRoutine(sequence));
        }

        private IEnumerator RunSequenceRoutine(CinematicSequence sequence)
        {
            CinematicSequenceStarted?.Invoke(sequence);
            PrepareSequence();
            foreach (CinematicAction action in sequence.Actions)
            {
                yield return DoActionCommand(action);
            }
            CleanupSequence();
            CinematicSequenceStopped?.Invoke(sequence);
        }

        protected virtual void PrepareSequence()
        {
            // lock player controls and deactivate all enemy AI
        }

        protected virtual void CleanupSequence()
        {
            // revert settings to resume gameplay
        }
        
        // COMMAND METHODS

        private IEnumerator DoActionCommand(CinematicAction action)
        {
            switch ((CinematicCommand)action.Command)
            {
                case CinematicCommand.None:
                    break;
                case CinematicCommand.CameraPanTo:
                    // TODO
                    break;
                case CinematicCommand.CameraZoomTo:
                    yield return CameraZoomTo(action);
                    break;
                case CinematicCommand.CharacterAnimate:
                    yield return CharacterAnimate(action);
                    break;
                case CinematicCommand.CharacterMoveTo:
                    yield return CharacterMoveTo(action);
                    break;
                case CinematicCommand.CharacterWarpTo:
                    CharacterWarpTo(action);
                    break;
                case CinematicCommand.Flowchart:
                    yield return Flowchart(action);
                    break;
                case CinematicCommand.Wait:
                    yield return TimeThread.WaitForSeconds(action.Float1);
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException();
            }
        }

        protected virtual IEnumerator CameraZoomTo(CinematicAction action)
        {
            yield return null;
        }

        private IEnumerator CharacterAnimate(CinematicAction action)
        {
            GameObjectBody cBody = G.obj.GetBodyByCharacterID(action.Character);
            AssetPackAccess access = G.obj.AccessForGameplay;
            CharacterDossier cd = cBody.CharacterDossier;
            string animName = action.String1;
            bool animExists = G.obj.HasAnimation(animName);
            bool packExists = false, animationEnd = false;
            if (!animExists)
            {
                packExists = G.obj.IsAssetPackLoaded<CharacterDossier>(cd.CharacterID, access);
                if (!packExists)
                {
                    G.obj.LoadAssetPack<CharacterDossier>(cd.CharacterID, access);
                }
                G.obj.AddAnimation(cd, animName, access);
            }
            cBody.Refs.GraphicController.SetAnimation(AnimationContext.Priority, animName,
                (_1, _2) => animationEnd = true);
            while (!animationEnd) yield return null;
            if (!animExists)
            {
                G.obj.RemoveAnimation(animName);
                if (!packExists)
                {
                    G.obj.UnloadAssetPack<CharacterDossier>(cd.CharacterID);
                }
            }
        }

        private IEnumerator CharacterMoveTo(CinematicAction action)
        {
            GameObjectBody cBody = G.obj.GetBodyByCharacterID(action.Character);
            var cMove = cBody.GetComponent<IMove>();
            yield return cMove.MoveTo(new Vector3(action.Float1, action.Float2, action.Float3));
        }

        private void CharacterWarpTo(CinematicAction action)
        {
            GameObjectBody cBody = G.obj.GetBodyByCharacterID(action.Character);
            cBody.transform.position = new Vector3(action.Float1, action.Float2, action.Float3);
        }

        protected virtual IEnumerator Flowchart(CinematicAction action)
        {
            yield return null;
        }
    }
}