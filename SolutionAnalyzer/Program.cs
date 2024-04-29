using System;
using Microsoft.Build.Evaluation;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            string resultFilePath = Path.Combine(folderPath, $"ProjectList_{timestamp}.csv");

            using (StreamWriter writer = new StreamWriter(resultFilePath))
            {
                writer.WriteLine("Project Path,Project File Name,Project Type,Referenced Projects");
                foreach (var project in libraryProjects.Concat(exeProjects))
                {
                    var referencedProjects = project.GetItems("ProjectReference");
                    string references = String.Join(", ", referencedProjects.Select(r => r.EvaluatedInclude));
                    string projectName = Path.GetFileName(project.FullPath);
                    writer.WriteLine($"\"{project.FullPath}\",\"{projectName}\",\"{(project.GetPropertyValue("OutputType") == "Library" ? "Library" : "Executable")}\",\"{references}\"");
                }

                // Summary at the end of the CSV
                writer.WriteLine("\nSummary,");
                writer.WriteLine($"Total Projects:,{projectFiles.Count}");
                writer.WriteLine($"Total Library Projects:,{libraryProjects.Count()}");
                writer.WriteLine($"Total Executable Projects:,{exeProjects.Count()}");
            }

            Console.WriteLine($"Results saved to: {resultFilePath}");
            Console.WriteLine($"Total projects processed: {projectFiles.Count}");
            Console.WriteLine($"Total library projects: {libraryProjects.Count()}");
            Console.WriteLine($"Total executable projects: {exeProjects.Count()}");
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
