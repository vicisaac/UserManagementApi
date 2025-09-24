
Building a simple ASP.NET Core Web API for a user management App with the help of Copilot.

Inplementing the codes, debugging and fixing errors using Copilot.
Implement middleware for logging, authentication, and error handling and configure the middleware pipeline for optimal performance.

Breakdown:
Log all incoming requests and outgoing responses for auditing purposes. Enforce standardised error handling across all endpoints. 
Secure API endpoints using token-based authentication. Write middleware that logs: HTTP method (e.g., GET, POST). Request path. Response status code. 
Implement error-handling middleware that: Catches unhandled exceptions. Return consistent error responses in JSON format (e.g., { "error": "Internal server error." }). 
Implement authentication middleware that: Validates tokens from incoming requests. Allows access only to users with valid tokens. 
Returns a 401 Unauthorised response for invalid tokens. 
Ensuring that middleware is configured in the correct order: Error-handling middleware first. Authentication middleware next. Logging middleware last.
