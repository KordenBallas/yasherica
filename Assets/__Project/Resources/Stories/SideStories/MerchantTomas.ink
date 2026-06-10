// Merchant Tomas Encounter
// A simple merchant who offers goods to the player

// === Variables ===
VAR npc_name = ""
VAR merchant_visited = false

// === Story Start ===
=== start ===
# speaker: Merchant Tomas
Greetings, traveler! I am Tomas, a humble merchant. Perhaps you'd like to see my wares?

+ [Show me what you have.]
    -> show_goods
+ [Not interested.]
    -> decline

// === Show Goods ===
=== show_goods ===
# speaker: Merchant Tomas
Excellent! I have potions, equipment, and rare trinkets from distant lands.
# outcome: Shop
~ merchant_visited = true
-> END

// === Decline ===
=== decline ===
# speaker: Merchant Tomas
No worries! Safe travels, friend. Come back anytime!
~ merchant_visited = true
-> END
