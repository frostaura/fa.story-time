#!/bin/bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
STATE_DIR="${ROOT_DIR}/.copilot-iteration-state"
HISTORY_DIR="${STATE_DIR}/history"
LATEST_DIR="${STATE_DIR}/latest"
RUN_ID="$(date -u +"%Y%m%dT%H%M%SZ")"
RUN_DIR="${HISTORY_DIR}/${RUN_ID}"
PREVIOUS_ROOT_DIR="${RUN_DIR}/previous-root"
CONTINUATION_CONTEXT_FILE="${RUN_DIR}/CONTINUATION_CONTEXT.md"
DISCOVERY_EVIDENCE_FILE="${RUN_DIR}/DISCOVERY_EVIDENCE.md"
OPEN_ISSUES_FILE="${ROOT_DIR}/OPEN_ISSUES.md"
ITERATION_STATUS_FILE="${RUN_DIR}/ITERATION_STATUS.md"
MAX_PASSES="${MAX_PASSES:-3}"

REPORT_FILES=(
    "COMPLETION_REPORT.md"
    "ROADMAP.md"
    "IMPLEMENTATION_SUMMARY.md"
    "QA_TEARDOWN_REPORT.md"
    "FINAL_IMPLEMENTATION_SUMMARY.md"
    "NEXT_ITERATION_MEMORY.md"
    "OPEN_ISSUES.md"
)

mkdir -p "${HISTORY_DIR}" "${LATEST_DIR}" "${RUN_DIR}" "${PREVIOUS_ROOT_DIR}"

copy_if_exists() {
    local source_path="$1"
    local destination_path="$2"

    if [[ -f "${source_path}" ]]; then
        cp "${source_path}" "${destination_path}"
    fi
}

archive_report() {
    local file_name="$1"
    local source_path="${ROOT_DIR}/${file_name}"

    if [[ -f "${source_path}" ]]; then
        cp "${source_path}" "${RUN_DIR}/${file_name}"
    fi
}

refresh_latest_snapshot() {
    rm -rf "${LATEST_DIR}"
    mkdir -p "${LATEST_DIR}"

    local file_name
    for file_name in "${REPORT_FILES[@]}"; do
        copy_if_exists "${RUN_DIR}/${file_name}" "${LATEST_DIR}/${file_name}"
    done

    copy_if_exists "${CONTINUATION_CONTEXT_FILE}" "${LATEST_DIR}/CONTINUATION_CONTEXT.md"
    copy_if_exists "${DISCOVERY_EVIDENCE_FILE}" "${LATEST_DIR}/DISCOVERY_EVIDENCE.md"
    copy_if_exists "${ITERATION_STATUS_FILE}" "${LATEST_DIR}/ITERATION_STATUS.md"

    printf '%s\n' "${RUN_ID}" > "${LATEST_DIR}/RUN_ID"
}

snapshot_existing_reports() {
    local file_name
    for file_name in "${REPORT_FILES[@]}"; do
        copy_if_exists "${ROOT_DIR}/${file_name}" "${PREVIOUS_ROOT_DIR}/${file_name}"
    done
}

append_command_output() {
    local title="$1"
    shift

    {
        printf '## %s\n\n' "${title}"
        printf '```text\n'
        if "$@" 2>&1; then
            :
        else
            printf '\n[Command exited with non-zero status]\n'
        fi
        printf '```\n\n'
    } >> "${DISCOVERY_EVIDENCE_FILE}"
}

append_root_docs_output() {
    {
        printf "## Root and docs files\n\n"
        printf "```text\n"
        if command -v rg >/dev/null 2>&1; then
            rg --files "${ROOT_DIR}" -g "docs/**" -g ".github/**" -g "Makefile" -g "docker-compose.yml" -g "*.md"
        else
            find "${ROOT_DIR}" -type f \( -path "${ROOT_DIR}/docs/*" -o -path "${ROOT_DIR}/.github/*" -o -name "*.md" -o -name "Makefile" -o -name "docker-compose.yml" \) | sort
            printf "\n[rg unavailable: used find fallback]\n"
        fi
        printf "```\n\n"
    } >> "${DISCOVERY_EVIDENCE_FILE}"
}

append_todo_marker_output() {
    {
        printf "## TODO markers in maintained source\n\n"
        printf "```text\n"
        if command -v rg >/dev/null 2>&1; then
            rg -n --glob "!src/frontend/playwright-report/**" --glob "!src/frontend/test-results/**" --glob "!src/frontend/coverage/**" --glob "!src/backend/TestResults/**" "TODO|FIXME|XXX" "${ROOT_DIR}/src" || true
        else
            find "${ROOT_DIR}/src" \
                \( -path "${ROOT_DIR}/src/frontend/playwright-report" -o -path "${ROOT_DIR}/src/frontend/test-results" -o -path "${ROOT_DIR}/src/frontend/coverage" -o -path "${ROOT_DIR}/src/backend/TestResults" \) -prune -o \
                -type f -print0 \
                | xargs -0 grep -nE "TODO|FIXME|XXX" || true
            printf "\n[rg unavailable: used grep fallback]\n"
        fi
        printf "```\n\n"
    } >> "${DISCOVERY_EVIDENCE_FILE}"
}

build_discovery_evidence() {
    cat > "${DISCOVERY_EVIDENCE_FILE}" <<EOF
# Deterministic Discovery Evidence

- Run id: ${RUN_ID}
- Repository root: ${ROOT_DIR}
- Generated at: $(date -u +"%Y-%m-%dT%H:%M:%SZ")

EOF

    append_command_output "Git status" git -C "${ROOT_DIR}" status --short --branch
    append_command_output "Git diff summary" git -C "${ROOT_DIR}" diff --stat
    append_root_docs_output
    append_todo_marker_output
}

ensure_open_issues_file() {
    if [[ ! -f "${OPEN_ISSUES_FILE}" ]]; then
        cat > "${OPEN_ISSUES_FILE}" <<EOF
# Open Issues Ledger

## Open Issues
- None yet.

## Resolved Issues
- None yet.
EOF
    fi
}

build_continuation_context() {
    local latest_run_id="none"
    if [[ -f "${LATEST_DIR}/RUN_ID" ]]; then
        latest_run_id="$(<"${LATEST_DIR}/RUN_ID")"
    fi

    cat > "${CONTINUATION_CONTEXT_FILE}" <<EOF
# Iteration Continuation Context
- Current run id: ${RUN_ID}
- Previous archived run id: ${latest_run_id}

## Required continuity behavior
1. Distinguish between resolved, open, and newly discovered issues.
2. If an issue was fixed in code, docs, or tests, do not reopen it without fresh repository evidence.
3. CRITICAL: Never truncate the OPEN_ISSUES.md file. When updating it, write the entire ledger.
EOF
}

run_copilot_step_ui() {
    local label="$1"
    local prompt_text="$2"
    shift 2
    local pending_stages=("$@")
    local num_pending=${#pending_stages[@]}

    local step_timestamp="$(date +%s%N)"
    local prompt_file="${RUN_DIR}/compiled_prompt_${step_timestamp}.txt"
    local result_file="${RUN_DIR}/result_${step_timestamp}.txt"

    printf '%s\n' "${prompt_text}" > "${prompt_file}"
    touch "$result_file"

    # Detach stdin from the terminal to prevent the background process from hanging
    copilot --yolo --model "gpt-5.4" -p "${prompt_text}" < /dev/null > "${result_file}" 2>/dev/null &
    local pid=$!

    local start_time=$SECONDS
    local term_width=$(tput cols 2>/dev/null || echo 80)
    term_width=$((term_width - 5))
    if [[ $term_width -lt 10 ]]; then term_width=80; fi

    # Calculate required terminal height: 1 header + 8 output lines + 2 blank lines + N pending stages
    local ui_height=$((9))
    if [[ $num_pending -gt 0 ]]; then
        ui_height=$((11 + num_pending))
    fi

    # Disable terminal wrapping, dynamically reserve required lines, and save cursor position (\0337)
    local reserve_newlines=""
    for ((i=0; i<ui_height; i++)); do reserve_newlines+="\n"; done
    printf "\033[?7l%b\033[%dA\0337" "$reserve_newlines" "$ui_height" >&2

    while kill -0 $pid 2>/dev/null; do
        local elapsed=$((SECONDS - start_time))

        # Restore cursor to exact saved position (\0338)
        printf "\0338" >&2

        # Draw the active header with cyan icon and yellow timer
        printf "\033[K\r\033[1;36m➤  %s\033[0m [\033[1;33m%ds\033[0m]\n" "$label" "$elapsed" >&2

        # Safe pipeline: direct pipe to awk prevents macOS BSD newline crashes.
        # "|| true" prevents set -e from crashing the script if grep finds an empty file.
        (grep -v '^[[:space:]]*$' "$result_file" 2>/dev/null | tail -n 8 | expand | cut -c 1-"$term_width" | awk '
        BEGIN { count = 0 }
        {
            lines[++count] = $0
        }
        END {
            for (i=1; i<=8; i++) {
                if (i <= count) {
                    # Print with a blue vertical pipe and dimmed text
                    printf "\033[K\033[1;34m│\033[0m \033[2m%s\033[0m\n", lines[i]
                } else {
                    printf "\033[K\n"
                }
            }
        }') >&2 || true

        # Draw pending stages if they exist
        if [[ $num_pending -gt 0 ]]; then
            printf "\033[K\n\033[K\n" >&2 # 2x empty spacer lines
            for stage in "${pending_stages[@]}"; do
                printf "\033[K\r\033[0;33m   ○  %s\033[0m\n" "$stage" >&2
            done
        fi

        sleep 0.15
    done

    wait $pid
    local exit_code=$?
    local final_elapsed=$((SECONDS - start_time))

    # Restore cursor, re-enable wrapping
    printf "\0338\033[?7h" >&2

    if [[ $exit_code -eq 0 ]]; then
        printf "\033[K\r\033[1;32m✔  %s\033[0m [\033[1;33m%ds\033[0m]\n" "$label" "$final_elapsed" >&2
    else
        printf "\033[K\r\033[1;31m✖  %s\033[0m [\033[1;33m%ds\033[0m] (Failed)\n" "$label" "$final_elapsed" >&2
    fi

    # Cleanly wipe all the UI lines below the completed header so the next step sits perfectly beneath it
    local wipe_lines=$((ui_height - 1))
    local wipe_seq=""
    for ((i=0; i<wipe_lines; i++)); do wipe_seq+="\033[K\n"; done
    printf "%b\033[%dA" "$wipe_seq" "$wipe_lines" >&2

    cat "$result_file"
}

count_open_issues() {
    if [[ ! -f "${OPEN_ISSUES_FILE}" ]]; then
        printf '0\n'
        return
    fi
    awk '
        /^## Open Issues/ { in_open=1; next }
        /^## / && in_open { in_open=0 }
        in_open && /ISSUE-[0-9]+/ { count++ }
        END { print count + 0 }
    ' "${OPEN_ISSUES_FILE}"
}

record_iteration_status() {
    local pass_number="$1"
    local open_issue_count="$2"
    local status_label="$3"

    cat > "${ITERATION_STATUS_FILE}" <<EOF
# Iteration Status
- Run id: ${RUN_ID}
- Last completed pass: ${pass_number}
- Open issue count: ${open_issue_count}
- Status: ${status_label}
EOF
}

snapshot_existing_reports
ensure_open_issues_file
build_discovery_evidence
build_continuation_context

clear

# Clean Title and Subtitle Hierarchy
printf "\n"
printf "\033[1;32m=== GAIA EXODUS ===\033[0m\n"
printf "\033[0;36mby frostaura\033[0m\n"
printf "\n\n"

printf "\033[1;36m▶ Initiating variable-driven implementation engine...\033[0m\n"
printf "\033[2m  Run id: %s\033[0m\n" "${RUN_ID}"

for pass_number in $(seq 1 "${MAX_PASSES}"); do
    printf "\n\n\033[1;35m✦ Starting convergence pass %s/%s...\033[0m\n\n" "${pass_number}" "${MAX_PASSES}"

    # 1. AUDIT
    audit_prompt=$(cat <<EOF
You are a dispassionate auditor. Assess the provided context.

<system_spec>
$(cat "${ROOT_DIR}/docs/specs/system_spec.md" 2>/dev/null || echo "No system spec found.")
</system_spec>

<continuation_context>
$(cat "${CONTINUATION_CONTEXT_FILE}" 2>/dev/null || echo "No context found.")
</continuation_context>

<discovery_evidence>
$(cat "${DISCOVERY_EVIDENCE_FILE}" 2>/dev/null || echo "No evidence found.")
</discovery_evidence>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

TASK 1: Use your agentic file-editing tools to update OPEN_ISSUES.md in the repository. Preserve existing IDs and update statuses.
TASK 2: Your text response (stdout) MUST ONLY be the Completion Report in the exact format below. Do not attempt to save the report to a file; I will capture your output. Provide ZERO commentary outside this structure.

# Completion Report
Overall Score: [0-100]%
Delta: [+/-]% since last iteration

| Missing Item (Issue ID) | % Complete | Details (Brief Overview) |
|---|---|---|
| ISSUE-001 | 50% | Feature X lacks test coverage |
EOF
)
    COMPLETION_REPORT_TEXT=$(run_copilot_step_ui "Assessing completion" "${audit_prompt}" \
        "Planning roadmap" "Implementing roadmap items" "Conducting QA teardown" "Implementing final fixes")

    echo "${COMPLETION_REPORT_TEXT}" > "${ROOT_DIR}/COMPLETION_REPORT.md"
    archive_report "COMPLETION_REPORT.md"
    archive_report "OPEN_ISSUES.md"


    # 2. ROADMAP
    roadmap_prompt=$(cat <<EOF
Create an execution roadmap based on the completion report and current ledger.

<completion_report>
${COMPLETION_REPORT_TEXT}
</completion_report>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

<discovery_evidence>
$(cat "${DISCOVERY_EVIDENCE_FILE}" 2>/dev/null || echo "No evidence found.")
</discovery_evidence>

Your text response (stdout) MUST ONLY be the Roadmap in markdown format. Do not use file saving tools for the roadmap. Include:
1. Carry-Forward Work Still Required
2. New Work Discovered
3. Ordered Execution Plan
EOF
)
    ROADMAP_TEXT=$(run_copilot_step_ui "Planning roadmap" "${roadmap_prompt}" \
        "Implementing roadmap items" "Conducting QA teardown" "Implementing final fixes")

    echo "${ROADMAP_TEXT}" > "${ROOT_DIR}/ROADMAP.md"
    archive_report "ROADMAP.md"


    # 3. IMPLEMENTATION
    implementation_prompt=$(cat <<EOF
Implement the active roadmap items.

<roadmap>
${ROADMAP_TEXT}
</roadmap>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

TASK 1: Use your agentic file-editing tools to modify the codebase to fix the issues, and update OPEN_ISSUES.md accordingly.
TASK 2: Your text response (stdout) MUST ONLY be the Implementation Summary in markdown format. Do not save it to a file. Include:
1. Items Completed
2. Remaining Blockers
3. Files Changed
EOF
)
    IMPLEMENTATION_TEXT=$(run_copilot_step_ui "Implementing roadmap items" "${implementation_prompt}" \
        "Conducting QA teardown" "Implementing final fixes")

    echo "${IMPLEMENTATION_TEXT}" > "${ROOT_DIR}/IMPLEMENTATION_SUMMARY.md"
    archive_report "IMPLEMENTATION_SUMMARY.md"
    archive_report "OPEN_ISSUES.md"


    # 4. QA TEARDOWN
    qa_prompt=$(cat <<EOF
You are a ruthless UI/UX expert and QA engineer.

<discovery_evidence>
$(cat "${DISCOVERY_EVIDENCE_FILE}" 2>/dev/null || echo "No evidence found.")
</discovery_evidence>

<implementation_summary>
${IMPLEMENTATION_TEXT}
</implementation_summary>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

TASK 1: Use your tools to verify the application. Update OPEN_ISSUES.md with any regressions or new findings.
TASK 2: Your text response (stdout) MUST ONLY be the QA Teardown Report. Include:
1. Regressions Fixed
2. Critical Issues Still Open
3. Visual/UX Offenses
4. Functional Breakages
EOF
)
    QA_REPORT_TEXT=$(run_copilot_step_ui "Conducting QA teardown" "${qa_prompt}" \
        "Implementing final fixes")

    echo "${QA_REPORT_TEXT}" > "${ROOT_DIR}/QA_TEARDOWN_REPORT.md"
    archive_report "QA_TEARDOWN_REPORT.md"
    archive_report "OPEN_ISSUES.md"


    # 5. FINAL FIXES
    final_fixes_prompt=$(cat <<EOF
Implement the fixes demanded by QA.

<qa_teardown_report>
${QA_REPORT_TEXT}
</qa_teardown_report>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

TASK 1: Edit the codebase to fix the issues and finalize OPEN_ISSUES.md.
TASK 2: Your text response (stdout) MUST ONLY be the Final Implementation Summary. Include:
1. QA Issues Fixed
2. Carry-Forward Issues Still Blocked
3. Files Changed
EOF
)
    FINAL_SUMMARY_TEXT=$(run_copilot_step_ui "Implementing final fixes" "${final_fixes_prompt}")

    echo "${FINAL_SUMMARY_TEXT}" > "${ROOT_DIR}/FINAL_IMPLEMENTATION_SUMMARY.md"
    archive_report "FINAL_IMPLEMENTATION_SUMMARY.md"
    archive_report "OPEN_ISSUES.md"


    # CHECK CONVERGENCE
    open_issue_count="$(count_open_issues)"
    if [[ "${open_issue_count}" == "0" ]]; then
        record_iteration_status "${pass_number}" "${open_issue_count}" "converged"
        printf "\n\033[1;32m★ No open issues remain after pass %s; stopping early.\033[0m\n\n" "${pass_number}"
        break
    fi

    record_iteration_status "${pass_number}" "${open_issue_count}" "needs-another-pass"
    if [[ "${pass_number}" == "${MAX_PASSES}" ]]; then
        printf "\n\033[1;33m⚠ Reached maximum passes with %s open issues still in the ledger.\033[0m\n\n" "${open_issue_count}"
    fi
done

# 6. NEXT ITERATION MEMORY
continuation_memory_prompt=$(cat <<EOF
Produce a durable handoff for the next iteration based on the current run's outputs.

<completion_report>
${COMPLETION_REPORT_TEXT:-"No completion report generated."}
</completion_report>

<final_implementation_summary>
${FINAL_SUMMARY_TEXT:-"No final summary generated."}
</final_implementation_summary>

<open_issues_ledger>
$(cat "${OPEN_ISSUES_FILE}" 2>/dev/null || echo "No ledger found.")
</open_issues_ledger>

Your text response (stdout) MUST ONLY be the Next Iteration Memory. Include:
1. Current Completion Estimate
2. Remaining Open Work
3. Immediate Priorities
EOF
)
printf "\n"
NEXT_ITERATION_TEXT=$(run_copilot_step_ui "Writing next-iteration memory" "${continuation_memory_prompt}")

echo "${NEXT_ITERATION_TEXT}" > "${ROOT_DIR}/NEXT_ITERATION_MEMORY.md"
archive_report "NEXT_ITERATION_MEMORY.md"

refresh_latest_snapshot

printf "\n\033[1;36m✔ Iteration artifacts archived to %s\033[0m\n" "${RUN_DIR}"
printf "\033[1;32m★ Script finished successfully!\033[0m\n\n"
