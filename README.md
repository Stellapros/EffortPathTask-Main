# EffortPatch
**Unity Editor Version: 6000.0.26f1**

EffortPatch is a Unity project designed for running effort-based decision-making experiments online using WebGL, with data hosted on an online server (Heroku).

### How to Run the Experiment

To run the experiment, start the game from the "Persistent" scene.

### Key Components
GameController.cs: This singleton script manages the main gameplay loop and controls the game's core logic.

ExperimentManager.cs: Responsible for configuring the experiment, including trial sequencing, randomization, and experiment-specific controlled variables such as block durations and rest breaks.

LogManager.cs: Handles data management, logging all relevant information during the experiment for later analysis.

### Gameplay Controls
Movement: Use the arrow keys to move your character.

Decisions: Press the A key to work (collect fruit) or the D key to skip (rest). 

Note that there may be slight variations in the sensitivity of key inputs across different computers.

### Additional Notes
There are a few things still pending cleanup, including some commented-out code and minor tasks to tidy up. I will address these in future updates :)
