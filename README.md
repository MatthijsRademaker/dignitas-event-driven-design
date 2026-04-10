# Event-Driven Design with Aimy

Local run guide for the Aspire-based demo app.

## Prerequisites

- .NET SDK 10.x (targets `net10.0`)
- Aspire CLI (`aspire` on your PATH)
- Node.js `^20.19.0` or `>=22.12.0` (frontend build)

## Start the app

```bash
cd src/EDA
aspire start
```

Use the Aspire dashboard URL printed in the terminal to open the app and view logs.

### Worktrees or multiple runs

```bash
cd src/EDA
aspire start --isolated
```

## Stop the app

```bash
aspire stop
```
