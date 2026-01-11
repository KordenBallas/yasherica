using System.Collections.Generic;
using UnityEngine;

namespace Character
{
    /// <summary>
    /// Singleton service for managing character references.
    /// Managed by Zenject, not a static singleton.
    /// </summary>
    public class CharacterRegistry : MonoBehaviour, ICharacterRegistry
    {
        private Transform _playerCharacter;
        private readonly List<Transform> _allCharacters = new();
        
        public void RegisterCharacter(Transform character)
        {
            if (character == null)
            {
                Debug.LogWarning("[CharacterRegistry] Attempted to register null character");
                return;
            }
            
            if (_allCharacters.Contains(character))
            {
                Debug.LogWarning($"[CharacterRegistry] Character {character.name} is already registered");
                return;
            }
            
            _allCharacters.Add(character);
            
            // Auto-detect player character by tag
            if (character.CompareTag("Player"))
            {
                _playerCharacter = character;
                Debug.Log($"[CharacterRegistry] Registered player character: {character.name}");
            }
            else
            {
                Debug.Log($"[CharacterRegistry] Registered character: {character.name}");
            }
        }
        
        public void UnregisterCharacter(Transform character)
        {
            if (character == null) return;
            
            if (_allCharacters.Remove(character))
            {
                if (_playerCharacter == character)
                {
                    _playerCharacter = null;
                }
                Debug.Log($"[CharacterRegistry] Unregistered character: {character.name}");
            }
        }
        
        public Transform GetPlayerCharacter()
        {
            if (_playerCharacter == null)
            {
                Debug.LogWarning("[CharacterRegistry] No player character registered");
            }
            return _playerCharacter;
        }
        
        public IReadOnlyList<Transform> GetAllCharacters()
        {
            return _allCharacters.AsReadOnly();
        }
        
        private void OnDestroy()
        {
            _allCharacters.Clear();
            _playerCharacter = null;
        }
    }
}
