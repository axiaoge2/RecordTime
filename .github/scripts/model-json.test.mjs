import assert from "node:assert/strict";
import test from "node:test";

import { parseModelJson } from "./model-json.mjs";

test("parses bare JSON", () => {
  assert.deepEqual(parseModelJson('{"findings":[]}'), { findings: [] });
});

test("parses JSON fully enclosed by a JSON Markdown fence", () => {
  assert.deepEqual(parseModelJson('```json\n{"findings":[]}\n```'), { findings: [] });
});

test("accepts CRLF, case, and surrounding whitespace on a JSON fence", () => {
  assert.deepEqual(parseModelJson(' \r\n```JSON  \r\n{"approved":true}\r\n```  \r\n'), {
    approved: true,
  });
});

test("rejects commentary outside the JSON fence", () => {
  assert.throws(
    () => parseModelJson('Here is the result:\n```json\n{"findings":[]}\n```'),
    SyntaxError,
  );
});

test("rejects untyped and multiple Markdown fences", () => {
  assert.throws(() => parseModelJson('```\n{"findings":[]}\n```'), SyntaxError);
  assert.throws(
    () => parseModelJson('```json\n{}\n```\n```json\n{}\n```'),
    SyntaxError,
  );
});

test("rejects malformed JSON inside a fence", () => {
  assert.throws(() => parseModelJson('```json\n{"findings":}\n```'), SyntaxError);
});
