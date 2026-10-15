// A persistent narrative choice changes both movement and survival.
VAR act = 1
VAR tactic = 0
VAR directive = 0
VAR intel = 0

=== intro ===
{ act:
- 1: NOVA: The lower city is vanishing. Find two fragments and the checkpoint relay.
- 2: NOVA: The clockwork quarter still transmits. The relay might tell us who built the rift.
- 3: NOVA: The guardian knows my voice. I have one chance to choose what survives.
}
-> END

=== choice ===
THE SIGNAL: Your route is encoded in the relay. Which frequency will you carry into the next district?
+ [Overclock the suit. Dash farther; no extra repairs.] 
    ~ tactic = 1
    NOVA: Speed. I will outrun the rupture.
    -> END
+ [Stabilize the suit. Restore thirty-five HP; dash stays shorter.]
    ~ tactic = 2
    NOVA: Hold the line. There are people behind me.
    -> END

=== revelation ===
{ act:
- 1: THE SIGNAL: The city did not fall by accident. Someone opened the rupture from inside the tower.
- 2: THE SIGNAL: The guardian's voiceprint matches Nova. It was built from a future version of you.
- 3: THE SIGNAL: The core can be stopped, but its shield draws power from three relays. Break them first.
}
-> END

=== mission_one ===
THE SIGNAL: The first district is failing. You can only save one route before the collapse reaches us.
+ [Evacuate civilians. Slow the rift in this district and recover their testimony.]
    ~ directive = 1
    ~ intel = intel + 1
    NOVA: I opened the shelters. Their testimony will follow us into the tower.
    -> END
+ [Raid the power cache. Gain three coins, but accelerate the rift.]
    ~ directive = 2
    NOVA: The cache is ours. I can hear the rift gaining on us.
    -> END
+ [Arm the blaster. Shots deal more damage; sacrifice twelve HP now.]
    ~ directive = 3
    NOVA: The blaster is overcharged. The feedback burns, but the patrols will feel it.
    -> END

=== mission_two ===
THE SIGNAL: The clockwork patrol network is exposed. Choose how to cross its kill zone.
+ [Jam patrol targeting. Guards fire slower; capture their network records.]
    ~ directive = 1
    ~ intel = intel + 1
    NOVA: Their sights are scrambling. The records mention a copy of me.
    -> END
+ [Steal three power cells. Gain coins; guards fire faster.]
    ~ directive = 2
    NOVA: I have the cells, but the alarm just changed pitch.
    -> END
+ [Fortify the suit. Restore thirty-five HP; the rift advances faster.]
    ~ directive = 3
    NOVA: The suit holds. The street behind us does not.
    -> END

=== mission_three ===
THE SIGNAL: The guardian shield draws power from three relays. Your last directive will decide the breach.
+ [Overload the relays. Shots hit relays harder; the rift speeds up.]
    ~ directive = 1
    NOVA: Give me one opening. I will tear down the shield.
    -> END
+ [Broadcast an evacuation. Slow the rift and preserve the final signal.]
    ~ directive = 2
    ~ intel = intel + 1
    NOVA: The signal is out. They know which streets are still safe.
    -> END
+ [Rebuild the armor. Restore forty HP; guards fire faster.]
    ~ directive = 3
    NOVA: Armor restored. The guardian knows where I am.
    -> END

=== ending ===
{ intel:
- 0: THE SIGNAL: The tower is quiet. No records remain of who was lost.
- 1: THE SIGNAL: One recovered transmission survives for the people outside.
- else: THE SIGNAL: The recovered records expose the tower and guide survivors home.
}
{ tactic:
- 1: NOVA: I outran the last wave. The signal reaches the city, and the streets remember us.
- 2: NOVA: I stayed to carry the wounded through. The signal survives because we did.
- else: NOVA: The signal is free. There is still a city left to save.
}
-> END
