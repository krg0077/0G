using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace _0G.Legacy
{
    public class RasterAnimationState
    {
        // CONSTANTS

        private const bool ADVANCE = true;
        private const bool REVERSE = false;

        // DELEGATES

        public delegate void StateHandler(RasterAnimationState ras);

        public delegate void AudioHandler(FrameSequence.AudioTrigger audioTrigger);

        // EVENTS

        /// <summary>
        /// Occurs when a frame sequence starts.
        /// </summary>
        public event StateHandler FrameSequenceStarted;

        /// <summary>
        /// Occurs when a frame sequence stops.
        /// </summary>
        public event StateHandler FrameSequenceStopped;

        /// <summary>
        /// Occurs when a frame sequence starts or after the play index is incremented.
        /// </summary>
        public event StateHandler FrameSequencePlayLoopStarted;

        /// <summary>
        /// Occurs when a frame sequence stops or before the play index is incremented.
        /// </summary>
        public event StateHandler FrameSequencePlayLoopStopped;

        /// <summary>
        /// Occurs when a frame sequence audio trigger is ready to play.
        /// </summary>
        public event AudioHandler FrameSequenceAudioTriggered;

        // FIELDS

        /// <summary>
        /// The raster animation scriptable object.
        /// </summary>
        private RasterAnimation _rasterAnimation;

        /// <summary>
        /// The current loop index. If the raster animation's "Loop" setting is unchecked, this will always be 0.
        /// </summary>
        private int _loopIndex;

        /// <summary>
        /// The current loop mode. This is different from the raster animation's "Loop" setting.
        /// </summary>
        private RasterAnimationLoopMode _loopMode;

        /// <summary>
        /// The current frame sequence index (zero-based).
        /// </summary>
        private int _frameSequenceIndex;

        /// <summary>
        /// The name of the current frame sequence.
        /// </summary>
        protected string _frameSequenceName;

        /// <summary>
        /// The ordered list of frames to play in the current frame sequence.
        /// </summary>
        private ReadOnlyCollection<int> _frameSequenceFrameList;

        /// <summary>
        /// The current frame sequence's play count (may be a cached random value).
        /// </summary>
        private int _frameSequencePlayCount;

        /// <summary>
        /// The current frame sequence's play index (zero-based).
        /// </summary>
        private int _frameSequencePlayIndex;

        private RasterAnimationOptions m_Options;

        // PROPERTIES

        public virtual int frameSequenceIndex => _frameSequenceIndex;

        public virtual string frameSequenceName => _frameSequenceName;

        public virtual int frameSequenceFromFrame => _frameSequenceFrameList[0];

        public virtual RasterAnimation rasterAnimation => _rasterAnimation;

        public List<int> FrameSequencePreActions { get; private set; }

        public List<FrameSequence.AudioTrigger> FrameSequenceAudioTriggers { get; private set; }

        // CONSTRUCTOR

        public RasterAnimationState(RasterAnimation rasterAnimation, RasterAnimationOptions options)
        {
            _rasterAnimation = rasterAnimation;
            G.U.Require(_rasterAnimation, "Raster Animation", "this Raster Animation State");
            m_Options = options;
            FrameSequenceStarted = options.FrameSequenceStartHandler;
            FrameSequenceStopped = options.FrameSequenceStopHandler;
            FrameSequencePlayLoopStarted = options.FrameSequencePlayLoopStartHandler;
            FrameSequencePlayLoopStopped = options.FrameSequencePlayLoopStopHandler;
            Reset();
            rasterAnimation.MarkAsPlayed();
        }

        // PUBLIC METHODS

        /// <summary>
        /// Advances the frame number.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">The index for the array containing the list of frame numbers.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool AdvanceFrame(ref int frameListIndex, out int frameNumber)
        {
            frameNumber = 0;
            // are there remaining frames in this sequence?
            if (frameListIndex < _frameSequenceFrameList.Count - 1)
            {
                // yep; go to the next frame
                frameNumber = _frameSequenceFrameList[++frameListIndex];
            }
            // do we need to loop this sequence?
            else if (CheckFrameSequenceLoop(ADVANCE))
            {
                FrameSequencePlayLoopStopped?.Invoke(this);
                ++_frameSequencePlayIndex;
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
                FrameSequencePlayLoopStarted?.Invoke(this);
            }
            // are there remaining frame sequences in this animation?
            else if (_frameSequenceIndex < _rasterAnimation.frameSequenceCount - 1)
            {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex + 1, ADVANCE);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            // do we need to loop this animation?
            else if (CheckAnimationLoop(ADVANCE))
            {
                InvokeFrameSequenceStopHandlers();
                ++_loopIndex;
                SetFrameSequence(_rasterAnimation.loopToSequence, ADVANCE);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            else
            {
                InvokeFrameSequenceStopHandlers();
                // the animation has finished playing
                return false;
            }
            OnFrameChanged(frameListIndex);
            return true;
        }

        /// <summary>
        /// Reverses the frame number.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">The index for the array containing the list of frame numbers.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool ReverseFrame(ref int frameListIndex, out int frameNumber)
        {
            frameNumber = 0;
            // are there remaining frames in this sequence?
            if (frameListIndex > 0)
            {
                // yep; go to the previous frame
                frameNumber = _frameSequenceFrameList[--frameListIndex];
            }
            // do we need to loop this sequence?
            else if (CheckFrameSequenceLoop(REVERSE))
            {
                FrameSequencePlayLoopStopped?.Invoke(this);
                --_frameSequencePlayIndex;
                frameListIndex = _frameSequenceFrameList.Count - 1;
                frameNumber = _frameSequenceFrameList[frameListIndex];
                FrameSequencePlayLoopStarted?.Invoke(this);
            }
            // are there remaining frame sequences in this animation?
            else if (_frameSequenceIndex > 0)
            {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex - 1, REVERSE);
                frameListIndex = _frameSequenceFrameList.Count - 1;
                frameNumber = _frameSequenceFrameList[frameListIndex];
            }
            // do we need to loop this animation?
            else if (CheckAnimationLoop(REVERSE))
            {
                InvokeFrameSequenceStopHandlers();
                --_loopIndex;
                SetFrameSequence(_rasterAnimation.frameSequenceCount - 1, REVERSE);
                frameListIndex = _frameSequenceFrameList.Count - 1;
                frameNumber = _frameSequenceFrameList[frameListIndex];
            }
            else
            {
                InvokeFrameSequenceStopHandlers();
                // the animation has finished playing
                return false;
            }
            OnFrameChanged(frameListIndex);
            return true;
        }

        /// <summary>
        /// Advances the frame sequence.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">The index for the array containing the list of frame numbers.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool AdvanceFrameSequence(ref int frameListIndex, out int frameNumber)
        {
            frameNumber = 0;
            // are there remaining frame sequences in this animation?
            if (_frameSequenceIndex < _rasterAnimation.frameSequenceCount - 1)
            {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex + 1, ADVANCE);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            // do we need to loop this animation?
            else if (CheckAnimationLoop(ADVANCE))
            {
                InvokeFrameSequenceStopHandlers();
                ++_loopIndex;
                SetFrameSequence(_rasterAnimation.loopToSequence, ADVANCE);
                frameListIndex = 0;
                frameNumber = _frameSequenceFrameList[0];
            }
            else
            {
                InvokeFrameSequenceStopHandlers();
                // the animation has finished playing
                return false;
            }
            OnFrameChanged(frameListIndex);
            return true;
        }

        /// <summary>
        /// Reverses the frame sequence. Will advance from the beginning of the previous sequence.
        /// </summary>
        /// <returns><c>true</c>, if the animation should continue playing, <c>false</c> otherwise.</returns>
        /// <param name="frameListIndex">The index for the array containing the list of frame numbers.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        public virtual bool ReverseFrameSequence(ref int frameListIndex, out int frameNumber)
        {
            frameNumber = 0;
            // are there remaining frame sequences in this animation?
            if (_frameSequenceIndex > 0)
            {
                InvokeFrameSequenceStopHandlers();
                SetFrameSequence(_frameSequenceIndex - 1, REVERSE);
                frameListIndex = 0; // <==== advance
                frameNumber = _frameSequenceFrameList[0]; // <==== advance
            }
            // do we need to loop this animation?
            else if (CheckAnimationLoop(REVERSE))
            {
                InvokeFrameSequenceStopHandlers();
                --_loopIndex;
                SetFrameSequence(_rasterAnimation.frameSequenceCount - 1, REVERSE);
                frameListIndex = 0; // <==== advance
                frameNumber = _frameSequenceFrameList[0]; // <==== advance
            }
            else
            {
                InvokeFrameSequenceStopHandlers();
                // the animation has finished playing
                return false;
            }
            OnFrameChanged(frameListIndex);
            return true;
        }

        /// <summary>
        /// Goes to the current or next frame sequence with the specified pre-action ID.
        /// NOTE: It will wrap around the animation to check earilier frame sequences as well.
        /// </summary>
        /// <param name="actionId">The specified pre-action ID.</param>
        /// <param name="frameListIndex">The index for the array containing the list of frame numbers.</param>
        /// <param name="frameNumber">Frame number (one-based).</param>
        /// <returns><c>true</c>, if this operation was successful, <c>false</c> otherwise.</returns>
        public virtual bool GoToFrameSequenceWithPreAction(int actionId, ref int frameListIndex, out int frameNumber)
        {
            bool doIndex(int i, ref int fListIndex, out int fNumber)
            {
                fNumber = 0;
                List<int> acts = _rasterAnimation.GetFrameSequencePreActions(i);
                for (int j = 0; j < acts.Count; ++j)
                {
                    if (acts[j] == actionId)
                    {
                        InvokeFrameSequenceStopHandlers();
                        SetFrameSequence(i, ADVANCE);
                        fListIndex = 0;
                        fNumber = _frameSequenceFrameList[0];
                        OnFrameChanged(fListIndex);
                        return true;
                    }
                }
                return false;
            }

            frameNumber = 0;
            for (int i = _frameSequenceIndex; i < _rasterAnimation.frameSequenceCount; ++i)
            {
                if (doIndex(i, ref frameListIndex, out frameNumber)) return true;
            }
            for (int i = 0; i < _frameSequenceIndex; ++i)
            {
                if (doIndex(i, ref frameListIndex, out frameNumber)) return true;
            }
            return false;
        }

        public void Reset()
        {
            SetFrameSequence(0, ADVANCE);
            OnFrameChanged(0);
        }

        public void SetLoopMode(RasterAnimationLoopMode loopMode) => _loopMode = loopMode;

        // PROTECTED METHODS

        /// <summary>
        /// Sets the frame sequence, or if not playable, advances to the next playable frame sequence.
        /// </summary>
        /// <param name="frameSequenceIndex">Frame sequence index.</param>
        /// <param name="advance">Advance? Else reverse.</param>
        protected virtual void SetFrameSequence(int frameSequenceIndex, bool advance)
        {
            if (!_rasterAnimation.hasPlayableFrameSequences)
            {
                G.U.Err("This Raster Animation must have playable Frame Sequences.", this, _rasterAnimation);
                return;
            }
            int playCount, fsLoopCount = 0;
            while (true)
            {
                if (advance)
                {
                    // if we are past the range, reset to 0
                    if (frameSequenceIndex >= _rasterAnimation.frameSequenceCount)
                    {
                        frameSequenceIndex = 0;
                    }
                }
                else
                {
                    // if we are past the range, reset to count - 1
                    if (frameSequenceIndex < 0)
                    {
                        frameSequenceIndex = _rasterAnimation.frameSequenceCount - 1;
                    }
                }
                // if it has a play count, we're good to go; break out of while loop
                playCount = _rasterAnimation.GetFrameSequencePlayCount(frameSequenceIndex);
                if (playCount > 0)
                {
                    break;
                }
                // check the next sequence
                frameSequenceIndex += advance ? 1 : -1;
                // handle worst-case scenario
                ++fsLoopCount;
                if (fsLoopCount >= _rasterAnimation.frameSequenceCountMax)
                {
                    G.U.Err("Stuck in an infinite loop.", this, _rasterAnimation);
                    return;
                }
            }
            if (playCount >= FrameSequence.INFINITE_PLAY_COUNT)
            {
                playCount = m_Options.InfiniteLoopReplacement > 0 ? m_Options.InfiniteLoopReplacement : int.MaxValue;
            }
            _frameSequenceIndex = frameSequenceIndex;
            _frameSequenceName = _rasterAnimation.GetFrameSequenceName(frameSequenceIndex);
            _frameSequenceFrameList = _rasterAnimation.GetFrameSequenceFrameList(frameSequenceIndex);
            _frameSequencePlayCount = playCount;
            _frameSequencePlayIndex = advance ? 0 : playCount - 1;
            FrameSequencePreActions = _rasterAnimation.GetFrameSequencePreActions(frameSequenceIndex);
            FrameSequenceAudioTriggers = _rasterAnimation.GetFrameSequenceAudioTriggers(frameSequenceIndex);
            InvokeFrameSequenceStartHandlers();
        }

        // PRIVATE METHODS

        private bool CheckAnimationLoop(bool advance)
        {
            switch (_loopMode)
            {
                case RasterAnimationLoopMode.LoopAnimationOn:
                    return true;
                case RasterAnimationLoopMode.LoopAnimationOff:
                case RasterAnimationLoopMode.LoopNothing:
                    return false;
                default:
                    return _rasterAnimation.DoesLoop(_loopIndex, advance);
            }
        }

        private bool CheckFrameSequenceLoop(bool advance)
        {
            switch (_loopMode)
            {
                case RasterAnimationLoopMode.LoopSequence:
                    return true;
                case RasterAnimationLoopMode.LoopNothing:
                    return false;
                default:
                    if (advance)
                        return _frameSequencePlayIndex < _frameSequencePlayCount - 1;
                    else
                        return _frameSequencePlayIndex > 0;
            }
        }

        private bool CheckPlayAudio(FrameSequence.AudioTrigger audioTrigger)
        {
            switch (audioTrigger.PlayStyle)
            {
                case AudioPlayStyle.None:
                    return false;
                case AudioPlayStyle.PlayOnce:
                    return _frameSequencePlayIndex == 0;
                case AudioPlayStyle.PlayEachIteration:
                    return true;
                default:
                    G.U.Unsupported(this, audioTrigger.PlayStyle);
                    return false;
            }
        }

        private void InvokeFrameSequenceStartHandlers()
        {
            FrameSequenceStarted?.Invoke(this);
            FrameSequencePlayLoopStarted?.Invoke(this);
        }

        private void InvokeFrameSequenceStopHandlers()
        {
            FrameSequencePlayLoopStopped?.Invoke(this);
            FrameSequenceStopped?.Invoke(this);
        }

        private void OnFrameChanged(int frameListIndex)
        {
            //play audio as needed
            for (int i = 0; i < FrameSequenceAudioTriggers.Count; ++i)
            {
                FrameSequence.AudioTrigger audioTrigger = FrameSequenceAudioTriggers[i];
                if (CheckPlayAudio(audioTrigger) && audioTrigger.FrameDelay == frameListIndex)
                {
                    FrameSequenceAudioTriggered?.Invoke(audioTrigger);
                }
            }
        }
    }
}