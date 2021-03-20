namespace _0G.Legacy
{
    public struct RasterAnimationOptions
    {
        public RasterAnimationState.StateHandler FrameSequenceStartHandler;
        public RasterAnimationState.StateHandler FrameSequenceStopHandler;
        public RasterAnimationState.StateHandler FrameSequencePlayLoopStartHandler;
        public RasterAnimationState.StateHandler FrameSequencePlayLoopStopHandler;
        public int InfiniteLoopReplacement; // 0 will retain the infinite loop(s)
    }
}