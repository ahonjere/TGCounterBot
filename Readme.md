# Counter bot for Telegram groups. Running in Azure.
This is a Telegram bot, which is made to keep track of increments done by each group membber. It is created to replace another bot,
and is made to match the needs of one specific group.

## Features
### Current
- Keeps track of all increments
- Keeps track of every users personal increments
- Can tell how many increments sender, or all the users combined have incremented on average per day

## What have I learned from this?
- Setting up a function app in Azure
- Webhooks exist
- Basics of C# (this was my first touch to C#)
- It would be much more flexible to save all the increments separatly, and then count them to get the amount.
  Of course then there would be more data to save, but that should not be a problem in project on this scale.
