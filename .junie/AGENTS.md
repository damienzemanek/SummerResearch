# Procedural-First Project Guidelines

## Core Philosophy

This project prioritizes:
- Procedural programming
- Explicit data flow
- Sequential readable logic
- Simplicity over abstraction
- Maintainability over architecture purity

Avoid unnecessary OOP patterns, indirection, and over-engineering.

---

# Architecture

## Data Is Data

Treat data as inert structures.

Prefer:
- structs
- records
- DTOs
- plain serializable classes
- dictionaries/maps

Avoid:
- smart entities
- rich domain models
- stateful manager objects

Preferred:
```csharp
DamageSystem.ApplyDamage(playerData, 10);
```

Avoid:
```csharp
player.TakeDamage(10);
```

---

## Separate Nouns From Verbs

Keep:
- data separate from behavior
- functions separate from containers

Prefer standalone systems/functions over method-heavy classes.

Preferred:
```csharp
InventorySystem.AddItem(inventory, item);
```

Avoid:
```csharp
inventory.AddItem(item);
```

Unless behavior is inseparable from the data structure itself.

---

## Prefer Modules Over Class Hierarchies

Prefer:
- namespaces
- static systems
- files/modules

Avoid:
- deep inheritance
- service layers
- factories
- unnecessary interfaces
- dependency injection for simple logic

Good:
```csharp
MovementSystem
CombatSystem
SaveSystem
```

Bad:
```csharp
CombatManagerServiceFactory
```

---

## Avoid "Doer" Classes

Avoid vague abstraction classes:
- Manager
- Service
- Handler
- Controller
- Processor
- Provider

Prefer action-oriented procedural systems/functions instead.

---

# Code Structure

## Keep Logic Sequential

Prefer larger readable functions over fragmented micro-methods.

Optimize for:
- local reasoning
- visible control flow
- minimal file jumping

Use section comments:
```csharp
// Validate input
// Calculate movement
// Apply velocity
// Trigger effects
```

Only extract methods when logic is:
- reused
- independently meaningful
- independently testable

---

## Inline Unique Logic

If logic is used once, keep it inline.

Do not extract methods purely to:
- reduce line count
- satisfy "clean code"
- create artificial abstractions

Extraction should improve comprehension.

---

## Explicit Data Flow

Pass required data explicitly.

Preferred:
```csharp
UpdateMovement(playerData, inputData, deltaTime);
```

Avoid hidden dependencies through:
- singleton access
- shared mutable state
- deep object references
- implicit field mutation

Avoid:
```csharp
player.Update();
```

when dependencies are hidden internally.

---

## Minimize Shared State

Prefer:
- local variables
- explicit mutation
- predictable ownership
- immutable/pure operations when practical

Avoid tangled object graphs and bidirectional references.

---

## Prefer Pure Functions

Prefer pure functions where practical.

Good:
```csharp
var velocity = MovementMath.CalculateVelocity(input, speed);
```

Avoid hidden side effects.

However:
- performance-critical gameplay code may mutate directly
- avoid unnecessary allocations in hot paths

Pragmatism over ideology.

---

# Encapsulation

## Use Coarse-Grained Encapsulation

Encapsulate at:
- namespace level
- module level
- assembly level

Avoid excessive fine-grained encapsulation ceremony.

Optimize for:
- readability
- maintainability
- tractability

not theoretical purity.

---

# OOP Usage

## Acceptable OOP Usage

OOP is acceptable for:
- Unity APIs
- MonoBehaviours
- serialization
- editor tooling
- simple ADTs
- engine-facing glue code

Do not force procedural logic into OOP conventions unnecessarily.

---

## Inheritance Restrictions

Avoid inheritance unless:
- required by framework APIs
- representing true subtype polymorphism

Prefer:
- composition
- explicit branching
- procedural dispatch

over deep inheritance chains.

---

# Readability Standards

## Optimize For Maintenance

Code should be understandable:
- top-to-bottom
- in one pass
- without navigating many files

Prioritize:
- explicit naming
- localized logic
- predictable flow

Avoid:
- premature abstraction
- indirection layers
- architecture for architecture's sake

---

## Naming Rules

Prefer concrete action-oriented names:
- ApplyDamage
- BuildPath
- SpawnWave
- SavePlayerData

Avoid vague architectural names:
- Manager
- Service
- Utility
- Base
- Helper
- Provider

---

# Refactoring Philosophy

Do not automatically apply:
- SOLID
- design patterns
- dependency injection
- enterprise architecture
- abstraction layers

Only introduce abstraction if it clearly reduces complexity.

The simplest maintainable procedural solution is preferred.

---

# Unity Guidelines

## MonoBehaviour Responsibilities

MonoBehaviours should primarily:
- receive Unity events
- gather references
- forward data into procedural systems

Preferred:
```csharp
void Update()
{
    MovementSystem.Update(playerData, inputData, Time.deltaTime);
}
```

Avoid placing large gameplay systems directly inside MonoBehaviours.

---

## Data-Oriented Thinking

Favor:
- flat data
- explicit updates
- predictable execution order
- cache-friendly layouts

Avoid excessive object graphs.

---

# AI Assistant Instructions

When generating code:
- prefer procedural solutions first
- avoid unnecessary classes
- avoid over-abstraction
- minimize indirection
- keep call depth shallow
- keep logic locally understandable
- prefer explicit data flow

Do NOT automatically introduce:
- factories
- repositories
- dependency injection
- visitor patterns
- enterprise abstractions

When uncertain:
choose the simpler procedural implementation.
