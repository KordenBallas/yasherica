using System.Collections.Generic;
using Narrative.Data.Definitions;
using Narrative.Generation;
using UnityEngine;

namespace Narrative.Dialogue
{
    /// <summary>
    /// Default implementation of IDialogueSessionInitializer.
    /// Ensures consistent initialization across all dialogue types.
    /// </summary>
    public class DialogueSessionInitializer : IDialogueSessionInitializer
    {
        private readonly IStoryManager _storyManager;
        private readonly IInkExternalFunctionBinder _externalFunctionBinder;

        private bool _externalFunctionsBound;

        public DialogueSessionInitializer(
            IStoryManager storyManager,
            IInkExternalFunctionBinder externalFunctionBinder = null)
        {
            _storyManager = storyManager;
            _externalFunctionBinder = externalFunctionBinder;
        }

        public void InitializeSession(IDialogueContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("[DialogueSessionInitializer] Cannot initialize: context is null");
                return;
            }

            Debug.Log($"[DialogueSessionInitializer] Initializing session for context: " +
                      $"NpcId={context.NpcId ?? "none"}, " +
                      $"IsSideStory={context.IsSideStory}, " +
                      $"HasBoundStory={context.BoundStory != null}");

            // If context has a bound story with Ink content, use that
            if (context.BoundStory != null)
            {
                InitializeSession(context.BoundStory);
                return;
            }

            // For NPC dialogue without bound story, just ensure external functions are bound
            // The story is already loaded by the main story system
            EnsureExternalFunctionsBound();
        }

        public void InitializeSession(BoundStory boundStory)
        {
            if (boundStory == null)
            {
                Debug.LogWarning("[DialogueSessionInitializer] Cannot initialize: boundStory is null");
                return;
            }

            var template = boundStory.Template;
            if (template == null)
            {
                Debug.LogWarning("[DialogueSessionInitializer] Cannot initialize: template is null");
                return;
            }

            Debug.Log($"[DialogueSessionInitializer] Initializing session for template: {template.StoryId}");

            // Load the Ink content if available
            if (template.HasInkContent)
            {
                var inkJson = template.GetInkJson();
                if (!string.IsNullOrEmpty(inkJson))
                {
                    _storyManager.LoadStory(inkJson);
                    Debug.Log($"[DialogueSessionInitializer] Loaded Ink content for template: {template.StoryId}");
                }
            }

            // ALWAYS bind external functions
            EnsureExternalFunctionsBound();

            // Inject bound parameters
            InjectBoundParameters(boundStory);
        }

        public void EnsureExternalFunctionsBound()
        {
            if (_externalFunctionBinder == null)
            {
                Debug.Log("[DialogueSessionInitializer] No external function binder available");
                return;
            }

            // Only bind once per story load
            if (!_externalFunctionsBound)
            {
                _externalFunctionBinder.BindAllExternalFunctions();
                _externalFunctionsBound = true;
                Debug.Log("[DialogueSessionInitializer] External functions bound");
            }
        }

        /// <summary>
        /// Resets the binding state (call when switching stories).
        /// </summary>
        public void ResetBindingState()
        {
            _externalFunctionsBound = false;
        }

        private void InjectBoundParameters(BoundStory boundStory)
        {
            if (boundStory.BoundParameters == null || boundStory.BoundParameters.Count == 0)
            {
                Debug.Log("[DialogueSessionInitializer] No bound parameters to inject");
                return;
            }

            foreach (var kvp in boundStory.BoundParameters)
            {
                var param = kvp.Value;
                if (param?.Slot == null)
                    continue;

                var variableName = param.Slot.InkVariableName;
                if (string.IsNullOrEmpty(variableName))
                    continue;

                _storyManager.SetVariable(variableName, param.InkValue);
                Debug.Log($"[DialogueSessionInitializer] Injected variable '{variableName}' = '{param.InkValue}'");
            }

            Debug.Log($"[DialogueSessionInitializer] Injected {boundStory.BoundParameters.Count} parameters");
        }
    }
}
