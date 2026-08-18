export function parseModelJson(raw) {
  if (typeof raw !== "string") {
    throw new TypeError("Model output must be a string.");
  }

  const trimmed = raw.trim();
  const fenced = /^```json[ \t]*\r?\n([\s\S]*?)\r?\n```[ \t]*$/i.exec(trimmed);
  return JSON.parse((fenced?.[1] ?? trimmed).trim());
}
