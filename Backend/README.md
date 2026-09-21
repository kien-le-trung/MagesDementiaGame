# MAGES Approach Judge

Small Cloudflare Worker used by the Unity game to evaluate Lan's natural-language approach to Minh.
The OpenRouter key remains on the server. The model is fixed to `openrouter/free`, so clients cannot
request a paid model.

## One-time setup

1. Install Node.js 20 or newer.
2. Open a terminal in this `Backend` directory.
3. Install dependencies:

   ```powershell
   npm install
   ```

4. Sign in to Cloudflare:

   ```powershell
   npx wrangler login
   ```

5. Create an API key in the OpenRouter dashboard, then store it as a Cloudflare secret:

   ```powershell
   npx wrangler secret put OPENROUTER_API_KEY
   ```

6. Deploy:

   ```powershell
   npm run deploy
   ```

Wrangler prints the public `workers.dev` URL after deployment.

## Local development

Copy `.dev.vars.example` to `.dev.vars`, enter the OpenRouter key, and run:

```powershell
npm run dev
```

Do not commit `.dev.vars`; it is ignored by Git.

## Endpoints

### `GET /health`

Returns service status and the fixed free-router identifier.

### `POST /evaluate-approach`

Request:

```json
{
  "text": "Hello Minh, it's Lan. May I sit with you for a moment?"
}
```

Successful response:

```json
{
  "recognition": 2,
  "trust": 2,
  "distress": 0,
  "clarity": 2,
  "outcome": "supportive",
  "feedback": "The calm introduction and single, respectful question support recognition and trust."
}
```

Scores are integers from 0 to 2. For `distress`, lower is better; for the other scores, higher is better.

Example local request:

```powershell
Invoke-RestMethod `
  -Method Post `
  -Uri http://localhost:8787/evaluate-approach `
  -ContentType application/json `
  -Body '{"text":"Hello Minh, it is Lan. Would you like to walk with me?"}'
```

## Unity/WebGL CORS

`ALLOWED_ORIGIN` defaults to `*`, which is convenient during development and for native Unity builds.
For a public WebGL release, change it in `wrangler.jsonc` to the exact website origin, for example
`https://example.pages.dev`, and redeploy.

## Free-tier behavior

`openrouter/free` routes requests among currently available free models. Availability and rate limits are
controlled by OpenRouter, so the Unity client should offer a Retry button and a non-AI fallback when the
service returns HTTP 429, 502, or 504.

Before publishing broadly, configure Cloudflare rate limiting for `POST /evaluate-approach`; this endpoint
is intentionally narrow but is publicly reachable after deployment.
