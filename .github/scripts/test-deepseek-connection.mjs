const apiKey = process.env.DEEPSEEK_API_KEY;
const baseUrl = (process.env.DEEPSEEK_BASE_URL ?? "").replace(/\/$/, "");
const model = process.env.DEEPSEEK_MODEL ?? "deepseek-chat";

if (!apiKey || !baseUrl) {
  throw new Error("DEEPSEEK_API_KEY and DEEPSEEK_BASE_URL are required.");
}

async function request(path, init = {}) {
  const response = await fetch(`${baseUrl}${path}`, {
    ...init,
    headers: {
      Authorization: `Bearer ${apiKey}`,
      "Content-Type": "application/json",
      ...init.headers,
    },
    signal: AbortSignal.timeout(30_000),
  });

  let body = null;
  try {
    body = await response.json();
  } catch {
    // Status and response shape are sufficient for this compatibility test.
  }

  return { ok: response.ok, status: response.status, body };
}

const models = await request("/models");
const chat = await request("/chat/completions", {
  method: "POST",
  body: JSON.stringify({
    model,
    messages: [{ role: "user", content: "Reply with only OK." }],
    max_tokens: 8,
    stream: false,
  }),
});
const responses = await request("/responses", {
  method: "POST",
  body: JSON.stringify({
    model,
    input: "Reply with only OK.",
    max_output_tokens: 8,
  }),
});

const chatShapeValid =
  chat.ok && typeof chat.body?.choices?.[0]?.message?.content === "string";
const responsesShapeValid =
  responses.ok &&
  (typeof responses.body?.output_text === "string" ||
    Array.isArray(responses.body?.output));

const summary = [
  "## DeepSeek compatibility test",
  "",
  "| Capability | HTTP status | Result |",
  "| --- | ---: | --- |",
  `| Models API | ${models.status} | ${models.ok ? "Available" : "Failed"} |`,
  `| Chat Completions | ${chat.status} | ${chatShapeValid ? "Compatible" : "Failed or incompatible"} |`,
  `| Responses API | ${responses.status} | ${responsesShapeValid ? "Compatible with Codex custom providers" : "Not compatible"} |`,
  "",
  responsesShapeValid
    ? "The Codex CLI custom-provider path can be tested next."
    : "The automation must use the DeepSeek Chat Completions API directly instead of the Codex CLI custom-provider path.",
  "",
].join("\n");

if (process.env.GITHUB_STEP_SUMMARY) {
  const { appendFile } = await import("node:fs/promises");
  await appendFile(process.env.GITHUB_STEP_SUMMARY, summary);
}

console.log(`Models API: HTTP ${models.status}`);
console.log(`Chat Completions: HTTP ${chat.status}`);
console.log(`Responses API: HTTP ${responses.status}`);

if (!models.ok || !chatShapeValid) {
  throw new Error("DeepSeek authentication or Chat Completions compatibility test failed.");
}
