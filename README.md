# NetForge

NetForge is an experimental C# networking framework for building custom client-server systems with compact packet-based communication, transport abstractions, and versioned protocol handling.

This project is currently in active development and should be treated as a foundation/framework rather than a finished production networking stack.

## Overview

NetForge is designed to provide a modular base for custom networking workflows where packet structure, protocol behavior, and transport logic are intentionally controlled by the developer.

The current design centers around:

- Custom packet headers and payload handling
- Packet-based client-server communication
- Transport-specific protocol implementations
- Node identity and role-aware processing
- Versioned packet library entry points
- Custom opcode generation for development convenience

The goal is to make it easier to build and evolve custom protocols without being locked into a one-size-fits-all networking model.

## Current Status

NetForge is in a developmental stage.

Parts of the architecture are already defined, but some areas are still being actively shaped, tested, or expanded. The project is public to share the direction of development and to provide a base others may learn from or build on.

At this stage, you should expect:

- Ongoing structural changes
- Incomplete protocol implementations
- Refactoring as the architecture matures
- Experimental features and internal tooling

## Project Goals

- Provide a reusable client-server networking foundation in .NET
- Support custom protocol and packet designs
- Keep node identity and packet intent explicit
- Allow protocol evolution through versioned packet libraries
- Make development and debugging easier through generated opcode maps
- Stay modular enough for future transports and protocol families

## Architecture Notes

NetForge is built around a packet-first design.

Packets are expected to carry a structured header plus payload, allowing the framework to reason about:

- source and target node identity
- protocol type
- packet flags
- encryption selection
- type scope
- command type
- message/session routing values
- payload length and integrity metadata

Direction can be interpreted from packet header values relative to the local node context, rather than requiring separate direction flags.

The framework is intended to support custom protocol families and does not assume a single fixed application domain.

## Main Components

### NetForge Networking

The main networking project contains the core packet, node, enum, and protocol abstractions used by the framework.

Examples of responsibilities include:

- packet and packet header models
- protocol interfaces and base classes
- TCP/IP and future transport implementations
- packet parsing and serialization support
- node identity and state ownership

### OpCodeBuilder

`OpCodeBuilder` is a small utility project used during development to generate opcode enum output from the project's scope and command enums.

This tool exists as a developer convenience. It helps create a readable combined opcode map for debugging, development, and experimentation without requiring those values to be maintained by hand.

The generated opcodes are primarily a development aid. The actual protocol meaning still comes from the packet header fields and processing logic.

## Design Philosophy

NetForge is being built with a few core ideas in mind:

- Keep packet structure explicit
- Keep protocol behavior modular
- Prefer developer-controlled logic over hidden magic
- Treat node identity as meaningful state
- Allow growth without forcing premature rigidity
- Make experimentation easier during early framework development

This project intentionally favors clarity and extensibility over trying to appear “finished” too early.

## Intended Use Cases

NetForge may be useful for developers interested in:

- building custom client-server applications
- experimenting with packet-based protocol design
- creating transport abstractions in .NET
- prototyping game/server communication layers
- building private protocol stacks for specialized systems

## Stability Notice

This repository is currently experimental.

APIs, folder layout, names, packet formats, and internal conventions may change as the framework evolves. If you use this project, expect breaking changes until the architecture is formally stabilized.

## Development Notes

Some tooling and helper projects exist only to support active development.

`OpCodeBuilder` is one of those tools. It is included because it helps speed up development and reduce manual maintenance while the packet and opcode system is still evolving.

## Contributing

Contributions, ideas, and feedback are welcome, but please understand that the project is still in a formative stage and some implementation choices may change as the architecture continues to mature.

## License

All rights reserved.

This project is publicly visible for portfolio reference and development tracking only. No permission is granted to use, copy, modify, distribute, sublicense, or create derivative works from this code unless and until a public license is explicitly added by the author.

Copyright (c) 2026 Kenneth Poston. All rights reserved.

This repository and its contents are provided for viewing and reference only.

No permission is granted to use, copy, modify, merge, publish, distribute, sublicense, create derivative works from, or commercially exploit this software, in whole or in part, without prior written permission from the author.

A public license may be added in the future. Until then, all rights remain reserved.

## Summary

NetForge is currently an experimental networking framework for developers who want to build custom client-server protocols, packet workflows, and transport logic in C# without being boxed into a rigid networking model.