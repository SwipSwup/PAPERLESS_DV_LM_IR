# Paperless Document Management System

A comprehensive document management system built with .NET 8, featuring OCR processing, AI-powered summarization, full-text search, and a modern web interface. The system is designed with a microservices architecture using Docker containers and message queues for scalable document processing.

## Architecture Overview

The system follows a clean architecture pattern with the following layers:

- **Core**: Domain models and business logic interfaces
- **DAL**: Data Access Layer with Entity Framework Core and PostgreSQL
- **BL**: Business Logic Layer with services and AutoMapper
- **API**: RESTful Web API with ASP.NET Core
- **UI**: Blazor Server web application
- **Workers**: Background services for document processing

## Technology Stack

### Backend
- **.NET 8** - Latest .NET framework
- **ASP.NET Core** - Web API and Blazor Server
- **Entity Framework Core 9.0.6** - ORM with PostgreSQL
- **AutoMapper 15.0.0** - Object-to-object mapping
- **PostgreSQL 15** - Primary database
- **RabbitMQ** - Message queuing for worker communication
- **Elasticsearch 8.15.0** - Full-text search engine
- **MinIO** - S3-compatible object storage

### Frontend
- **Blazor Server** - Interactive web UI
- **Bootstrap** - CSS framework

### Infrastructure
- **Docker & Docker Compose** - Containerization and orchestration
- **Message Queues** - Asynchronous processing
- **Microservices Architecture** - Scalable service design

## Project Structure

```
Paperless/
├── API/                    # REST API layer
├── BL/                     # Business Logic layer
├── Core/                   # Domain layer
├── DAL/                    # Data Access layer
├── UI/                     # Blazor Server application
├── Workers/               # Background services
│   ├── OcrWorker/        # OCR processing service
│   ├── GenAIWorker/      # AI summarization service
│   ├── IndexingWorker/   # Elasticsearch indexing
│   └── BatchWorker/      # Batch processing service
├── Tests.Unit/           # Unit tests
└── compose.yaml          # Docker Compose configuration
```

## Services Architecture

The system consists of the following services:

### Core Services
- **API** (Port 8085/8086) - REST API for document operations
- **UI** (Port 5012/7195) - Web interface for users

### Worker Services
- **OCR Worker** - Processes documents for text extraction
- **GenAI Worker** - Generates AI-powered document summaries
- **Indexing Worker** - Indexes documents in Elasticsearch
- **Batch Worker** - Handles bulk document operations

### Infrastructure Services
- **PostgreSQL** (Port 5432) - Primary database
- **RabbitMQ** (Port 5672/15672) - Message broker
- **Elasticsearch** (Port 9200) - Search engine
- **MinIO** (Port 9000/9001) - Object storage

## Configuration

### Environment Variables
The application requires a `.env` file in the `Paperless/` root directory to configure the GenAI service.

1. Create a file named `.env` in the `Paperless/` directory.
2. Add your Google Gemini API key:
   ```
   GENAI_API_KEY=your_actual_api_key_here
   ```

## Getting Started

### Prerequisites
- Docker and Docker Compose installed
- Git installed
- Valid Gemini API Key

### Installation and Run
1. Clone the repository.
2. Ensure the `.env` file is created as described in the Configuration section.
3. Navigate to the `Paperless` directory.
4. Build and start the services using Docker Compose:
   ```bash
   docker-compose up --build -d
   ```

## Integration Tests
To verify the Batch Processing feature, we have provided an automated PowerShell script.

### Prerequisites
- Docker services must be running (`docker-compose up`).
- At least one document must exist in the database.

### How to Run
1. Open PowerShell.
2. Navigate to `Paperless/tests`.
3. Run the script:
   ```powershell
   ./integration_test.ps1
   ```

The script will:
- Check current access count of a document.
- Generate a test XML in `batch_input`.
- Wait for processing.
- Verify the access count increased.

## Access the Application

- **Web UI**: http://localhost:5012
- **API**: http://localhost:8085
- **RabbitMQ Management**: http://localhost:15672
- **MinIO Console**: http://localhost:9001
- **Elasticsearch**: http://localhost:9200

### Default Credentials
- **PostgreSQL**: admin/admin
- **MinIO**: admin/password123
- **RabbitMQ**: admin/admin

## API Documentation

### Document Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/document` | Get all documents |
| GET | `/api/document/{id}` | Get document by ID |
| POST | `/api/document` | Create new document |
| PUT | `/api/document/{id}` | Update document |
| DELETE | `/api/document/{id}` | Delete document |
| GET | `/api/document/search?keyword={term}` | Search documents |

### Tag Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tag` | Get all tags |
| POST | `/api/tag` | Create new tag |
| PUT | `/api/tag/{id}` | Update tag |
| DELETE | `/api/tag/{id}` | Delete tag |

### Access Log Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/accesslog` | Get access logs |
| GET | `/api/accesslog/{id}` | Get access log by ID |

## Data Models

### Document
- **Id**: Unique identifier
- **FileName**: Original file name
- **FilePath**: Storage path
- **OcrText**: Extracted text content
- **Summary**: AI-generated summary
- **UploadedAt**: Upload timestamp
- **Tags**: Associated tags
- **Logs**: Document activity logs
- **AccessLogs**: Access tracking

### Tag
- **Id**: Unique identifier
- **Name**: Tag name
- **Documents**: Associated documents

### AccessLog
- **Id**: Unique identifier
- **DocumentId**: Reference to document
- **Date**: Access date
- **Count**: Number of accesses
