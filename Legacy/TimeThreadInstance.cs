namespace _0G.Legacy
{
    /// <summary>
    /// Time thread instance.
    /// See TimeManager for usage and restrictions.
    /// </summary>
    public enum TimeThreadInstance
    {
        // 0 ~ 5 reserved for _0G.Legacy
        
        // no time thread preference; use the script's default time thread instead
        UseDefault = -1,

        // always unscaled; never paused
        Application = 0,

        // should be used for functionality that can be paused, slowed, or sped up
        // NOTE: modal dialogue windows count as gameplay
        Gameplay = 1,

        // character movement/actions on the field; not in a modal dialogue window
        Field = 2,

        Unused_3 = 3,

        Unused_4 = 4,

        // non-interactive cinematic animation events and cutscenes
        Cinematic = 5,
    }
}