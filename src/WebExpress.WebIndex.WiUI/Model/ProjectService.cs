using WebExpress.WebIndex.WiUI.Localization;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace WebExpress.WebIndex.WiUI.Model
{
    /// <summary>
    /// Provides services for saving and loading projects.
    /// </summary>
    public class ProjectService
    {
        private static readonly string _appFolderName = "WebExpress.WebIndex.WiUI";
        private static readonly string _projectsFileName = "projects.xml";

        /// <summary>
        /// Returns the application data path for storing project files.
        /// </summary>
        /// <returns>The path to the application data directory.</returns>
        private static string GetAppDataPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var appFolder = Path.Combine(appDataPath, _appFolderName);
            Directory.CreateDirectory(appFolder);

            return appFolder;
        }

        /// <summary>
        /// Saves the collection of projects to an XML file in the application data directory.
        /// </summary>
        /// <param name="projects">The collection of projects to save.</param>
        /// <param name="filePath">An optional isolated settings file.</param>
        public static void SaveProjects(ObservableCollection<Project> projects, string? filePath = null)
        {
            filePath ??= Path.Combine(GetAppDataPath(), _projectsFileName);

            var serializer = new XmlSerializer(typeof(ObservableCollection<Project>));
            var temporary = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var writer = new StreamWriter(temporary))
                {
                    serializer.Serialize(writer, projects);
                }
                File.Move(temporary, filePath, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        /// <summary>
        /// Loads the collection of projects from an XML file in the application data directory.
        /// </summary>
        /// <returns>The collection of projects loaded from the XML file.</returns>
        /// <param name="filePath">An optional isolated settings file.</param>
        public static ObservableCollection<Project> LoadProjects(string? filePath = null)
        {
            filePath ??= Path.Combine(GetAppDataPath(), _projectsFileName);
            if (!File.Exists(filePath))
            {
                return [];
            }

            var serializer = new XmlSerializer(typeof(ObservableCollection<Project>));
            using StreamReader reader = new StreamReader(filePath);
            var result = serializer.Deserialize(reader) as ObservableCollection<Project>;

            return result ?? [];
        }

        /// <summary>Creates an independent workspace and optionally links a new sample index.</summary>
        /// <param name="name">The required display name.</param>
        /// <param name="sample">Whether to provision sample documents.</param>
        /// <param name="dataRoot">The parent directory for project-owned sample data.</param>
        /// <returns>The project ready to persist with the existing project collection.</returns>
        public static Project Create(string name, bool sample, string dataRoot)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(AppText.EnterAProjectName, nameof(name));
            var project = new Project { ProjectName = name.Trim(), IsDiscovered = true };
            if (sample)
            {
                var directory = Path.Combine(dataRoot, Guid.NewGuid().ToString("N"));
                var path = Explorer.ExplorerSample.Create(directory);
                project.IndexPath = directory;
                LinkIndex(project, path).Culture = "en";
            }
            return project;
        }

        /// <summary>Adds discovered schemas without removing durable links to absent files.</summary>
        /// <param name="project">The project receiving new references.</param>
        public static void Discover(Project project)
        {
            if (project.IsDiscovered || !Directory.Exists(project.IndexPath)) return;
            foreach (var path in Directory.EnumerateFiles(project.IndexPath!, "*.ws").Order()) LinkIndex(project, path);
            project.IsDiscovered = true;
        }

        /// <summary>Reuses canonical paths so repeated linking preserves document references.</summary>
        /// <param name="project">The owning project.</param>
        /// <param name="path">The schema file.</param>
        /// <returns>The existing or added reference.</returns>
        public static ProjectIndex LinkIndex(Project project, string path)
        {
            var fullPath = Path.GetFullPath(path);
            var link = project.Indexes.FirstOrDefault(x => string.Equals(x.Path, fullPath, StringComparison.OrdinalIgnoreCase));
            if (link != null) return link;
            link = new ProjectIndex { Path = fullPath, Name = Path.GetFileNameWithoutExtension(fullPath) };
            project.Indexes.Add(link);
            return link;
        }

        /// <summary>Keeps one durable reference per document identity within an index.</summary>
        /// <param name="index">The owning index reference.</param>
        /// <param name="id">The document identity.</param>
        /// <param name="name">The friendly label.</param>
        /// <returns>The existing or added reference.</returns>
        public static ProjectDocument LinkDocument(ProjectIndex index, Guid id, string name)
        {
            if (id == Guid.Empty) throw new ArgumentException(AppText.ADocumentIdentifierIsRequired, nameof(id));
            var link = index.Documents.FirstOrDefault(x => x.Id == id);
            if (link != null) return link;
            link = new ProjectDocument { Id = id, Name = name };
            index.Documents.Add(link);
            return link;
        }

        /// <summary>Requires both schema and document store before enabling a file reference.</summary>
        /// <param name="index">The reference to inspect.</param>
        /// <returns>Whether the required files currently exist.</returns>
        public static bool IsAvailable(ProjectIndex index) => File.Exists(index.Path) && File.Exists(Path.ChangeExtension(index.Path, ".wds"));
    }
}
