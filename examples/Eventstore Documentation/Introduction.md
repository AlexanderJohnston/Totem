# EventStoreDB 22.10 — Introduction

Welcome to the EventStoreDB 22.10 documentation.

EventStoreDB is a database designed for Event Sourcing. This documentation introduces key concepts of EventStoreDB and explains its installation, configuration, and operational concerns.

EventStoreDB is available in both an Open-Source and an Enterprise version:

- **EventStoreDB v22.10 OSS** is the open-source and free-to-use edition of EventStoreDB.  
- **EventStoreDB v22.10 Enterprise** is available for customers with an EventStoreDB paid support subscription. EventStoreDB Enterprise adds enterprise-focused features such as LDAP integration, correlation event sequence visualisation, and management CLI.

> Note  
> Although version 22.10 is licensed under the Event Store License based on BSD 3-Clause, starting from version 24.10, EventStoreDB will be licensed under the Event Store License v2 (ESLv2), which is not an OSI-approved Open Source License.

## Getting started

Get started by learning more about the principles of EventStoreDB, Event Sourcing, database installation guidelines and choosing a client SDK.

## Support

### EventStoreDB community

Users of the OSS version of EventStoreDB can use the community forum for questions, discussions and getting help from community members.

### Enterprise customers

Customers with the paid support plan can open tickets using the support portal.

### Issues

Since EventStoreDB is an open-source product, most issues are tracked openly in the EventStoreDB repository on GitHub. Before opening an issue, please ensure that a similar issue hasn't been opened already and search closed issues for possible solutions or workarounds.

When opening an issue, follow the project's guidelines for bug reports and feature requests to help the team address your concerns efficiently.

## Protocols, clients, and SDKs

This guide shows how to get started with EventStoreDB by setting up an instance or cluster and configuring it.

EventStoreDB supports two primary protocols: gRPC and TCP. There is also an AtomPub-based HTTP interface; details follow.

### gRPC protocol

The gRPC protocol is based on open standards and is widely supported by many programming languages. EventStoreDB uses gRPC for internal communication between cluster nodes as well as for client-server communication.

We recommend using gRPC since it is the primary protocol for EventStoreDB moving forward. When developing software that uses EventStoreDB, we recommend using one of the official SDKs.

#### EventStoreDB supported clients (gRPC)

- Python: pyeventsourcing/esdbclient  
- Node.js (JavaScript/TypeScript): EventStore/EventStore-Client-NodeJS  
- Java: EventStore/EventStoreDB-Client-Java  
- .NET: EventStore/EventStore-Client-Dotnet  
- Go: EventStore/EventStore-Client-Go  
- Rust: EventStore/EventStoreDB-Client-Rust

Read more in the gRPC clients documentation.

#### Community developed clients (gRPC)

- Ruby: yousty/event_store_client  
- Elixir: NFIBrokerage/spear

---

### Legacy TCP protocol (support ends with 23.10 LTS)

EventStoreDB offers a low-level asynchronous TCP protocol that exchanges protobuf objects. At present this protocol has adapters for .NET and the JVM.

> Deprecation Note  
> The TCP protocol will be available only through version 23.10. Please plan to migrate your applications that use the TCP client SDK to use the gRPC SDK instead.

Find out more about configuring the TCP protocol on the TCP configuration page.

#### EventStoreDB supported clients (TCP)

- .NET Framework and .NET Core

#### Community supported clients (TCP)

Community supported clients are developed and maintained by community members, not Event Store staff. If possible, open issues and PRs in the client's GitHub repository. The following TCP clients will not be compatible with EventStore server versions after 23.10:

- Node.js: x-cubed/event-store-client  
- Node.js: nicdex/node-eventstore-client  
- Elixir: exponentially/extreme  
- Java 8: msemys/esjc  
- Maven plugin: fuinorg/event-store-maven-plugin (archived)  
- Go: jdextraze/go-gesclient  
- PHP: prooph/event-store-client  
- JVM Client: EventStore/EventStore.JVM  
- Haskell: EventStore/EventStoreDB-Client-Haskell

---

### HTTP

EventStoreDB also offers an HTTP-based interface, based specifically on the AtomPub protocol. Because it operates over HTTP this interface is less efficient than gRPC, but nearly every environment supports it.

Find out more about configuring the HTTP protocol on the HTTP configuration page.

> Deprecation Note  
> The current AtomPub-based HTTP application API is disabled by default since v20 of EventStoreDB. You can enable it by adding an option to the server configuration. Although AtomPub support is planned for removal from future server versions, the server management HTTP API will remain available.

As the AtomPub protocol doesn't change, you can refer to the v5 HTTP API documentation for details.

#### Community developed clients (HTTP)

- PHP: prooph/event-store-http-client  
- Ruby: yousty/event_store_client