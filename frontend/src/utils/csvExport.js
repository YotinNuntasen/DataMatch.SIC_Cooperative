
/**
 * Export matched data to CSV format
 */
export async function exportToCSV(
  data,
  filename = "matched-data.csv",
  selectedColumns = null
) {
  try {
    console.log(`📁 Exporting ${data.length} records to CSV...`);

    const csvContent = generateCSVContent(data, selectedColumns);
    downloadCSVFile(csvContent, filename);

    console.log(`✅ CSV export completed: ${filename}`);
    return { success: true, filename, recordCount: data.length };
  } catch (error) {
    console.error("❌ CSV export failed:", error);
    throw error;
  }
}

function getColumnLabels() {
  return {
    'SP: opportunityName': 'Opportunity Name',
    'SP: opportunityId': 'Opportunity ID',
    'AZ: selltoCustName_SalesHeader': 'Customer Name',
    'AZ: custShortDimName': 'Short Name',
    'AZ: itemReferenceNo': 'Product Code',
    'AZ: documentDate': 'Document Date',
    'SP: s9DWINEntryDate': 'DWIN Date',
    'AZ: salespersonDimName': 'Sales Person',
    'AZ: prodChipNameDimName': 'Chip Name',
    'AZ: quantity': 'Quantity',
    'AZ: sellToCustomerNo': 'Customer No',
    'AZ: totalSales': 'Total Sales',
    'AZ: description': 'Description',
    'Similarity': 'Match Similarity',
    'Match Timestamp': 'Match Date'
  };
}

/**
 * Generate CSV content from data array
 */
function generateCSVContent(data, selectedColumns) {
  if (!data || data.length === 0) return "";

  const columnLabels = getColumnLabels();
  let columnsToExport = selectedColumns || [];

  // สร้าง header row โดยใช้ label ที่อ่านง่าย
  const headerRow = columnsToExport.map(colKey => {
    const label = columnLabels[colKey] || colKey;
    return escapeCSVField(label);
  }).join(',');
  
  // Generate data rows
  const dataRows = data.map(record => {
    const row = columnsToExport.map(colName => {
      let value = '';
      if (colName.startsWith('SP: ')) {
        const key = colName.substring(4);
        value = record.sharepoint ? record.sharepoint[key] : '';
      } else if (colName.startsWith('AZ: ')) {
        const key = colName.substring(4);
        value = record.azure ? record.azure[key] : '';
      } else if (colName === 'Similarity') {
        value = record.similarity;
      } else if (colName === 'Match Timestamp') {
        value = record.azure ? record.azure.timestamp : new Date().toISOString(); 
      }
      
     
      return escapeCSVField(getFieldValue({ value }, 'value')); 
    });
    return row.join(',');
  }); 
  
  return [headerRow, ...dataRows].join('\n');
}

/**
 * Get field value with fallback
 */
function getFieldValue(record, field, fallback) {
  let value = record[field];

  // If primary field is empty/null, try fallback
  if ((value === null || value === undefined || value === '') && fallback) {
    value = record[fallback];
  }

  // Format different data types
  if (value === null || value === undefined) {
    return "";
  } else if (typeof value === "boolean") {
    return value ? "Yes" : "No";
  } else if (typeof value === "number") {
    return value.toString();
  } else if (value instanceof Date) {
    return value.toISOString().split("T")[0]; // YYYY-MM-DD format
  } else if (typeof value === "object") {
    return JSON.stringify(value);
  } else {
    return value.toString();
  }
}

/**
 * Escape CSV field (handle quotes and commas)
 */
function escapeCSVField(field) {
  if (field === null || field === undefined) {
    return '""';
  }

  const stringField = String(field);

  // If field contains comma, newline, or quote, wrap in quotes
  if (
    stringField.includes(",") ||
    stringField.includes("\n") ||
    stringField.includes('"')
  ) {
    // Escape existing quotes by doubling them
    const escapedField = stringField.replace(/"/g, '""');
    return `"${escapedField}"`;
  }

  return stringField;
}

/**
 * Download CSV file to user's computer
 */
function downloadCSVFile(csvContent, filename) {
  try {
    // Create Blob with UTF-8 BOM for proper Excel compatibility
    const BOM = "\uFEFF";
    const blob = new Blob([BOM + csvContent], {
      type: "text/csv;charset=utf-8;",
    });

    // Create download link
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);

    link.setAttribute("href", url);
    link.setAttribute("download", filename);
    link.style.visibility = "hidden";

    // Trigger download
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up URL
    URL.revokeObjectURL(url);
  } catch (error) {
    console.error("❌ Failed to download CSV file:", error);
    throw new Error("Failed to download CSV file");
  }
}

/**
 * Export data to Excel format (XLSX)
 */
export async function exportToExcel(data, filename = "matched-data.xlsx", selectedColumns = null) {
  try {
    console.log(`📊 Exporting ${data.length} records to Excel...`);

    const csvFilename = filename.replace(".xlsx", ".csv");

    return await exportToCSV(data, csvFilename, selectedColumns);

  } catch (error) {
    console.error("❌ Excel export failed:", error);
    throw error;
  }
}

/**
 * Export with custom column selection
 */
export async function exportCustomColumns(
  data,
  selectedColumns,
  filename = "custom-export.csv"
) {
  try {
    console.log(
      `📋 Exporting ${selectedColumns.length} columns for ${data.length} records...`
    );

    if (!selectedColumns || selectedColumns.length === 0) {
      throw new Error("No columns selected for export");
    }

    // Filter data to only include selected columns
    const filteredData = data.map((record) => {
      const filteredRecord = {};
      selectedColumns.forEach((column) => {
        if (record.hasOwnProperty(column)) {
          filteredRecord[column] = record[column];
        }
      });
      return filteredRecord;
    });

    return await exportToCSV(filteredData, filename);
  } catch (error) {
    console.error("❌ Custom column export failed:", error);
    throw error;
  }
}

/**
 * Export summary statistics
 */
export async function exportSummary(data, filename = "matching-summary.csv") {
  try {
    console.log("📈 Generating export summary...");

    const summary = generateSummaryData(data);
    return await exportToCSV(summary, filename);
  } catch (error) {
    console.error("❌ Summary export failed:", erroัวr);
    throw error;
  }
}

/**
 * Generate summary data for export
 */
function generateSummaryData(data) {
  const totalRecords = data.length;
  const manualMatches = data.filter((d) => d.matchType === "manual").length;
  const autoMatches = data.filter((d) => d.matchType === "auto").length;
  const aiMatches = data.filter((d) => d.matchType === "ai").length;

  const similarities = data.map((d) => d.similarity || 0).filter((s) => s > 0);
  const avgSimilarity =
    similarities.length > 0
      ? similarities.reduce((a, b) => a + b, 0) / similarities.length
      : 0;

  const excellentMatches = data.filter((d) => (d.similarity || 0) >= 90).length;
  const goodMatches = data.filter(
    (d) => (d.similarity || 0) >= 80 && (d.similarity || 0) < 90
  ).length;
  const mediumMatches = data.filter(
    (d) => (d.similarity || 0) >= 60 && (d.similarity || 0) < 80
  ).length;
  const lowMatches = data.filter((d) => (d.similarity || 0) < 60).length;

  // Group by country
  const countryStats = {};
  data.forEach((record) => {
    const country = record.country || "Unknown";
    countryStats[country] = (countryStats[country] || 0) + 1;
  });

  // Group by industry
  const industryStats = {};
  data.forEach((record) => {
    const industry = record.industry || record.customerIndustry || "Unknown";
    industryStats[industry] = (industryStats[industry] || 0) + 1;
  });

  const summary = [
    { metric: "Total Records", value: totalRecords },
    { metric: "Manual Matches", value: manualMatches },
    { metric: "Auto Matches", value: autoMatches },
    { metric: "AI Matches", value: aiMatches },
    {
      metric: "Average Similarity",
      value: Math.round(avgSimilarity * 100) / 100 + "%",
    },
    { metric: "Excellent Matches (90%+)", value: excellentMatches },
    { metric: "Good Matches (80-89%)", value: goodMatches },
    { metric: "Medium Matches (60-79%)", value: mediumMatches },
    { metric: "Low Matches (<60%)", value: lowMatches },
    { metric: "Export Date", value: new Date().toISOString().split("T")[0] },
  ];

  // Add country breakdown
  Object.entries(countryStats)
    .sort((a, b) => b[1] - a[1])
    .forEach(([country, count]) => {
      summary.push({ metric: `Country: ${country}`, value: count });
    });

  // Add industry breakdown
  Object.entries(industryStats)
    .sort((a, b) => b[1] - a[1])
    .forEach(([industry, count]) => {
      summary.push({ metric: `Industry: ${industry}`, value: count });
    });

  return summary;
}

/**
 * Validate data before export
 */
export function validateExportData(data) {
  const issues = [];

  if (!data || !Array.isArray(data)) {
    issues.push("Data must be an array");
    return issues;
  }

  if (data.length === 0) {
    issues.push("No data to export");
    return issues;
  }

  // Check for required fields
  const requiredFields = ["name", "email"];
  const sampleRecord = data[0];

  requiredFields.forEach((field) => {
    if (
      !sampleRecord.hasOwnProperty(field) &&
      !sampleRecord.hasOwnProperty(
        `customer${field.charAt(0).toUpperCase()}${field.slice(1)}`
      )
    ) {
      issues.push(`Missing required field: ${field}`);
    }
  });

  // Check data consistency
  const recordWithMostFields = data.reduce(
    (max, record) =>
      Object.keys(record).length > Object.keys(max).length ? record : max,
    {}
  );

  const maxFieldCount = Object.keys(recordWithMostFields).length;
  const inconsistentRecords = data.filter(
    (record) => Object.keys(record).length < maxFieldCount * 0.5
  ).length;

  if (inconsistentRecords > 0) {
    issues.push(
      `${inconsistentRecords} records have significantly fewer fields than others`
    );
  }

  return issues;
}

/**
 * Get available export formats
 */
export function getAvailableFormats() {
  return [
    {
      format: "csv",
      label: "CSV (Excel Compatible)",
      description: "Comma-separated values file that opens in Excel",
      mimeType: "text/csv",
      extension: ".csv",
      supported: true,
    },
    {
      format: "json",
      label: "JSON (Raw Data)",
      description: "JavaScript Object Notation for technical use",
      mimeType: "application/json",
      extension: ".json",
      supported: true,
    },
    {
      format: "xlsx",
      label: "Excel Spreadsheet",
      description: "Native Excel format with formatting",
      mimeType:
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
      extension: ".xlsx",
      supported: false, // Requires additional library
    },
  ];
}

/**
 * Export to JSON format
 */
export async function exportToJSON(data, filename = "matched-data.json") {
  try {
    console.log(`📄 Exporting ${data.length} records to JSON...`);

    const jsonContent = JSON.stringify(
      {
        exportInfo: {
          timestamp: new Date().toISOString(),
          recordCount: data.length,
          exportedBy: "Data Match Portal",
          version: "1.0.0",
        },
        data: data,
      },
      null,
      2
    );

    // Create and download JSON file
    const blob = new Blob([jsonContent], {
      type: "application/json;charset=utf-8;",
    });
    const link = document.createElement("a");
    const url = URL.createObjectURL(blob);

    link.setAttribute("href", url);
    link.setAttribute("download", filename);
    link.style.visibility = "hidden";

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    URL.revokeObjectURL(url);

    console.log(`✅ JSON export completed: ${filename}`);
    return {
      success: true,
      filename,
      recordCount: data.length,
    };
  } catch (error) {
    console.error("❌ JSON export failed:", error);
    throw error;
  }
}

/**
 * Export filtered data based on criteria
 */
export async function exportFiltered(
  data,
  filters,
  filename = "filtered-data.csv"
) {
  try {
    console.log("🔍 Applying filters before export...");

    let filteredData = [...data];

    // Apply similarity filter
    if (filters.minSimilarity) {
      filteredData = filteredData.filter(
        (record) => (record.similarity || 0) >= filters.minSimilarity
      );
    }

    // Apply match type filter
    if (filters.matchType && filters.matchType !== "all") {
      filteredData = filteredData.filter(
        (record) => record.matchType === filters.matchType
      );
    }

    // Apply country filter
    if (filters.country && filters.country !== "all") {
      filteredData = filteredData.filter(
        (record) => record.country === filters.country
      );
    }

    // Apply industry filter
    if (filters.industry && filters.industry !== "all") {
      filteredData = filteredData.filter(
        (record) =>
          (record.industry || record.customerIndustry) === filters.industry
      );
    }

    // Apply date range filter
    if (filters.dateFrom || filters.dateTo) {
      filteredData = filteredData.filter((record) => {
        const recordDate = new Date(
          record.matchTimestamp || record.created || record.modified
        );

        if (filters.dateFrom && recordDate < new Date(filters.dateFrom)) {
          return false;
        }

        if (filters.dateTo && recordDate > new Date(filters.dateTo)) {
          return false;
        }

        return true;
      });
    }

    console.log(
      `📊 Filtered from ${data.length} to ${filteredData.length} records`
    );

    return await exportToCSV(filteredData, filename);
  } catch (error) {
    console.error("❌ Filtered export failed:", error);
    throw error;
  }
}
