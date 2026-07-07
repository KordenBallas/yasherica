using System;

namespace GameInput.Core
{
    /// <summary>
    /// Resolves prompt cues from the binding catalog for whatever source is active right now, and
    /// re-raises the active-source switch as <see cref="CuesChanged"/> so every prompt re-renders at
    /// once (Input Foundation R5/R6). Pure C#: catalog + <see cref="IActiveInputSource"/> in, cue
    /// strings out.
    /// </summary>
    public sealed class PromptCueProvider : IPromptCueProvider, IDisposable
    {
        private const int AbilitySlotCount = 6;

        private readonly InputBindingCatalog _catalog;
        private readonly IActiveInputSource _activeSource;

        public event Action CuesChanged;

        public PromptCueProvider(InputBindingCatalog catalog, IActiveInputSource activeSource)
        {
            _catalog = catalog;
            _activeSource = activeSource;
            _activeSource.Changed += HandleSourceChanged;
        }

        public void Dispose()
        {
            _activeSource.Changed -= HandleSourceChanged;
        }

        public string GetCue(GameAction action)
        {
            return _catalog.GetCue(action, _activeSource.Current) ?? string.Empty;
        }

        public string GetAbilitySlotCue(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= AbilitySlotCount)
            {
                return string.Empty;
            }

            return GetCue(GameAction.AbilitySlot1 + slotIndex);
        }

        private void HandleSourceChanged(InputSource source)
        {
            CuesChanged?.Invoke();
        }
    }
}
