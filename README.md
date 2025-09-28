# Paperless Document Management System

A comprehensive document management system built with .NET 9, featuring OCR processing, AI-powered summarization, full-text search, and a modern web interface. The system is designed with a microservices architecture using Docker containers and message queues for scalable document processing.

## 🏗️ Architecture Overview

The system follows a clean architecture pattern with the following layers:

- **Core**: Domain models and business logic interfaces
- **DAL**: Data Access Layer with Entity Framework Core and PostgreSQL
- **BL**: Business Logic Layer with services and AutoMapper
- **API**: RESTful Web API with ASP.NET Core
- **UI**: Blazor Server web application
- **Workers**: Background services for document processing

## 🚀 Key Features

- **Document Management**: Upload, store, and organize documents with metadata
- **OCR Processing**: Automatic text extraction from images and PDFs
- **AI Summarization**: Generate intelligent summaries using GenAI
- **Full-Text Search**: Elasticsearch-powered search across document content
- **Tag System**: Flexible tagging for document categorization
- **Access Logging**: Track document access patterns and usage statistics
- **Batch Processing**: Handle large document collections efficiently
- **Modern UI**: Responsive Blazor Server interface

## 🛠️ Technology Stack

### Backend
- **.NET 9** - Latest .NET framework
- **ASP.NET Core** - Web API and Blazor Server
- **Entity Framework Core 9.0.6** - ORM with PostgreSQL
- **AutoMapper 15.0.0** - Object-to-object mapping
- **PostgreSQL 15** - Primary database
- **RabbitMQ** - Message queuing for worker communication
- **Elasticsearch 8.15.0** - Full-text search engine
- **MinIO** - S3-compatible object storage

### Frontend
- **Blazor Server** - Interactive web UI
- **Bootstrap** - CSS framework (inferred from typical Blazor setup)

### Infrastructure
- **Docker & Docker Compose** - Containerization and orchestration
- **Message Queues** - Asynchronous processing
- **Microservices Architecture** - Scalable service design

## 📁 Project Structure

```
Paperless/
├── API/                    # REST API layer
│   ├── Controllers/        # API controllers
│   ├── Program.cs         # API startup configuration
│   └── Dockerfile         # API container definition
├── BL/                     # Business Logic layer
│   ├── Services/          # Business services
│   └── Mappings/          # AutoMapper profiles
├── Core/                   # Domain layer
│   ├── Models/            # Domain models
│   ├── DTOs/              # Data Transfer Objects
│   └── Repositories/      # Repository interfaces
├── DAL/                    # Data Access layer
│   ├── Models/            # Entity models
│   ├── Repositories/      # Repository implementations
│   ├── Migrations/        # EF Core migrations
│   └── PaperlessDBContext.cs
├── UI/                     # Blazor Server application
│   ├── Components/        # Blazor components
│   ├── wwwroot/          # Static web assets
│   └── Program.cs        # UI startup configuration
├── Workers/               # Background services
│   ├── OcrWorker/        # OCR processing service
│   ├── GenAIWorker/      # AI summarization service
│   ├── IndexingWorker/   # Elasticsearch indexing
│   └── BatchWorker/      # Batch processing service
├── Tests.Unit/           # Unit tests
└── compose.yaml          # Docker Compose configuration
```

## 🐳 Services Architecture

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

## 🚀 Quick Start

### Prerequisites
- Docker and Docker Compose
- .NET 9 SDK (for local development)

### Running with Docker Compose

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PAPERLESS_DV_LM_IR
   ```

2. **Start all services**
   ```bash
   cd Paperless
   docker-compose up -d
   ```

3. **Access the application**
   - Web UI: http://localhost:5012
   - API: http://localhost:8085
   - RabbitMQ Management: http://localhost:15672
   - MinIO Console: http://localhost:9001
   - Elasticsearch: http://localhost:9200

### Default Credentials
- **PostgreSQL**: admin/admin
- **MinIO**: admin/password123
- **RabbitMQ**: guest/guest

## 📚 API Documentation

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

## 🔧 Development

### Local Development Setup

1. **Start infrastructure services**
   ```bash
   docker-compose up postgres rabbitmq elasticsearch minio -d
   ```

2. **Run migrations**
   ```bash
   cd Paperless/API
   dotnet ef database update
   ```

3. **Start API**
   ```bash
   cd Paperless/API
   dotnet run
   ```

4. **Start UI**
   ```bash
   cd Paperless/UI
   dotnet run
   ```

### Building Individual Services

```bash
# Build API
cd Paperless/API
dotnet build

# Build UI
cd Paperless/UI
dotnet build

# Build Workers
cd Paperless/OcrWorker
dotnet build
```

## 🧪 Testing

Run unit tests:
```bash
cd Paperless/Tests.Unit
dotnet test
```

## 📊 Data Models

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

## 🔄 Workflow

1. **Document Upload**: User uploads document via UI
2. **Storage**: Document stored in MinIO object storage
3. **OCR Processing**: OCR Worker extracts text content
4. **AI Processing**: GenAI Worker generates summary
5. **Indexing**: Indexing Worker adds to Elasticsearch
6. **Search**: Users can search and retrieve documents

## 🚀 Deployment

### Production Considerations

1. **Environment Variables**: Configure production settings
2. **SSL/TLS**: Enable HTTPS for all services
3. **Database**: Use managed PostgreSQL service
4. **Storage**: Configure persistent volumes
5. **Monitoring**: Add logging and monitoring
6. **Security**: Implement authentication and authorization

### Scaling

- **Horizontal Scaling**: Add more worker instances
- **Load Balancing**: Use reverse proxy for API/UI
- **Database**: Configure read replicas
- **Caching**: Add Redis for session management

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

## 📄 License

[Add your license information here]

## 🆘 Support

For issues and questions:
- Create an issue in the repository
- Check the documentation
- Review the API endpoints

---

**Built with ❤️ using .NET 9 and modern microservices architecture**