using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Casting.Core;
using Narrative.Dialogue;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// The production entry point that turns a placed actor into a running dialogue — the orchestration
    /// the slice's tests proved but no gameplay code drove yet. On <see cref="BeginEncounter"/> it asks the
    /// <see cref="IRunDirector"/> for an eligible storylet against the live fact store (R6/R7), casts the
    /// chosen storylet onto the actor (R3/R5), and starts the <see cref="DialogueRunner"/>. Selection is by
    /// current facts only, evaluated at encounter time — never by authored edges between stories.
    ///
    /// Pure C# (no UnityEngine): a platform-state adapter owns the trigger and the runner's combat/quest/
    /// ended signals; this class only resolves and begins.
    /// </summary>
    public sealed class EncounterDirector
    {
        private readonly IRunDirector _director;
        private readonly ICastingFactory _castingFactory;
        private readonly IFragmentLibrary _library;
        private readonly IReadOnlyList<StoryTemplateData> _storylets;
        private readonly IFactStore _store;
        private readonly DialogueRunner _runner;

        public EncounterDirector(
            IRunDirector director,
            ICastingFactory castingFactory,
            IFragmentLibrary library,
            IReadOnlyList<StoryTemplateData> storylets,
            IFactStore store,
            DialogueRunner runner)
        {
            _director = director;
            _castingFactory = castingFactory;
            _library = library;
            _storylets = storylets;
            _store = store;
            _runner = runner;
        }

        /// <summary>
        /// Selects, casts, and begins a dialogue for <paramref name="actor"/>. Returns false (the runner is
        /// left untouched) when the actor is null, no storylet is eligible, or the chosen storylet cannot be
        /// cast — the caller then completes the platform without a dialogue.
        /// </summary>
        public bool BeginEncounter(NpcInstance actor)
        {
            if (actor == null)
            {
                return false;
            }

            // Bind the actor's subject tokens so actor/faction-scoped preconditions resolve (R10);
            // world-scoped preconditions ignore the context.
            var context = new ContextBag()
                .BindSubject("$self", actor.InstanceId)
                .BindSubject("$faction", actor.FactionId);

            var selection = _director.SelectNext(_storylets, _store, context);
            if (!selection.HasSelection)
            {
                return false;
            }

            var casting = _castingFactory.Cast(selection.Chosen, actor, _library);
            if (casting == null)
            {
                return false;
            }

            _runner.Begin(casting);
            return true;
        }

        /// <summary>
        /// Begins an encounter for a story the windowed planner already selected and an actor it already
        /// minted (story-first flow): no selection here — just <c>Cast</c> (so the casting context reflects
        /// the live facts at entry) and <c>DialogueRunner.Begin</c>. Returns false (runner untouched) when
        /// the story/actor is null or the story cannot be cast.
        /// </summary>
        public bool BeginPlanned(StoryTemplateData story, NpcInstance actor)
        {
            if (story == null || actor == null)
            {
                return false;
            }

            var casting = _castingFactory.Cast(story, actor, _library);
            if (casting == null)
            {
                return false;
            }

            _runner.Begin(casting);
            return true;
        }
    }
}
