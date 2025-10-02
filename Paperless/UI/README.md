# Paperless-ngx Frontend

A modern, responsive frontend for the Paperless document management system built with Blazor Server and .NET 8.0.

## Features

### ✅ Implemented
- **Dashboard**: Overview with statistics and recent documents
- **Documents Management**: Grid and list views with search, filtering, and pagination
- **Document Detail**: Individual document viewing with metadata
- **Tags Management**: Full CRUD operations for document tags
- **Search Functionality**: Real-time search across documents, summaries, and tags
- **Responsive Design**: Mobile-friendly interface
- **Dark Theme**: Modern dark theme matching Paperless-ngx design
- **Navigation**: Comprehensive sidebar navigation with all sections

### 🚧 Placeholder Pages
- Correspondents
- Document Types
- Storage Paths
- Custom Fields
- Templates
- Mail
- Settings
- Users & Groups
- File Tasks
- Logs
- Documentation

## Tech Stack

- **Framework**: Blazor Server (.NET 9.0)
- **Styling**: Custom CSS with CSS Variables
- **Icons**: Bootstrap Icons
- **HTTP Client**: Built-in HttpClient with dependency injection
- **State Management**: Component-based state management

## Project Structure

```
UI/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor          # Main application layout
│   │   └── NavMenu.razor             # Sidebar navigation
│   ├── Pages/
│   │   ├── Home.razor                # Dashboard page
│   │   ├── Documents.razor           # Documents listing page
│   │   ├── DocumentDetail.razor      # Individual document view
│   │   ├── Tags.razor                # Tags management page
│   │   └── Placeholder.razor         # Placeholder for unimplemented pages
│   ├── DocumentCard.razor            # Document card component (grid view)
│   ├── DocumentListItem.razor        # Document list item component
│   └── App.razor                     # Root application component
├── Services/
│   ├── IDocumentService.cs           # Document service interface
│   ├── DocumentService.cs            # Document service implementation
│   ├── ITagService.cs                # Tag service interface
│   ├── TagService.cs                 # Tag service implementation
│   ├── IAccessLogService.cs          # Access log service interface
│   └── AccessLogService.cs           # Access log service implementation
├── wwwroot/
│   └── app.css                       # Main stylesheet with dark theme
└── Program.cs                        # Application startup configuration
```

## API Integration

The frontend communicates with the REST API through HTTP services:

- **Base URL**: `https://localhost:7001/` (configurable in Program.cs)
- **Endpoints**:
  - `GET /api/document` - Get all documents
  - `GET /api/document/{id}` - Get document by ID
  - `POST /api/document` - Create document
  - `PUT /api/document/{id}` - Update document
  - `DELETE /api/document/{id}` - Delete document
  - `GET /api/document/search?keyword={keyword}` - Search documents
  - `GET /api/tag` - Get all tags
  - `POST /api/tag` - Create tag
  - `PUT /api/tag/{id}` - Update tag
  - `DELETE /api/tag/{id}` - Delete tag
  - `GET /api/accesslog` - Get access logs

## Key Components

### MainLayout.razor
- Application shell with header, sidebar, and main content area
- Global search functionality
- User menu placeholder

### NavMenu.razor
- Comprehensive navigation sidebar
- Organized sections: Main, Saved Views, Manage, Administration
- Active state management
- Collapsible design

### Documents.razor
- Document listing with grid and list views
- Search and filtering capabilities
- Pagination
- Real-time filtering by tags and search terms

### DocumentCard.razor & DocumentListItem.razor
- Reusable components for displaying documents
- Action buttons (view, edit, download, delete)
- Tag display with overflow handling
- Responsive design

### Tags.razor
- Full CRUD operations for tags
- Modal-based create/edit interface
- Grid layout with hover effects

## Styling

The application uses a comprehensive CSS system with:

- **CSS Variables**: For consistent theming
- **Dark Theme**: Primary color scheme matching Paperless-ngx
- **Responsive Design**: Mobile-first approach
- **Component-based Styles**: Organized by functionality
- **Hover Effects**: Interactive feedback
- **Loading States**: User experience enhancements

### Color Scheme
- Primary: `#4caf50` (Green)
- Background: `#1a1a1a` (Dark)
- Surface: `#2d2d2d` (Medium Dark)
- Secondary: `#2c2c2c` (Dark Gray)
- Text Primary: `#ffffff` (White)
- Text Secondary: `#b0b0b0` (Light Gray)

## Getting Started

1. **Prerequisites**:
   - .NET 8.0 SDK
   - Running API server on `https://localhost:7001/`

2. **Configuration**:
   - Update API base URL in `Program.cs` if needed
   - Ensure CORS is configured on the API server

3. **Run the Application**:
   ```bash
   cd Paperless/UI
   dotnet run
   ```

4. **Access the Application**:
   - Navigate to `https://localhost:7002/` (or the configured port)

## Development Notes

### Adding New Pages
1. Create a new `.razor` file in `Components/Pages/`
2. Add the `@page` directive with the route
3. Use the existing layout and styling patterns
4. Add navigation links in `NavMenu.razor`

### Adding New Services
1. Create interface in `Services/` folder
2. Implement the service class
3. Register in `Program.cs` with HttpClient
4. Inject into components as needed

### Styling Guidelines
- Use CSS variables for colors and spacing
- Follow the existing component structure
- Ensure responsive design
- Test hover states and interactions

## Future Enhancements

- [ ] Document upload functionality
- [ ] Document preview/PDF viewer
- [ ] Advanced search with filters
- [ ] Bulk operations
- [ ] User authentication
- [ ] Real-time notifications
- [ ] Document versioning
- [ ] Advanced tagging system
- [ ] Export functionality
- [ ] Theme customization

## Contributing

1. Follow the existing code structure and patterns
2. Ensure responsive design for all new components
3. Add appropriate error handling
4. Test with the API integration
5. Update documentation as needed

## License

This project is part of the Paperless document management system.
