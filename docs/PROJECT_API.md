# Project Management API Documentation

## Base URL
`/api/v1/projects`

## Authentication
All endpoints require authentication. Include a valid JWT token in the `Authorization` header.

## Endpoints

### 1. Create a New Project
**Endpoint:** `POST /api/v1/projects`  
**Description:** Creates a new project for the authenticated user.

**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string (optional)",
  "messageChannel": "string (optional)",
  "channelNumber": "string (optional)"
}
```

**Success Response (201 Created):**
```json
{
  "projectId": "string",
  "name": "string",
  "apiKey": "string"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `500 Internal Server Error`: Failed to create project

### 2. Get Project by ID
**Endpoint:** `GET /api/v1/projects/{id}`  
**Description:** Retrieves project details by ID.

**Parameters:**
- `id` (path, required): The ID of the project to retrieve

**Success Response (200 OK):**
```json
{
  "id": "string",
  "userId": "string",
  "name": "string",
  "description": "string",
  "messageChannel": "string",
  "channelNumber": "string",
  "isActive": true,
  "createdAt": "2025-11-01T14:46:00Z",
  "updatedAt": "2025-11-01T14:46:00Z"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to access this project
- `404 Not Found`: Project not found

### 3. Get All Projects
**Endpoint:** `GET /api/v1/projects`  
**Description:** Retrieves projects based on user role. The endpoint checks the user's role first:
- **If user has SystemAdmin role:** Returns all projects across all users
- **Otherwise:** Returns only the projects belonging to the authenticated user's user ID

**Behavior:**
- Role check is performed first to determine which projects to return
- SystemAdmin users see all projects in the system regardless of ownership
- Regular users see only their own projects

**Success Response (200 OK):**
```json
[
  {
    "id": "string",
    "userId": "string",
    "name": "string",
    "description": "string",
    "messageChannel": "string",
    "channelNumber": "string",
    "isActive": true,
    "createdAt": "2025-11-01T14:46:00Z",
    "updatedAt": "2025-11-01T14:46:00Z"
  }
]
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `500 Internal Server Error`: Failed to retrieve projects

### 4. Update Project
**Endpoint:** `PUT /api/v1/projects/{id}`  
**Description:** Updates project information (excluding API key).

**Parameters:**
- `id` (path, required): The ID of the project to update

**Request Body:**
```json
{
  "name": "string (required)",
  "description": "string (optional)",
  "messageChannel": "string (optional)",
  "channelNumber": "string (optional)"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Project updated successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid input
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to update this project
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Failed to update project

### 5. Toggle Project Active Status
**Endpoint:** `PATCH /api/v1/projects/{id}/toggle-active`  
**Description:** Toggles the active status of a project (soft delete).

**Parameters:**
- `id` (path, required): The ID of the project to toggle

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Project active status toggled successfully"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to modify this project
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Failed to toggle project status

### 6. Delete Project
**Endpoint:** `DELETE /api/v1/projects/{id}`  
**Description:** Permanently deletes a project (hard delete).

**Parameters:**
- `id` (path, required): The ID of the project to delete

**Success Response (204 No Content)**

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to delete this project
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Failed to delete project

### 7. Generate Project Key
**Endpoint:** `POST /api/v1/projects/{id}/generate-key`  
**Description:** Generates a new API key for the project.

**Parameters:**
- `id` (path, required): The ID of the project

**Success Response (200 OK):**
```json
{
  "projectId": "string",
  "apiKey": "string"
}
```

**Error Responses:**
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to modify this project
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Failed to generate project key

### 8. Update Project Key
**Endpoint:** `PUT /api/v1/projects/{id}/key`  
**Description:** Updates the project's API key with a custom value.

**Parameters:**
- `id` (path, required): The ID of the project

**Request Body:**
```json
{
  "newProjectKey": "string (required)"
}
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Project key updated successfully"
}
```

**Error Responses:**
- `400 Bad Request`: Invalid project key
- `401 Unauthorized`: Missing or invalid authentication
- `403 Forbidden`: User doesn't have permission to modify this project
- `404 Not Found`: Project not found
- `500 Internal Server Error`: Failed to update project key
