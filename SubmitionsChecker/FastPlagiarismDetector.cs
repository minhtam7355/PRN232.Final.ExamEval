using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SubmitionsChecker
{
    public class PlagiarismResult
    {
        public string Student1 { get; set; } = string.Empty;
        public string Student2 { get; set; } = string.Empty;
        public double SimilarityScore { get; set; } // 0-100
        public string Analysis { get; set; } = string.Empty;
        public bool IsSuspicious { get; set; }
        public Dictionary<string, int> CommonPatterns { get; set; } = new();
    }

    /// <summary>
    /// Code fingerprint based on variable names, namespaces, and coding style
    /// </summary>
    public class CodeFingerprint
    {
        public HashSet<string> VariableNames { get; set; } = new();
        public HashSet<string> Namespaces { get; set; } = new();
        public HashSet<string> ClassNames { get; set; } = new();
        public HashSet<string> MethodNames { get; set; } = new();
        public HashSet<string> PropertyNames { get; set; } = new();
        public int TotalVariables { get; set; }
        public int TotalMethods { get; set; }
    }

    /// <summary>
    /// Plagiarism detection focused on variable names and code style
    /// Perfect for checking submissions with same requirements
    /// </summary>
    public class StyleBasedPlagiarismDetector
    {
        private readonly ILogger<StyleBasedPlagiarismDetector>? _logger;
        private const double SuspiciousThreshold = 70.0; // 70% similar names = suspicious

        public StyleBasedPlagiarismDetector(ILogger<StyleBasedPlagiarismDetector>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Detect plagiarism by comparing variable names, namespaces, and code style
        /// </summary>
        public async Task<List<PlagiarismResult>> DetectPlagiarismAsync(
            string normalizedOutputDir,
            CancellationToken ct = default)
        {
            _logger?.LogInformation("Starting style-based plagiarism detection");

            var results = new List<PlagiarismResult>();

            // Step 1: Load all student files and extract original code (before normalization)
            var studentCodes = await LoadStudentCodesAsync(normalizedOutputDir, ct);

            if (studentCodes.Count < 2)
            {
                _logger?.LogWarning("Need at least 2 students for plagiarism detection");
                return results;
            }

            _logger?.LogInformation("Processing {Count} student submissions", studentCodes.Count);

            // Step 2: Extract code fingerprints (variable names, namespaces, etc.)
            var fingerprints = new Dictionary<string, CodeFingerprint>();
            foreach (var (student, code) in studentCodes)
            {
                ct.ThrowIfCancellationRequested();
                fingerprints[student] = ExtractCodeFingerprint(code);
                _logger?.LogDebug("Extracted fingerprint for {Student}: {Vars} vars, {Methods} methods", 
                    student, fingerprints[student].TotalVariables, fingerprints[student].TotalMethods);
            }

            // Step 3: Compare all pairs
            var students = studentCodes.Keys.ToList();
            var totalComparisons = 0;

            for (int i = 0; i < students.Count - 1; i++)
            {
                for (int j = i + 1; j < students.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();

                    var student1 = students[i];
                    var student2 = students[j];

                    var similarity = CalculateStyleSimilarity(
                        fingerprints[student1], 
                        fingerprints[student2],
                        out var commonPatterns,
                        out var analysis);

                    totalComparisons++;

                    if (similarity >= SuspiciousThreshold)
                    {
                        results.Add(new PlagiarismResult
                        {
                            Student1 = student1,
                            Student2 = student2,
                            SimilarityScore = Math.Round(similarity, 2),
                            Analysis = analysis,
                            IsSuspicious = true,
                            CommonPatterns = commonPatterns
                        });

                        _logger?.LogWarning("⚠️ SUSPICIOUS: {S1} vs {S2} - {Score}% similar coding style", 
                            student1, student2, Math.Round(similarity, 2));
                    }

                    if (totalComparisons % 100 == 0)
                    {
                        _logger?.LogInformation("Compared {Count} pairs...", totalComparisons);
                    }
                }
            }

            _logger?.LogInformation("Completed {Total} comparisons. Found {Count} suspicious pairs", 
                totalComparisons, results.Count);

            return results.OrderByDescending(r => r.SimilarityScore).ToList();
        }

        /// <summary>
        /// Detect plagiarism across ALL submissions in history (cross-submission detection)
        /// This compares current submission with all previous submissions
        /// </summary>
        public async Task<List<PlagiarismResult>> DetectPlagiarismWithHistoryAsync(
            string normalizedOutputDir,
            string historyStoragePath,
            string currentSubmissionId,
            CancellationToken ct = default)
        {
            _logger?.LogInformation("Starting CROSS-SUBMISSION plagiarism detection with history");

            var results = new List<PlagiarismResult>();

            // Initialize history manager
            var historyManager = new PlagiarismHistoryManager(historyStoragePath, _logger as ILogger<PlagiarismHistoryManager>);

            // Step 1: Load current submission codes
            var currentCodes = await LoadStudentCodesAsync(normalizedOutputDir, ct);
            
            if (currentCodes.Count == 0)
            {
                _logger?.LogWarning("No student codes found in current submission");
                return results;
            }

            _logger?.LogInformation("Loaded {Count} students from current submission", currentCodes.Count);

            // Step 2: Save current submission to history
            await historyManager.SaveSubmissionAsync(currentSubmissionId, currentCodes);
            
            // Step 3: Load ALL student codes from history (including current submission)
            var allCodes = await historyManager.GetAllStudentCodesAsync();
            
            _logger?.LogInformation("Total students in history: {Total} (including current submission)", allCodes.Count);

            if (allCodes.Count < 2)
            {
                _logger?.LogWarning("Need at least 2 students total for plagiarism detection");
                return results;
            }

            // Step 4: Extract fingerprints for ALL students
            var fingerprints = new Dictionary<string, CodeFingerprint>();
            foreach (var (student, code) in allCodes)
            {
                ct.ThrowIfCancellationRequested();
                fingerprints[student] = ExtractCodeFingerprint(code);
            }

            _logger?.LogInformation("Extracted fingerprints for {Count} students", fingerprints.Count);

            // Step 5: Compare ALL pairs (not just current submission)
            var students = allCodes.Keys.ToList();
            var totalComparisons = 0;
            var possibleComparisons = (students.Count * (students.Count - 1)) / 2;

            _logger?.LogInformation("Will perform {Total} comparisons ({Students} students)", possibleComparisons, students.Count);

            for (int i = 0; i < students.Count - 1; i++)
            {
                for (int j = i + 1; j < students.Count; j++)
                {
                    ct.ThrowIfCancellationRequested();

                    var student1 = students[i];
                    var student2 = students[j];

                    var similarity = CalculateStyleSimilarity(
                        fingerprints[student1], 
                        fingerprints[student2],
                        out var commonPatterns,
                        out var analysis);

                    totalComparisons++;

                    if (similarity >= SuspiciousThreshold)
                    {
                        results.Add(new PlagiarismResult
                        {
                            Student1 = student1,
                            Student2 = student2,
                            SimilarityScore = Math.Round(similarity, 2),
                            Analysis = analysis,
                            IsSuspicious = true,
                            CommonPatterns = commonPatterns
                        });

                        _logger?.LogWarning("⚠️ SUSPICIOUS: {S1} vs {S2} - {Score}% similar (CROSS-SUBMISSION)", 
                            student1, student2, Math.Round(similarity, 2));
                    }

                    if (totalComparisons % 100 == 0)
                    {
                        _logger?.LogInformation("Progress: {Current}/{Total} comparisons ({Percent:F1}%)...", 
                            totalComparisons, possibleComparisons, (totalComparisons * 100.0 / possibleComparisons));
                    }
                }
            }

            _logger?.LogInformation("✅ Completed {Total} cross-submission comparisons. Found {Count} suspicious pairs", 
                totalComparisons, results.Count);

            // Get history summary
            var historySummary = await historyManager.GetHistorySummaryAsync();
            _logger?.LogInformation("📊 History: {Submissions} submissions, {Students} unique students total", 
                historySummary.TotalSubmissions, historySummary.TotalStudents);

            return results.OrderByDescending(r => r.SimilarityScore).ToList();
        }

        /// <summary>
        /// Load student codes from NORMALIZED output files (already processed and cleaned)
        /// This is MUCH simpler than trying to extract from source!
        /// </summary>
        private async Task<Dictionary<string, string>> LoadStudentCodesAsync(
            string normalizedOutputDir, 
            CancellationToken ct)
        {
            var studentCodes = new Dictionary<string, string>();
            
            _logger?.LogInformation("🔍 Loading student codes from NORMALIZED directory: {Dir}", normalizedOutputDir);

            if (!Directory.Exists(normalizedOutputDir))
            {
                _logger?.LogError("❌ Normalized directory does not exist: {Dir}", normalizedOutputDir);
                return studentCodes;
            }

            // Get ALL .txt files from Normalized directory
            var normalizedFiles = Directory.GetFiles(normalizedOutputDir, "*.txt", SearchOption.TopDirectoryOnly);
            
            _logger?.LogInformation("📂 Found {Count} normalized files", normalizedFiles.Length);

            if (normalizedFiles.Length == 0)
            {
                _logger?.LogError("❌ No .txt files found in Normalized directory!");
                _logger?.LogError("   This means the submission processing failed or no students were processed.");
                return studentCodes;
            }

            foreach (var file in normalizedFiles)
            {
                ct.ThrowIfCancellationRequested();

                var studentName = Path.GetFileNameWithoutExtension(file);
                
                try
                {
                    var content = await File.ReadAllTextAsync(file, ct);
                    
                    if (string.IsNullOrWhiteSpace(content) || content.Length < 100)
                    {
                        _logger?.LogWarning("   ⚠️ Normalized file for {Student} is empty or too small ({Size} chars)", 
                            studentName, content.Length);
                        continue;
                    }

                    studentCodes[studentName] = content;
                    _logger?.LogInformation("   ✅ Loaded {Size} chars of normalized code for {Student}", 
                        content.Length, studentName);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "   ❌ Failed to read normalized file for {Student}", studentName);
                }
            }

            _logger?.LogInformation("📊 SUMMARY: Loaded {Success}/{Total} students from normalized files", 
                studentCodes.Count, normalizedFiles.Length);
            
            if (studentCodes.Count == 0)
            {
                _logger?.LogError("❌❌❌ CRITICAL: NO STUDENT CODES LOADED!");
                _logger?.LogError("Normalized directory: {Dir}", normalizedOutputDir);
                _logger?.LogError("Files found: {Count}", normalizedFiles.Length);
            }
            else if (studentCodes.Count < normalizedFiles.Length)
            {
                _logger?.LogWarning("⚠️ Only loaded {Success}/{Total} students. Some files may be empty.", 
                    studentCodes.Count, normalizedFiles.Length);
            }
            else
            {
                _logger?.LogInformation("✅ Successfully loaded ALL {Count} students from normalized files!", studentCodes.Count);
            }

            return studentCodes;
        }

        /// <summary>
        /// Extract code fingerprint: variable names, namespaces, class names, etc.
        /// </summary>
        private CodeFingerprint ExtractCodeFingerprint(string code)
        {
            var fingerprint = new CodeFingerprint();

            // Extract namespaces
            var namespaceMatches = Regex.Matches(code, @"namespace\s+([A-Za-z0-9_.]+)", RegexOptions.Multiline);
            foreach (Match match in namespaceMatches)
            {
                fingerprint.Namespaces.Add(match.Groups[1].Value.ToLower());
            }

            // Extract class names
            var classMatches = Regex.Matches(code, @"class\s+([A-Za-z0-9_]+)", RegexOptions.Multiline);
            foreach (Match match in classMatches)
            {
                fingerprint.ClassNames.Add(match.Groups[1].Value.ToLower());
            }

            // Extract method names
            var methodMatches = Regex.Matches(code, 
                @"(?:public|private|protected|internal|static)\s+(?:async\s+)?(?:\w+)\s+([A-ZaZ0-9_]+)\s*\(", 
                RegexOptions.Multiline);
            foreach (Match match in methodMatches)
            {
                var methodName = match.Groups[1].Value;
                if (!IsCommonMethodName(methodName))
                {
                    fingerprint.MethodNames.Add(methodName.ToLower());
                }
            }

            // Extract property names
            var propertyMatches = Regex.Matches(code, 
                @"(?:public|private|protected|internal)\s+\w+\s+([A-Za-z0-9_]+)\s*{\s*get", 
                RegexOptions.Multiline);
            foreach (Match match in propertyMatches)
            {
                fingerprint.PropertyNames.Add(match.Groups[1].Value.ToLower());
            }

            // Extract variable names (local variables and fields)
            var varMatches = Regex.Matches(code, 
                @"(?:var|int|string|bool|double|float|decimal|long|List<[^>]+>|Dictionary<[^>]+>|[A-Z][A-Za-z0-9]*)\s+([a-z][A-Za-z0-9_]*)\s*[=;]", 
                RegexOptions.Multiline);
            foreach (Match match in varMatches)
            {
                var varName = match.Groups[1].Value;
                if (!IsCommonVariableName(varName))
                {
                    fingerprint.VariableNames.Add(varName.ToLower());
                }
            }

            fingerprint.TotalVariables = fingerprint.VariableNames.Count;
            fingerprint.TotalMethods = fingerprint.MethodNames.Count;

            return fingerprint;
        }

        /// <summary>
        /// Calculate similarity based on coding style (variable names, namespaces, etc.)
        /// </summary>
        private double CalculateStyleSimilarity(
            CodeFingerprint fp1, 
            CodeFingerprint fp2,
            out Dictionary<string, int> commonPatterns,
            out string analysis)
        {
            commonPatterns = new Dictionary<string, int>();
            var analysisParts = new List<string>();
            var scores = new List<double>();

            // 1. Compare variable names (40% weight)
            var commonVars = fp1.VariableNames.Intersect(fp2.VariableNames).ToList();
            var varSimilarity = CalculateJaccardSimilarity(fp1.VariableNames, fp2.VariableNames);
            scores.Add(varSimilarity * 0.40);
            commonPatterns["commonVariables"] = commonVars.Count;
            
            if (commonVars.Count > 5)
            {
                analysisParts.Add($"{commonVars.Count} identical variable names: {string.Join(", ", commonVars.Take(5))}...");
            }

            // 2. Compare namespaces (15% weight)
            var commonNamespaces = fp1.Namespaces.Intersect(fp2.Namespaces).ToList();
            var nsSimilarity = CalculateJaccardSimilarity(fp1.Namespaces, fp2.Namespaces);
            scores.Add(nsSimilarity * 0.15);
            commonPatterns["commonNamespaces"] = commonNamespaces.Count;

            if (commonNamespaces.Count > 0)
            {
                analysisParts.Add($"Identical namespaces: {string.Join(", ", commonNamespaces)}");
            }

            // 3. Compare class names (15% weight)
            var commonClasses = fp1.ClassNames.Intersect(fp2.ClassNames).ToList();
            var classSimilarity = CalculateJaccardSimilarity(fp1.ClassNames, fp2.ClassNames);
            scores.Add(classSimilarity * 0.15);
            commonPatterns["commonClasses"] = commonClasses.Count;

            if (commonClasses.Count > 2)
            {
                analysisParts.Add($"{commonClasses.Count} identical class names");
            }

            // 4. Compare method names (20% weight)
            var commonMethods = fp1.MethodNames.Intersect(fp2.MethodNames).ToList();
            var methodSimilarity = CalculateJaccardSimilarity(fp1.MethodNames, fp2.MethodNames);
            scores.Add(methodSimilarity * 0.20);
            commonPatterns["commonMethods"] = commonMethods.Count;

            if (commonMethods.Count > 3)
            {
                analysisParts.Add($"{commonMethods.Count} identical method names");
            }

            // 5. Compare property names (10% weight)
            var commonProperties = fp1.PropertyNames.Intersect(fp2.PropertyNames).ToList();
            var propSimilarity = CalculateJaccardSimilarity(fp1.PropertyNames, fp2.PropertyNames);
            scores.Add(propSimilarity * 0.10);
            commonPatterns["commonProperties"] = commonProperties.Count;

            // Calculate final score
            var finalScore = scores.Sum() * 100;

            // Build analysis
            if (analysisParts.Count == 0)
            {
                analysis = "Different coding styles detected.";
            }
            else
            {
                analysis = string.Join(". ", analysisParts) + ".";
            }

            return finalScore;
        }

        private double CalculateJaccardSimilarity(HashSet<string> set1, HashSet<string> set2)
        {
            if (set1.Count == 0 && set2.Count == 0) return 0;
            if (set1.Count == 0 || set2.Count == 0) return 0;

            var intersection = set1.Intersect(set2).Count();
            var union = set1.Union(set2).Count();

            return union > 0 ? intersection / (double)union : 0;
        }

        private bool IsCommonMethodName(string name)
        {
            var commonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ToString", "GetHashCode", "Equals", "Main", "Dispose",
                "OnConfiguring", "OnModelCreating", "Configure", "ConfigureServices"
            };
            return commonNames.Contains(name);
        }

        private bool IsCommonVariableName(string name)
        {
            var commonNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "i", "j", "k", "x", "y", "z", "temp", "result", "data", "value",
                "id", "name", "ex", "item", "list", "count"
            };
            return commonNames.Contains(name);
        }

        /// <summary>
        /// Generate detailed plagiarism report with grouping
        /// </summary>
        public async Task<string> GeneratePlagiarismReportAsync(
            List<PlagiarismResult> results,
            string outputPath)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
            sb.AppendLine("║     PLAGIARISM DETECTION REPORT (Style-Based Analysis)       ║");
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
            sb.AppendLine();
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Detection Method: Variable Names & Code Style Analysis");
            sb.AppendLine($"Total Suspicious Pairs: {results.Count}");
            sb.AppendLine();
            sb.AppendLine(new string('=', 80));
            sb.AppendLine();

            if (results.Count == 0)
            {
                sb.AppendLine("✅ No suspicious plagiarism detected!");
                sb.AppendLine("All students appear to have unique coding styles.");
            }
            else
            {
                // Group students into plagiarism clusters
                var groups = GroupSuspiciousStudents(results);

                sb.AppendLine($"⚠️  FOUND {groups.Count} SUSPICIOUS GROUPS:");
                sb.AppendLine();

                var groupNum = 1;
                foreach (var group in groups.OrderByDescending(g => g.AverageSimilarity))
                {
                    sb.AppendLine($"📋 GROUP #{groupNum} - Average Similarity: {group.AverageSimilarity:F2}%");
                    sb.AppendLine($"   Members ({group.Students.Count}): {string.Join(", ", group.Students)}");
                    sb.AppendLine();
                    
                    foreach (var pair in group.Pairs.OrderByDescending(p => p.SimilarityScore).Take(3))
                    {
                        sb.AppendLine($"   • {pair.Student1} ↔ {pair.Student2}: {pair.SimilarityScore:F2}%");
                        sb.AppendLine($"     Analysis: {pair.Analysis}");
                        if (pair.CommonPatterns.Any())
                        {
                            var commonStr = string.Join(", ", pair.CommonPatterns.Select(kv => $"{kv.Value} {kv.Key}"));
                            sb.AppendLine($"     Common: {commonStr}");
                        }
                        sb.AppendLine();
                    }
                    
                    groupNum++;
                    sb.AppendLine(new string('-', 80));
                    sb.AppendLine();
                }

                sb.AppendLine();
                sb.AppendLine("📊 DETAILED PAIRS:");
                sb.AppendLine();

                var rank = 1;
                foreach (var result in results.OrderByDescending(r => r.SimilarityScore))
                {
                    sb.AppendLine($"#{rank}. {result.Student1} ↔ {result.Student2}");
                    sb.AppendLine($"    Similarity: {result.SimilarityScore:F2}%");
                    sb.AppendLine($"    {result.Analysis}");
                    sb.AppendLine();
                    rank++;
                }
            }

            sb.AppendLine(new string('=', 80));
            sb.AppendLine();
            sb.AppendLine("SEVERITY LEVELS:");
            sb.AppendLine("  90-100%: Almost identical coding style (Critical Risk)");
            sb.AppendLine("  80-89%:  Very similar naming conventions (High Risk)");
            sb.AppendLine("  70-79%:  Similar coding patterns (Medium Risk)");
            sb.AppendLine();
            sb.AppendLine("NOTE: This analysis focuses on variable names, namespaces, and code style.");
            sb.AppendLine("      Logic similarity is expected since all students solve the same problem.");
            sb.AppendLine();

            var report = sb.ToString();
            await File.WriteAllTextAsync(outputPath, report);

            return report;
        }

        /// <summary>
        /// Group students who have similar code into clusters
        /// </summary>
        private List<PlagiarismGroup> GroupSuspiciousStudents(List<PlagiarismResult> results)
        {
            var groups = new List<PlagiarismGroup>();
            var processed = new HashSet<string>();

            foreach (var result in results.OrderByDescending(r => r.SimilarityScore))
            {
                if (processed.Contains(result.Student1) && processed.Contains(result.Student2))
                    continue;

                // Find or create group
                var existingGroup = groups.FirstOrDefault(g => 
                    g.Students.Contains(result.Student1) || g.Students.Contains(result.Student2));

                if (existingGroup != null)
                {
                    existingGroup.Students.Add(result.Student1);
                    existingGroup.Students.Add(result.Student2);
                    existingGroup.Pairs.Add(result);
                }
                else
                {
                    var newGroup = new PlagiarismGroup
                    {
                        Students = new HashSet<string> { result.Student1, result.Student2 },
                        Pairs = new List<PlagiarismResult> { result }
                    };
                    groups.Add(newGroup);
                }

                processed.Add(result.Student1);
                processed.Add(result.Student2);
            }

            // Calculate average similarity for each group
            foreach (var group in groups)
            {
                group.AverageSimilarity = group.Pairs.Average(p => p.SimilarityScore);
            }

            return groups;
        }

        private class PlagiarismGroup
        {
            public HashSet<string> Students { get; set; } = new();
            public List<PlagiarismResult> Pairs { get; set; } = new();
            public double AverageSimilarity { get; set; }
        }
    }
}
