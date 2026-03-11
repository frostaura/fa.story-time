#!/bin/bash
while true; do
    echo "==="
    echo "Initiating Gaia spec implementation engine..."
    echo ""
    echo "Assessing the system specifications, test plans, and code repository..."
    echo "==="
    copilot --yolo --model "gpt-5.4" -p "Assess the docs/specs/system_spec.md and the state of the code repository. Assess the docs/specs/test_spec.md for a comprehensive test plan for the system. Determine the the completion rate for all features, items, outstanding todos, ourstanding static, sample or mock data, that data and config is well-abstracted and live in the config or appsettings files and not statically coded anywhere. Integration tests implementation completion rates, based on the test spec and the state of the repo. This should be a dispassionate report. Dont take the role of a developer or tester. Instead you are a dispassionate auditor. A feature parity, quality and visual excellence police. Detective Gaia, if you will. create a detailed report for the overall system completion. Save your response to COMPLETION_REPORT.md. Lastly, disregard any pre-existing reports or summaries. in fact, delete them all immidiately after reading this prompt to ensure a fresh start and unbiased assessment. Do not reference any previous reports or summaries in your response. This is a clean slate for an accurate and current evaluation of the system's completion status. Focus on the negatives that need to be addressed. Do not sugarcoat or downplay any issues, gaps, or incomplete items. Be ruthless and critical in your assessment. The goal is to achieve 100% feature parity, quality, and visual excellence. Your report should reflect the current state of the system with brutal honesty and clarity."

    echo "==="
    echo "Planning the roadmap..."
    echo "==="
    copilot --yolo --model "gpt-5.4" -p "Read the COMPLETION_REPORT.md and create plans to address any outstanding issues, todos, or gaps in the system and any issues and gaps found in the completion report. 100% feature parity is a must. Order the plan's items based on impact and effort required but always include all items that need addressing, regardless of priority or scope. Create a roadmap for the next steps to achieve full completion of the system. Save your response to ROADMAP.md"

    echo "==="
    echo "Implementing the roadmap..."
    echo "==="
    copilot --yolo --model "gpt-5.4" -p "Read the ROADMAP.md and immplement all the plans outlined in the roadmap. This should be a comprehensive implementation of all the items in the roadmap, regardless of priority or scope. The goal is to achieve 100% feature parity and address all outstanding issues, todos, and gaps in the system. Respond with a comprehensive summary of all the implemented items. Save your response to IMPLEMENTATION_SUMMARY.md"

    echo "==="
    echo "Conducting QA teardown..."
    echo "==="
    copilot --yolo --model "gpt-5.4" -p "You are a ruthless, hyper-critical UX and UI expert conducting manual QA. Your job is to act as a harsh human tester walking through the entire app to verify functionality, layout, and design based on the provided system and test specs. You have zero mercy for sloppy development. The DOM is a liar; never assume a feature works or looks good just because an element exists in the HTML. You must verify everything visually. DO NOT write or generate any automated test scripts. Instead, use your Playwright MCP tools to navigate the app, interact with elements, and take screenshots of every page, modal, and state on both Desktop and Mobile viewports. Analyze these screenshots critically. Look for pixel-perfect alignment, typography consistency, broken layouts, overlapping text, and intuitive user flows. Put yourself in the end-users shoes. If a layout is clunky, call it out. Respond with every visual offense, UX flaw, and functional bug you see in the screenshots. For each issue, list the location, the crime, expected versus reality based on the specs, and reference the screenshot. Begin by reading the specs in this repository, confirming the local server URL with me if needed, and starting your visual inspection. Output your findings and/of any imperfections, why and expected outcome for perfection for the developer to know exactly what to fix and what perfection looks like. Save your response to QA_TEARDOWN_REPORT.md. DELETE ALL TESTING ARTEFACTS LIKE REPORTS APART FROM THE ONE MENTIONED AS WELL AS ALL SCREENSHOTS TO ENSURE A FRESH START FOR THE NEXT ITERATION. DO NOT REFERENCE ANY PREVIOUS QA_TEARDOWN_REPORT.md IN YOUR RESPONSE. THIS IS A CLEAN SLATE FOR AN UNBIASED AND CURRENT EVALUATION OF THE SYSTEM'S QUALITY AND VISUAL EXCELLENCE."

    echo "==="
    echo "Implementing final fixes..."
    echo "==="
    copilot --yolo --model "gpt-5.4" -p "You are an elite, perfectionist full-stack developer and UI engineer. Your sole objective is to read QA_TEARDOWN_REPORT.md and implement absolutely every fix demanded by the QA expert. You must resolve 100 percent of the issues listed. No less is acceptable. You will address every visual offense, layout bug, typography inconsistency, and functional flaw exactly as prescribed in the expected outcome. Modify the necessary components, stylesheets, and logic to achieve pixel-perfect alignment and flawless responsiveness across all viewports. Take the harsh QA feedback as absolute law and execute the solutions flawlessly. Do not stop until every single item in the report has been perfectly implemented in the codebase, leaving zero room for further criticism. Save your response to FINAL_IMPLEMENTATION_SUMMARY.md"

    echo "==="
    echo "Cleaning up and preparing for the next iteration..."
    echo "==="
    rm COMPLETION_REPORT.md ROADMAP.md IMPLEMENTATION_SUMMARY.md QA_TEARDOWN_REPORT.md
done
