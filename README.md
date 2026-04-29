# S3 File Explorer

This is a learning project. It is not intended for production use.

## Overview

S3 File Explorer is a desktop application for browsing and managing S3-compatible storage. It consists of three projects:

- **S3FE.Server** - ASP.NET Core REST API that communicates with the S3 storage backend
- **S3FE.Client** - Avalonia desktop UI that consumes the REST API
- **S3FE.Shared** - Shared models and types used by both the server and client

## Requirements

- .NET 9 (server)
- .NET 10 (client)
- MinIO running locally

## MinIO Setup

This project expects MinIO to be running locally on the default MinIO port **9000**, not the standard S3 port (443). Make sure your MinIO instance is started before running the server.

The default credentials configured in `appsettings.json` are:

- Access key: `minioadmin`
- Secret key: `minioadmin`

These can be changed in `S3FE.Server/appsettings.json` to match your local MinIO configuration.

## Running the Server

```
dotnet run --project S3FE.Server
```

The API will be available at `http://localhost:12000`.

## Running the Client

```
dotnet run --project S3FE.Client
```

Make sure the server is running before starting the client.
