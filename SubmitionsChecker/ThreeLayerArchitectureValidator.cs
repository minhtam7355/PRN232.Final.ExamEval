using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace SubmitionsChecker
{
    public record ProjectInfo(string Name, string Path, List<string> ProjectReferences);

    public record ArchitectureValidationResult(bool IsValid, string Message, List<string> Details);

    public class ThreeLayerArchitectureValidator
    {
        private readonly ILogger<ThreeLayerArchitectureValidator>? _logger;
        
        // Common naming patterns for 3-layer architecture
        private static readonly string[] DalPatterns = { "dal", "data", "dataaccess", "repository", "repositories" };
        private static readonly string[] BllPatterns = { "bll", "business", "businesslogic", "service", "services" };
        private static readonly string[] GuiPatterns = { "gui", "presentation", "ui", "web", "app", "wpf", "winforms", "client" };

        public ThreeLayerArchitectureValidator(ILogger<ThreeLayerArchitectureValidator>? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Validate that the solution follows 3-layer architecture:
        /// - Must have exactly 3 projects (DAL, BLL, GUI)
        /// - References must follow: GUI -> BLL -> DAL (no cross-references)
        /// - GUI cannot directly reference DAL
        /// </summary>
        public ArchitectureValidationResult ValidateArchitecture(string extractedSolutionPath)
        {
            try
            {
                var details = new List<string>();

                // Find all .csproj files
                var projectFiles = Directory.GetFiles(extractedSolutionPath, "*.csproj", SearchOption.AllDirectories)
                    .Where(p => !p.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar)
                                && !p.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                    .ToArray();

                _logger?.LogInformation("Found {Count} .csproj files", projectFiles.Length);

                if (projectFiles.Length < 3)
                {
                    return new ArchitectureValidationResult(
                        false,
                        $"Expected 3 projects (DAL, BLL, GUI) but found only {projectFiles.Length}",
                        new List<string> { $"Found {projectFiles.Length} project(s): {string.Join(", ", projectFiles.Select(Path.GetFileNameWithoutExtension))}" }
                    );
                }

                if (projectFiles.Length > 3)
                {
                    details.Add($"Warning: Found {projectFiles.Length} projects. Expected exactly 3 for 3-layer architecture.");
                }

                // Parse projects and their references
                var projects = new List<ProjectInfo>();
                foreach (var projFile in projectFiles)
                {
                    var projName = Path.GetFileNameWithoutExtension(projFile);
                    var references = GetProjectReferences(projFile);
                    projects.Add(new ProjectInfo(projName, projFile, references));
                    _logger?.LogDebug("Project {Name} references: {Refs}", projName, string.Join(", ", references));
                }

                // Identify layers based on naming conventions
                var dalProjects = projects.Where(p => MatchesPattern(p.Name, DalPatterns)).ToList();
                var bllProjects = projects.Where(p => MatchesPattern(p.Name, BllPatterns)).ToList();
                var guiProjects = projects.Where(p => MatchesPattern(p.Name, GuiPatterns)).ToList();

                details.Add($"DAL projects: {string.Join(", ", dalProjects.Select(p => p.Name))}");
                details.Add($"BLL projects: {string.Join(", ", bllProjects.Select(p => p.Name))}");
                details.Add($"GUI projects: {string.Join(", ", guiProjects.Select(p => p.Name))}");

                // Validate that we have at least one of each layer
                if (dalProjects.Count == 0)
                {
                    return new ArchitectureValidationResult(
                        false,
                        "No DAL (Data Access Layer) project found. Expected project name containing: DAL, Data, DataAccess, Repository",
                        details
                    );
                }

                if (bllProjects.Count == 0)
                {
                    return new ArchitectureValidationResult(
                        false,
                        "No BLL (Business Logic Layer) project found. Expected project name containing: BLL, Business, BusinessLogic, Service",
                        details
                    );
                }

                if (guiProjects.Count == 0)
                {
                    return new ArchitectureValidationResult(
                        false,
                        "No GUI/Presentation project found. Expected project name containing: GUI, Presentation, UI, Web, App, WPF, Client",
                        details
                    );
                }

                // Validate references
                var violations = new List<string>();

                // Check BLL -> DAL (should reference DAL)
                foreach (var bll in bllProjects)
                {
                    var referencesAnyDal = dalProjects.Any(dal => bll.ProjectReferences.Contains(dal.Name));
                    if (!referencesAnyDal && dalProjects.Count > 0)
                    {
                        violations.Add($"BLL project '{bll.Name}' should reference DAL project but doesn't");
                    }
                }

                // Check GUI -> BLL (should reference BLL)
                foreach (var gui in guiProjects)
                {
                    var referencesAnyBll = bllProjects.Any(bll => gui.ProjectReferences.Contains(bll.Name));
                    if (!referencesAnyBll && bllProjects.Count > 0)
                    {
                        violations.Add($"GUI project '{gui.Name}' should reference BLL project but doesn't");
                    }

                    // Check GUI should NOT reference DAL directly
                    var referencesAnyDal = dalProjects.Any(dal => gui.ProjectReferences.Contains(dal.Name));
                    if (referencesAnyDal)
                    {
                        var referencedDals = dalProjects.Where(dal => gui.ProjectReferences.Contains(dal.Name)).Select(d => d.Name);
                        violations.Add($"GUI project '{gui.Name}' directly references DAL project(s) [{string.Join(", ", referencedDals)}]. This violates 3-layer architecture!");
                    }
                }

                // Check DAL should not reference BLL or GUI (circular reference)
                foreach (var dal in dalProjects)
                {
                    var referencesAnyBll = bllProjects.Any(bll => dal.ProjectReferences.Contains(bll.Name));
                    var referencesAnyGui = guiProjects.Any(gui => dal.ProjectReferences.Contains(gui.Name));
                    
                    if (referencesAnyBll || referencesAnyGui)
                    {
                        violations.Add($"DAL project '{dal.Name}' should not reference BLL or GUI projects (circular dependency detected)");
                    }
                }

                // Check BLL should not reference GUI (wrong direction)
                foreach (var bll in bllProjects)
                {
                    var referencesAnyGui = guiProjects.Any(gui => bll.ProjectReferences.Contains(gui.Name));
                    if (referencesAnyGui)
                    {
                        violations.Add($"BLL project '{bll.Name}' should not reference GUI projects (wrong dependency direction)");
                    }
                }

                if (violations.Any())
                {
                    details.AddRange(violations);
                    return new ArchitectureValidationResult(
                        false,
                        $"3-Layer architecture violations detected: {string.Join("; ", violations)}",
                        details
                    );
                }

                details.Add("✓ All projects follow correct 3-layer architecture pattern");
                return new ArchitectureValidationResult(true, "Valid 3-layer architecture", details);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error validating 3-layer architecture");
                return new ArchitectureValidationResult(
                    false,
                    $"Error during validation: {ex.Message}",
                    new List<string>()
                );
            }
        }

        private bool MatchesPattern(string projectName, string[] patterns)
        {
            var lowerName = projectName.ToLowerInvariant();
            return patterns.Any(pattern => lowerName.Contains(pattern));
        }

        private List<string> GetProjectReferences(string csprojPath)
        {
            var references = new List<string>();
            try
            {
                var doc = XDocument.Load(csprojPath);
                var projectReferences = doc.Descendants("ProjectReference");

                foreach (var projRef in projectReferences)
                {
                    var include = projRef.Attribute("Include")?.Value;
                    if (!string.IsNullOrEmpty(include))
                    {
                        // Extract project name from path like "..\DAL\DAL.csproj"
                        var refProjectName = Path.GetFileNameWithoutExtension(include);
                        references.Add(refProjectName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to parse project references from {File}", csprojPath);
            }

            return references;
        }
    }
}

