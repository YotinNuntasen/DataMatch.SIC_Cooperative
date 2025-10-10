using System.Collections;
using System.Text.Json.Serialization;

namespace DataMatchBackend.Models;

/// <summary>
/// Standard API response wrapper for all endpoints
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; } = 200;
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "Success";
    
    [JsonPropertyName("data")]
    public T? Data { get; set; }
    
    [JsonPropertyName("count")]
    public int? Count { get; set; }
    
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    [JsonPropertyName("success")]
    public bool Success { get; set; } = true;
    
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();
    
    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
    
    [JsonPropertyName("metadata")]
    public Dictionary<string, object> Metadata { get; set; } = new();

    // Helper methods for creating standard responses
    public static ApiResponse<T> Ok(T data, string message = "Success")
    {
        return new ApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data,
            Success = true,
            Count = data is ICollection collection ? collection.Count : null
        };
    }

    public static ApiResponse<T> Created(T data, string message = "Created successfully")
    {
        return new ApiResponse<T>
        {
            Code = 201,
            Message = message,
            Data = data,
            Success = true
        };
    }

    public static ApiResponse<T> BadRequest(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Code = 400,
            Message = message,
            Success = false,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> Unauthorized(string message = "Unauthorized")
    {
        return new ApiResponse<T>
        {
            Code = 401,
            Message = message,
            Success = false
        };
    }

    public static ApiResponse<T> Forbidden(string message = "Forbidden")
    {
        return new ApiResponse<T>
        {
            Code = 403,
            Message = message,
            Success = false
        };
    }

    public static ApiResponse<T> NotFound(string message = "Not found")
    {
        return new ApiResponse<T>
        {
            Code = 404,
            Message = message,
            Success = false
        };
    }

    public static ApiResponse<T> InternalError(string message = "Internal server error", List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Code = 500,
            Message = message,
            Success = false,
            Errors = errors ?? new List<string>()
        };
    }

    public static ApiResponse<T> ServiceUnavailable(string message = "Service unavailable")
    {
        return new ApiResponse<T>
        {
            Code = 503,
            Message = message,
            Success = false
        };
    }

    // Instance methods
    public ApiResponse<T> WithCount(int count)
    {
        Count = count;
        return this;
    }

    public ApiResponse<T> WithMetadata(string key, object value)
    {
        Metadata[key] = value;
        return this;
    }

    public ApiResponse<T> WithMetadata(Dictionary<string, object> metadata)
    {
        foreach (var item in metadata)
        {
            Metadata[item.Key] = item.Value;
        }
        return this;
    }

    public ApiResponse<T> WithWarning(string warning)
    {
        Warnings.Add(warning);
        return this;
    }

    public ApiResponse<T> WithWarnings(List<string> warnings)
    {
        Warnings.AddRange(warnings);
        return this;
    }

    public ApiResponse<T> WithError(string error)
    {
        Errors.Add(error);
        Success = false;
        return this;
    }

    public ApiResponse<T> WithErrors(List<string> errors)
    {
        Errors.AddRange(errors);
        Success = false;
        return this;
    }
}

/// <summary>
/// Non-generic API response for endpoints that don't return data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    // เพิ่ม new keywords เพื่อแก้ warnings
    public new static ApiResponse BadRequest(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Code = 400,
            Message = message,
            Success = false,
            Errors = errors ?? new List<string>()
        };
    }

    public new static ApiResponse Unauthorized(string message = "Unauthorized")
    {
        return new ApiResponse
        {
            Code = 401,
            Message = message,
            Success = false
        };
    }

    public new static ApiResponse Forbidden(string message = "Forbidden")
    {
        return new ApiResponse
        {
            Code = 403,
            Message = message,
            Success = false
        };
    }

    public new static ApiResponse NotFound(string message = "Not found")
    {
        return new ApiResponse
        {
            Code = 404,
            Message = message,
            Success = false
        };
    }

    public new static ApiResponse InternalError(string message = "Internal server error", List<string>? errors = null)
    {
        return new ApiResponse
        {
            Code = 500,
            Message = message,
            Success = false,
            Errors = errors ?? new List<string>()
        };
    }
}

/// <summary>
/// Paginated API response for list endpoints
/// </summary>
/// <typeparam name="T">Type of items in the list</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<List<T>>
{
    [JsonPropertyName("pagination")]
    public PaginationInfo Pagination { get; set; } = new();

    public static PaginatedApiResponse<T> Ok(
        List<T> data, 
        int pageNumber, 
        int pageSize, 
        int totalRecords, 
        string message = "Success")
    {
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        
        return new PaginatedApiResponse<T>
        {
            Code = 200,
            Message = message,
            Data = data,
            Count = data.Count,
            Success = true,
            Pagination = new PaginationInfo
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = totalPages,
                HasNextPage = pageNumber < totalPages,
                HasPreviousPage = pageNumber > 1
            }
        };
    }
}

/// <summary>
/// Pagination information for paginated responses
/// </summary>
public class PaginationInfo
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; } = 1;
    
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; } = 50;
    
    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; } = 0;
    
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; } = 0;
    
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; } = false;
    
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; } = false;
    
    [JsonPropertyName("firstRecordIndex")]
    public int FirstRecordIndex => (PageNumber - 1) * PageSize + 1;
    
    [JsonPropertyName("lastRecordIndex")]
    public int LastRecordIndex => Math.Min(PageNumber * PageSize, TotalRecords);
}

/// <summary>
/// Error details for validation errors
/// </summary>
public class ValidationError
{
    [JsonPropertyName("field")]
    public string Field { get; set; } = "";
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = "";
    
    [JsonPropertyName("code")]
    public string Code { get; set; } = "";
    
    [JsonPropertyName("value")]
    public object? Value { get; set; }
}

/// <summary>
/// API response with validation errors
/// </summary>
/// <typeparam name="T">Type of data</typeparam>
public class ValidationApiResponse<T> : ApiResponse<T>
{
    [JsonPropertyName("validationErrors")]
    public List<ValidationError> ValidationErrors { get; set; } = new();

    public static ValidationApiResponse<T> ValidationFailed(List<ValidationError> validationErrors, string message = "Validation failed")
    {
        return new ValidationApiResponse<T>
        {
            Code = 400,
            Message = message,
            Success = false,
            ValidationErrors = validationErrors
        };
    }

    public ValidationApiResponse<T> WithValidationError(string field, string message, string code = "", object? value = null)
    {
        ValidationErrors.Add(new ValidationError
        {
            Field = field,
            Message = message,
            Code = code,
            Value = value
        });
        return this;
    }
}