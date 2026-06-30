# Quantum Totem Agent Playbook

This is a lightweight operating brief for the custom agents working in the Quantum / Totem repo. It is not a full architecture document. Use it to route work, preserve role boundaries, and keep implementation decisions aligned with Totem's event-driven model.

## How Orchestrator Should Route Quantum Work

The Orchestrator remains the entry point. Classify the request first, then choose the lightest lifecycle path that still protects quality and traceability.

| Request type | Default route | Quantum-specific cue |
| --- | --- | --- |
| New feature / initiative | Vision -> Product -> Design -> Execution -> QA -> Governance | Use when the request changes domain behavior, user workflows, APIs, or timeline semantics. |
| Research spike | Research -> reclassify | Use when Totem behavior, Quantum domain language, WASP/Microfilm behavior, or technical feasibility is unclear. |
| Bug fix / hotfix | Product-lite -> Execution -> QA -> Governance | Use when intended behavior is clear and the fix is localized. Insert Design if timeline behavior or architecture changes. |
| Release readiness | Governance preflight -> QA -> Governance final | Use when code exists and the user wants sign-off, evidence, or launch confidence. |
| Unclear / discovery | Research and/or Vision -> reclassify | Use when goals, users, scope, or intended behavior are not yet stable. |

## Agent Ownership

| Agent | Owns | Quantum/Totem focus |
| --- | --- | --- |
| Orchestrator | Classification, lifecycle path, handoffs, loopbacks | Decide which agent owns the next move. Do not replace specialists. |
| Vision | Problem framing and goals | Clarify why the Quantum change matters and what success looks like. |
| Research | Evidence and unknowns | Study `How To\How Totem Works.md`, existing Quantum code, WASP/Microfilm behavior, and prior docs when uncertainty blocks progress. |
| Product | Scope and acceptance criteria | Translate desired behavior into testable outcomes, especially commands, reads, and visible user/API results. |
| Design | Technical approach and trade-offs | Decide where changes belong: shared area, service host, web host, or Totem infrastructure. |
| Execution | Code implementation | Implement approved scope without bypassing timeline/domain boundaries. |
| QA | Validation strategy and results | Validate command flows, query projections, topic behavior, API behavior, and regressions. |
| Governance | Traceability and gates | Confirm docs, tests, repo hygiene, risk notes, and stage evidence are sufficient. |

## Repo Areas Agents Should Know

| Area | Path | Agent interpretation |
| --- | --- | --- |
| Shared Quantum Totem Area | `Outermind\` | Core domain boundary. Events, commands, queries, topics, and `QuantumArea` live here. |
| Quantum Service | `Outermind.Service\` | Timeline host, background work, service-side dependencies, seeds, and integrations. |
| Quantum Web | `Outermind.Web\` | HTTP/API/UI layer that issues commands, reads queries, and exposes web-facing integrations. |
| Totem framework | `src\` | Shared runtime/app/timeline/web/service infrastructure. Change only when the framework itself must change. |
| Tests | `tests\` | Validation targets for framework and app behavior. |
| Concept docs | `How To\How Totem Works.md` | Source for Totem vocabulary and mental model. |

## Totem Concepts for Agent Decisions

| Concept | Agent instruction |
| --- | --- |
| Event | Treat as a past-tense fact on the timeline. If the work changes domain history, Product/Design should name the event deliberately. |
| Command | Treat as an imperative request. If Web receives user/API intent, determine whether it should issue a command. |
| Query | Treat as read state projected from events. QA should validate projection behavior and API-visible state. |
| Topic | Treat as the workflow/decision owner. Design and Execution should put decision logic here, not in controllers. |
| Area | Treat as the domain language boundary. Shared Quantum behavior belongs in `Outermind\` unless it is purely host/API infrastructure. |

## Placement Rules

| If the work adds or changes... | Route/design toward... |
| --- | --- |
| Domain fact, command, query, topic, or route | Shared area: `Outermind\` |
| Timeline hosting, background process, service dependency, seed behavior | Quantum Service: `Outermind.Service\` |
| HTTP endpoint, API response shape, browser/client integration | Quantum Web: `Outermind.Web\` |
| WASP client wiring or token-dependent behavior | Service/Web host wiring, depending on caller |
| General Totem runtime behavior | `src\`, with Design + QA + Governance rigor |
| Test coverage | Existing test projects under `tests\` |

## Standard Quantum Flow to Preserve

### Write / command flow

1. Web receives an API request.
2. Web issues a command or appends the appropriate timeline event.
3. A topic observes the event.
4. The topic updates internal state, decides, and emits follow-up events.
5. Queries observe those events and update read state.
6. Web returns command outcome or exposes the updated query state.

### Read / query flow

1. Web receives a read request.
2. Controller reads from the query server.
3. Query state is returned to the client.
4. Version/timeline position may support caching, ETags, or subscriptions.

## Current Quantum Slices

| Slice | Agent cue |
| --- | --- |
| Microfilm | Likely domain-heavy. Expect shared area logic plus Web/API and Service seed behavior. |
| WASP | External integration. Expect token-sensitive config and host-level service registration. |
| Imaging / Cards / Clients | Domain vocabulary under the shared area. Confirm existing names before adding new ones. |
| Topics / Queries | Core Totem workflow and read model surfaces. Validate with timeline-aware tests where possible. |

## Handoff Checklist for Orchestrator

When handing work to a specialist, include:

1. Work classification and selected lifecycle path.
2. Why this stage is next.
3. Relevant repo areas: `Outermind\`, `Outermind.Service\`, `Outermind.Web\`, `src\`, `tests\`, or docs.
4. Known Quantum/Totem concepts involved: command, event, query, topic, area, service host, web host.
5. Required output artifact for that stage.
6. Open questions, assumptions, risks, and loopback triggers.

## Loopback Triggers

Loop back instead of forcing progress when:

| Trigger | Loop back to |
| --- | --- |
| Problem, users, or success criteria are unclear | Vision |
| Totem behavior, domain behavior, or integration behavior is unknown | Research |
| Acceptance criteria are missing or not testable | Product |
| Timeline semantics, architecture, API contract, or integration boundaries are unclear | Design |
| QA finds implementation gaps or regressions | Execution |
| Evidence, docs, tests, or repo hygiene are insufficient | Governance or the owning prior stage |

## Agent Guardrails

- Do not put domain decisions in Web controllers when a topic should own them.
- Do not treat queries as side-effect handlers.
- Do not add new event or command names without checking existing Quantum vocabulary.
- Do not hardcode secrets or WASP tokens.
- Do not change Totem framework infrastructure for an app-level issue unless Design confirms the need.
- Keep docs proportional: lightweight notes for small fixes, full lifecycle artifacts for risky or cross-cutting work.

## Useful Commands

```powershell
dotnet build .\Totem.sln -c Release
```

```powershell
dotnet build .\Outermind.Service\Quantum.Service.csproj
dotnet build .\Outermind.Web\Quantum.Web.csproj
```
