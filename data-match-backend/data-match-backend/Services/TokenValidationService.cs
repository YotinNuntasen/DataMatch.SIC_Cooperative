using DataMatchBackend.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace DataMatchBackend.Services;

public interface IValidationService
{
    ValidationResult ValidatePersonDocument(PersonDocument person);
    ValidationResult ValidateSharePointContact(SharePointContact contact);
    ValidationResult ValidateBulkUpdate(BulkUpdateRequest request);
    bool IsValidProductGroup(string custAppDimName);

}

public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    
    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
    
    public void AddWarning(string warning)
    {
        Warnings.Add(warning);
    }
}

public class ValidationService : IValidationService
{
    private readonly ILogger<ValidationService> _logger;
    private readonly bool _enableDataValidation;
    private readonly bool _validateProductGroupFormat;
    private readonly bool _requireCustomerName;

    public ValidationService(ILogger<ValidationService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _enableDataValidation = Environment.GetEnvironmentVariable("ENABLE_DATA_VALIDATION")?.ToLower() != "false";
        _validateProductGroupFormat = Environment.GetEnvironmentVariable("VALIDATE_PRODUCT_GROUP_FORMAT")?.ToLower() != "false";
        _requireCustomerName = Environment.GetEnvironmentVariable("REQUIRE_CUSTOMER_NAME")?.ToLower() != "false";
    }

    /// <summary>
    /// Validate PersonDocument
    /// </summary>
    public ValidationResult ValidatePersonDocument(PersonDocument person)
    {
        var result = new ValidationResult();
        
        if (!_enableDataValidation)
        {
            return result;
        }

        try
        {
            // Required field validation
            if (_requireCustomerName && string.IsNullOrWhiteSpace(person.CustShortDimName))
            {
                result.AddError("Customer name is required");
            }


            // if (!string.IsNullOrWhiteSpace(person.CustAppDimName))
            // {
            //     if (_validateProductGroupFormat && !IsValidProductGroup
            //     (person.CustAppDimName))
            //     {
            //         result.AddError($"Invalid CustAppDim format: {person.CustAppDimName}");
            //     }
            // }
            // else if (!string.IsNullOrWhiteSpace(person.CustShortDimName))
            // {
            //     result.AddWarning("Customer CustAppDim is missing");
            // }


            // Data completeness validation
            var completenessScore = CalculateCompletenessScore(person);
            if (completenessScore < 50)
            {
                result.AddWarning($"Low data completeness: {completenessScore}%");
            }


            // Date validation
            if (person.Created > DateTime.UtcNow)
            {
                result.AddError("Created date cannot be in the future");
            }

            if (person.Modified > DateTime.UtcNow.AddMinutes(5)) // Allow 5-minute buffer
            {
                result.AddError("Modified date cannot be significantly in the future");
            }

            // if (person.LastContactDate.HasValue && person.LastContactDate > DateTime.UtcNow)
            // {
            //     result.AddError("Last contact date cannot be in the future");
            // }

            _logger.LogDebug("Validated PersonDocument {CustShortDimName}: {IsValid} ({ErrorCount} errors, {WarningCount} warnings)",
                person.CustShortDimName, result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating PersonDocument");
            result.AddError("Validation error occurred");
            return result;
        }
    }

    /// <summary>
    /// Validate SharePointContact
    /// </summary>
    public ValidationResult ValidateSharePointContact(SharePointContact contact)
    {
        var result = new ValidationResult();
        
        if (!_enableDataValidation)
        {
            return result;
        }

        try
        {

            // // URL validation
            // if (!string.IsNullOrWhiteSpace(contact.Website))
            // {
            //     if (!IsValidUrl(contact.Website))
            //     {
            //         result.AddWarning($"Invalid website URL: {contact.Website}");
            //     }
            // }

            _logger.LogDebug("Validated SharePointContact {Name}: {IsValid} ({ErrorCount} errors, {WarningCount} warnings)",
                contact.opportunityName, result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating SharePointContact");
            result.AddError("Validation error occurred");
            return result;
        }
    }

    /// <summary>
    /// Validate bulk update request
    /// </summary>
    public ValidationResult ValidateBulkUpdate(BulkUpdateRequest request)
    {
        var result = new ValidationResult();
        
        try
        {
            if (request.Records == null || !request.Records.Any())
            {
                result.AddError("No records provided for bulk update");
                return result;
            }

            if (request.Records.Count > 1000) // Configurable limit
            {
                result.AddError($"Too many records for bulk update: {request.Records.Count} (max: 1000)");
            }

            var recordIndex = 0;
            var duplicateKeys = new HashSet<string>();
            
            foreach (var record in request.Records)
            {
                recordIndex++;
                
                // Check for duplicate RowKeys
                if (!string.IsNullOrEmpty(record.RowKey))
                {
                    if (duplicateKeys.Contains(record.RowKey))
                    {
                        result.AddError($"Duplicate RowKey found: {record.RowKey}");
                    }
                    else
                    {
                        duplicateKeys.Add(record.RowKey);
                    }
                }

                // Validate individual record
                if (request.ValidateData)
                {
                    var recordValidation = ValidatePersonDocument(record);
                    
                    foreach (var error in recordValidation.Errors)
                    {
                        result.AddError($"Record {recordIndex}: {error}");
                    }
                    
                    foreach (var warning in recordValidation.Warnings)
                    {
                        result.AddWarning($"Record {recordIndex}: {warning}");
                    }
                }
            }

            _logger.LogInformation("Validated bulk update request: {RecordCount} records, {IsValid} ({ErrorCount} errors, {WarningCount} warnings)",
                request.Records.Count, result.IsValid, result.Errors.Count, result.Warnings.Count);

            return result;
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error validating bulk update request");
           result.AddError("Validation error occurred");
           return result;
       }
   }

   /// <summary>
   /// Validate ProductGroup format
   /// </summary>
   public bool IsValidProductGroup(string custAppDimName)
   {
       if (string.IsNullOrWhiteSpace(custAppDimName))
           return false;

       try
       {
           var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
           return emailRegex.IsMatch(custAppDimName.Trim());
       }
       catch
       {
           return false;
       }
   }

   /// <summary>
   /// Validate URL format
   /// </summary>
   private bool IsValidUrl(string url)
   {
       if (string.IsNullOrWhiteSpace(url))
           return false;

       try
       {
           return Uri.TryCreate(url, UriKind.Absolute, out var result) &&
                  (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
       }
       catch
       {
           return false;
       }
   }

   /// <summary>
   /// Calculate data completeness score
   /// </summary>
   private int CalculateCompletenessScore(PersonDocument person)
   {
       var fields = new List<string?>
       {
           person.CustShortDimName,
           person.CustAppDimName,
           person.ProdChipNameDimName,
           person.documentNo,
           person.SalespersonDimName,
        //    person.LeadChannel
       };
       
       var filledFields = fields.Count(f => !string.IsNullOrWhiteSpace(f));
       return (int)Math.Round((double)filledFields / fields.Count * 100);
   }


   /// <summary>
   /// Validate confidence level
   /// </summary>
   private bool IsValidConfidenceLevel(string confidence)
   {
       var validLevels = new[] { "high", "medium", "low", "very low" };
       return validLevels.Contains(confidence?.ToLowerInvariant());
   }

   /// <summary>
   /// Validate status
   /// </summary>
   private bool IsValidStatus(string status)
   {
       var validStatuses = new[] { "active", "inactive", "prospect", "lead", "customer" };
       return validStatuses.Contains(status?.ToLowerInvariant());
   }

   /// <summary>
   /// Validate priority
   /// </summary>
   private bool IsValidPriority(string priority)
   {
       var validPriorities = new[] { "high", "medium", "low" };
       return validPriorities.Contains(priority?.ToLowerInvariant());
   }
}