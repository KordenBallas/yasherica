using System.Collections.Generic;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// Registry for managing character references in the scene.
    /// Provides centralized access to character GameObjects.
    /// </summary>
    public interface ICharacterRegistry
    {
        /// <summary>
        /// Registers a character in the registry.
        /// </summary>
        void RegisterCharacter(Transform character);
        
        /// <summary>
        /// Unregisters a character from the registry.
        /// </summary>
        void UnregisterCharacter(Transform character);
        
        /// <summary>
        /// Gets the player-controlled character.
        /// </summary>
        Transform GetPlayerCharacter();
        
        /// <summary>
        /// Gets all registered characters.
        /// </summary>
        IReadOnlyList<Transform> GetAllCharacters();
    }
}
