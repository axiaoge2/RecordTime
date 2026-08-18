export function parseModelJson(raw) {
  if (typeof raw !== "string") {
    throw new TypeError("Model output must be a string.");
  }

  const trimmed = raw.trim();
  const fenced = /^```json[ \t]*\r?\n([\s\S]*?)\r?\n```[ \t]*$/i.exec(trimmed);
  return JSON.parse((fenced?.[1] ?? trimmed).trim());
}

export function truncateModelText(value, maxLength) {
  if (typeof value !== "string") {
    throw new TypeError("Model text must be a string.");
  }
  if (!Number.isInteger(maxLength) || maxLength < 4) {
    throw new RangeError("Maximum length must be an integer of at least 4.");
  }
  if (value.length <= maxLength) return value;

  const prefixLimit = maxLength - 3;
  let prefix = "";
  for (const character of value) {
    if (prefix.length + character.length > prefixLimit) break;
    prefix += character;
  }
  return `${prefix.trimEnd()}...`;
}
