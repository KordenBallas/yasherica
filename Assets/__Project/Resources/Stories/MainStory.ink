// Main Story - Demo Storyline
// This Ink file demonstrates the narrative system integration

VAR player_name = "Traveler"
VAR has_met_merchant = false
VAR bandit_hostile = false
VAR gold = 0

// External functions for game integration
EXTERNAL trigger_combat(enemy_count)

=== function trigger_combat(enemy_count) ===
// Stub - game will bind this function
~ return

=== start ===
# platform_type: simple
# key_node: true
# priority: 0

You awaken on a floating platform, the world stretching endlessly below.
A path of stone and magic leads forward into the mist.

+ [Look around] -> look_around
+ [Move forward] -> move_forward

=== look_around ===
The platform beneath you hums with ancient power.
Runes etched into the stone pulse with a faint blue light.
In the distance, you can make out other platforms connected by bridges of light.

+ [Continue forward] -> move_forward

=== move_forward ===
You step onto the bridge, feeling it solidify under your feet.
Ahead, you see a figure standing near a small stall.

-> merchant_intro

=== merchant_intro ===
# platform_type: dialogue
# npc: merchant_tomas
# key_node: true
# priority: 1

# speaker: Merchant Tomas
"Ah, a traveler! Welcome, welcome!"

The merchant, a stout man with a warm smile, gestures to his wares.

# speaker: Merchant Tomas
"I am Tomas. These platforms can be treacherous for the unprepared. Perhaps I can help?"

+ ["What do you sell?"] -> merchant_wares
+ ["Tell me about this place."] -> merchant_lore
+ ["Who are you?"] -> merchant_who
+ [Leave] -> merchant_leave

=== merchant_who ===
~ has_met_merchant = true
# speaker: Merchant Tomas
"I'm just a humble trader, making my way through the platforms like everyone else."

# speaker: Merchant Tomas
"Been doing this for... oh, must be twenty years now. These stones have stories, traveler."

+ ["What stories?"] -> merchant_lore
+ ["What do you sell?"] -> merchant_wares
+ ["Goodbye."] -> merchant_leave

=== merchant_wares ===
~ has_met_merchant = true
# speaker: Merchant Tomas
"Potions, trinkets, the occasional weapon... whatever travelers need!"

# speaker: Merchant Tomas
"Gold is scarce in these parts, but I'm always fair in my dealings."

+ ["I'll browse later."] -> merchant_leave
+ ["Tell me about this place."] -> merchant_lore

=== merchant_lore ===
~ has_met_merchant = true
# speaker: Merchant Tomas
"These platforms? Ancient, older than any kingdom. Some say gods built them."

# speaker: Merchant Tomas
"But be careful ahead. There's been reports of bandits on the next few platforms."

+ ["Bandits?"] -> merchant_bandit_warning
+ ["Thank you for the warning."] -> merchant_leave

=== merchant_bandit_warning ===
# speaker: Merchant Tomas
"Aye, rough folk. They prey on travelers. If you see them, best to talk your way out..."

# speaker: Merchant Tomas
"...or be ready for a fight. They don't take kindly to resistance."

+ ["I can handle myself."] -> merchant_leave
+ ["Thanks for the advice."] -> merchant_leave

=== merchant_leave ===
# speaker: Merchant Tomas
"Safe travels, friend! Come back if you need anything."

-> END

=== bandit_encounter ===
# platform_type: combat
# npc: bandit_leader
# npc_can_become_enemy: true
# enemy_id: 1
# key_node: true
# priority: 2

A rough-looking figure blocks your path, hand resting on a sword hilt.

# speaker: Bandit
"Hold there, traveler. This here's a toll road now."

+ ["How much?"] -> bandit_toll
+ ["I won't pay."] -> bandit_refuse
+ [Attack immediately] -> bandit_attack

=== bandit_toll ===
# speaker: Bandit
"Smart one, eh? Ten gold pieces, and you pass unharmed."

{gold >= 10:
    + [Pay the toll] -> bandit_paid
}
+ ["That's too much."] -> bandit_negotiate
+ ["I'd rather fight."] -> bandit_refuse

=== bandit_negotiate ===
# speaker: Bandit
"Too much? Ha! Consider it a... survival tax."

The bandit grins, revealing missing teeth.

# speaker: Bandit
"But I'm feeling generous. Five gold, final offer."

{gold >= 5:
    + [Pay five gold] -> bandit_paid
}
+ ["No deal."] -> bandit_refuse
+ [Attack] -> bandit_attack

=== bandit_paid ===
~ gold = gold - 5
# speaker: Bandit
"Wise choice. Move along then, and don't come back this way without more coin!"

The bandit steps aside, counting your gold with a satisfied smirk.

-> END

=== bandit_refuse ===
# speaker: Bandit
"Wrong answer, friend."

The bandit draws their sword.

-> bandit_attack

=== bandit_attack ===
# outcome: Combat
The bandit lunges at you with blade drawn!

~ bandit_hostile = true
~ trigger_combat(1)

-> END

=== chapter_end ===
# platform_type: cutscene
# key_node: true
# priority: 10

The path opens to a grand vista. More platforms stretch into the distance, each holding secrets untold.

Your journey has only just begun.

-> END
