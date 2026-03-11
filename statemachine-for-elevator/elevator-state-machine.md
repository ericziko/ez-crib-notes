---
title: Elevator State Machine
created: 2026-03-13
modified: 2026-03-13
tags:
  - state-machine
  - architecture
  - diagrams
  - mermaid
---

# 🛗 Elevator State Machine — Office Building

> **Why did the elevator break up with the escalator?**
> Because it had too many ups and downs — but the escalator just kept going in one direction.

---

## 🤖💡 Overview

An office building elevator can be modelled as a finite state machine (FSM). The key states revolve around idle waiting, responding to floor calls, moving, and managing door transitions. Edge cases include emergency stops and out-of-service modes.

---

## 📊 Diagram 1 — Core State Diagram (`stateDiagram-v2`)

The cleanest view: pure states and transitions.

```mermaid
stateDiagram-v2
    [*] --> Idle

    Idle --> MovingUp   : RequestAbove
    Idle --> MovingDown : RequestBelow
    Idle --> DoorOpening : RequestSameFloor

    MovingUp --> ArrivedAtFloor : ReachedTargetFloor
    MovingDown --> ArrivedAtFloor : ReachedTargetFloor

    ArrivedAtFloor --> DoorOpening : Stopped

    DoorOpening --> DoorOpen : DoorFullyOpen
    DoorOpen --> DoorClosing : TimeoutOrCloseButton
    DoorClosing --> DoorOpen : ObstacleDetected
    DoorClosing --> Idle : DoorFullyClosed

    Idle --> EmergencyStop : AlarmTriggered
    MovingUp --> EmergencyStop : AlarmTriggered
    MovingDown --> EmergencyStop : AlarmTriggered

    EmergencyStop --> OutOfService : EngineerReset
    OutOfService --> Idle : ServiceCleared
```

---

## 📊 Diagram 2 — Flowchart Style (top-down narrative flow)

A flowchart makes the decision logic more explicit — useful for understanding the control flow in code.

```mermaid
flowchart TD
    START([Power On]) --> IDLE[Idle at Floor N]

    IDLE -->|Call received above| MOVEUP[Moving Up]
    IDLE -->|Call received below| MOVEDOWN[Moving Down]
    IDLE -->|Call at current floor| DOOROPEN_DIRECT[Open Door]

    MOVEUP -->|Target floor reached| ARRIVE[Arrive at Floor]
    MOVEDOWN -->|Target floor reached| ARRIVE

    ARRIVE --> DOOROPENING[Door Opening]
    DOOROPEN_DIRECT --> DOOROPENING

    DOOROPENING -->|Door fully open| DOOROPEN[Door Open]
    DOOROPEN -->|Timeout / Close button| DOORCLOSING[Door Closing]
    DOORCLOSING -->|Obstacle detected| DOOROPEN
    DOORCLOSING -->|Door fully closed| IDLE

    IDLE -->|Alarm triggered| ESTOP[🚨 Emergency Stop]
    MOVEUP -->|Alarm triggered| ESTOP
    MOVEDOWN -->|Alarm triggered| ESTOP

    ESTOP -->|Engineer reset| OOS[Out of Service]
    OOS -->|Service cleared| IDLE
```

---

## 📊 Diagram 3 — Multi-Floor Context (floors as data, states as nodes)

This diagram shows how floor requests queue up and influence direction decisions.

```mermaid
stateDiagram-v2
    direction LR

    [*] --> Idle

    state "Serving Requests" as Serving {
        [*] --> EvaluatingQueue
        EvaluatingQueue --> MovingUp   : QueueHasHigherFloor
        EvaluatingQueue --> MovingDown : QueueHasLowerFloor
        EvaluatingQueue --> Idle       : QueueEmpty

        MovingUp --> PassingFloor  : IntermediateFloorInQueue
        MovingDown --> PassingFloor : IntermediateFloorInQueue
        PassingFloor --> DoorCycle

        MovingUp --> FinalFloor    : LastFloorInDirection
        MovingDown --> FinalFloor  : LastFloorInDirection
        FinalFloor --> DoorCycle

        DoorCycle --> EvaluatingQueue : DoorCycleComplete
    }

    Idle --> Serving : RequestReceived
    Serving --> Idle : QueueEmpty

    Idle --> Emergency : AlarmTriggered
    Serving --> Emergency : AlarmTriggered
    Emergency --> Idle : Reset
```

---

## 📊 Diagram 4 — Sequence Diagram (passenger perspective)

What does a single elevator trip look like from the outside? A sequence diagram captures the interactions between actors.

```mermaid
sequenceDiagram
    participant P as Passenger
    participant B as Call Button
    participant E as Elevator Controller
    participant M as Motor
    participant D as Door Mechanism

    P->>B: Press Up button (Floor 3)
    B->>E: FloorRequest(3, Up)
    E->>M: MoveToFloor(3)
    M-->>E: ArrivedAtFloor(3)
    E->>D: OpenDoor()
    D-->>E: DoorOpen
    E-->>P: Chime + Door Open

    P->>E: Press Floor 7 inside cab
    E->>D: CloseDoor()
    D-->>E: DoorClosed
    E->>M: MoveToFloor(7)
    M-->>E: ArrivedAtFloor(7)
    E->>D: OpenDoor()
    D-->>E: DoorOpen
    E-->>P: Chime + Door Open

    Note over E,D: Timeout after 5s
    E->>D: CloseDoor()
    D-->>E: DoorClosed
    E->>M: ReturnToIdleOrNextRequest()
```

---

## 🤖💡 Key States Summary

| State           | Description                                              |
|-----------------|----------------------------------------------------------|
| `Idle`          | Elevator at rest on a floor, no pending requests         |
| `MovingUp`      | Travelling upward toward a target floor                  |
| `MovingDown`    | Travelling downward toward a target floor                |
| `ArrivedAtFloor`| Transition state: stopped, about to open doors           |
| `DoorOpening`   | Door motor engaged, opening                              |
| `DoorOpen`      | Doors fully open, waiting for passengers                 |
| `DoorClosing`   | Door motor engaged, closing                              |
| `EmergencyStop` | Alarm triggered, elevator halted in place                |
| `OutOfService`  | Engineer intervention required before resuming           |

---

## 🤖💡 Key Transitions & Guards

| Transition                   | Guard / Trigger                        |
|------------------------------|----------------------------------------|
| `Idle → MovingUp`            | Request exists on a higher floor       |
| `Idle → MovingDown`          | Request exists on a lower floor        |
| `DoorClosing → DoorOpen`     | Obstacle sensor triggered              |
| `DoorClosing → Idle`         | Door fully closed, no pending requests |
| `* → EmergencyStop`          | Alarm signal received in any state     |
| `EmergencyStop → OutOfService`| Engineer resets the alarm              |

---

## 🤖💡 Implementation Notes

- **Direction bias**: Real elevators use a *scan algorithm* (like a disk scheduler) — serve all requests in the current direction before reversing. The queue evaluation in Diagram 3 hints at this.
- **Door re-open**: The `DoorClosing → DoorOpen` obstacle transition is safety-critical and must be hardware-enforced, not just software.
- **Emergency Stop**: Should be reachable from *any* state — modelled here from the most likely states; in production code treat it as a global guard.
- **Idle floor**: Some buildings configure the elevator to return to a "home floor" (e.g., Ground) when idle for N seconds.
