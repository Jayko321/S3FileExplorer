# S3 File Explorer

A cross-platform desktop application for browsing and managing MinIO object storage. Built as a learning project — not intended for production use.

## Architecture

The app is split into three projects:

```
┌─────────────┐     HTTP/REST     ┌──────────────┐     S3 API     ┌───────────┐
│ S3FE.Client │ ────────────────> │ S3FE.Server  │ ────────────> │  MinIO /  │
│ (Avalonia   │  (Bearer Auth)    │ (ASP.NET Core│               │ AWS S3    │
│  Desktop)   │ <──────────────── │  REST API)   │ <──────────── │           │
└─────────────┘                   └──────────────┘               └───────────┘
                                          ↕
                                  ┌──────────────┐
                                  │ S3FE.Shared   │
                                  │ (DTOs /       │
                                  │  netstandard) │
                                  └──────────────┘
```

- **S3FE.Server** — ASP.NET Core REST API (.NET 9) that proxies requests to MinIO. Handles auth, bucket CRUD, object listing, upload/download, copy, rename, and delete.
- **S3FE.Client** — Avalonia desktop UI (.NET 10) using MVVM (CommunityToolkit.Mvvm) and compiled bindings. Connects to the server API with a bearer token.
- **S3FE.Shared** — Shared DTOs in a netstandard2.0 library with zero external dependencies.

## Features

- Connect to any MinIO endpoint with custom credentials
- List, create, and delete buckets
- Browse objects with folder-like navigation
- Upload (up to 5 GB per file), download, copy, rename, and delete objects
- Object versioning support (latest / all versions)
- Cross-platform (Windows, macOS, Linux)

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (server)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (client)
- A running [MinIO](https://github.com/minio/minio) instance

## Quick Start

### 1. Start MinIO

```bash
minio server {path/to/data}
```

### 2. Run the Server

```bash
dotnet run --project S3FE.Server
```

The API starts on `http://localhost:12000`.

### 3. Run the Client

```bash
dotnet run --project S3FE.Client
```

## API Endpoints

| Method | Route                                              | Auth | Description                                            |
| ------ | -------------------------------------------------- | ---- | ------------------------------------------------------ |
| POST   | `/api/auth/connect`                                | No   | Validate MinIO credentials, returns bearer token       |
| GET    | `/api/buckets`                                     | Yes  | List all buckets                                       |
| PUT    | `/api/buckets/{name}`                              | Yes  | Create a bucket                                        |
| DELETE | `/api/buckets/{name}`                              | Yes  | Delete a bucket                                        |
| GET    | `/api/buckets/{name}/objects`                      | Yes  | List objects (with optional `?versioning=latest\|all`) |
| POST   | `/api/buckets/{name}/objects`                      | Yes  | Upload an object (multipart form, 5 GB limit)          |
| DELETE | `/api/buckets/{name}/objects/{**key}`              | Yes  | Delete an object or version                            |
| GET    | `/api/buckets/{name}/objects/download/{**key}`     | Yes  | Download an object                                     |
| POST   | `/api/buckets/{name}/objects/copy/{**sourceKey}`   | Yes  | Copy an object                                         |
| POST   | `/api/buckets/{name}/objects/rename/{**sourceKey}` | Yes  | Rename (copy + delete)                                 |

## Screenshots

_Screenshots go here. Consider adding them to a `screenshots/` directory in the repo, or upload to the project's [GitHub releases page](https://github.com/jayko/S3FileExplorer/releases) and reference them with full URLs._

## Tech Stack

| Layer           | Technology                  |
| --------------- | --------------------------- |
| Desktop UI      | Avalonia 12.0.1             |
| MVVM Toolkit    | CommunityToolkit.Mvvm 8.4.1 |
| Backend API     | ASP.NET Core 9.0            |
| S3 SDK          | AWSSDK.S3 4.0.22.1          |
| Shared DTOs     | netstandard2.0              |
| Solution format | `.slnx`                     |

## Notes

- This is a **learning project** — not hardened for production use.
- The server stores S3 sessions in memory; restarting the server invalidates all sessions.
- MinIO requires `ForcePathStyle = true` (configured in the server).
