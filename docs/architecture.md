# Architecture Overview

StoryTime uses a split architecture:
- **Backend**: ASP.NET Core minimal API for generation orchestration, subscription limits, and library metadata.
- **Frontend**: React + Vite PWA shell focused on Quick Generate and shelf browsing.
- **Storage model**: story artifacts remain client-side; backend stores only non-PII metadata in memory.
