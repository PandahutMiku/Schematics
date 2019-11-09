# Schematics

## Setup:
- Grab the lastest release: https://github.com/PandahutMiku/Schematics/releases
- Either just plop in, or just plop in and edit in your Mysql Configuration

## Configuration:
- SchematicsDatabaseInfo:Fill in your database info here and set UseDatabase to true

- UseDatabase: Set to true if you want to use your database.

- MaxDistanceToLoadSchematic (Default 500): This makes it so you can't aim into space and accidentally spawn in your schematic there, you can disable it by setting it to 0.


## Usage:
- Essentially, you would do /ss Testing 50 to save elements around you, and do /ls Testing and it would plop that same thing you just saved whereever your crosshair is. There is a lot of controllable things, but it's important to keep in mind the positions of the elements are saved relative to your position on saving them. If you can imagine wherever you're aiming is where you were standing when you saved it, all of the elements will be replicated relative to that position.


- LoadSchematic (/ls):  <Name> [Optional: -KeepPos (Keeps -NoState -KeepHealth -SetOwner -SetGroup, Input any Steamid64 to set owner to it]. 
  - KeepPos loads the schematic in at the saved position inside of the dat file
  - KeepHealth keeps the saved health
  - SetOwner sets Owner to you
  -SetGroup sets group to you
  -inputting any Steamid64 (76561198138254281) will set the elements to be owned by you.
  
- SaveSchematic (/ss): /SaveSchematic <name> <distance> [Optional Parameters: -Owner  -Group, Input any Steamid64 to only get results from it
  - Owner only grabs elements owned by you
  - Group only grabs elements owned by your group
  - inputting any Steamid64 (76561198138254281) will only grab elements owned by it.
 

## Disclaimer:
- This plugin is super light and only does anything on you doing it, it's entirely sync meaning if your database has a ping of 9000ms, it will block the main game thread for 9000ms until it gets a response, there is plans to make it async in the future but it really doesn't matter from my testing, if your database has a reasonable ping it'll be fine. (Keep in mind, a ton of things are sync anyway, Uconomy, Zaupshop, Playerinfolib, etc)
