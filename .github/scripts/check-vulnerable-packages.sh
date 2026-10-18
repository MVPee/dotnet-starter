#!/usr/bin/env bash
# Usage: check-vulnerable-packages.sh [report.json]   (without argument, the report is generated for the solution)
set -euo pipefail

readonly SOLUTION_FILE="Starter.slnx"
# Fail closed: any severity outside this list (High, Critical, or a future value) blocks.
readonly WARNING_ONLY_SEVERITIES='["Low","Moderate"]'

if [[ $# -gt 0 ]]; then
  report=$(cat "$1")
else
  report=$(dotnet list "$SOLUTION_FILE" package --vulnerable --include-transitive --format json)
fi

# dotnet can exit 0 with a "problems" report (unreachable feed, missing assets): treat that as a failed audit, not a clean one.
if ! jq -e '(.problems // []) == [] and (.projects | type) == "array"' <<<"$report" >/dev/null; then
  jq -r '(.problems // [])[] | "::error::Vulnerability audit could not run: \(.text)"' <<<"$report"
  echo "Vulnerability audit report is incomplete; see the errors above." >&2
  exit 1
fi

vulnerabilities=$(jq --argjson warningOnly "$WARNING_ONLY_SEVERITIES" '[
  .projects[]
  | (.path | split("/") | last) as $project
  | .frameworks[]?
  | ((.topLevelPackages // []) + (.transitivePackages // []))[]
  | . as $package
  | .vulnerabilities[]?
  | {project: $project, package: $package.id, version: $package.resolvedVersion, severity: .severity, advisory: .advisoryurl}
]
| group_by([.package, .version, .advisory])
| map(.[0] + {projects: (map(.project) | unique | join(", "))}
    | . + {level: (.severity as $severity | if ($warningOnly | index($severity)) then "warning" else "error" end)})' <<<"$report")

if [[ $(jq 'length' <<<"$vulnerabilities") -eq 0 ]]; then
  echo "No vulnerable NuGet packages."
  exit 0
fi

jq -r '.[] | "::\(.level)::\(.severity) vulnerability in \(.package) \(.version) (\(.projects)): \(.advisory)"' <<<"$vulnerabilities"

blocking_count=$(jq '[.[] | select(.level == "error")] | length' <<<"$vulnerabilities")
if [[ $blocking_count -gt 0 ]]; then
  echo "$blocking_count vulnerable package(s) above $(jq -r 'join("/")' <<<"$WARNING_ONLY_SEVERITIES"): update them (Dependabot opens PRs weekly)." >&2
  exit 1
fi
