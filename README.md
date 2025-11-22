# 🧭 Echo-Location Maze System (ELMS)

> _Procedural logic meets sensory exploration._

**Platform:** PC (Standalone Build)  
**Engine:** Unity (2023.x)  
**Status:** Playable prototype — web & Itch.io build coming soon  

---

## 🎮 Overview

**ELMS (Echo-Location Maze System)** is a Unity **technical prototype** focused on **procedural generation, system decoupling, and designer-centric tooling**.  

At runtime, the game **builds its entire maze structure dynamically** — no prebuilt levels exist.  
The player navigates a fog-covered labyrinth using an **Echo ability** that temporarily reveals walls and paths through sound waves.  

> The prototype demonstrates a runtime-generated world, an event-driven input pipeline, and a modular visual system — all designed for clarity, expandability, and designer control.

---

## 🧠 Core Systems Architecture

Built around four personal design principles:
**Expandability**, **Simplicity**, **Empowerment**, and **Immersion through Logic**.

### 🔹 1. Echo System — Decoupled Core Mechanic

| Component | File(s) | Description |
|------------|----------|-------------|
| **Contract** | `IEchoRevealable.cs` | Interface declaring the rule: any GameObject implementing this can respond to echo waves. |
| **Controller** | `EchoSystem.cs` | Central logic using `Physics2D.OverlapCircleAll` to detect nearby colliders and trigger `Reveal()` only on valid targets. Manages cooldowns and scaling via an `AnimationCurve`. |
| **Implementer** | `MazeTile.cs` | Handles the **visual reaction** — timed fade of visibility via coroutine. |
| **Data Hook** | `MainMenuController.cs` | Passes chosen difficulty into `EchoSystem`, modifying the `echoRadius` dynamically. |

> The mechanic is fully decoupled — any object can become “echo-reactive” by implementing the interface.

---

### 🔹 2. Procedural Maze Generation (PCG)

The maze is **entirely generated at runtime**, showcasing procedural logic separation between data and visualization.

| Component | File(s) | Description |
|------------|----------|-------------|
| **Data Model** | `MazeCell.cs` | Pure C# struct-like class storing wall states, position, and pathfinding metadata (visited, distance). |
| **Generator Tool** | `MazeGenerator.cs` | Implements the **Recursive Backtracker Algorithm** to carve a perfect maze. Once generated, it spawns physical tiles based on the data grid. |
| **Flow Control** | `ExitTile.cs` | Detects player proximity and triggers `GameEvents.OnPlayerReachedExit` for win condition. |

> The result is a lightweight PCG pipeline — data first, visuals second.

---

### 🔹 3. Actor Logic & Input Pipeline

The player’s control scheme uses Unity’s **Input System** and a clean **event-based abstraction layer**.

| Component | File(s) | Description |
|------------|----------|-------------|
| **Input Definition** | `PlayerInputActions.cs` | Auto-generated input maps for `Player` and `UI`. |
| **Abstraction Layer** | `InputHandler.cs` | Singleton that normalizes input data and broadcasts movement/actions via C# events. |
| **Controller** | `PlayerController.cs` | Subscribes to input events, applies movement through `FixedUpdate()` for stable physics. Locks input on win event. |
| **Event Hub** | `GameEvents.cs` | Central observer pattern for gameplay state changes. |

> Input and gameplay are fully decoupled, ensuring platform flexibility (future mobile support planned).

---

### 🔹 4. RGBSync Visual System — Designer-Centric Tooling

A global color-cycling system that synchronizes dynamic theming across objects.

| Component | File(s) | Description |
|------------|----------|-------------|
| **Global Manager** | `RGBSyncManager.cs` | Singleton controlling theme cycle speed, saturation, and brightness. |
| **Data Struct** | `RGBEffectSettings.cs` | Serializable struct for saving user preferences; persisted via `PlayerPrefs`. |
| **Integration** | `MazeTile.cs` | Optional opt-in (`enableRGBSync`) lets designers toggle per-tile theme syncing. |
| **Editor Integration** | (via **Odin Inspector**) | Exposes runtime debug data — hue cycle bars, toggle switches, sliders — directly inside the Unity editor. |

> Demonstrates **runtime color theming**, **data persistence**, and **designer empowerment** through custom tooling.

---

## 🧩 Technical Summary

| Skill Area | Key Files | Concepts Demonstrated |
|-------------|------------|------------------------|
| **Architecture** | `IEchoRevealable.cs`, `GameEvents.cs`, `RGBSyncManager.cs` | Dependency Inversion, Observer Pattern, Singletons, separation of logic & view. |
| **Procedural Generation** | `MazeGenerator.cs`, `MazeCell.cs` | Recursive Backtracker, PCG structure, data-driven design. |
| **Input Pipeline** | `InputHandler.cs`, `PlayerController.cs`, `PlayerInputActions.cs` | Unity Input System, event-based control, physics-integrated movement. |
| **Editor Tooling** | (All, via Odin Inspector) | Runtime debugging, data visualization, live parameter tuning. |
| **Persistence** | `RGBSyncManager.cs`, `MainMenuController.cs` | Saving custom data and theme profiles via `PlayerPrefs`. |

---


## 📸 Showcase & Footage

The prototype features **real-time maze regeneration**, showcasing the system’s ability to rebuild the maze start and end dynamically each run.  
Visuals use **simple shapes and colors** to focus on system design and clarity.  

🎥 *Demo video & screenshots available*
[YouTube](https://youtu.be/9TGZjnqw-4M)

---

## ⚙️ Summary

**ELMS** is a self-contained demonstration of **procedural logic, decoupled architecture, and runtime adaptability** in Unity.  
Every mechanic is written to be **extendable**, **designer-friendly**, and **data-driven** — embodying a true systems-first approach to gameplay.

> _“The maze builds itself — all I did was give it rules.”_

---

## 🔗 Links

- 🐙 **Source Code:** [Echo-Location Maze System (ELMS) / MazePrototype](https://github.com/D4RKL0RD-J0571N/MazePrototype)  
- 📫 **Contact:** [LinkedIn](https://www.linkedin.com/in/jostin-lopez-b761261bb) [Email](jostinlopezsobalbarro@gmail.com)
