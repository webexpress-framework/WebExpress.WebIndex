using CommunityToolkit.Maui.Core.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using WebExpress.WebIndex.Storage;
using WebExpress.WebIndex.WiUI.Converters;

namespace WebExpress.WebIndex.WiUI.Model
{
    /// <summary>
    /// Represents the main view model that implements the INotifyPropertyChanged interface.
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private Project? _selectedProject = null;
        private ObservableCollection<Project> _projects = [];
        private Index? _selectedIndex = null;
        private ObservableCollection<Index> _indexes = [];
        private ObjectType? _selectedObjectType = null;
        private Field? _selectedIndexField = null;
        private Term? _selectedTerms = null;
        private ObservableCollection<Term> _terms = [];

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Manages the indexing of project objects.
        /// </summary>
        public static IndexManager? IndexManager { get; private set; }

        /// <summary>
        /// Returns or sets the selected project.
        /// </summary>
        /// <value>
        /// The selected project.
        /// </value>
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                _selectedProject = value; OnPropertyChanged();

                if (_selectedProject?.IndexPath != null && !Directory.Exists(_selectedProject?.IndexPath))
                {
                    Indexes = new ObservableCollection<Index>();

                    return;
                }

                Indexes = Directory
                   .GetFiles(_selectedProject?.IndexPath ?? Environment.CurrentDirectory)
                   .Where(x => x.EndsWith(".ws"))
                   .Select(x => new Index()
                   {
                       Name = Path.GetFileNameWithoutExtension(x),
                       FileNameWithPath = x
                   }).ToObservableCollection();
            }
        }

        /// <summary>
        /// Returns or sets the collection of projects.
        /// </summary>
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set { _projects = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Returns or sets the selected index.
        /// </summary>
        public Index? SelectedIndex
        {
            get => _selectedIndex;
            set { _selectedIndex = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Returns or sets the collection of indexes.
        /// </summary>
        public ObservableCollection<Index> Indexes
        {
            get => _indexes;
            set { _indexes = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Returns the selected field.
        /// </summary>
        /// <value>
        /// The selected field.
        /// </value>
        public Field? SelectedField
        {
            get => _selectedIndexField;
            set
            {
                _selectedIndexField = value;
                _terms = GetIndexTerms();
                OnPropertyChanged(nameof(Terms));
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Returns the currently selected index file.
        /// </summary>
        /// <value>
        /// The name of the currently selected index file, or <c>null</c> if no index file is selected.
        /// </value>
        public ObservableCollection<Field>? Fields
        {
            get { return _selectedObjectType?.Fields; }
        }

        /// <summary>
        /// Returns or sets the selected term.
        /// </summary>
        /// <value>
        /// The selected term.
        /// </value>
        public Term? SelectedTerm
        {
            get { return _selectedTerms; }
            set { _selectedTerms = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Returns the collection of terms.
        /// </summary>
        /// <value>
        /// The collection of terms.
        /// </value>
        public ObservableCollection<Term>? Terms
        {
            get { return _terms; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MainViewModel"/> class.
        /// </summary>
        public MainViewModel()
        {
            Projects = ProjectService.LoadProjects();

            SelectedProject = Projects.Where(x => x.IsSelected ?? false).FirstOrDefault();
            if (SelectedProject == null)
            {
                SelectedProject = Projects.FirstOrDefault();
            }
        }

        /// <summary>
        /// Opens the specified index file.
        /// </summary>
        /// <param name="indexFile">The full path to the index file.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool CreateIndexFile(string indexFile)
        {
            _selectedObjectType = new ObjectType() { Name = indexFile };
            var indexFilewithPath = Path.Combine(SelectedProject?.IndexPath!, $"{indexFile}.ws");

            var runtimeClass = _selectedObjectType.BuildRuntimeClass();
            var context = new IndexContext { IndexDirectory = indexFilewithPath };
            IndexManager = new IndexManager();

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManager).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(IndexManager, [context]);

            IndexManager.Create(runtimeClass, CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            OnPropertyChanged(nameof(Fields));

            return true;
        }

        /// <summary>
        /// Opens the specified index file.
        /// </summary>
        /// <param name="indexFile">The full path to the index file.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool OpenIndexFile(string? indexFile)
        {
            if (indexFile == null)
            {
                return false;
            }

            CloseIndexFile();

            var schema = File.ReadAllText(indexFile);
            var options = new JsonSerializerOptions { Converters = { new FieldTypeConverter() } };
            _selectedObjectType = JsonSerializer.Deserialize<ObjectType>(schema, options);

            var runtimeClass = _selectedObjectType?.BuildRuntimeClass();

            var context = new IndexContext { IndexDirectory = Path.GetDirectoryName(indexFile) };
            IndexManager = new IndexManager();

            // use reflection to call the protected Initialization method
            var method = typeof(IndexManager).GetMethod("Initialization", BindingFlags.Instance | BindingFlags.NonPublic);
            method?.Invoke(IndexManager, [context]);

            IndexManager.Create(runtimeClass!, CultureInfo.GetCultureInfo("en"), IndexType.Storage);

            OnPropertyChanged(nameof(Fields));

            return true;
        }

        /// <summary>
        /// Opens the specified index field.
        /// </summary>
        /// <param name="indexField">The the index field.</param>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool OpenIndexField(Field? indexField)
        {
            SelectedField = indexField;

            return true;
        }

        /// <summary>
        /// Close the current index file.
        /// </summary>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool CloseIndexFile()
        {
            var runtimeClass = _selectedObjectType?.BuildRuntimeClass();
            IndexManager?.Close(runtimeClass!);
            SelectedIndex = null;

            _selectedObjectType = null;
            OnPropertyChanged(nameof(Fields));

            return true;
        }

        /// <summary>
        /// Drop the current index file.
        /// </summary>
        /// <returns>True if successful, otherwise fasle.</returns>
        public bool DropIndexFile()
        {
            var runtimeClass = _selectedObjectType?.BuildRuntimeClass();
            IndexManager?.Drop(runtimeClass!);
            SelectedIndex = null;
            _selectedObjectType = null;

            OnPropertyChanged(nameof(Fields));

            return true;
        }

        /// <summary>
        /// Returns the index terms.
        /// </summary>
        /// <returns>The index terms</returns>
        public ObservableCollection<Term> GetIndexTerms()
        {
            var runtimeClass = _selectedObjectType?.BuildRuntimeClass();
            var document = IndexManager?.GetIndexDocument(runtimeClass!);
            var fieldProperty = runtimeClass?.GetProperty(_selectedIndexField?.Name!);
            var fieldData = new IndexFieldData(fieldProperty);
            var methodInfo = document?.GetType().GetMethod("GetReverseIndex");
            var reverseIndex = methodInfo?.Invoke(document, [fieldData]);
            var termProperty = reverseIndex?.GetType().GetProperty("Term");
            var term = termProperty?.GetValue(reverseIndex) as IndexStorageSegmentTerm;

            return term?.Terms.Select(x =>
            (
                new Term()
                {
                    Value = x.Item1,
                    Frequency = x.Item2.Frequency,
                    Height = x.Item2.Posting.Height,
                    Balance = x.Item2.Posting.Balance,
                    DocumentIDs = x.Item2.Posting.PreOrder.Select(y => y.DocumentID)
                }
            )).ToObservableCollection()!;
        }

        /// <summary>
        /// Raises the PropertyChanged event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
