using DataMatchBackend.Models;
using Microsoft.Extensions.Logging;

namespace DataMatchBackend.Services;

public interface ISimilarityService
{
    double CalculateSimilarity(SharePointContact sharePointItem, PersonDocument azureItem);
    double CalculateSimilarity(PersonDocument item1, PersonDocument item2);
    List<MatchedRecord> FindBestMatches(List<SharePointContact> sharePointItems, List<PersonDocument> azureItems, double threshold = 80);
    string GetConfidenceLevel(double similarity);
}

public class SimilarityService : ISimilarityService
{
    private readonly ILogger<SimilarityService> _logger;
    private readonly double _autoThreshold;
    private readonly double _suggestThreshold;
    private readonly int _maxSuggestions;

    public SimilarityService(ILogger<SimilarityService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        _autoThreshold = double.Parse(Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_AUTO") ?? "80");
        _suggestThreshold = double.Parse(Environment.GetEnvironmentVariable("SIMILARITY_THRESHOLD_SUGGEST") ?? "60");
        _maxSuggestions = int.Parse(Environment.GetEnvironmentVariable("MAX_SUGGESTIONS_PER_RECORD") ?? "5");
    }

    /// <summary>
    /// Calculate similarity between SharePoint and Azure Table records
    /// </summary>
    public double CalculateSimilarity(SharePointContact spContact, PersonDocument azureDoc)
{
    var score = 0.0;
    var totalFields = 0;
    
    // Customer Name matching (main field)
    if (!string.IsNullOrEmpty(spContact.CustomerName) && !string.IsNullOrEmpty(azureDoc.CustShortDimName))
    {
        score += CalculateStringSimilarity(spContact.CustomerName, azureDoc.CustShortDimName) * 0.4; // High weight
        totalFields++;
    }
    
    // Product interest matching
    if (!string.IsNullOrEmpty(spContact.ProductGroup) && !string.IsNullOrEmpty(azureDoc.CustAppDimName))
    {
        score += CalculateStringSimilarity(spContact.ProductGroup, azureDoc.CustAppDimName) * 0.2;
        totalFields++;
    }
    
    // Product matching
    if (!string.IsNullOrEmpty(spContact.ProductName) && !string.IsNullOrEmpty(azureDoc.ProdChipNameDimName))
    {
        score += CalculateStringSimilarity(spContact.SourceOfLead, azureDoc.ProdChipNameDimName) * 0.2;
        totalFields++;
    }
    
    //Sale matching
        if (!string.IsNullOrEmpty(spContact.CustomerNameSalePersonCode) && !string.IsNullOrEmpty(azureDoc.SalespersonDimName))
        {
            score += CalculateStringSimilarity(spContact.SourceOfLead, azureDoc.SalespersonDimName) * 0.2;
            totalFields++;
        }

    // //Post Date matching
    //     if (!DateTime(spContact.S9DWINEntryDate) && !DateTime(azureDoc.PostingDate))
    //     {
    //         score += CalculateStringSimilarity(spContact.SourceOfLead, azureDoc.SalespersonDimName) * 0.1;
    //         totalFields++;
    //     }
    
    return totalFields > 0 ? (score / totalFields) * 100 : 0;
}

    /// <summary>
    /// Calculate similarity between two PersonDocument records
    /// </summary>
    public double CalculateSimilarity(PersonDocument item1, PersonDocument item2)
    {
        try
        {
            double totalScore = 0;
            double totalWeight = 0;

            // Name similarity (40% weight)
            var nameScore = CalculateNameSimilarity(item1.CustShortDimName, item2.CustShortDimName);
            if (nameScore > 0)
            {
                totalScore += nameScore * 40;
                totalWeight += 40;
            }

            // Product Group similarity (30% weight)
            var ProductScore = CalculateEmailSimilarity(item1.CustAppDimName, item2.CustAppDimName);
            if (ProductScore > 0)
            {
                totalScore += ProductScore * 30;
                totalWeight += 30;
            }

            // ProductName similarity (20% weight)
            var ProdChipNameDimNameScore = CalculateStringSimilarity(item1.ProdChipNameDimName, item2.ProdChipNameDimName);
            if (ProdChipNameDimNameScore > 0)
            {
                totalScore += ProdChipNameDimNameScore * 20;
                totalWeight += 20;
            }

            //Sale similarity (10% weight)
            var SaleScore = CalculateExactMatch(item1.SalespersonDimName, item2.SalespersonDimName);
            if (SaleScore> 0)
            {
                totalScore += SaleScore * 10;
                totalWeight += 10;
            }

            // //PostDate similarity (10% weight)
            // var PostDateScore = CalculateExactMatch(item1.PostingDate, item2.PostingDate);
            // if (PostDateScore > 0)
            // {
            //     totalScore += PostDateScore * 10;
            //     totalWeight += 10;
            // }

            var finalScore = totalWeight > 0 ? (totalScore / totalWeight) : 0;
            return Math.Max(0, Math.Min(100, finalScore));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating similarity between PersonDocuments");
            return 0;
        }
    }

    /// <summary>
    /// Find best matches above threshold
    /// </summary>
    public List<MatchedRecord> FindBestMatches(List<SharePointContact> sharePointItems, List<PersonDocument> azureItems, double threshold = 80)
{
    try
    {
        _logger.LogInformation("Finding best matches with threshold {Threshold}%", threshold);
        
        var bestMatches = new List<MatchedRecord>();
        var usedAzureItems = new HashSet<string>();

        foreach (var spItem in sharePointItems)
        {
            var candidates = new List<(PersonDocument azureItem, double similarity)>();

            foreach (var azureItem in azureItems)
            {
                if (usedAzureItems.Contains(azureItem.RowKey))
                    continue;

                // azureItem เป็น PersonDocument อยู่แล้ว ไม่ต้องแปลง
                var similarity = CalculateSimilarity(spItem, azureItem);
                
                if (similarity >= threshold)
                {
                    candidates.Add((azureItem, similarity));
                }
            }

            var bestCandidate = candidates
                .OrderByDescending(c => c.similarity)
                .FirstOrDefault();

            if (bestCandidate.azureItem != null)
            {
                var matchedRecord = new MatchedRecord
                {
                    SharePointData = spItem,
                    AzureData = bestCandidate.azureItem.ToCustomerDataEntity(), // แปลงให้ตรงกับ AzureData type
                    SimilarityScore = bestCandidate.similarity,
                    MatchType = "auto",
                    Confidence = GetConfidenceLevel(bestCandidate.similarity),
                    MatchedBy = "SimilarityEngine",
                    MatchDetails = new Dictionary<string, object>
                    {
                        { "Threshold", threshold },
                        { "Algorithm", "Weighted" },
                        { "CandidatesConsidered", candidates.Count }
                    }
                };

                bestMatches.Add(matchedRecord);
                usedAzureItems.Add(bestCandidate.azureItem.RowKey);

                _logger.LogDebug("Auto-matched: {SharePointName} → {AzureName} ({Similarity}%)", 
                    spItem.opportunityName, bestCandidate.azureItem.CustShortDimName, Math.Round(bestCandidate.similarity));
            }
        }

        _logger.LogInformation("Found {MatchCount} automatic matches above {Threshold}% threshold", 
            bestMatches.Count, threshold);
        
        return bestMatches;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error finding best matches");
        return new List<MatchedRecord>();
    }
}

    /// <summary>
    /// Get confidence level based on similarity score
    /// </summary>
    public string GetConfidenceLevel(double similarity)
    {
        return similarity switch
        {
            >= 90 => "High",
            >= 80 => "Medium",
            >= 60 => "Low",
            _ => "Very Low"
        };
    }

    // Private helper methods
    private double CalculateNameSimilarity(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2)) return 0;
        
        var cleanName1 = CleanString(name1);
        var cleanName2 = CleanString(name2);
        
        if (cleanName1 == cleanName2) return 100;
        
        // Check if one name contains the other
        if (cleanName1.Contains(cleanName2) || cleanName2.Contains(cleanName1))
        {
            return 85;
        }
        
        // Levenshtein distance similarity
        var levenshteinScore = CalculateLevenshteinSimilarity(cleanName1, cleanName2);
        
        // Word-based similarity
        var wordScore = CalculateWordSimilarity(cleanName1, cleanName2);
        
        return Math.Max(levenshteinScore, wordScore);
    }

    private double CalculateEmailSimilarity(string email1, string email2)
    {
        if (string.IsNullOrWhiteSpace(email1) || string.IsNullOrWhiteSpace(email2)) return 0;
        
        var cleanEmail1 = email1.ToLowerInvariant().Trim();
        var cleanEmail2 = email2.ToLowerInvariant().Trim();
        
        if (cleanEmail1 == cleanEmail2) return 100;
        
        // Extract username parts (before @)
        var username1 = cleanEmail1.Split('@')[0];
        var username2 = cleanEmail2.Split('@')[0];
        
        if (username1 == username2) return 90;
        
        // Extract domain parts (after @)
        var domain1 = cleanEmail1.Contains('@') ? cleanEmail1.Split('@')[1] : "";
        var domain2 = cleanEmail2.Contains('@') ? cleanEmail2.Split('@')[1] : "";
        
        var domainScore = domain1 == domain2 ? 50 : 0; // Same domain gives bonus
        var usernameScore = CalculateStringSimilarity(username1, username2) * 0.7;
        
        return Math.Min(100, usernameScore + domainScore);
    }

    private double CalculateStringSimilarity(string str1, string str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2)) return 0;
        
        var clean1 = CleanString(str1);
        var clean2 = CleanString(str2);
        
        if (clean1 == clean2) return 100;
        
        return CalculateLevenshteinSimilarity(clean1, clean2);
    }

    private double CalculateExactMatch(string value1, string value2)
    {
        if (string.IsNullOrWhiteSpace(value1) || string.IsNullOrWhiteSpace(value2)) return 0;
        
        var clean1 = CleanString(value1);
        var clean2 = CleanString(value2);
        
        return clean1 == clean2 ? 100 : 0;
    }

    private double CalculateLevenshteinSimilarity(string str1, string str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2)) return 0;
        if (str1 == str2) return 100;
        
        var distance = LevenshteinDistance(str1, str2);
        var maxLength = Math.Max(str1.Length, str2.Length);
        
        if (maxLength == 0) return 100;
        
        var similarity = ((maxLength - distance) / (double)maxLength) * 100;
        return Math.Max(0, similarity);
    }

    private double CalculateWordSimilarity(string str1, string str2)
    {
        if (string.IsNullOrWhiteSpace(str1) || string.IsNullOrWhiteSpace(str2)) return 0;
        
        var words1 = str1.ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        var words2 = str2.ToLowerInvariant().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (words1.Length == 0 || words2.Length == 0) return 0;
        
        var matchingWords = 0;
        var totalWords = Math.Max(words1.Length, words2.Length);
        
        foreach (var word1 in words1)
        {
            var hasMatch = words2.Any(word2 => 
                word1 == word2 || 
                word1.Contains(word2) || 
                word2.Contains(word1) ||
                CalculateLevenshteinSimilarity(word1, word2) > 80
            );
            
            if (hasMatch) matchingWords++;
        }
        
        return (matchingWords / (double)totalWords) * 100;
    }

    private int LevenshteinDistance(string str1, string str2)
    {
        var matrix = new int[str2.Length + 1, str1.Length + 1];
        
        for (int i = 0; i <= str1.Length; i++)
            matrix[0, i] = i;
        
        for (int j = 0; j <= str2.Length; j++)
            matrix[j, 0] = j;
        
        for (int j = 1; j <= str2.Length; j++)
        {
            for (int i = 1; i <= str1.Length; i++)
            {
                var cost = str1[i - 1] == str2[j - 1] ? 0 : 1;
                
                matrix[j, i] = Math.Min(
                    Math.Min(matrix[j - 1, i] + 1, matrix[j, i - 1] + 1),
                    matrix[j - 1, i - 1] + cost
                );
            }
        }
        
        return matrix[str2.Length, str1.Length];
    }

    private string CleanString(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) return "";
        
        return str
            .ToLowerInvariant()
            .Trim()
            .Replace("[^\\w\\s]", "") // Remove special characters
            .Replace("\\s+", " ");    // Normalize whitespace
    }
}