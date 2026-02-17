---
name: repository-structure
description: A skill for understanding and adhering to the repository structure. This includes knowledge of where different types of files are located, how the codebase is organized, and best practices for maintaining a clean and efficient repository structure. The agent should be able to navigate the repository, understand the purpose of different directories and files, and provide suggestions for improving the structure if necessary.
---

<skill>
  <name>repository-structure</name>
  <description>
  A skill for understanding and adhering to the repository structure. This includes knowledge of where different types of files are located, how the codebase is organized, and best practices for maintaining a clean and efficient repository structure. The agent should be able to navigate the repository, understand the purpose of different directories and files, and provide suggestions for improving the structure if necessary.
  </description>
  <responsibilities>
    <responsibility>Understand the current repository structure and organization.</responsibility>
    <responsibility>Navigate the repository effectively to find relevant files and information.</responsibility>
    <responsibility>Adhere to best practices for maintaining a clean and efficient repository structure.</responsibility>
    <responsibility>Provide suggestions for improving the repository structure if necessary.</responsibility>
  </responsibilities>
  <hints>
    <hint>Familiarize yourself with common directory structures used in software projects (e.g., src/, tests/, docs/).</hint>
    <hint>Use the search tool to find specific files or information within the repository.</hint>
    <hint>Use the read tool to understand the contents of specific files or sections of code.</hint>
    <hint>Use the web tool for any external information needed to understand best practices for repository structure.</hint>
  </hints>
  <repository-structure>
    <file name=".env">
      <description>Contains environment variables for local development and testing.</description>
    </file>
    <directory name="src">
      <description>Contains the source code of the project and linting systems for code quality. Especially for AI contribution.</description>
      <children>
        <directory name="frontend">
          <description>Contains the Flutter frontend code for web, Android, and iOS platforms.</description>
          <children>
            <directory name="lib">
              <description>Flutter source code organized by architectural layers (MVVM with Repository Pattern).</description>
              <children>
                <file name="main.dart">
                  <description>Application entry point and initialization.</description>
                </file>
                <directory name="presentation">
                  <description>UI layer: widgets, screens, and view-specific logic using MVVM pattern.</description>
                </directory>
                <directory name="domain">
                  <description>Business logic layer: models, use cases, and business rules.</description>
                </directory>
                <directory name="data">
                  <description>Data layer: repositories, data sources (API clients, local storage), and data models.</description>
                </directory>
                <directory name="core">
                  <description>Shared utilities: themes, constants, helpers, and cross-cutting concerns.</description>
                </directory>
              </children>
            </directory>
            <directory name="test">
              <description>Flutter test files organized by test type.</description>
              <children>
                <directory name="unit">
                  <description>Unit tests for business logic and pure Dart functions.</description>
                </directory>
                <directory name="widget">
                  <description>Widget tests for UI components and screens.</description>
                </directory>
                <directory name="integration">
                  <description>Integration tests for end-to-end user flows across platforms.</description>
                </directory>
                <directory name="golden">
                  <description>Golden file tests for visual regression testing.</description>
                </directory>
              </children>
            </directory>
            <file name="pubspec.yaml">
              <description>Flutter project dependencies and configuration.</description>
            </file>
            <file name="analysis_options.yaml">
              <description>Dart analyzer and linter configuration.</description>
            </file>
          </children>
        </directory>
        <directory name="backend">
          <description>Contains the backend code of the project, including server-side logic and API endpoints.</description>
          <children>
            <directory name="src">
              <description>Backend source code organized by architectural layers.</description>
              <children>
                <directory name="managers">
                  <description>Orchestration and transformation layers for backend operations.</description>
                </directory>
                <directory name="engines">
                  <description>Business logic and core processing for backend functionality.</description>
                </directory>
                <directory name="data-access">
                  <description>Database access, repositories, and data persistence logic.</description>
                </directory>
              </children>
            </directory>
            <directory name="tests">
              <description>Backend test cases and testing-related files.</description>
            </directory>
          </children>
        </directory>
      </children>
    </directory>
    <directory name="docs">
      <description>Contains documentation for the project, including design docs and user guides.</description>
      <children>
        <file name="frontend.md">
          <description>Design language specifications, user flows, and Flutter frontend design documentation including Material/Cupertino components, widget patterns, adaptive design for web/mobile, theming with design tokens, and interaction patterns.</description>
        </file>
        <file name="use-cases.md">
          <description>Detailed use cases derived from requirements. Defines exact scenarios and behaviors that regression tests must cover to ensure all functional requirements are met.</description>
        </file>
        <file name="database.md">
          <description>Database schema design documentation including table structures, relationships, indexes, constraints, and data models.</description>
        </file>
        <file name="classes.md">
          <description>Class diagrams showing object-oriented design, class relationships, inheritance hierarchies, and system structure.</description>
        </file>
        <file name="sequence.md">
          <description>Sequence diagrams illustrating interactions between components, services, and systems over time for key workflows and processes.</description>
        </file>
      </children>
    </directory>
    <file name="docker-compose.yml">
      <description>Defines the services, networks, and volumes for Docker-based development and deployment.</description>
    </file>
    <file name="docker-compose.prod.yml">
      <description>Defines the services, networks, and volumes for Docker-based production deployment.</description>
    </file>
    <file name="README.md">
      <description>Provides an overview of the project, including setup instructions and usage guidelines.</description>
    </file>
    <file name="LICENSE">
      <description>Contains the license information for the project.</description>
    </file>
</skill>
