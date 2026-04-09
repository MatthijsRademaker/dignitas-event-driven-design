# Conversation Flow (Short)

This folder owns the websocket entry point, Genesys event ingress, conversation saga, agent registry/orchestrator, and frontend broadcasters.

## High-level flow

```mermaid
flowchart LR
    UI[Frontend WebSocket] --> Orch[AgentOrchestrator]
    Orch --> Session[AgentSession]
    Session --> Genesys[Genesys API]
    Genesys --> Receiver[GenesysSessionReceiver]
    Receiver --> Handlers[Topic Handlers]
    Handlers --> Bus[MassTransit Bus]
    Bus --> Saga[AgentConversationSaga]
    Bus --> Activity[AgentActivityBroadcaster]
    Bus --> CallState[CallStateBroadcaster]
    Bus --> Recs[RecommendationsBroadcaster]
    Bus --> Summary[SummaryBroadcaster]
    Saga --> Registry[AgentConversationRegistry]
    Registry --> Orch
    Orch --> UI
```

## Component responsibilities

```mermaid
flowchart TD
    Orch[AgentOrchestrator]
    Session[AgentSession]
    Registry[AgentConversationRegistry]
    Saga[AgentConversationSaga]
    Broadcasters[Frontend Broadcasters]
    Pipeline[Transcript/Recommendation/Summary]

    Orch -->|manages websockets| Session
    Session -->|runs Genesys loop| Orch
    Saga -->|sets agent->conversation on CallAccepted| Registry
    Registry -->|lookup for reconnect| Orch
    Saga -->|publishes domain events| Broadcasters
    Pipeline -->|feeds saga events| Saga
```

## Conversation sequence (simplified)

```mermaid
sequenceDiagram
    participant UI as Frontend
    participant Orch as AgentOrchestrator
    participant Sess as AgentSession
    participant Genesys as Genesys API
    participant Saga as AgentConversationSaga
    participant Broad as Broadcasters

    UI->>Orch: /ws/connect
    Orch->>Sess: create/replace session
    Sess->>Genesys: start notification loop
    Genesys-->>Sess: activity/call events
    Sess-->>Saga: CallAccepted (starts saga)
    Saga-->>Broad: CallAccepted
    Genesys-->>Saga: TranscriptReceived
    Saga-->>Broad: Recommendations
    Genesys-->>Saga: CallEnded
    Saga-->>Broad: CallEnded + Summary
    Saga-->>Orch: ConversationCompleted (cleanup)
```

## Reconnect behavior (current)

```mermaid
flowchart LR
    UI[Frontend WebSocket] -->|disconnect| Orch[AgentOrchestrator]
    Orch -->|FrontendDisconnected event if registry has mapping| Bus[MassTransit Bus]
    Bus --> Saga[AgentConversationSaga]
    UI -->|reconnect| Orch
    Orch -->|FrontendReconnected event if registry has mapping| Bus
```

## Notes

- Saga starts on `CallAccepted`, not on `CallArrived`.
- Conversation events are routed by `AgentId` on the saga events.
- Registry is agent -> conversation only, used for reconnect/session flows.
