## Secure File Storage and Sharing Service with MVC Core 9 (.NET)
This project is a robust backend service for secure file storage and sharing, inspired by platforms like Dropbox and Google Drive. It enables users to upload files, organize them into folders, share content with others, and manage access permissions.
Built with **Domain-Driven Design (DDD)** principles, the service leverages **.NET Core 9** and integrates with **SQL Server** or **Azure SQL** Database for metadata storage, while file content is stored in **Azure Blob Storage**.

### Core Functionality
- **File Uploads**: Supports various file types, including documents, images, and videos.
- **Folder Management**: This organizes files in a hierarchical folder structure for intuitive navigation.
- **Sharing Options**: Allows the sharing of files or folders via unique links or email invitations.
- **Access Control**: Provides granular permission levels (e.g., view-only, edit, comment) for shared content.
- **Version Control**: Includes basic versioning to view and restore previous file versions.
### API
- **RESTful Design**: Offers a comprehensive API for file management, sharing, and access control.
- **Documentation**: Fully documented with **Swagger** for ease of use and integration.
- **Security**: It implements robust endpoint security to prevent unauthorized access.
### Backend
- **Technology**: Developed using **.NET/C#** with MVC Core 9 for a scalable and maintainable architecture.
- **Features**: Handles file uploads, storage, sharing logic, and permission enforcement.
- **Database Integration**: Seamlessly connects to **SQL Server** or **Azure SQL Database** for metadata and user data management.
### Data Storage
- **Metadata**: Stores file details (e.g., name, type, size, owner, permissions) and user information  **SQL Server** or **Azure SQL Database**.
- **File Content**: Utilizes **Azure Blob Storage** for efficient and scalable file storage.
### Deployment and CI/CD
- **GitHub Actions Workflow**: Automates the build, test, and deployment pipeline, seamlessly deploying the application to **Azure App Service**.
- **Database Deployment**: Includes the database schema in the repository, automatically deployed during the CI/CD process to **SQL Server** or **Azure SQL Database**.
- **Configuration**: Fully configured for Azure, with secure management of connection strings, storage settings, and environment-specific variables.

### Testing
- **Unit Tests**: Comprehensive tests verify core backend functionality, including file uploads, sharing logic, and permission enforcement.
- **Integration Tests**: Validates seamless interaction between the backend, **SQL Server/Azure SQL Database**, and **Azure Blob Storage**.
### Why This Version is Better:
**1.Clarity**: Simplified language (e.g., "robust backend service" instead of "I developed a backend service") to focus on the project rather than the developer.

**2.Professional Tone**: Adjusted phrasing for a polished, technical audience (e.g., "leverages .NET Core 9" instead of "is developed using").

**3.Conciseness**: Removed redundant details while retaining key information.

**4.Formatting**: Added bolding for emphasis (e.g., **Core Functionality**) and consistent bullet styling for readability.

**5.Flow**: Reorganized sentences to improve logical progression and highlight the DDD architecture upfront.
