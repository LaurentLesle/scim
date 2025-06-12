---
applyTo: '**'
---
Coding standards, domain knowledge, and preferences that AI should follow.


Always refer to the SCIM source of truth when an error is reported and before making changes to the codebase:
- SCIM protocol: https://www.rfc-editor.org/rfc/rfc7644
- SCIM Definitions, Overview, Concepts, and Requirements: https://datatracker.ietf.org/doc/html/rfc7642
- SCIM Schema: https://datatracker.ietf.org/doc/html/rfc7643


DO:
- Prefer dotnet test for unit test and integration test.
- Write all test code in C# using xUnit framework to match the project technology stack.
- Use bash scripts only for external API testing or integration scenarios.
- Ensure unit tests coverage is above 80%.
- Improve test logic when error is reported.
- When making changes to the code, always write unit tests to cover the changes.
- Always run `dotnet build` before `dotnet test` to ensure compilation success.
- Keep using port 5000 for the SCIM server and kill it before running the tests.
- Always run the tests to confirm changes are working and there is no regression.
- Store json test data in the `Tests/Resources` directory.
- Split Models and test files into smaller files. Make sure the name is descriptive if they are larger than 100 lines to maintain readability.
- Make sure json test data for PATCH operations include the initial state and the expected state after the PATCH operation.
- PATCH operations are operated in sequential order and the expected state is the final state after all PATCH operations are applied.
- SCIM schema must only expose valid attributes. For instance The attribute emails[type eq "work"].display for User is not supported by the SCIM protocol. Please refer to the SCIM RFC
- Follow SCIM RFC compliance for external references (e.g., manager references don't need to exist locally).
- Ensure PATCH operations handle multi-valued attributes (roles, emails) correctly according to SCIM specification.
- Test both valid external references and truly invalid data types (malformed JSON, wrong data types).
- Always validate Enterprise User Extension attributes comply with RFC 7643 requirements.
- Use in-memory database for unit tests and proper test isolation between test methods.
- When temp files are created, make sure you use a naming convention that excludes them  from the source control.
- When refactoring code, folder structure or test structure, ensure no code or test is lost, except for  those that are explicitly marked as deprecated or obsolete.

DONT:
- Do not store script test in the root directory, but in the `Scripts` directory.
- Move existing scripts tests to the `Scripts` directory as needed.
- Do not use any other port than 5000 for the SCIM server.
