# KernelMemory.FileWatcher 
## Automated document ingestion for Semantic Kernel's KernelMemory service


## Overview

KernelMemory File Watcher is a service designed to automate the document ingestion process for Semantic Kernel's KernelMemory service. It monitors specified directories for file changes and sends these changes to the KernelMemory service for processing. This enables the automatic creation of embeddings for Retrieval Augmented Generation (RAG) whenever a file is modified. The service is designed to run on the edge or wherever your files reside, and can be deployed as a standalone service or a Docker container.

## Main Components

### MessageStore

The `MessageStore` is responsible for storing and managing file events. It implements the `IMessageStore` interface which defines methods for adding a file event, retrieving the next file event, and checking if there are any file events in the store. The `MessageStore` uses a `ConcurrentDictionary` to store file events, ensuring thread-safety.

### FileWatcherService

The `FileWatcherService` is responsible for watching specified directories for file changes. It uses the `FileSystemWatcher` class to monitor directories and raises events when files are created, deleted, or modified. These events are then added to the `MessageStore`.

### HttpWorker

The `HttpWorker` is a hosted service that periodically checks the `MessageStore` for new file events and sends them to the KernelMemory service. It uses an `HttpClient` to send HTTP requests and includes logic for handling different types of file events (e.g., upserts and deletes).

## How It Works

1. The `FileWatcherService` starts watching the specified directories for file changes.
2. When a file change is detected, a file event is created and added to the `MessageStore`.
3. The `HttpWorker` periodically checks the `MessageStore` for new file events.
4. When a new file event is found, the `HttpWorker` sends it to the KernelMemory service for processing.

## Configuration

The service's configuration is defined in the `appsettings.json` file. Here you can specify the directories to watch, the KernelMemory service's endpoint and API key, and other options.

```json
{
  "FileWatcher": {
    "Directories": [
      {
        "Path": "/tmp/folder_01",
        "Filter": "*.md", // single filter
        "Index": "folder-01",
        "IncludeSubdirectories": true
      },
      {
        "Path": "/tmp/folder_02",
        "Filters": {   // multiple filters
            "*.md",
            "*.pdf"
        },
        "Index": "folder-02",
        "IncludeSubdirectories": true
      },
      // More directories...
    ]
  },
  "KernelMemory": {
    "Endpoint": "http://127.0.0.1:9001",
    "ApiKey": "", // not required
    "Schedule":  "00:00:30"
  }
}

```

In the `FileWatcher` section, you can specify multiple directories to watch. For each directory, you can specify a path, a filter for the types of files to watch, an index, and whether to include subdirectories.

In the `KernelMemory` section, you can specify the endpoint of the KernelMemory service, your API key, and the schedule for the `HttpWorker` to check for new file events.

## Running the Service

To run the service, you can either run the `KernelMemory.FileWatcher` project directly or build and run the Docker container.

### Running as a Standalone Service

To run the service as a standalone service, you can build and run the `KernelMemory.FileWatcher` project using the following commands:

```bash
dotnet run --project KernelMemory.FileWatcher
```

### Running as a Docker Container

```sh
docker run -v /path/to/your/appsettings.json:/config/appsettings.json -v /path/to/your/documents-01:/data/documents-01 mplogas/km-filewatcher:latest
```
