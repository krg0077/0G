using UnityEngine;

namespace _0G.Legacy
{
    public enum GraphicsLosslessAnimations { Never, GalleryOnly, Always, }

    public class GraphicsManager : Manager
    {
        public override float priority => 70;

        protected PersistentData<int> m_LosslessAnimations = new PersistentData<int>(
            Persist.PlayerPrefs, "0G.graphics.lossless_animations");

        /// <summary>
        /// Get or set if/when lossless (high quality) animations are enabled.
        /// </summary>
        public GraphicsLosslessAnimations LosslessAnimations
        {
            get => (GraphicsLosslessAnimations)m_LosslessAnimations.Value;
            set => m_LosslessAnimations.Value = (int)value;
        }

        public override void Awake()
        {
            m_LosslessAnimations.LoadValueOrSetDefault((int)GetDefaultLosslessAnimations());
        }

        public static GraphicsLosslessAnimations GetDefaultLosslessAnimations()
        {
            return SystemInfo.systemMemorySize >= 8192 ? GraphicsLosslessAnimations.GalleryOnly : GraphicsLosslessAnimations.Never;
        }
    }
}