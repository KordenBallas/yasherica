// Guild Recruiter Side Story
// This is the entry point for the Ironclad Guild faction questline

VAR guild_reputation = 0
VAR joined_guild = false

=== guild_recruiter ===
# platform_type: dialogue
# npc: guild_recruiter
# key_node: true

# speaker: Guild Recruiter
"Hail, traveler! The Ironclad Guild seeks worthy members."

+ ["What is the Ironclad Guild?"] -> guild_info
+ ["I'm interested in joining."] -> guild_join_check
+ ["Not interested."] -> guild_leave

=== guild_info ===
# speaker: Guild Recruiter
"We are an ancient order of warriors and craftsmen."
"Our members gain access to exclusive contracts and training."
~ guild_reputation = guild_reputation + 1

+ ["How do I join?"] -> guild_join_check
+ ["Sounds interesting."] -> guild_leave

=== guild_join_check ===
{guild_reputation >= 3:
    -> guild_join_success
- else:
    -> guild_join_requirements
}

=== guild_join_requirements ===
# speaker: Guild Recruiter
"You must first prove your worth. Complete tasks for our members."
"Return when you have earned our respect."

-> END

=== guild_join_success ===
# speaker: Guild Recruiter
"You have proven yourself worthy!"
~ joined_guild = true
# outcome: Quest
# quest: guild_initiation

-> END

=== guild_leave ===
# speaker: Guild Recruiter
"The door remains open should you change your mind."
-> END
