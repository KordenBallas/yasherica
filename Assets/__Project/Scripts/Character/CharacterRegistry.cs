using System.Collections.Generic;
using Core.Logging;
using UnityEngine;
using Zenject;

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
        [Inject] private IGameLogger _logger;
        
        public void RegisterCharacter(Transform character)
        {
            if (character == null)
            {
                _logger?.Warning(LogCategory.Character,"[CharacterRegistry] Attempted to register null character");
                return;
            }
            
            if (_allCharacters.Contains(character))
            {
                _logger?.Warning(LogCategory.Character,$"[CharacterRegistry] Character {character.name} is already registered");
                return;
            }
            
            _allCharacters.Add(character);
            
            // Auto-detect player character by tag
            if (character.CompareTag("Player"))
            {
                _playerCharacter = character;
                _logger?.Info(LogCategory.Character,$"[CharacterRegistry] Registered player character: {character.name}");
            }
            else
            {
                _logger?.Info(LogCategory.Character,$"[CharacterRegistry] Registered character: {character.name}");
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
                _logger?.Info(LogCategory.Character,$"[CharacterRegistry] Unregistered character: {character.name}");
            }
        }
        
        public Transform GetPlayerCharacter()
        {
            if (_playerCharacter == null)
            {
                _logger?.Warning(LogCategory.Character,"[CharacterRegistry] No player character registered");
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
