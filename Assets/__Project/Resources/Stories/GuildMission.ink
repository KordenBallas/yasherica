// Guild Mission Side Story
// This becomes available after completing the guild initiation quest

=== guild_mission_giver ===
# platform_type: dialogue
# npc: guild_captain
# require_quest_completed: guild_initiation

# speaker: Guild Captain
"Initiate! I have a mission worthy of your skills."

+ ["What's the mission?"] -> mission_details
+ ["I'm busy."] -> mission_decline

=== mission_details ===
# speaker: Guild Captain
"Bandits have been raiding our supply routes. Deal with them."

+ ["I'll handle it."] -> mission_accept
+ ["Sounds dangerous..."] -> mission_caution

=== mission_accept ===
# outcome: Quest
# quest: guild_bandit_hunt

# speaker: Guild Captain
"Excellent. Return when the deed is done."

-> END

=== mission_caution ===
# speaker: Guild Captain
"A true guild member shows no fear. Return when you're ready."

-> END

=== mission_decline ===
# speaker: Guild Captain
"Very well. The mission will wait."

-> END
