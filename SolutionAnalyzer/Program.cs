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
                writer.WriteLine("Project Path,Project Type");
                foreach (var project in libraryProjects)
                {
                    writer.WriteLine($"\"{project.FullPath}\",Library");
                }

                foreach (var project in exeProjects)
                {
                    writer.WriteLine($"\"{project.FullPath}\",Executable");
                    // Get referenced projects
                    var referencedProjects = project.GetItems("ProjectReference");
                    foreach (var refProject in referencedProjects)
                    {
                        writer.WriteLine($",Referenced Project,\"{refProject.EvaluatedInclude}\"");
                    }
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
            return Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories).ToList();
        }

        static List<Project> LoadProjects(IEnumerable<string> projectFiles)
        {
            ProjectCollection projectCollection = new ProjectCollection();
            return projectFiles.Select(file => new Project(file, null, null, projectCollection, ProjectLoadSettings.IgnoreMissingImports)).ToList();
        }
    }
}
