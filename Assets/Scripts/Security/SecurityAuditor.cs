using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Security auditor that scans the project for potential security vulnerabilities.
/// Runs automatically in the Unity Editor to detect common security issues.
/// </summary>
public static class SecurityAuditor
{
    private static readonly string[] SENSITIVE_PATTERNS = new string[]
    {
        @"(?i)(api[_-]?key|apikey)\s*[=:]\s*['""][^'""]{8,}['""]",
        @"(?i)(secret|password|token)\s*[=:]\s*['""][^'""]{6,}['""]",
        @"(?i)(private[_-]?key|privatekey)\s*[=:]\s*['""][^'""]{20,}['""]",
        @"(?i)(access[_-]?token|accesstoken)\s*[=:]\s*['""][^'""]{10,}['""]",
        @"(?i)(client[_-]?secret|clientsecret)\s*[=:]\s*['""][^'""]{10,}['""]",
        @"pk_[a-zA-Z0-9]{20,}",
        @"sk_[a-zA-Z0-9]{20,}",
        @"AIza[0-9A-Za-z\\-_]{35}",
        @"[0-9a-f]{32}",
        @"(?i)http://[^\s'""]+", // HTTP URLs (should be HTTPS)
    };

    private static readonly string[] EXCLUDED_EXTENSIONS = new string[]
    {
        ".meta", ".asset", ".prefab", ".unity", ".mat", ".anim", ".controller",
        ".dll", ".so", ".exe", ".png", ".jpg", ".jpeg", ".tga", ".psd",
        ".fbx", ".obj", ".dae", ".blend", ".wav", ".mp3", ".ogg"
    };

    [MenuItem("Tools/Security/Run Security Audit")]
    public static void RunSecurityAudit()
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, "Starting security audit...");
        
        var report = new SecurityAuditReport();
        
        // Scan all script files
        ScanScriptFiles(report);
        
        // Check Unity project settings
        CheckProjectSettings(report);
        
        // Check build settings
        CheckBuildSettings(report);
        
        // Generate final report
        LogAuditReport(report);
    }
    
    private static void ScanScriptFiles(SecurityAuditReport report)
    {
        string[] scriptFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        
        foreach (string filePath in scriptFiles)
        {
            if (ShouldSkipFile(filePath)) continue;
            
            try
            {
                string content = File.ReadAllText(filePath);
                ScanFileContent(filePath, content, report);
            }
            catch (System.Exception ex)
            {
                GameLogger.LogWarning(GameLogger.LogCategory.General, $"Failed to scan file {filePath}: {ex.Message}");
            }
        }
    }
    
    private static void ScanFileContent(string filePath, string content, SecurityAuditReport report)
    {
        string relativePath = filePath.Replace(Application.dataPath, "Assets");
        
        // Check for sensitive patterns
        foreach (string pattern in SENSITIVE_PATTERNS)
        {
            var matches = Regex.Matches(content, pattern);
            foreach (Match match in matches)
            {
                int lineNumber = GetLineNumber(content, match.Index);
                
                var issue = new SecurityIssue
                {
                    FilePath = relativePath,
                    LineNumber = lineNumber,
                    IssueType = GetIssueType(pattern),
                    Description = $"Potential sensitive data: {match.Value.Substring(0, System.Math.Min(match.Value.Length, 50))}...",
                    Severity = GetSeverity(pattern)
                };
                
                report.Issues.Add(issue);
            }
        }
        
        // Check for common security anti-patterns
        CheckSecurityAntiPatterns(relativePath, content, report);
    }
    
    private static void CheckSecurityAntiPatterns(string filePath, string content, SecurityAuditReport report)
    {
        // Check for Debug.Log with potentially sensitive data
        var debugLogMatches = Regex.Matches(content, @"Debug\.Log[^;]*(?:key|token|password|secret)[^;]*;", RegexOptions.IgnoreCase);
        foreach (Match match in debugLogMatches)
        {
            int lineNumber = GetLineNumber(content, match.Index);
            report.Issues.Add(new SecurityIssue
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                IssueType = "Debug Logging",
                Description = "Debug.Log statement may expose sensitive data",
                Severity = SecuritySeverity.Medium
            });
        }
        
        // Check for hardcoded IPs
        var ipMatches = Regex.Matches(content, @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b");
        foreach (Match match in ipMatches)
        {
            // Skip common safe IPs
            if (match.Value.StartsWith("127.") || match.Value.StartsWith("0.0.0.") || match.Value.StartsWith("255.255.255."))
                continue;
                
            int lineNumber = GetLineNumber(content, match.Index);
            report.Issues.Add(new SecurityIssue
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                IssueType = "Hardcoded IP",
                Description = $"Hardcoded IP address: {match.Value}",
                Severity = SecuritySeverity.Medium
            });
        }
        
        // Check for SQL injection patterns (if using databases)
        var sqlMatches = Regex.Matches(content, @"SELECT.*\+.*FROM|INSERT.*\+.*INTO|UPDATE.*\+.*SET", RegexOptions.IgnoreCase);
        foreach (Match match in sqlMatches)
        {
            int lineNumber = GetLineNumber(content, match.Index);
            report.Issues.Add(new SecurityIssue
            {
                FilePath = filePath,
                LineNumber = lineNumber,
                IssueType = "SQL Injection Risk",
                Description = "Potential SQL injection vulnerability",
                Severity = SecuritySeverity.High
            });
        }
    }
    
    private static void CheckProjectSettings(SecurityAuditReport report)
    {
        // Check if development build is disabled for release
        if (EditorUserBuildSettings.development)
        {
            report.Issues.Add(new SecurityIssue
            {
                FilePath = "Project Settings",
                LineNumber = 0,
                IssueType = "Build Configuration",
                Description = "Development build is enabled",
                Severity = SecuritySeverity.Low
            });
        }
        
        // Check script debugging
        if (EditorUserBuildSettings.allowDebugging)
        {
            report.Issues.Add(new SecurityIssue
            {
                FilePath = "Project Settings",
                LineNumber = 0,
                IssueType = "Build Configuration",
                Description = "Script debugging is enabled",
                Severity = SecuritySeverity.Medium
            });
        }
    }
    
    private static void CheckBuildSettings(SecurityAuditReport report)
    {
        // Check for scenes with test/debug in name in build settings
        var buildScenes = EditorBuildSettings.scenes;
        foreach (var scene in buildScenes)
        {
            if (scene.enabled && (scene.path.ToLower().Contains("test") || scene.path.ToLower().Contains("debug")))
            {
                report.Issues.Add(new SecurityIssue
                {
                    FilePath = "Build Settings",
                    LineNumber = 0,
                    IssueType = "Build Configuration",
                    Description = $"Test/Debug scene included in build: {scene.path}",
                    Severity = SecuritySeverity.Medium
                });
            }
        }
    }
    
    private static bool ShouldSkipFile(string filePath)
    {
        return EXCLUDED_EXTENSIONS.Any(ext => filePath.EndsWith(ext, System.StringComparison.OrdinalIgnoreCase)) ||
               filePath.Contains("/.git/") ||
               filePath.Contains("/Library/") ||
               filePath.Contains("/Temp/") ||
               filePath.Contains("/obj/") ||
               filePath.Contains("/bin/");
    }
    
    private static int GetLineNumber(string content, int index)
    {
        return content.Substring(0, index).Split('\n').Length;
    }
    
    private static string GetIssueType(string pattern)
    {
        if (pattern.Contains("api") || pattern.Contains("key")) return "API Key";
        if (pattern.Contains("secret")) return "Secret";
        if (pattern.Contains("password")) return "Password";
        if (pattern.Contains("token")) return "Token";
        if (pattern.Contains("http://")) return "Insecure HTTP";
        return "Sensitive Data";
    }
    
    private static SecuritySeverity GetSeverity(string pattern)
    {
        if (pattern.Contains("private") || pattern.Contains("secret")) return SecuritySeverity.High;
        if (pattern.Contains("password") || pattern.Contains("token")) return SecuritySeverity.High;
        if (pattern.Contains("http://")) return SecuritySeverity.Medium;
        return SecuritySeverity.Medium;
    }
    
    private static void LogAuditReport(SecurityAuditReport report)
    {
        GameLogger.LogInfo(GameLogger.LogCategory.General, $"Security audit complete. Found {report.Issues.Count} issues.");
        
        var groupedIssues = report.Issues.GroupBy(i => i.Severity).OrderByDescending(g => g.Key);
        
        foreach (var group in groupedIssues)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, $"{group.Key} Severity Issues: {group.Count()}");
            
            foreach (var issue in group.Take(10)) // Show first 10 of each severity
            {
                string logLevel = group.Key == SecuritySeverity.High ? "Error" :
                                 group.Key == SecuritySeverity.Medium ? "Warning" : "Info";
                                 
                string message = $"[{issue.IssueType}] {issue.FilePath}:{issue.LineNumber} - {issue.Description}";
                
                switch (group.Key)
                {
                    case SecuritySeverity.High:
                        GameLogger.LogError(GameLogger.LogCategory.General, message);
                        break;
                    case SecuritySeverity.Medium:
                        GameLogger.LogWarning(GameLogger.LogCategory.General, message);
                        break;
                    default:
                        GameLogger.LogInfo(GameLogger.LogCategory.General, message);
                        break;
                }
            }
            
            if (group.Count() > 10)
            {
                GameLogger.LogInfo(GameLogger.LogCategory.General, $"... and {group.Count() - 10} more {group.Key.ToString().ToLower()} severity issues");
            }
        }
        
        if (report.Issues.Count == 0)
        {
            GameLogger.LogInfo(GameLogger.LogCategory.General, "✓ No security issues found!");
        }
        else
        {
            int highCount = report.Issues.Count(i => i.Severity == SecuritySeverity.High);
            if (highCount > 0)
            {
                GameLogger.LogError(GameLogger.LogCategory.General, $"⚠️ {highCount} HIGH SEVERITY security issues require immediate attention!");
            }
        }
    }
}

public class SecurityAuditReport
{
    public List<SecurityIssue> Issues { get; set; } = new List<SecurityIssue>();
}

public class SecurityIssue
{
    public string FilePath { get; set; }
    public int LineNumber { get; set; }
    public string IssueType { get; set; }
    public string Description { get; set; }
    public SecuritySeverity Severity { get; set; }
}

public enum SecuritySeverity
{
    Low = 1,
    Medium = 2,
    High = 3
}

#endif