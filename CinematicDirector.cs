using System;
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
            // TODO: stop field/gameplay time, start cinematic time, lock player controls, and deactivate all enemy AI
            foreach (CinematicAction action in sequence.Actions)
            {
                yield return DoActionCommand(action);
            }
            // TODO: revert settings to resume gameplay
            CinematicSequenceStopped?.Invoke(sequence);
        }

        private IEnumerator DoActionCommand(CinematicAction action)
        {
            switch ((CinematicCommand)action.Command)
            {
                case CinematicCommand.None:
                    break;
                case CinematicCommand.CameraPanTo:
                    // TODO
                    break;
                case CinematicCommand.CharacterMoveTo:
                    yield return CharacterMoveTo(action);
                    break;
                case CinematicCommand.CharacterWarpTo:
                    CharacterWarpTo(action);
                    break;
                case CinematicCommand.CharacterAnimate:
                    // TODO
                    break;
                case CinematicCommand.Wait:
                    yield return TimeThread.WaitForSeconds(action.Float1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
    }
}