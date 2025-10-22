/**
 * Calculate similarity between SharePoint and Azure Table records
 * Uses weighted scoring
 */
export function calculateSimilarity(sharePointItem, azureItem) {
  let totalScore = 0;
  let totalWeight = 0;


  // Name similarity (45% weight)
  const nameScore = calculateNameSimilarity(
    sharePointItem.customerName || "",
    azureItem.customerName || ""
  );
  totalScore += nameScore * 45;
  totalWeight += 45;

  // PCode similarity (30% weight)
  const PCodeScore = calculatePcodeSimilarity(
    sharePointItem.productCode || "",
    azureItem.itemReferenceNo || ""
  );

  totalScore += PCodeScore * 30;
  totalWeight += 30;

  // documentDate similarity (25% weight)
  const documentDateScore = calculateDateSimilarity(
    sharePointItem.s9DWINEntryDate || "",
    azureItem.documentDate
      ? new Date(azureItem.documentDate).toLocaleDateString("th-TH")
      : "N/A" || ""
  );
  totalScore += documentDateScore * 25;
  totalWeight += 25;

  const finalScore = totalWeight > 0 ? totalScore / totalWeight : 0;

  console.log(
    `🔍 Similarity calculated: ${sharePointItem.name} ↔ ${
      azureItem.customerName || azureItem.name
    } = ${Math.round(finalScore)}%`
  );

  return Math.max(0, Math.min(100, finalScore));
}

/**
 * Calculate name similarity with enhanced matching
 */
export function calculateNameSimilarity(name1, name2) {
  if (!name1 || !name2) return 0;

  const cleanName1 = cleanStringForNameComparison(name1);
  const cleanName2 = cleanStringForNameComparison(name2);

  if (cleanName1 === cleanName2) return 100;

  if (cleanName1.includes(cleanName2) || cleanName2.includes(cleanName1)) {
    const minLength = Math.min(cleanName1.length, cleanName2.length);
    const maxLength = Math.max(cleanName1.length, cleanName2.length);
   
    return Math.max(70, calculateLevenshteinSimilarity(cleanName1, cleanName2));
  }

  return calculateLevenshteinSimilarity(cleanName1, cleanName2);
}

/**
 * Calculate PCode similarity
 */
export function calculatePcodeSimilarity(pcode1, pcode2) {
  if (!pcode1 || !pcode2) return 0;

  const cleanPcode1 = pcode1.toLowerCase().trim();
  const cleanPcode2 = pcode2.toLowerCase().trim();
  if (cleanPcode1 === cleanPcode2) return 100;
  if (cleanPcode1.includes(cleanPcode2) || cleanPcode2.includes(cleanPcode1)) return 75;
  return calculateLevenshteinSimilarity(cleanPcode1, cleanPcode2); 
}

/**
 * Calculate string similarity using Levenshtein distance
 */
export function calculateStringSimilarity(str1, str2) {
  if (!str1 || !str2) return 0;
  const clean1 = cleanString(str1);
  const clean2 = cleanString(str2);
  return calculateLevenshteinSimilarity(clean1, clean2);
}

/**
 * Calculate exact match score
 */
export function calculateExactMatch(value1, value2) {
  if (!value1 || !value2) return 0;
  const clean1 = cleanString(value1);
  const clean2 = cleanString(value2);
  return clean1 === clean2 ? 100 : 0;
}

/**
 * Calculate Levenshtein distance similarity
 */
export function calculateLevenshteinSimilarity(str1, str2) {
  if (str1 === null || str2 === null) return 0;
  const distance = levenshteinDistance(str1, str2);
  const maxLength = Math.max(str1.length, str2.length);
  if (maxLength === 0) return 100;
  return ((maxLength - distance) / maxLength) * 100;
}

export function calculateDateSimilarity(spDateStr, azureDateStr) {
  if (!spDateStr || !azureDateStr) return 0;

  try {
    const spDate = new Date(spDateStr);
    const azureDate = new Date(azureDateStr);

    if (isNaN(spDate.getTime()) || isNaN(azureDate.getTime())) {
      console.warn("Invalid date format for similarity comparison.", { spDateStr, azureDateStr });
      return 0;
    }

    spDate.setHours(0, 0, 0, 0);
    azureDate.setHours(0, 0, 0, 0);

    const diffTime = Math.abs(azureDate.getTime() - spDate.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

    const thresholdDays = 30; 

    if (diffDays <= 0) { 
      return 100;
    } else if (diffDays <= thresholdDays) { 
      return 85;
    } else if (diffDays <= thresholdDays * 2) { 
      return 60; 
    } else if (diffDays <= thresholdDays * 3) { 
      return 40; 
    }
    return 0; 
  } catch (error) {
    console.warn("Could not parse dates for similarity comparison. Returning 0.", { spDateStr, azureDateStr, error });
    return 0;
  }
}

/**
 * Calculate Levenshtein distance
 */
function levenshteinDistance(str1, str2) {
  const matrix = Array(str2.length + 1)
    .fill(null)
    .map(() => Array(str1.length + 1).fill(null));

  for (let i = 0; i <= str1.length; i++) {
    matrix[0][i] = i;
  }

  for (let j = 0; j <= str2.length; j++) {
    matrix[j][0] = j;
  }

  for (let j = 1; j <= str2.length; j++) {
    for (let i = 1; i <= str1.length; i++) {
      const indicator = str1[i - 1] === str2[j - 1] ? 0 : 1;
      matrix[j][i] = Math.min(
        matrix[j][i - 1] + 1, // deletion
        matrix[j - 1][i] + 1, // insertion
        matrix[j - 1][i - 1] + indicator // substitution
      );
    }
  }

  return matrix[str2.length][str1.length];
}

/**
 * Clean string for comparison
 */
function cleanString(str) {
  if (!str) return "";
  return str
    .toLowerCase()
    .trim()
    .replace(/[^\w\s-]/g, "")
    .replace(/\s+/g, " ");
}

function cleanStringForNameComparison(str) {
  if (!str) return "";
  return str.toLowerCase().replace(/[^\w]/g, "");
}

/**
 * Calculate similarity score with confidence level
 */
export function calculateSimilarityWithConfidence(sharePointItem, azureItem) {
  const similarity = calculateSimilarity(sharePointItem, azureItem);

  let confidence = "Low";
  let confidenceColor = "#e53e3e";

  if (similarity >= 90) {
    confidence = "Excellent";
    confidenceColor = "#38a169";
  } else if (similarity >= 60) {
    confidence = "Medium";
    confidenceColor = "#d69e2e";
  }

  return {
    similarity,
    confidence,
    confidenceColor,
    recommendation:
      similarity >= 80
        ? "Recommended Match"
        : similarity >= 60
        ? "Possible Match"
        : "Manual Review Required",
  };
}

/**
 * Batch calculate similarities for multiple items
 */
export function batchCalculateSimilarities(sharePointItems, azureItems) {
  const results = [];

  console.log(
    `🔄 Batch calculating similarities: ${sharePointItems.length} × ${
      azureItems.length
    } = ${sharePointItems.length * azureItems.length} comparisons`
  );

  sharePointItems.forEach((spItem) => {
    const similarities = azureItems.map((azureItem) => ({
      ...azureItem,
      similarity: calculateSimilarity(spItem, azureItem),
      sharePointId: spItem.id,
    }));

    // Sort by similarity (highest first)
    similarities.sort((a, b) => b.similarity - a.similarity);

    results.push({
      sharePointItem: spItem,
      matches: similarities,
    });
  });

  console.log(`✅ Batch similarity calculation completed`);
  return results;
}

/**
 * Find best matches above threshold
 */
export function findBestMatches(sharePointItems, azureItems, threshold = 80) {
  const bestMatches = [];

  sharePointItems.forEach((spItem) => {
    let bestMatch = null;
    let bestScore = 0;

    azureItems.forEach((azureItem) => {
      const score = calculateSimilarity(spItem, azureItem);
      if (score > bestScore && score >= threshold) {
        bestScore = score;
        bestMatch = azureItem;
      }
    });

    if (bestMatch) {
      bestMatches.push({
        sharepoint: spItem,
        azure: bestMatch,
        similarity: bestScore,
        matchType: "auto",
        confidence:
          bestScore >= 90 ? "high" : bestScore >= 80 ? "medium" : "low",
      });
    }
  });

  console.log(
    `🎯 Found ${bestMatches.length} automatic matches above ${threshold}% threshold`
  );
  return bestMatches;
}

export function compareFieldStatus(sharePointItem, azureItem, fieldType) {
  let spValue = '';
  let azValue = '';
  
  switch (fieldType) {
    case 'customerName':
      spValue = sharePointItem.customerName || '';
      azValue = azureItem.customerName || azureItem.selltoCustName_SalesHeader || '';
      break;
    case 'productCode':
      spValue = sharePointItem.productCode || '';
      azValue = azureItem.pCode || azureItem.itemReferenceNo || '';
      break;
    case 'documentDate':
      spValue = sharePointItem.s9DWINEntryDate || '';
      azValue = azureItem.documentDate || '';
      return compareDateStatus(spValue, azValue);
    case 'region':
      spValue = sharePointItem.country || '';
      azValue = azureItem.regionDimName3 || '';
      break;
    case 'salesperson':
      spValue = sharePointItem.customerNameSalePersonCode || '';
      azValue = azureItem.salespersonDimName || '';
      break;
    case 'product':
      spValue = sharePointItem.productGroup || '';
      azValue = azureItem.custAppDimName || azureItem.prodChipNameDimName || '';
      break;
    default:
      return 'neutral';
  }
  
  return compareTextStatus(spValue, azValue);
}

export function compareTextStatus(value1, value2) {
  const clean1 = cleanStringForComparison(value1);
  const clean2 = cleanStringForComparison(value2);
  
  if (!clean1 && !clean2) return 'neutral'; 
  if (!clean1 || !clean2) return 'missing'; 
  
  if (clean1 === clean2) return 'exact-match'; 
 
  if (clean1.includes(clean2) || clean2.includes(clean1)) {
    return 'partial-match'; 
  }
  
 
  const similarity = calculateLevenshteinSimilarity(clean1, clean2);
  if (similarity >= 80) return 'high-match';
  if (similarity >= 60) return 'medium-match';
  
  return 'no-match'; 
}

/**
 * Compare DateTime For Show Color
 */
export function compareDateStatus(date1, date2) {
  if (!date1 && !date2) return 'neutral';
  if (!date1 || !date2) return 'missing';
  
  try {
    const d1 = new Date(date1);
    const d2 = new Date(date2);
    
    if (isNaN(d1.getTime()) || isNaN(d2.getTime())) return 'invalid';
    
    d1.setHours(0, 0, 0, 0);
    d2.setHours(0, 0, 0, 0);
    
    const diffDays = Math.abs((d2.getTime() - d1.getTime()) / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'exact-match';
    if (diffDays <= 30) return 'high-match';
    if (diffDays <= 90) return 'medium-match';
    if (diffDays <= 180) return 'low-match';
    
    return 'no-match';
  } catch (error) {
    return 'invalid';
  }
}

/**
 * CLean String 
 */
function cleanStringForComparison(str) {
  if (!str) return '';
  return str.toString().toLowerCase().trim().replace(/[^\w\s]/g, '').replace(/\s+/g, ' ');
}

/**
 * คืนค่าข้อมูลการเปรียบเทียบทั้งหมดสำหรับ 1 record
 */
export function getFieldComparisons(sharePointItem, azureItem) {
  if (!sharePointItem || !azureItem) return {};
  
  return {
    customerName: compareFieldStatus(sharePointItem, azureItem, 'customerName'),
    productCode: compareFieldStatus(sharePointItem, azureItem, 'productCode'),
    documentDate: compareFieldStatus(sharePointItem, azureItem, 'documentDate'),
    region: compareFieldStatus(sharePointItem, azureItem, 'region'),
    salesperson: compareFieldStatus(sharePointItem, azureItem, 'salesperson'),
    product: compareFieldStatus(sharePointItem, azureItem, 'product')
  };
}

export function getStatusFromScore(score) {
  if (score >= 95) return 'exact-match';
  if (score >= 70) return 'partial-match';
  if (score >= 30) return 'low-match';
  if (score > 0) return 'no-match';
  return 'missing';
}
