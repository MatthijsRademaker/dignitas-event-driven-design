Conversation
├── AgentConversation
│   ├── AgentConversationEvents.cs
│   ├── AgentConversationSaga.cs
│   ├── AgentConversationSagaDefinition.cs
│   └── States.cs
├── ConversationCleanup
│   └── ConversationCleanup.cs
├── Core
│   ├── AgentConversationRegistry.cs
│   ├── ConversationCorrelationId.cs
│   ├── ConversationEvent.cs
│   └── ConversationTypes.cs
├── FrontendCommunication
│   ├── Agent
│   │   ├── AgentActivityBroadcaster.cs
│   │   ├── AgentCallArrivedBroadcaster.cs
│   │   └── FrontendMessages.cs
│   ├── Conversation
│   │   ├── CallAcceptedBroadcaster.cs
│   │   ├── CallEndedBroadcaster.cs
│   │   ├── FrontendMessages.cs
│   │   ├── RecommendationsBroadcaster.cs
│   │   └── SummaryBroadcaster.cs
│   └── Core
│       ├── BroadcasterBase.cs
│       └── Messages.cs
├── GenesysNotifications
│   ├── GenesysNotificationEvents.cs
│   ├── GenesysNotificationHandler.cs
│   ├── GenesysNotificationSessionState.cs
│   ├── GenesysOrchestrator.cs
│   ├── Session
│   │   ├── GenesysSessionConnector.cs
│   │   └── GenesysSessionReceiver.cs
│   └── TopicHandlers
│       ├── GenesysActivityTopicHandler.cs
│       ├── GenesysCallsTopicHandler.cs
│       ├── GenesysConversationsTopicHandler.cs
│       ├── GenesysTranscriptionTopicHandler.cs
│       └── IGenesysTopicHandler.cs
├── Orchestration
│   ├── AgentOrchestrator.cs
│   ├── FrontendSessionTerminator.cs
│   ├── OrchestrationContracts.cs
│   └── Session
│       ├── AgentSession.cs
│       ├── FrontendCycle.cs
│       └── GenesysLoop.cs
├── README.md
├── RegisterDependencies.cs
├── Tracing
│   ├── ConversationTraceContext.cs
│   └── TraceContextConsumeFilter.cs
└── Transcripts
    ├── ConversationTranscriptStore.cs
    ├── RecommendationGenerator
    │   ├── BasisvraagRecommendation.cs
    │   └── RecommendationGenerator.cs
    ├── RecommendationsGenerated.cs
    ├── SummaryGenerator
    │   └── SummaryGenerator.cs
    └── TranscriptUpdater.cs
 
17 directories, 46 files
