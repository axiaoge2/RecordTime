import { createHash } from "node:crypto";
import { execFileSync } from "node:child_process";
import { appendFile, readFile, stat } from "node:fs/promises";
import path from "node:path";

const mode = process.argv[2];
const root = process.cwd();

function fail(message) {
  throw new Error(message);
}

function text(value, name, min, max) {
  if (typeof value !== "string" || value.trim().length < min || value.length > max) {
    fail(`${name} must be a string between ${min} and ${max} characters.`);
  }
  return value.trim();
}

function safePath(value) {
  const normalized = text(value, "path", 1, 240).replaceAll("\\", "/");
  if (
    path.isAbsolute(normalized) ||
    normalized.split("/").includes("..") ||
    (!normalized.startsWith("src/") && !normalized.startsWith("tests/"))
  ) {
    fail(`Finding path is outside source or tests: ${normalized}`);
  }
  return normalized;
}

async function loadFindings(file) {
  let parsed;
  try {
    parsed = JSON.parse(await readFile(file, "utf8"));
  } catch (error) {
    fail(`Findings are not valid JSON: ${error.message}`);
  }
  if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
    fail("Findings root must be an object.");
  }
  if (Object.keys(parsed).join(",") !== "findings" || !Array.isArray(parsed.findings)) {
    fail("Findings root must contain only a findings array.");
  }
  if (parsed.findings.length > 10) fail("Daily finding limit is 10.");

  const required = [
    "title", "severity", "confidence", "path", "line", "symbol",
    "summary", "evidence", "reproduction", "suggested_fix",
  ];
  const findings = [];
  for (const [index, item] of parsed.findings.entries()) {
    if (!item || typeof item !== "object" || Array.isArray(item)) {
      fail(`Finding ${index + 1} must be an object.`);
    }
    const keys = Object.keys(item).sort();
    if (keys.join(",") !== [...required].sort().join(",")) {
      fail(`Finding ${index + 1} has missing or unsupported fields.`);
    }
    const finding = {
      title: text(item.title, "title", 10, 120),
      severity: item.severity,
      confidence: item.confidence,
      path: safePath(item.path),
      line: item.line,
      symbol: text(item.symbol, "symbol", 1, 160),
      summary: text(item.summary, "summary", 20, 1000),
      evidence: text(item.evidence, "evidence", 20, 2000),
      reproduction: text(item.reproduction, "reproduction", 20, 1500),
      suggested_fix: text(item.suggested_fix, "suggested_fix", 20, 1500),
    };
    if (!["high", "medium", "low"].includes(finding.severity)) {
      fail(`Finding ${index + 1} has an invalid severity.`);
    }
    if (finding.confidence !== "high") fail("Only high-confidence findings are accepted.");
    if (!Number.isInteger(finding.line) || finding.line < 1) fail("Finding line must be positive.");

    const absolute = path.join(root, finding.path);
    let source;
    try {
      if (!(await stat(absolute)).isFile()) fail(`${finding.path} is not a file.`);
      source = await readFile(absolute, "utf8");
    } catch (error) {
      fail(`Cannot read finding path ${finding.path}: ${error.message}`);
    }
    const lineCount = source.split(/\r?\n/).length;
    if (finding.line > lineCount) fail(`${finding.path}:${finding.line} is outside the file.`);
    findings.push(finding);
  }
  return findings;
}

async function validateEvents(file, requireCommand = true) {
  const events = (await readFile(file, "utf8"))
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line));
  const itemTypes = events.map((event) => event.item?.type).filter(Boolean);
  if (events.some((event) => event.type === "turn.failed")) fail("Codex turn failed.");
  if (requireCommand && !itemTypes.includes("command_execution")) {
    fail(`Codex did not use a shell tool. Item types: ${JSON.stringify(itemTypes)}`);
  }
}

function fingerprint(finding) {
  const identity = `${finding.path}\n${finding.symbol.toLowerCase()}\n${finding.title.toLowerCase()}`;
  return createHash("sha256").update(identity).digest("hex").slice(0, 24);
}

async function githubApi(endpoint, init = {}) {
  const token = process.env.GITHUB_TOKEN;
  const repository = process.env.GITHUB_REPOSITORY;
  if (!token || !repository) fail("GITHUB_TOKEN and GITHUB_REPOSITORY are required.");
  const response = await fetch(`https://api.github.com/repos/${repository}${endpoint}`, {
    ...init,
    headers: {
      Accept: "application/vnd.github+json",
      Authorization: `Bearer ${token}`,
      "User-Agent": "RecordTime-DeepSeek-automation",
      "X-GitHub-Api-Version": "2022-11-28",
      ...init.headers,
    },
  });
  if (!response.ok) {
    const detail = await response.text();
    fail(`GitHub API ${init.method ?? "GET"} ${endpoint} failed: ${response.status} ${detail}`);
  }
  return response.status === 204 ? null : response.json();
}

async function ensureLabel(name, color, description) {
  const encoded = encodeURIComponent(name);
  const response = await fetch(
    `https://api.github.com/repos/${process.env.GITHUB_REPOSITORY}/labels/${encoded}`,
    {
      headers: {
        Accept: "application/vnd.github+json",
        Authorization: `Bearer ${process.env.GITHUB_TOKEN}`,
        "User-Agent": "RecordTime-DeepSeek-automation",
        "X-GitHub-Api-Version": "2022-11-28",
      },
    },
  );
  if (response.status === 404) {
    await githubApi("/labels", {
      method: "POST",
      body: JSON.stringify({ name, color, description }),
    });
  } else if (!response.ok) {
    fail(`Unable to inspect label ${name}: HTTP ${response.status}`);
  }
}

async function publishFindings(file) {
  const findings = await loadFindings(file);
  await ensureLabel("deepseek-bug", "b60205", "Verified by the daily DeepSeek bug hunt");
  await ensureLabel("automated", "6f42c1", "Created by repository automation");

  const existing = await githubApi("/issues?state=all&labels=deepseek-bug&per_page=100");
  const existingFingerprints = new Set();
  for (const issue of existing) {
    const match = issue.body?.match(/deepseek-fingerprint:\s*([a-f0-9]+)/i);
    if (match) existingFingerprints.add(match[1]);
  }

  const created = [];
  for (const finding of findings) {
    const hash = fingerprint(finding);
    if (existingFingerprints.has(hash)) continue;
    const body = [
      `<!-- deepseek-fingerprint: ${hash} -->`,
      "## Summary", finding.summary, "",
      "## Evidence", `Location: \`${finding.path}:${finding.line}\` (\`${finding.symbol}\`)`, "", finding.evidence, "",
      "## Reproduction", finding.reproduction, "",
      "## Suggested fix", finding.suggested_fix, "",
      `Severity: **${finding.severity}** | Confidence: **${finding.confidence}**`,
      "", "_Automatically reported by the daily DeepSeek review. Machine validation is required before any fix is merged._",
    ].join("\n");
    const issue = await githubApi("/issues", {
      method: "POST",
      body: JSON.stringify({
        title: `[DeepSeek] ${finding.title}`.slice(0, 256),
        body,
        labels: ["deepseek-bug", "automated"],
      }),
    });
    created.push(issue.number);
    existingFingerprints.add(hash);
  }

  if (process.env.GITHUB_OUTPUT) {
    const first = created[0]?.toString() ?? "";
    const second = created[1]?.toString() ?? "";
    await appendFile(process.env.GITHUB_OUTPUT, `created_count=${created.length}\nfix_issue_1=${first}\nfix_issue_2=${second}\n`);
  }
  if (process.env.GITHUB_STEP_SUMMARY) {
    await appendFile(
      process.env.GITHUB_STEP_SUMMARY,
      `## Daily bug hunt\n\nValidated findings: ${findings.length}\n\nNew Issues: ${created.length}\n`,
    );
  }
}

async function validatePatch(maxFiles, maxLines) {
  execFileSync("git", ["add", "-N", "."], { stdio: "inherit" });
  const names = execFileSync("git", ["diff", "--name-only", "HEAD"], { encoding: "utf8" })
    .split(/\r?\n/).filter(Boolean).map((name) => name.replaceAll("\\", "/"));
  if (names.length === 0) fail("Codex produced no patch.");
  if (names.length > maxFiles) fail(`Patch changes ${names.length} files; limit is ${maxFiles}.`);
  for (const name of names) {
    const lower = name.toLowerCase();
    if (
      (!lower.startsWith("src/") && !lower.startsWith("tests/")) ||
      lower.startsWith(".github/") || lower.includes("/.claude/") ||
      lower.endsWith("/agents.md") || lower === "agents.md" ||
      lower.endsWith("/claude.md") || lower === "claude.md"
    ) fail(`Forbidden patch path: ${name}`);
  }
  const numstat = execFileSync("git", ["diff", "--numstat", "HEAD"], { encoding: "utf8" });
  let total = 0;
  for (const row of numstat.split(/\r?\n/).filter(Boolean)) {
    const [added, deleted] = row.split("\t");
    if (added === "-" || deleted === "-") fail("Binary patches are not allowed.");
    total += Number(added) + Number(deleted);
  }
  if (total > maxLines) fail(`Patch changes ${total} lines; limit is ${maxLines}.`);
  console.log(`Validated patch: ${names.length} files, ${total} changed lines.`);
}

switch (mode) {
  case "validate-events":
    await validateEvents(process.argv[3]);
    break;
  case "validate-findings":
    console.log(`Validated ${(await loadFindings(process.argv[3])).length} findings.`);
    break;
  case "publish-findings":
    await publishFindings(process.argv[3]);
    break;
  case "validate-patch":
    await validatePatch(Number(process.argv[3] ?? 8), Number(process.argv[4] ?? 300));
    break;
  default:
    fail(`Unknown mode: ${mode}`);
}
