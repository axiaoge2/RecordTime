import { appendFile, readFile } from "node:fs/promises";

const mode = process.argv[2] ?? "api";

if (mode === "codex") {
  const [, , , eventsPath, messagePath] = process.argv;
  const expectedMessage = process.env.CODEX_EXPECTED_MESSAGE;
  if (!eventsPath || !messagePath || !expectedMessage) {
    throw new Error(
      "Codex event path, final-message path, and expected result are required.",
    );
  }

  const events = (await readFile(eventsPath, "utf8"))
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line));
  const message = (await readFile(messagePath, "utf8")).trim();
  const itemTypes = events
    .map((event) => event.item?.type)
    .filter((type) => typeof type === "string");
  const usedShellTool = events.some(
    (event) => event.item?.type === "command_execution",
  );

  if (!usedShellTool) {
    throw new Error(
      `Codex completed without a shell tool call. Observed item types: ${JSON.stringify(itemTypes)}. Final message: ${JSON.stringify(message)}.`,
    );
  }
  if (message !== expectedMessage) {
    throw new Error(
      `Unexpected Codex result: ${JSON.stringify(message)}; expected ${expectedMessage}.`,
    );
  }

  const summary = [
    "## Codex CLI tool test",
    "",
    "| Check | Result |",
    "| --- | --- |",
    "| DeepSeek custom provider | Connected |",
    "| Read-only shell tool | Used successfully |",
    `| Verified result | \`${expectedMessage}\` |`,
    "",
  ].join("\n");

  if (process.env.GITHUB_STEP_SUMMARY) {
    await appendFile(process.env.GITHUB_STEP_SUMMARY, summary);
  }
  console.log("Codex custom-provider tool call succeeded.");
  process.exit(0);
}

if (mode !== "api") {
  throw new Error(`Unknown test mode: ${mode}`);
}

const apiKey = process.env.DEEPSEEK_API_KEY;
const baseUrl = (process.env.DEEPSEEK_BASE_URL ?? "").replace(/\/$/, "");
const model = process.env.DEEPSEEK_MODEL ?? "deepseek-v4-flash";

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
  await appendFile(process.env.GITHUB_STEP_SUMMARY, summary);
}

console.log(`Models API: HTTP ${models.status}`);
console.log(`Chat Completions: HTTP ${chat.status}`);
console.log(`Responses API: HTTP ${responses.status}`);

if (!models.ok || !chatShapeValid || !responsesShapeValid) {
  throw new Error("DeepSeek authentication or API compatibility test failed.");
}
