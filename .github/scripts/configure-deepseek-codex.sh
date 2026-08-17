#!/usr/bin/env bash
set -euo pipefail

codex_home="${1:-${RUNNER_TEMP:?RUNNER_TEMP is required}/codex-home}"
model="${DEEPSEEK_MODEL:-deepseek-v4-flash}"

case "$model" in
  deepseek-v4-flash|deepseek-v4-pro) ;;
  *)
    echo "Unsupported DEEPSEEK_MODEL: $model" >&2
    exit 1
    ;;
esac

mkdir -p "$codex_home"

cat > "$codex_home/config.toml" <<EOF
model = "$model"
model_provider = "deepseek"
approval_policy = "never"
sandbox_mode = "danger-full-access"

[shell_environment_policy]
inherit = "core"
ignore_default_excludes = false

[shell_environment_policy.filters]
"DEEPSEEK_API_KEY" = "exclude"
"GITHUB_TOKEN" = "exclude"
"GH_TOKEN" = "exclude"

[model_providers.deepseek]
name = "DeepSeek"
base_url = "https://api.deepseek.com"
env_key = "DEEPSEEK_API_KEY"
wire_api = "responses"
EOF

if [[ -n "${GITHUB_ENV:-}" ]]; then
  echo "CODEX_HOME=$codex_home" >> "$GITHUB_ENV"
fi
