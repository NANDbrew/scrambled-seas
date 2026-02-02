# ScrambledSeas
This is a mod for Sailwind.
Scrambled Seas Randomizer

NANDbrew edition 
- [BorderExpander](https://github.com/NANDbrew/BorderExpander) integration
- scale your scramble up or down on new game
- saves island positions now instead of re-scrambling from seed every load, to shorten load times
- still scrambles new islands that aren't in the save (and then saves them)
- save can be put in a file for manual editing of island positions
  - these are offsets in meters from the vanilla positions
- includes vitalijbeam's JSON export for MoffKalast map

## Settings
- randomEN
  - enable random. also controlled by a checkbox in the 'new game' menu
- saveCoordsToJSON
  - Save island coords to JSON file for online map
  - Writes the file to the mod folder when a save is started or loaded
  - File will be named scramble_(slot).json for a scrambled save, or islandCoords.json for vanilla positions
- Eastwind Fix
  - Fix eastwind market position
  - NANDbrew does not understand this fix
- ExternalSave
  - Save and load island/archipelago offsets to xml file to allow manual editing
  - File will be named scramble_(slot).xml
  - Writes the file to the mod folder when a save is started or loaded
- Destination Hint
  - None
  - Heading: show compass heading e.g. north-northeast
  - Coords: show latitude/longitude
- Ordinal Precision
  - Number of ordinal heading directions given in the mission screen
  - Accepts 8, 16 or 32
- Decimal Precision
  - Number of decimal places in destination coordinates
  - Accepts 0, 1 or 2
