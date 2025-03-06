# AdoptAPet API - Technical Interview

This project is a starting point for a technical interview focused on building a pet adoption center API using ASP.NET Core.

## Project Synopsis

### Overview

The AdoptAPet API is a RESTful web service that manages a pet adoption center. It allows users to browse available pets, submit adoption applications, and manage shelters. The API is built using ASP.NET Core 8.0 with Entity Framework Core for data access and JWT authentication for security.

### Core Functionality

1. **Pet Management**: Browse, search, create, update, and delete pets
2. **Shelter Management**: Register and manage pet shelters
3. **Adoption Applications**: Submit and process adoption applications
4. **User Authentication**: Register, login, and manage user accounts with different roles

### Entity Relationships

The system consists of four main entities with the following relationships:

#### Pets
- Central entity representing animals available for adoption
- Each pet belongs to a single shelter (many-to-one relationship)
- Pets can have multiple adoption applications (one-to-many relationship)
- Pets have properties like name, type (dog, cat, etc.), breed, age, and adoption status

#### Shelters
- Organizations that house and care for pets
- Each shelter can have multiple pets (one-to-many relationship)
- Shelters have properties like name, address, contact information

#### Adoption Applications
- Requests from users to adopt specific pets
- Each application is for a single pet (many-to-one relationship)
- Applications include applicant information and status (pending, approved, rejected)

#### Users
- People interacting with the system
- Users have different roles (Admin, ShelterStaff, Adopter)
- ShelterStaff users are associated with a specific shelter (many-to-one relationship)

![DB Schema](<DB Schema.png>)

### Authentication & Authorization

- JWT-based authentication system
- Three user roles with different permissions:
  - **Admin**: Full access to all resources
  - **ShelterStaff**: Can manage pets and applications for their shelter
  - **Adopter**: Can browse pets and submit applications

### API Structure

- **Controllers**: Handle HTTP requests and responses
- **Services**: Implement business logic and data operations
- **DTOs**: Transfer data between the API and clients
- **Models**: Define the domain entities and relationships
- **Data**: Configure the database context and entity relationships

### Technical Details

- ASP.NET Core 8.0 Web API
- Entity Framework Core with in-memory database (for simplicity)
- JWT authentication with role-based authorization
- Swagger UI for API documentation and testing

## Getting Started

1. Clone this repository
2. Open the solution in Visual Studio or your preferred IDE
3. Run the application (it uses an in-memory database for simplicity)
4. Explore the API using Swagger at https://localhost:7001/swagger

## Project Structure

- **Models**: Domain entities (Pet, Shelter, AdoptionApplication, User)
- **DTOs**: Data Transfer Objects for API requests and responses
- **Data**: Database context and configuration
- **Services**: Business logic implementation
- **Controllers**: API endpoints
- **Interfaces**: Service contracts

### Entity Relationships

- **Shelter to Pet**: One-to-Many (A shelter can have many pets)
- **Pet to AdoptionApplication**: One-to-Many (A pet can have many adoption applications)
- **User to Shelter**: Many-to-One (Many users can be associated with one shelter)
- **User to AdoptionAppliction**: One-to-Many (A user can have many adoption applications)

### Enumerations

- **PetType**: Dog, Cat, Bird, Rabbit, Other
- **PetStatus**: Available, Pending, Adopted
- **ApplicationStatus**: Pending, Approved, Rejected
- **UserRole**: Admin, ShelterStaff, Adopter

## Technical Interview Tasks

### Task 1: Implement Missing Service Method
- Implement the UpdateApplicationAsync method in the AdoptionApplicationService
  - Update all populated fields in the DTO
  - If the status is changed to approved, update the pet status to Adopted and reject other applications for this pet
  - If the status is changed to rejected and there are no more pending applications for this pet, change pet status back to Available

### Task 2: Create Missing Controller Endpoint
- Implement the **GetAll** method on AdoptionApplicationController 
- Returns all applications (with role-based filtering)
- Implement proper authorization:
  - Admin can view all applications
  - ShelterStaff can only view applications for their shelter
  - Adopters view their own applications

### Task 3: Add Filtering and Pagination
- Modify the `GetAllPets` endpoint to include filtering by pet type and status in addition to the pagination parameters

### Task 4: Implement Caching
- Add caching for the `GetAllPets` and `GetPetById` endpoints to reduce traffic to the database
- Implement cache invalidation when entities are modified

### Task 5: Fix the Bug
- When updating a Pet and changing their Shelter to a new one, the data appears to be updated correctly in the database, however updated Pet that is returned still shows the name of the original Shelter.
- Fix the bug and ensure proper error handling


## Evaluation Criteria

- Code quality and organization
- Proper use of async/await patterns
- Error handling and validation
- Security considerations
- Performance optimization
- API design best practices
- Problem-solving approach

Good luck!