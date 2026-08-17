#!/usr/bin/env bash
set -euo pipefail

codex_home="${1:-${RUNNER_TEMP:?RUNNER_TEMP is required}/codex-home}"
mkdir -p "$codex_home"

cat > "$codex_home/config.toml" <<'EOF'
model = "deepseek-chat"
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
