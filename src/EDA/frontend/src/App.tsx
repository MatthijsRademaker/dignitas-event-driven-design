import { useEffect, useMemo, useState } from 'react';
import './App.css';

type CallSummary = {
  id: string;
  agentName: string;
  callerName: string;
  status: string;
  startedAt: string;
};

type TranscriptView = {
  id: string;
  speaker: string;
  text: string;
  receivedAt: string;
};

type DashboardView = {
  lastSpeaker: string;
  lastSnippet: string;
  segmentCount: number;
  updatedAt: string;
};

type SuggestionView = {
  id: string;
  text: string;
  category: string;
  createdAt: string;
};

type ChatMessageView = {
  id: string;
  speaker: string;
  text: string;
  receivedAt: string;
};

type DemoState = {
  call: CallSummary | null;
  transcripts: TranscriptView[];
  chat: ChatMessageView[];
  dashboard: DashboardView | null;
  suggestions: SuggestionView[];
};

type TranscriptRecordResult = {
  callId: string;
  segmentId: string;
  published: boolean;
  error: string | null;
};

type QuickPhrase = {
  label: string;
  text: string;
  forceFailure?: boolean;
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
    forceFailure: true,
  },
  {
    label: 'Delivery delay',
    text: 'My order is late and I need an updated delivery estimate.',
  },
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

function App() {
  const [state, setState] = useState<DemoState | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [speaker, setSpeaker] = useState<'Caller' | 'Agent'>('Caller');
  const [text, setText] = useState('');
  const [notice, setNotice] = useState<TranscriptRecordResult | null>(null);
  const [actionPending, setActionPending] = useState(false);
  const [forcePublishFailure, setForcePublishFailure] = useState(false);

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
    const timer = window.setInterval(fetchState, 4000);
    return () => window.clearInterval(timer);
  }, []);

  const consistency = useMemo(() => {
    if (!state) return null;
    const writeCount = state.transcripts.length;
    const readCount = state.dashboard?.segmentCount ?? 0;
    const inSync = writeCount === readCount;
    return { writeCount, readCount, inSync };
  }, [state]);

  const submitTranscript = async (simulatePublishFailure: boolean) => {
    if (!text.trim()) {
      setError('Transcript text is required.');
      return;
    }

    setActionPending(true);
    setError(null);
    try {
      const response = await fetch('/api/demo/transcripts', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          callId: state?.call?.id ?? null,
          speaker,
          text,
          simulatePublishFailure,
        }),
      });

      if (!response.ok) {
        const message = await response.json().catch(() => null);
        throw new Error(message?.message ?? `HTTP ${response.status}`);
      }

      const result: TranscriptRecordResult = await response.json();
      setNotice(result);
      setText('');
      await fetchState();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to record transcript');
    } finally {
      setActionPending(false);
      setForcePublishFailure(false);
    }
  };

  const resetDemo = async () => {
    setActionPending(true);
    setNotice(null);
    setError(null);
    try {
      const response = await fetch('/api/demo/reset', { method: 'POST' });
      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }
      await fetchState();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to reset demo');
    } finally {
      setActionPending(false);
    }
  };

  return (
    <div className="app">
      <header className="hero">
        <div className="hero__eyebrow">Event-driven architecture lab</div>
        <h1>Call Center CQRS Inconsistency</h1>
        <p>
          Record transcript segments, then watch the write model move ahead of the read model
          when the event publish step is skipped.
        </p>
        <div className="hero__meta">
          <div className="meta-card">
            <span className="meta-label">Active call</span>
            <span className="meta-value">
              {state?.call
                ? `${state.call.callerName} with ${state.call.agentName}`
                : 'Loading call...'}
            </span>
            {state?.call && (
              <span className="meta-sub">
                Started {formatDateTime(state.call.startedAt)} · Status {state.call.status}
              </span>
            )}
          </div>
          <div className={`meta-card ${consistency?.inSync ? 'meta-card--ok' : 'meta-card--warn'}`}>
            <span className="meta-label">Agent chat dashboard</span>
            <span className="meta-value">{consistency?.readCount ?? 0} segments</span>
            <span className="meta-sub">
              {consistency?.inSync ? 'Projections are aligned.' : 'Projections are stale.'}
            </span>
          </div>
        </div>
      </header>

      <main className="grid">
        <section className="panel control-panel">
          <div className="panel__header">
            <h2>Transcript Input</h2>
            <button className="ghost" type="button" onClick={fetchState} disabled={loading}>
              Refresh state
            </button>
          </div>
          <p className="panel__subtitle">
            Send a transcript segment and watch how the dashboard responds.
          </p>
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
              onChange={(event) => {
                setText(event.target.value);
                setForcePublishFailure(false);
              }}
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
                onClick={() => {
                  setText(phrase.text);
                  setForcePublishFailure(Boolean(phrase.forceFailure));
                }}
              >
                {phrase.label}
              </button>
            ))}
          </div>
          <div className="action-row">
            <button
              className="primary"
              type="button"
              onClick={() => submitTranscript(forcePublishFailure)}
              disabled={actionPending}
            >
              Send
            </button>
            <button className="ghost" type="button" onClick={resetDemo} disabled={actionPending}>
              Reset demo
            </button>
          </div>
          {notice && (
            <div className={`notice ${notice.published ? 'notice--ok' : 'notice--warn'}`}>
              <span>
                {notice.published ? 'Transcript sent.' : 'Transcript recorded.'}
              </span>
            </div>
          )}
          {error && <div className="notice notice--error">{error}</div>}
        </section>

        <section className="panel chat-panel">
          <div className="panel__header">
            <h2>Agent Chat Dashboard</h2>
            <span className="panel__meta">
              {state?.dashboard ? `Updated ${formatTime(state.dashboard.updatedAt)}` : 'Waiting for events'}
            </span>
          </div>
          <div className="chat-window">
            {state?.chat.length ? (
              state.chat.map((message) => (
                <div
                  key={message.id}
                  className={`chat-message ${message.speaker === 'Agent' ? 'chat-message--agent' : 'chat-message--caller'}`}
                >
                  <div className="chat-bubble">
                    <p>{message.text}</p>
                    <span className="chat-time">{formatTime(message.receivedAt)}</span>
                  </div>
                </div>
              ))
            ) : (
              <div className="empty">Awaiting transcript events.</div>
            )}
          </div>
        </section>

        <section className="panel actions-panel">
          <div className="panel__header">
            <h2>Next Best Actions</h2>
            <span className="panel__meta">AI suggestions</span>
          </div>
          {state?.suggestions.length ? (
            <ul className="suggestions">
              {state.suggestions.map((suggestion) => (
                <li key={suggestion.id}>
                  <span className="tag">{suggestion.category}</span>
                  <span>{suggestion.text}</span>
                </li>
              ))}
            </ul>
          ) : (
            <div className="empty">Awaiting transcript events.</div>
          )}
        </section>
      </main>
    </div>
  );
}

export default App;
