# WORK IN PROGRESS
# Seletek Firefly ROR Driver

ASCOM Dome Driver for Lunatico Seletek Firefly for Roll-off-Roofs

**Advantages to VBS Scripts**
* Can be used by mulitple programs simultaneously
* Can recover from unknown roof state
* Timeout by time instead of number of retries
* Control UI
* VBS (VBScript) was declared deprecated by Microsoft and will be removed from future Windows releases
  https://learn.microsoft.com/en-us/windows/whats-new/deprecated-features


## Requirements
* Garage door type opener for Roll-off-Roof with 2 limit switches
* Seletek Firefly Hardware and Software
* ASCOM Platform 6.5 or later
---
## Configuration

<img align="left" width="264" alt="image" src="https://github.com/photon1503/SeletekROR/assets/14548927/6ff3cd28-74d4-4ace-a208-fb40873f7394">

* Roof Open Sensor - Sensor ID from Seletek which is true when the roof is open
* Roof Closed Sensor - SensoID from Seletek which is true when the roof is closed
* Roof Relay Number - Relay ID from Seletek which will start/stop a motion of the roof
* No motion timeout - Timeout in seconds after a sensor must become false when a open/close command was triggerd
* Total timeout - Timout in seconds when a open/close command fails
* Relay pause - Delay between two relay pushes
* Sensor polling - Frequency of sensor polling
  
---
## Architecture
### Using VBS
```mermaid
flowchart LR
    n1("ASCOM Client")
	n1 -->|ASCOM|n2
	n2 --> n3["VB Scripts"]
    n3 -->|COM|n2["Seletek Software"]
    n2 -->|USB|n4["Seletek Firefly Hardware"]
	
```

### Using Seletek Firefly ROR ASCOM Driver
```mermaid
flowchart LR
    n1("ASCOM Client")
	n1 -->|ASCOM|n2["Firefly Seletek ROR"]
	n2 -->|COM|n3["Seletek Software"]
    n3 -->|USB|n4["Seletek Firefly Hardware"]
```
	
