## `mushymato.Merchant/Customers`

Customers are social NPC who have spawned in the world, they are the regular visitors to your shop.
Every social NPC with a Data/Characters entry will get a default entry to start.
There's a few things you can customize by editing the entry to `mushymato.Merchant/Customers`.

### Structure
```json
{"Krobus": {
  // condition
  "Condition": "PLAYER_HAS_MAIL Current ccMovieTheater",
  // chance to appear
  "Chance": 1.0,
  // a specific appearance id to always apply
  "OverrideAppearanceId": "MovieTheater",
  // haggle dialogues
  "Dialogue": {
    "mushymato.Merchant_Thug": {
      "Haggle_Ask": "[LocalizedText mushymato.Merchant.i18n:Haggle_Ask_Krobus]",
      "Haggle_Compromise": "[LocalizedText mushymato.Merchant.i18n:Haggle_Compromise_Krobus]",
      "Haggle_Overpriced": "[LocalizedText mushymato.Merchant.i18n:Haggle_Overpriced_Krobus]",
      "Haggle_Success": "[LocalizedText mushymato.Merchant.i18n:Haggle_Success_Krobus]",
      "Haggle_Fail": "[LocalizedText mushymato.Merchant.i18n:Haggle_Fail_Krobus]"
    },
    // you can have multiple sets of dialogues, key does not matter
  },
}}
```

[Example for adding new haggling dialogue.](../Merchant.Examples/data/haggle_dialogue.json)

### Haggle Dialogue

There are 5 dialogue fields on the asset:
- `Haggle_Ask`: When haggle begins.
- `Haggle_Compromise`: When your price is somewhat above the NPC's target price but they are willing to negotiate.
- `Haggle_Overpriced`: When your price is far above the NPC's target price, and they do not want to negotiate.
- `Haggle_Fail`: When you fail to sell the item.
- `Haggle_Success`: When you successfully sold the item.

You can `{0}` for item name and `{1}` for the current price.

Tokenized text is also supported and resolves before substitutions.

### Condition and Chance

By default, only social NPCs that the player has met may come and buy things from a player's shop.

You can change when exactly the NPCs come and go by using the `Condition` field.

Each customer also has a `Chance`, which is checked after their condition is confirmed true.
This can be used to reduce how often a particular customer frequent the shop.

### Override Appearance

This is used if you want a particular NPC to always use a specific appearance entry regardless of conditions when they are a customer.
In the example above, `Krobus` will always use appearance `MovieTheater` when shopping.



