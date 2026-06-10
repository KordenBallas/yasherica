using Combat.Battlefield;
using Zenject;

namespace Combat.Config
{
    /// <summary>
    /// Configuration for Combat system.
    /// Contains all Combat-specific settings that external systems should not know about.
    /// </summary>
    public class CombatConfig
    {
        public float HexCellSize { get; }
        public HexOrientation HexOrientation { get; }

        /// <summary>
        /// Maximum number of abilities that can be queued (0 = unlimited).
        /// </summary>
        public int MaxAbilityQueueSize { get; }

        /// <summary>
        /// Creates a CombatConfig with specified parameters.
        /// This constructor is used by Zenject for dependency injection.
        /// </summary>
        [Inject]
        public CombatConfig(float hexCellSize, HexOrientation hexOrientation, int maxAbilityQueueSize = 3)
        {
            HexCellSize = hexCellSize;
            HexOrientation = hexOrientation;
            MaxAbilityQueueSize = maxAbilityQueueSize;
        }

        /// <summary>
        /// Creates a CombatConfig with default values.
        /// </summary>
        public CombatConfig() : this(2f, HexOrientation.Flat, 3)
        {
        }
        
        // Future: add more combat settings
        // - Default ability ranges
        // - Turn time limits
        // - AI difficulty settings
    }
}

