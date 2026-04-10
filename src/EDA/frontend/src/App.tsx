import { useEffect, useState } from 'react';
import './App.css';

type CallSummary = {
  id: string;
  agentName: string;
  callerName: string;
  status: string;
  startedAt: string;
};

type DemoState = {
  call: CallSummary | null;
};

type SagaPhase = 'Idle' | 'In Progress' | 'On Hold' | 'Ended';

type EventEntry = {
  id: string;
  label: string;
  detail?: string;
  at: string;
};

type QuickPhrase = {
  label: string;
  text: string;
};

const quickPhrases: QuickPhrase[] = [
  {
    label: 'Refund request',
    text: 'I need a refund for the last charge. It was a mistake.',
  },
  {
    label: 'Cancel subscription',
    text: 'Please cancel my subscription today. I do not want to be billed again.',
  },
  {
    label: 'Escalation',
    text: 'I am really frustrated and want to speak with a supervisor.',
  },
  {
    label: 'Delivery delay',
    text: 'My order is late and I need an updated delivery estimate.',
  },
];

const exerciseSteps = [
  'Start the call to initialize the saga.',
  'Say something to emit a transcript event.',
  'Place the call on hold, then say something again.',
  'Implement the saga rule that ignores transcripts while on hold.',
  'Resume the call and confirm transcripts are processed again.',
  'Hang up to end the saga run.',
];

const formatTime = (iso: string) =>
  new Date(iso).toLocaleTimeString(undefined, {
    hour: '2-digit',
    minute: '2-digit',
  });

const formatDateTime = (iso: string) =>
  new Date(iso).toLocaleString(undefined, {
    weekday: 'short',
    hour: '2-digit',
    minute: '2-digit',
  });

const createEntryId = () => `${Date.now()}-${Math.random().toString(16).slice(2)}`;

function App() {
  const [state, setState] = useState<DemoState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [speaker, setSpeaker] = useState<'Caller' | 'Agent'>('Caller');
  const [text, setText] = useState('');
  const [actionPending, setActionPending] = useState(false);
  const [callPhase, setCallPhase] = useState<SagaPhase>('Idle');
  const [eventLog, setEventLog] = useState<EventEntry[]>([]);

  const fetchState = async () => {
    setError(null);
    try {
      const response = await fetch('/api/demo/state');
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      const data: DemoState = await response.json();
      setState(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load demo state');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchState();
    const timer = window.setInterval(fetchState, 6000);
    return () => window.clearInterval(timer);
  }, []);

  const logEvent = (label: string, detail?: string) => {
    const entry: EventEntry = {
      id: createEntryId(),
      label,
      detail,
      at: new Date().toISOString(),
    };
    setEventLog((prev) => [entry, ...prev].slice(0, 12));
  };

  const requestSaga = async (
    path: string,
    body: Record<string, unknown>,
    onSuccess: () => void,
    failureMessage: string,
  ) => {
    setActionPending(true);
    setError(null);
    try {
      const response = await fetch(`/api/demo/saga/${path}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(body),
      });

      if (!response.ok) {
        const message = await response.json().catch(() => null);
        throw new Error(message?.message ?? `HTTP ${response.status}`);
      }

      await response.json().catch(() => null);
      onSuccess();
      await fetchState();
    } catch (err) {
      setError(err instanceof Error ? err.message : failureMessage);
    } finally {
      setActionPending(false);
    }
  };

  const startCall = () =>
    requestSaga(
      'start',
      { callId: state?.call?.id ?? null },
      () => {
        setCallPhase('In Progress');
        logEvent('CallStarted', 'Saga initialized for the active call.');
      },
      'Failed to start saga',
    );

  const holdCall = () =>
    requestSaga(
      'hold',
      { callId: state?.call?.id ?? null, reason: 'Agent placed the caller on hold.' },
      () => {
        setCallPhase('On Hold');
        logEvent('CallerHeld', 'Call placed on hold.');
      },
      'Failed to place call on hold',
    );

  const resumeCall = () =>
    requestSaga(
      'resume',
      { callId: state?.call?.id ?? null },
      () => {
        setCallPhase('In Progress');
        logEvent('CallerResumed', 'Call resumed from hold.');
      },
      'Failed to resume call',
    );

  const hangupCall = () =>
    requestSaga(
      'hangup',
      { callId: state?.call?.id ?? null, reason: 'Caller ended the call.' },
      () => {
        setCallPhase('Ended');
        logEvent('CallEnded', 'Call ended and saga completed.');
      },
      'Failed to end call',
    );

  const streamTranscript = () => {
    if (!text.trim()) {
      setError('Transcript text is required.');
      return;
    }

    requestSaga(
      'stream',
      {
        callId: state?.call?.id ?? null,
        speaker,
        text,
      },
      () => {
        logEvent('TranscriptStreaming', `${speaker}: ${text.trim()}`);
        setText('');
      },
      'Failed to stream transcript',
    );
  };

  const clearLog = () => setEventLog([]);

  const lastEvent = eventLog[0];
  const eventCount = eventLog.length;

  const canStart = callPhase === 'Idle' && !actionPending;
  const canHold = callPhase === 'In Progress' && !actionPending;
  const canResume = callPhase === 'On Hold' && !actionPending;
  const canHangup = (callPhase === 'In Progress' || callPhase === 'On Hold') && !actionPending;
  const canSpeak = callPhase !== 'Idle' && callPhase !== 'Ended' && !actionPending;

  return (
    <div className="app">
      <header className="hero">
        <div className="hero__eyebrow">Event-driven architecture lab</div>
        <h1>Call Resolution Saga</h1>
        <p>
          Publish saga events with the call controls, then implement the state machine rule that ignores
          transcript events while the call is on hold.
        </p>
        <div className="hero__meta">
          <div className="meta-card">
            <span className="meta-label">Active call</span>
            <span className="meta-value">
              {state?.call
                ? `${state.call.callerName} with ${state.call.agentName}`
                : loading
                  ? 'Loading call...'
                  : 'No active call'}
            </span>
            {state?.call && (
              <span className="meta-sub">
                Started {formatDateTime(state.call.startedAt)} · Status {state.call.status}
              </span>
            )}
          </div>
          <div className="meta-card">
            <span className="meta-label">Saga phase</span>
            <span className="meta-value">{callPhase}</span>
            <span className="meta-sub">
              {lastEvent ? `Last event: ${lastEvent.label}` : 'No events published yet.'}
            </span>
          </div>
          <div className="meta-card">
            <span className="meta-label">Event log</span>
            <span className="meta-value">{eventCount} events</span>
            <span className="meta-sub">
              {lastEvent ? `Most recent at ${formatTime(lastEvent.at)}` : 'Waiting for saga events.'}
            </span>
          </div>
        </div>
      </header>

      <main className="grid">
        <section className="panel control-panel">
          <div className="panel__header">
            <h2>Call Controls</h2>
            <button className="ghost" type="button" onClick={fetchState} disabled={loading}>
              Refresh state
            </button>
          </div>
          <p className="panel__subtitle">
            Use the call actions to drive the saga. You can publish transcript events even while on hold
            to verify they are ignored.
          </p>
          <div className="action-row">
            <button className="primary" type="button" onClick={startCall} disabled={!canStart}>
              Start call
            </button>
            <button className="ghost" type="button" onClick={holdCall} disabled={!canHold}>
              Hold
            </button>
            <button className="ghost" type="button" onClick={resumeCall} disabled={!canResume}>
              Resume
            </button>
            <button className="danger" type="button" onClick={hangupCall} disabled={!canHangup}>
              Hang up
            </button>
          </div>
          <div className="field">
            <label htmlFor="speaker">Speaker</label>
            <select
              id="speaker"
              value={speaker}
              onChange={(event) => setSpeaker(event.target.value as 'Caller' | 'Agent')}
            >
              <option value="Caller">Caller</option>
              <option value="Agent">Agent</option>
            </select>
          </div>
          <div className="field">
            <label htmlFor="transcript">Transcript</label>
            <textarea
              id="transcript"
              value={text}
              onChange={(event) => setText(event.target.value)}
              placeholder="Type the transcript segment..."
              rows={4}
            />
          </div>
          <div className="quick-row">
            {quickPhrases.map((phrase) => (
              <button
                key={phrase.label}
                type="button"
                className="chip"
                onClick={() => setText(phrase.text)}
              >
                {phrase.label}
              </button>
            ))}
          </div>
          <div className="action-row">
            <button className="primary" type="button" onClick={streamTranscript} disabled={!canSpeak}>
              Say something
            </button>
          </div>
          {error && <div className="notice notice--error">{error}</div>}
        </section>

        <section className="panel log-panel">
          <div className="panel__header">
            <h2>Saga Event Log</h2>
            <button className="ghost" type="button" onClick={clearLog} disabled={!eventLog.length}>
              Clear log
            </button>
          </div>
          {eventLog.length ? (
            <div className="timeline">
              {eventLog.map((event) => (
                <div key={event.id} className="timeline-item">
                  <div className="timeline-time">{formatTime(event.at)}</div>
                  <div className="timeline-body">
                    <span className="timeline-speaker">{event.label}</span>
                    {event.detail && <p>{event.detail}</p>}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="empty">No saga events yet.</div>
          )}
        </section>

        <section className="panel flow-panel">
          <div className="panel__header">
            <h2>Exercise Flow</h2>
            <span className="panel__meta">Guided scenario</span>
          </div>
          <ol className="flow-list">
            {exerciseSteps.map((step) => (
              <li key={step}>{step}</li>
            ))}
          </ol>
        </section>
      </main>
    </div>
  );
}

export default App;
