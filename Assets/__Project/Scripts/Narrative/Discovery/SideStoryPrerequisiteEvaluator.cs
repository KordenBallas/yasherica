using System;
using Narrative.Data.Definitions;
using UnityEngine;

namespace Narrative.Discovery
{
    /// <summary>
    /// Evaluates story prerequisites against the current story state.
    /// Works with any BaseStoryDefinition (chapters, side stories, dialogues, events).
    /// Pure C# class with no Unity dependencies beyond Debug logging.
    /// </summary>
    public class SideStoryPrerequisiteEvaluator
    {
        /// <summary>
        /// Evaluates if all prerequisites for a story are met.
        /// </summary>
        /// <param name="definition">The story to evaluate.</param>
        /// <param name="storyState">Current story state.</param>
        /// <param name="currentChapterNumber">Current chapter number (1-based).</param>
        /// <returns>True if all prerequisites are met.</returns>
        public bool ArePrerequisitesMet(
            BaseStoryDefinition definition,
            StoryState storyState,
            int currentChapterNumber)
        {
            if (definition == null || storyState == null)
                return false;

            var prereqs = definition.Prerequisites;
            if (prereqs == null || !prereqs.HasAnyPrerequisites)
                return true;

            // Check chapter number bounds
            if (!CheckChapterBounds(prereqs, currentChapterNumber))
                return false;

            // Check required completed quests
            if (!CheckCompletedQuests(prereqs, storyState))
                return false;

            // Check required active quests
            if (!CheckActiveQuests(prereqs, storyState))
                return false;

            // Check required NPC encounters
            if (!CheckEncounteredNpcs(prereqs, storyState))
                return false;

            // Check custom variable conditions
            if (!CheckCustomVariables(prereqs, storyState))
                return false;

            return true;
        }

        private bool CheckChapterBounds(StoryPrerequisites prereqs, int currentChapter)
        {
            if (prereqs.MinChapterNumber > 0 && currentChapter < prereqs.MinChapterNumber)
                return false;

            if (prereqs.MaxChapterNumber > 0 && currentChapter > prereqs.MaxChapterNumber)
                return false;

            return true;
        }

        private bool CheckCompletedQuests(StoryPrerequisites prereqs, StoryState state)
        {
            foreach (var questId in prereqs.RequiredCompletedQuests)
            {
                if (!state.IsQuestCompleted(questId))
                    return false;
            }
            return true;
        }

        private bool CheckActiveQuests(StoryPrerequisites prereqs, StoryState state)
        {
            foreach (var questId in prereqs.RequiredActiveQuests)
            {
                if (!state.IsQuestActive(questId))
                    return false;
            }
            return true;
        }

        private bool CheckEncounteredNpcs(StoryPrerequisites prereqs, StoryState state)
        {
            foreach (var npcId in prereqs.RequiredEncounteredNpcs)
            {
                if (!state.HasEncounteredNpc(npcId))
                    return false;
            }
            return true;
        }

        private bool CheckCustomVariables(StoryPrerequisites prereqs, StoryState state)
        {
            foreach (var condition in prereqs.CustomVariableConditions)
            {
                if (!EvaluateCondition(condition, state))
                    return false;
            }
            return true;
        }

        private bool EvaluateCondition(VariableCondition condition, StoryState state)
        {
            if (string.IsNullOrEmpty(condition.VariableName))
                return true;

            // Handle boolean conditions first
            if (condition.Comparison == ComparisonOperator.IsTrue)
            {
                return state.GetCustomVariable(condition.VariableName, false);
            }

            if (condition.Comparison == ComparisonOperator.IsFalse)
            {
                return !state.GetCustomVariable(condition.VariableName, false);
            }

            // Get the value as object for other comparisons
            if (!state.CustomVariables.TryGetValue(condition.VariableName, out var value))
            {
                // Variable doesn't exist - only NotEquals with non-null can be true
                return condition.Comparison == ComparisonOperator.NotEquals
                       && !string.IsNullOrEmpty(condition.CompareValue);
            }

            return EvaluateComparison(value, condition.Comparison, condition.CompareValue);
        }

        private bool EvaluateComparison(object value, ComparisonOperator comparison, string compareValue)
        {
            // Handle null value
            if (value == null)
            {
                return comparison switch
                {
                    ComparisonOperator.Equals => string.IsNullOrEmpty(compareValue),
                    ComparisonOperator.NotEquals => !string.IsNullOrEmpty(compareValue),
                    _ => false
                };
            }

            // Handle numeric comparisons
            if (value is int intValue || value is float || value is double || value is long)
            {
                if (!double.TryParse(compareValue, out var compareNum))
                    return false;

                var numValue = Convert.ToDouble(value);

                return comparison switch
                {
                    ComparisonOperator.Equals => Math.Abs(numValue - compareNum) < 0.0001,
                    ComparisonOperator.NotEquals => Math.Abs(numValue - compareNum) >= 0.0001,
                    ComparisonOperator.GreaterThan => numValue > compareNum,
                    ComparisonOperator.GreaterThanOrEqual => numValue >= compareNum,
                    ComparisonOperator.LessThan => numValue < compareNum,
                    ComparisonOperator.LessThanOrEqual => numValue <= compareNum,
                    _ => false
                };
            }

            // Handle string comparisons
            var stringValue = value.ToString();

            return comparison switch
            {
                ComparisonOperator.Equals => stringValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.NotEquals => !stringValue.Equals(compareValue, StringComparison.OrdinalIgnoreCase),
                ComparisonOperator.Contains => stringValue.Contains(compareValue, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }
    }
}
