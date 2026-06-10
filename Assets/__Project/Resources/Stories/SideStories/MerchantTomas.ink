// Merchant Tomas Encounter
// A simple merchant who offers goods to the player

// === Variables ===
VAR npc_name = ""
VAR merchant_visited = false

// === Story Start ===
// Greeting text is provided by MerchantTomasCharacter.ink; this knot starts at choices.
=== start ===
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
