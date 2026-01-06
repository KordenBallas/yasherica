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
        /// Creates a CombatConfig with specified parameters.
        /// This constructor is used by Zenject for dependency injection.
        /// </summary>
        [Inject]
        public CombatConfig(float hexCellSize, HexOrientation hexOrientation)
        {
            HexCellSize = hexCellSize;
            HexOrientation = hexOrientation;
        }
        
        /// <summary>
        /// Creates a CombatConfig with default values.
        /// </summary>
        public CombatConfig() : this(2f, HexOrientation.Flat)
        {
        }
        
        // Future: add more combat settings
        // - Default ability ranges
        // - Turn time limits
        // - AI difficulty settings
    }
}

