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
	