using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public record UpdateProfileRequest(
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters.")] string? FirstName, 
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters.")] string? LastName, 
    [Phone(ErrorMessage = "Invalid phone number format.")] [StringLength(20)] string? PhoneNumber);
