## `mushymato.Merchant/TourismWaves` and `mushymato.Merchant/Tourists`

These two assets form the tourism system.

## Tourism Waves

A tourism wave is an event where extra customers, called tourists, come to the shop.
These tourists only buy specific items, but will offer very good prices for said items.
Tourists do not appear if the shop doesn't offer what they want.

Aside from content defined waves, there is a special tourism wave `"Default"` which is always active.
This is used for any non-social characters that you want to see as customers.

### Structure
```json
{"mushymato.Merchant_BooksellerDay": {
  // game state query specifying when this tourism wave is active
  "Condition": "mushymato.Merchant_BOOK_SELLER_IN_TOWN",
  // tourism wave display name in UI
  "DisplayName": "[LocalizedText mushymato.Merchant.i18n:Tourism_BooksellerDay_Name]",
  // tourism wave description in UI
  "Description": "[LocalizedText mushymato.Merchant.i18n:Tourism_BooksellerDay_Desc]",
  // context tags for items the tourists will buy
  "ContextTags": [
    "book_item", // can use "tag1,tag2" for AND
    // can specify additional tags for OR
  ],
  // minimum number of tourists per shopkeeping session
  "TouristMinCount": 3,
  // maximum number of tourists per shopkeeping session, will be ignored if less than mincount
  "TouristMaxCount": -1,
  // haggle dialogue applied to all tourists that have come due to this tourism wave
  "Dialogue": {
    "mushymato.Merchant_Bookbuyer": {
      "Haggle_Ask": "[LocalizedText mushymato.Merchant.i18n:Haggle_Ask_Bookseller]",
      "Haggle_Compromise": "[LocalizedText mushymato.Merchant.i18n:Haggle_Compromise_Bookseller]",
      "Haggle_Overpriced": "[LocalizedText mushymato.Merchant.i18n:Haggle_Overpriced_Bookseller]",
      "Haggle_Success": "[LocalizedText mushymato.Merchant.i18n:Haggle_Success_Bookseller]",
      "Haggle_Fail": "[LocalizedText mushymato.Merchant.i18n:Haggle_Fail_Bookseller]"
    },
    // you can have multiple sets of dialogues, key does not matter
  },
}}
```

## Tourists

Tourist are buyers who arrive with tourism waves.
A particular tourist can appear in multiple tourism waves.

### Structure
```json
{"mushymato.Merchant_Marcello": {
  // defines which tourism waves this tourist will appear in
  // there is a special value "Default" for tourists that should appear all year around
  "AppearsDuring": [
    "mushymato.Merchant_BooksellerDay"
  ],
  // a tourist can have extra context tags in addition to the one from their tourism wave
  // default wave tourists must specify this otherwise they will never purchase anything
  "ContextTags": null,

  // if the tourist is actually meant to match an existing NPC (e.g. Elliott but he's here just for books)
  // this can be used to inherit fields rather than specifying again
  "NPC": null,

  // for case where tourist is not NPC, these fields should be specified for their appearance
  // tourist display name
  "DisplayName": "[LocalizedText mushymato.Merchant.i18n:Marcello_Name]",
  // tourist portrait (shown in haggle dialogue)
  "Portrait": null,
  // tourist sprite (required)
  "Sprite": "Characters/Marcello",
  // tourist sprite size
  "Size": {
    "X": 16,
    "Y": 32
  },
  // tourist mug shot, used for the target pointer indicator in haggle UI
  "MugShotSourceRect": null,
  // whether tourist should display shadow
  "ShowShadow": true,

  // These fields are same as customer
  // condition
  "Condition": null,
  // chance to appear
  "Chance": 1.0,
  // haggle dialogue, will override the dialogue set by tourism waves
  "Dialogue": {},
}}
```
