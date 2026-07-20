# ArchitectsJourney

> Event-driven architecture simulation platform built with .NET 9, Clean Architecture, DDD and asynchronous engine orchestration.

![.NET](https://img.shields.io/badge/.NET-9-purple)
![C#](https://img.shields.io/badge/C%23-Backend-blue)
![Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-green)
![Tests](https://img.shields.io/badge/Tests-28%2F28-success)

---

# Overview

ArchitectsJourney is a backend architecture simulation platform designed with **Domain Driven Design (DDD)** and **Event Driven Architecture** principles.

The system simulates architectural decision-making processes where user decisions trigger domain events, independent engines process those events, and the system state evolves through controlled aggregate mutations.

The main goal of the project is to demonstrate scalable backend architecture patterns:

- Clean Architecture
- Domain Driven Design
- Event Driven Architecture
- SOLID Principles
- Stateless Engine Design
- Dependency Injection
- Automated Testing
- Domain Events

---

# System Architecture

The system consists of independent engines communicating through an Event Bus.

```
                     User Decision
                          |
                          v
                    GameEngine
                          |
                          |
          DecisionProcessedEvent Published
                          |
        +-----------------+-----------------+
        |                 |                 |
        v                 v                 v

   RuleEngine       MetricEngine     ArchitectureEngine

        |                 |                 |
        v                 v                 v

 Rule Evaluation     Metric State     Architecture Graph

        |
        v

      Domain Events

        |
        v

  Playthrough Aggregate

        |
        v

     Checkpoint State
```

---

# Core Engine Architecture

## GameEngine

GameEngine is responsible only for orchestration.

Responsibilities:

- Starting game sessions
- Processing user decisions
- Publishing domain events
- Managing lifecycle flow

GameEngine does **not** contain:

- Metric calculations
- Rule evaluation logic
- Architecture mutations

---

# RuleEngine

RuleEngine manages business rule execution.

Responsibilities:

- Evaluating conditions
- Processing rule effects
- Publishing consequences
- Preventing duplicate executions

Features:

- Event-driven execution
- Idempotent processing
- Conflict resolution
- Rule isolation

Example flow:

```
DecisionProcessedEvent

        |

        v

RuleEngine

        |

        v

RuleEffect Events
```

---

# MetricEngine

MetricEngine is the single owner of metric mutations.

Responsibilities:

- Applying metric changes
- Managing metric boundaries
- Detecting threshold crossings
- Maintaining metric history

Features:

- Exactly-once mutation
- Duplicate event prevention
- Configurable thresholds
- Boundary enforcement

Flow:

```
MetricDeltaAppliedEvent

            |

            v

       MetricEngine

            |

            v

     Updated Metrics
```

Example:

```
Cost +10

Old Value: 40

New Value: 50

Threshold Check

History Updated
```

---

# ArchitectureEngine

ArchitectureEngine manages the dynamic architecture graph.

Responsibilities:

- Processing architecture changes
- Maintaining nodes and edges
- Validating dependencies
- Detecting invalid states
- Publishing architecture events

ArchitectureEngine is:

- Stateless
- Event-driven
- Thread-safe
- Aggregate oriented

Execution pipeline:

```
ArchitectureChangedEvent

          |

          v

Idempotency Check

          |

          v

Load Playthrough

          |

          v

Validate Architecture

          |

          v

Mutate Aggregate

          |

          v

Save Aggregate

          |

          v

Publish Architecture Events
```

Rules:

- Validation never mutates state
- Graph mutations happen only inside Playthrough aggregate
- Failed validation produces no mutation
- Failed persistence produces no events

---

# Domain Model

## Playthrough Aggregate

The Playthrough aggregate is the main domain boundary.

It contains:

- Session state
- Metrics
- Architecture graph
- Checkpoint snapshots
- History information


The aggregate protects internal state.

External components cannot directly modify:

- Metrics
- Architecture nodes
- Architecture edges


All modifications happen through domain behaviors.

---

# Architecture Graph Model

The Architecture Graph consists of:

## ArchitectureNode

Represents architecture components.

Example:

```
Frontend
Backend
Database
Message Queue
Cache
```

Properties:

- Id
- Type
- Label
- TechnologyId


---

## ArchitectureEdge

Represents relationships between architecture components.

Example:

```
API Gateway

      |

      v

Backend Service
```

Properties:

- Source
- Target
- Type
- Communication

---

# Event Driven Architecture

The system uses domain events for communication.

Examples:

```
DecisionProcessedEvent

MetricDeltaAppliedEvent

ArchitectureChangedEvent

ArchitectureNodeAddedEvent

ArchitectureEdgeAddedEvent

MetricThresholdCrossedEvent
```

Events allow engines to remain independent.

---

# Technology Stack

## Backend

- .NET 9
- C#
- ASP.NET Core
- Clean Architecture
- Domain Driven Design

---

## Infrastructure

- Docker
- Docker Compose
- Nginx

---

## Testing

- xUnit
- Unit Testing
- Integration Testing
- Event Flow Testing

---

# Solution Structure

```
src/backend

│
├── ArchitectsJourney.Domain
│
│   ├── Aggregates
│   ├── Entities
│   ├── ValueObjects
│   └── Events
│
├── ArchitectsJourney.Application
│
│   ├── Contracts
│   ├── Interfaces
│   └── DTOs
│
├── ArchitectsJourney.Infrastructure
│
│   ├── Persistence
│   ├── EventBus
│   └── Services
│
├── ArchitectsJourney.Engines
│
│   ├── GameEngine
│   ├── RuleEngine
│   ├── MetricEngine
│   └── ArchitectureEngine
│
├── ArchitectsJourney.Api
│
└── ArchitectsJourney.Tests
```

---

# Clean Architecture Principles

The project follows Clean Architecture:

## Domain Layer

Contains:

- Business rules
- Aggregates
- Entities
- Domain events


No dependency on:

- Database
- API
- Infrastructure


---

## Application Layer

Contains:

- Interfaces
- Contracts
- Use case abstractions


---

## Infrastructure Layer

Contains:

- External implementations
- Persistence
- Event infrastructure


---

## API Layer

Responsible for:

- HTTP communication
- Dependency Injection
- Application startup

---

# SOLID Principles

Implemented principles:

## Single Responsibility

Each engine has one responsibility.

Example:

```
MetricEngine
     |
     only metric mutation
```

```
ArchitectureEngine
     |
     only architecture state management
```

---

## Dependency Inversion

High-level modules depend on abstractions.

Example:

```
Engine

   |

IEventBus

   |

Implementation
```

---

# Testing

Current verification:

```
dotnet restore     ✅

dotnet build       ✅

dotnet test        ✅
```

Test Result:

```
28 / 28 Passing

0 Failed

0 Errors

0 Warnings
```

---

# Test Coverage

Implemented tests:

## Metric Tests

- Metric update
- Duplicate event prevention
- Threshold detection
- Boundary validation


## Rule Tests

- Rule evaluation
- Invalid conditions
- Duplicate execution
- Conflict resolution


## Architecture Tests

- Graph mutation
- Node addition
- Edge validation
- Cycle detection
- Invalid architecture handling
- Idempotency


## Integration Tests

- Complete mission flow
- Event propagation
- Aggregate updates
- Snapshot handling

---

# Running The Project

Clone:

```bash
git clone https://github.com/kadircnkya/ArchitectsJourney.git
```

Navigate:

```bash
cd ArchitectsJourney
```

Restore:

```bash
dotnet restore
```

Build:

```bash
dotnet build
```

Run tests:

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

# Development Roadmap

Completed:

✅ Phase 1 - Core Domain Infrastructure

✅ Phase 2 - Event Infrastructure

✅ Phase 3 - Game Engine

✅ Phase 4 - Rule Engine

✅ Phase 5 - Metric Engine

✅ Phase 6 - Architecture Engine


Future:

- Phase 7 - Extended API Integration
- Advanced Persistence
- Architecture Visualization Dashboard
- Frontend Interface
- Deployment Pipeline

---

# Project Goals

This project demonstrates:

- Enterprise backend architecture design
- Event-driven systems
- Domain modeling
- Scalable engine architecture
- Software architecture simulation


---

# Author

**Kadir Çankaya**

Software Engineer

GitHub:

https://github.com/kadircnkya