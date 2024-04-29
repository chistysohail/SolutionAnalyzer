using System;
using Microsoft.Build.Evaluation;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SolutionAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter the path to the folder containing your projects:");
            string folderPath = Console.ReadLine(); // User inputs the path to the folder

            if (!Directory.Exists(folderPath))
            {
                Console.WriteLine("Directory not found.");
                return;
            }

            var projectFiles = FindProjectFiles(folderPath);
            if (!projectFiles.Any())
            {
                Console.WriteLine("No project files found in the directory.");
                return;
            }

            List<Project> projects = LoadProjects(projectFiles);

            // Filter projects by Output Type
            var libraryProjects = projects.Where(p => p.GetPropertyValue("OutputType") == "Library");
            var exeProjects = projects.Where(p => p.GetPropertyValue("OutputType") == "Exe");

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string jsonResultFilePath = Path.Combine(folderPath, $"ProjectList_{timestamp}.json");

            var jsonProjects = new List<object>();

            foreach (var project in libraryProjects.Concat(exeProjects))
            {
                string projectName = Path.GetFileName(project.FullPath);
                // Get referenced projects
                var referencedProjects = project.GetItems("ProjectReference");

                var jsonRefs = referencedProjects.Select(refProject => new
                {
                    Path = refProject.EvaluatedInclude,
                    FileName = Path.GetFileName(refProject.EvaluatedInclude)
                }).ToList();

                jsonProjects.Add(new
                {
                    Path = project.FullPath,
                    FileName = projectName,
                    Type = project.GetPropertyValue("OutputType"),
                    References = jsonRefs
                });
            }

            string jsonString = JsonSerializer.Serialize(jsonProjects, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(jsonResultFilePath, jsonString);

            Console.WriteLine($"JSON results saved to: {jsonResultFilePath}");
            Console.WriteLine($"Total projects processed: {projectFiles.Count}");
        }

        static List<string> FindProjectFiles(string directoryPath)
        {
            // Recursively gets all files ending with .csproj in the directory
            var allProjectFiles = Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories);
            // Filter out any files or paths containing the word "Test"
            return allProjectFiles.Where(file => !file.Contains("Test", StringComparison.OrdinalIgnoreCase)).ToList();
        }

        static List<Project> LoadProjects(IEnumerable<string> projectFiles)
        {
            ProjectCollection projectCollection = new ProjectCollection();
            return projectFiles.Select(file => new Project(file, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports)).ToList();
        }
    }
}
