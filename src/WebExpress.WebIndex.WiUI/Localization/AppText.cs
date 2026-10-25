using System.Globalization;
using System.Resources;

namespace WebExpress.WebIndex.WiUI.Localization;

/// <summary>Resolves desktop text using the display culture without changing index or query semantics.</summary>
public static class AppText
{
    /// <summary>Gets the optional sample data setting for new projects.</summary>
    public static string IncludeSampleData => Get("IncludeSampleData");
    private static readonly ResourceManager _resources = new("WebExpress.WebIndex.WiUI.Localization.Strings", typeof(AppText).Assembly);

    /// <summary>Uses the neutral English resource when no matching display translation exists.</summary>
    /// <param name="key">The stable resource key.</param>
    /// <returns>The localized text.</returns>
    public static string Get(string key) => _resources.GetString(key, CultureInfo.CurrentUICulture)
        ?? throw new MissingManifestResourceException($"Missing UI resource: {key}.");

    /// <summary>Formats a complete translated message while retaining typed placeholder arguments.</summary>
    /// <param name="key">The resource containing format placeholders.</param>
    /// <param name="arguments">The values inserted into the translated message.</param>
    /// <returns>The formatted display text.</returns>
    public static string Format(string key, params object?[] arguments) => string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    /// <summary>Resolves parser message keys while preserving external diagnostic details.</summary>
    /// <param name="message">The parser key or an already readable diagnostic.</param>
    /// <returns>The localized diagnostic, or the original external message.</returns>
    public static string Diagnostic(string message) => _resources.GetString(message, CultureInfo.CurrentUICulture) ?? message;

    /// <summary>Selects the singular or plural count form supported by the desktop languages.</summary>
    /// <param name="kind">The stable Documents, Schema, Terms or Links category.</param>
    /// <param name="count">The displayed count.</param>
    /// <returns>The translated count phrase.</returns>
    public static string Count(string kind, long count) => Format(kind + "Count" + (count == 1 ? "One" : "Many"), count);

    /// <summary>Gets the localized Cancel text.</summary>
    public static string Cancel => Get("Cancel");

    /// <summary>Gets the localized WQLHelp text.</summary>
    public static string WQLHelp => Get("WQLHelp");

    /// <summary>Gets the localized DocumentsWQLSchemaReverseIndexes text.</summary>
    public static string DocumentsWQLSchemaReverseIndexes => Get("DocumentsWQLSchemaReverseIndexes");

    /// <summary>Gets the localized PROJECTEXPLORER text.</summary>
    public static string PROJECTEXPLORER => Get("PROJECTEXPLORER");

    /// <summary>Gets the localized Manage text.</summary>
    public static string Manage => Get("Manage");

    /// <summary>Gets the localized NewProjectButton text.</summary>
    public static string NewProjectButton => Get("NewProjectButton");

    /// <summary>Gets the localized OpenLink text.</summary>
    public static string OpenLink => Get("OpenLink");

    /// <summary>Gets the localized Sample text.</summary>
    public static string Sample => Get("Sample");

    /// <summary>Gets the localized SearchProjectTree text.</summary>
    public static string SearchProjectTree => Get("SearchProjectTree");

    /// <summary>Gets the localized LinkAnIndexOrCreateAProject text.</summary>
    public static string LinkAnIndexOrCreateAProject => Get("LinkAnIndexOrCreateAProject");

    /// <summary>Gets the localized IndexLanguage text.</summary>
    public static string IndexLanguage => Get("IndexLanguage");

    /// <summary>Gets the localized Refresh text.</summary>
    public static string Refresh => Get("Refresh");

    /// <summary>Gets the localized Close text.</summary>
    public static string Close => Get("Close");

    /// <summary>Gets the localized WelcomeToYourIndexes text.</summary>
    public static string WelcomeToYourIndexes => Get("WelcomeToYourIndexes");

    /// <summary>Gets the localized DocumentsSchemaAndReverseTermsInOneWorkspace text.</summary>
    public static string DocumentsSchemaAndReverseTermsInOneWorkspace => Get("DocumentsSchemaAndReverseTermsInOneWorkspace");

    /// <summary>Gets the localized EnableEditing text.</summary>
    public static string EnableEditing => Get("EnableEditing");

    /// <summary>Gets the localized WQLQUERY text.</summary>
    public static string WQLQUERY => Get("WQLQUERY");

    /// <summary>Gets the localized Clear text.</summary>
    public static string Clear => Get("Clear");

    /// <summary>Gets the localized Save text.</summary>
    public static string Save => Get("Save");

    /// <summary>Gets the localized RunQuery text.</summary>
    public static string RunQuery => Get("RunQuery");

    /// <summary>Gets the localized LeaveBlankToBrowseAllDocuments text.</summary>
    public static string LeaveBlankToBrowseAllDocuments => Get("LeaveBlankToBrowseAllDocuments");

    /// <summary>Gets the localized QueryEditorDescription text.</summary>
    public static string QueryEditorDescription => Get("QueryEditorDescription");

    /// <summary>Gets the localized OpenAnIndexToStartSearching text.</summary>
    public static string OpenAnIndexToStartSearching => Get("OpenAnIndexToStartSearching");

    /// <summary>Gets the localized HistoryAndSavedQueries text.</summary>
    public static string HistoryAndSavedQueries => Get("HistoryAndSavedQueries");

    /// <summary>Gets the localized Remove text.</summary>
    public static string Remove => Get("Remove");

    /// <summary>Gets the localized Documents text.</summary>
    public static string Documents => Get("Documents");

    /// <summary>Gets the localized Schema text.</summary>
    public static string Schema => Get("Schema");

    /// <summary>Gets the localized Terms text.</summary>
    public static string Terms => Get("Terms");

    /// <summary>Gets the localized New text.</summary>
    public static string New => Get("New");

    /// <summary>Gets the localized FilterLoadedRows text.</summary>
    public static string FilterLoadedRows => Get("FilterLoadedRows");

    /// <summary>Gets the localized SelectAFieldForTerms text.</summary>
    public static string SelectAFieldForTerms => Get("SelectAFieldForTerms");

    /// <summary>Gets the localized StartExploring text.</summary>
    public static string StartExploring => Get("StartExploring");

    /// <summary>Gets the localized ChooseAnIndexOnTheLeftToBrowseItsDocuments text.</summary>
    public static string ChooseAnIndexOnTheLeftToBrowseItsDocuments => Get("ChooseAnIndexOnTheLeftToBrowseItsDocuments");

    /// <summary>Gets the localized NoIndexSelected text.</summary>
    public static string NoIndexSelected => Get("NoIndexSelected");

    /// <summary>Gets the localized RowsPerPage text.</summary>
    public static string RowsPerPage => Get("RowsPerPage");

    /// <summary>Gets the localized Previous text.</summary>
    public static string Previous => Get("Previous");

    /// <summary>Gets the localized Next text.</summary>
    public static string Next => Get("Next");

    /// <summary>Gets the localized SelectARowToInspectItsFullValues text.</summary>
    public static string SelectARowToInspectItsFullValues => Get("SelectARowToInspectItsFullValues");

    /// <summary>Gets the localized LinkToProject text.</summary>
    public static string LinkToProject => Get("LinkToProject");

    /// <summary>Gets the localized DetailsCopy text.</summary>
    public static string DetailsCopy => Get("DetailsCopy");

    /// <summary>Gets the localized EditDocument text.</summary>
    public static string EditDocument => Get("EditDocument");

    /// <summary>Gets the localized Delete text.</summary>
    public static string Delete => Get("Delete");

    /// <summary>Gets the localized Ready text.</summary>
    public static string Ready => Get("Ready");

    /// <summary>Gets the localized Projects text.</summary>
    public static string Projects => Get("Projects");

    /// <summary>Gets the localized ProjectsHeading text.</summary>
    public static string ProjectsHeading => Get("ProjectsHeading");

    /// <summary>Gets the localized AddProjectButton text.</summary>
    public static string AddProjectButton => Get("AddProjectButton");

    /// <summary>Gets the localized BackToIndexExplorer text.</summary>
    public static string BackToIndexExplorer => Get("BackToIndexExplorer");

    /// <summary>Gets the localized CreateAProjectToGroupIndexFilesAndDurableDocumentLinks text.</summary>
    public static string CreateAProjectToGroupIndexFilesAndDurableDocumentLinks => Get("CreateAProjectToGroupIndexFilesAndDurableDocumentLinks");

    /// <summary>Gets the localized ProjectProperties text.</summary>
    public static string ProjectProperties => Get("ProjectProperties");

    /// <summary>Gets the localized ProjectPropertiesDescription text.</summary>
    public static string ProjectPropertiesDescription => Get("ProjectPropertiesDescription");

    /// <summary>Gets the localized Name text.</summary>
    public static string Name => Get("Name");

    /// <summary>Gets the localized MyIndexes text.</summary>
    public static string MyIndexes => Get("MyIndexes");

    /// <summary>Gets the localized DiscoveryFolderOptional text.</summary>
    public static string DiscoveryFolderOptional => Get("DiscoveryFolderOptional");

    /// <summary>Gets the localized FolderContainingIndexSchemas text.</summary>
    public static string FolderContainingIndexSchemas => Get("FolderContainingIndexSchemas");

    /// <summary>Gets the localized Browse text.</summary>
    public static string Browse => Get("Browse");

    /// <summary>Gets the localized DiscoveryFolderDescription text.</summary>
    public static string DiscoveryFolderDescription => Get("DiscoveryFolderDescription");

    /// <summary>Gets the localized SaveProject text.</summary>
    public static string SaveProject => Get("SaveProject");

    /// <summary>Gets the localized RemoveProject text.</summary>
    public static string RemoveProject => Get("RemoveProject");

    /// <summary>Gets the localized LinkedIndexes text.</summary>
    public static string LinkedIndexes => Get("LinkedIndexes");

    /// <summary>Gets the localized LinkIndex text.</summary>
    public static string LinkIndex => Get("LinkIndex");

    /// <summary>Gets the localized ProjectLinksDescription text.</summary>
    public static string ProjectLinksDescription => Get("ProjectLinksDescription");

    /// <summary>Gets the localized SelectAProjectToEditItsProperties text.</summary>
    public static string SelectAProjectToEditItsProperties => Get("SelectAProjectToEditItsProperties");

    /// <summary>Gets the localized SaveDocument text.</summary>
    public static string SaveDocument => Get("SaveDocument");

    /// <summary>Gets the localized DocumentEditorDescription text.</summary>
    public static string DocumentEditorDescription => Get("DocumentEditorDescription");

    /// <summary>Gets the localized EditAsJSON text.</summary>
    public static string EditAsJSON => Get("EditAsJSON");

    /// <summary>Gets the localized JSONModeReopenEditorForFields text.</summary>
    public static string JSONModeReopenEditorForFields => Get("JSONModeReopenEditorForFields");

    /// <summary>Gets the localized Saving text.</summary>
    public static string Saving => Get("Saving");

    /// <summary>Gets the localized DiscardChanges text.</summary>
    public static string DiscardChanges => Get("DiscardChanges");

    /// <summary>Gets the localized LeaveWithoutSavingThisDocument text.</summary>
    public static string LeaveWithoutSavingThisDocument => Get("LeaveWithoutSavingThisDocument");

    /// <summary>Gets the localized Discard text.</summary>
    public static string Discard => Get("Discard");

    /// <summary>Gets the localized KeepEditing text.</summary>
    public static string KeepEditing => Get("KeepEditing");

    /// <summary>Gets the localized PreparingSampleIndex text.</summary>
    public static string PreparingSampleIndex => Get("PreparingSampleIndex");

    /// <summary>Gets the localized ValidWQLDisplayedResult text.</summary>
    public static string ValidWQLDisplayedResult => Get("ValidWQLDisplayedResult");

    /// <summary>Gets the localized ValidWQLRunToUpdateResults text.</summary>
    public static string ValidWQLRunToUpdateResults => Get("ValidWQLRunToUpdateResults");

    /// <summary>Gets the localized LoadingReverseTerms text.</summary>
    public static string LoadingReverseTerms => Get("LoadingReverseTerms");

    /// <summary>Gets the localized LimitedTo10000Terms text.</summary>
    public static string LimitedTo10000Terms => Get("LimitedTo10000Terms");

    /// <summary>Gets the localized ExportingLoadedRows text.</summary>
    public static string ExportingLoadedRows => Get("ExportingLoadedRows");

    /// <summary>Gets the localized NoLoadedRowsToExport text.</summary>
    public static string NoLoadedRowsToExport => Get("NoLoadedRowsToExport");

    /// <summary>Gets the localized ExportCanceled text.</summary>
    public static string ExportCanceled => Get("ExportCanceled");

    /// <summary>Gets the localized NewDocument text.</summary>
    public static string NewDocument => Get("NewDocument");

    /// <summary>Gets the localized RefreshingAfterSave text.</summary>
    public static string RefreshingAfterSave => Get("RefreshingAfterSave");

    /// <summary>Gets the localized OpenIndexSchema text.</summary>
    public static string OpenIndexSchema => Get("OpenIndexSchema");

    /// <summary>Gets the localized ExecutingWQL text.</summary>
    public static string ExecutingWQL => Get("ExecutingWQL");

    /// <summary>Gets the localized RefreshingProjects text.</summary>
    public static string RefreshingProjects => Get("RefreshingProjects");

    /// <summary>Gets the localized ClosingIndex text.</summary>
    public static string ClosingIndex => Get("ClosingIndex");

    /// <summary>Gets the localized IndexClosed text.</summary>
    public static string IndexClosed => Get("IndexClosed");

    /// <summary>Gets the localized EnableEditingConfirmation text.</summary>
    public static string EnableEditingConfirmation => Get("EnableEditingConfirmation");

    /// <summary>Gets the localized DeleteDocument text.</summary>
    public static string DeleteDocument => Get("DeleteDocument");

    /// <summary>Gets the localized DeletingDocument text.</summary>
    public static string DeletingDocument => Get("DeletingDocument");

    /// <summary>Gets the localized DocumentDeletedResultsAndReverseIndexesUpdated text.</summary>
    public static string DocumentDeletedResultsAndReverseIndexesUpdated => Get("DocumentDeletedResultsAndReverseIndexesUpdated");

    /// <summary>Gets the localized RowDetails text.</summary>
    public static string RowDetails => Get("RowDetails");

    /// <summary>Gets the localized CopyFullJSON text.</summary>
    public static string CopyFullJSON => Get("CopyFullJSON");

    /// <summary>Gets the localized Copied text.</summary>
    public static string Copied => Get("Copied");

    /// <summary>Gets the localized EnterValidWQLBeforeSavingIt text.</summary>
    public static string EnterValidWQLBeforeSavingIt => Get("EnterValidWQLBeforeSavingIt");

    /// <summary>Gets the localized QuerySavedToFavorites text.</summary>
    public static string QuerySavedToFavorites => Get("QuerySavedToFavorites");

    /// <summary>Gets the localized WQLQuickReference text.</summary>
    public static string WQLQuickReference => Get("WQLQuickReference");

    /// <summary>Gets the localized GotIt text.</summary>
    public static string GotIt => Get("GotIt");

    /// <summary>Gets the localized ProjectLabel text.</summary>
    public static string ProjectLabel => Get("ProjectLabel");

    /// <summary>Gets the localized LinkedIndexesFromMultipleFolders text.</summary>
    public static string LinkedIndexesFromMultipleFolders => Get("LinkedIndexesFromMultipleFolders");

    /// <summary>Gets the localized Missing text.</summary>
    public static string Missing => Get("Missing");

    /// <summary>Gets the localized Unavailable text.</summary>
    public static string Unavailable => Get("Unavailable");

    /// <summary>Gets the localized Unchecked text.</summary>
    public static string Unchecked => Get("Unchecked");

    /// <summary>Gets the localized NoProjectsYetCreateAProjectThenLinkIndexesAndDocuments text.</summary>
    public static string NoProjectsYetCreateAProjectThenLinkIndexesAndDocuments => Get("NoProjectsYetCreateAProjectThenLinkIndexesAndDocuments");

    /// <summary>Gets the localized ExpandOrCollapse text.</summary>
    public static string ExpandOrCollapse => Get("ExpandOrCollapse");

    /// <summary>Gets the localized ManageLink text.</summary>
    public static string ManageLink => Get("ManageLink");

    /// <summary>Gets the localized OpeningLinkedDocument text.</summary>
    public static string OpeningLinkedDocument => Get("OpeningLinkedDocument");

    /// <summary>Gets the localized NewProjectTitle text.</summary>
    public static string NewProjectTitle => Get("NewProjectTitle");

    /// <summary>Gets the localized ProjectName text.</summary>
    public static string ProjectName => Get("ProjectName");

    /// <summary>Gets the localized CreateProject text.</summary>
    public static string CreateProject => Get("CreateProject");

    /// <summary>Gets the localized NameTheProjectForThisDocument text.</summary>
    public static string NameTheProjectForThisDocument => Get("NameTheProjectForThisDocument");

    /// <summary>Gets the localized RenameLink text.</summary>
    public static string RenameLink => Get("RenameLink");

    /// <summary>Gets the localized LocateIndex text.</summary>
    public static string LocateIndex => Get("LocateIndex");

    /// <summary>Gets the localized RemoveLink text.</summary>
    public static string RemoveLink => Get("RemoveLink");

    /// <summary>Gets the localized DisplayName text.</summary>
    public static string DisplayName => Get("DisplayName");

    /// <summary>Gets the localized ThisIndexIsAlreadyLinkedToTheProject text.</summary>
    public static string ThisIndexIsAlreadyLinkedToTheProject => Get("ThisIndexIsAlreadyLinkedToTheProject");

    /// <summary>Gets the localized RemoveIndexLink text.</summary>
    public static string RemoveIndexLink => Get("RemoveIndexLink");

    /// <summary>Gets the localized RemoveIndexConfirmation text.</summary>
    public static string RemoveIndexConfirmation => Get("RemoveIndexConfirmation");

    /// <summary>Gets the localized RefreshingLinks text.</summary>
    public static string RefreshingLinks => Get("RefreshingLinks");

    /// <summary>Gets the localized SettingsCouldNotBeLoaded text.</summary>
    public static string SettingsCouldNotBeLoaded => Get("SettingsCouldNotBeLoaded");

    /// <summary>Gets the localized CheckingProjectLinks text.</summary>
    public static string CheckingProjectLinks => Get("CheckingProjectLinks");

    /// <summary>Gets the localized OperationCanceled text.</summary>
    public static string OperationCanceled => Get("OperationCanceled");

    /// <summary>Gets the localized IndexUnavailableCloseOtherApplicationsUsingItAndRetry text.</summary>
    public static string IndexUnavailableCloseOtherApplicationsUsingItAndRetry => Get("IndexUnavailableCloseOtherApplicationsUsingItAndRetry");

    /// <summary>Gets the localized OpenAWsSchemaFileToBegin text.</summary>
    public static string OpenAWsSchemaFileToBegin => Get("OpenAWsSchemaFileToBegin");

    /// <summary>Gets the localized OpeningIndex text.</summary>
    public static string OpeningIndex => Get("OpeningIndex");

    /// <summary>Gets the localized QueryNotExecutedPreviousResultsRemainDisplayed text.</summary>
    public static string QueryNotExecutedPreviousResultsRemainDisplayed => Get("QueryNotExecutedPreviousResultsRemainDisplayed");

    /// <summary>Gets the localized LimitedTo10000RowsRefineWQLOrUseSkipTake text.</summary>
    public static string LimitedTo10000RowsRefineWQLOrUseSkipTake => Get("LimitedTo10000RowsRefineWQLOrUseSkipTake");

    /// <summary>Gets the localized HistoryCouldNotBeSaved text.</summary>
    public static string HistoryCouldNotBeSaved => Get("HistoryCouldNotBeSaved");

    /// <summary>Gets the localized OpenAnIndexToBrowseItsDocuments text.</summary>
    public static string OpenAnIndexToBrowseItsDocuments => Get("OpenAnIndexToBrowseItsDocuments");

    /// <summary>Gets the localized EditingEnabledExclusiveAccess text.</summary>
    public static string EditingEnabledExclusiveAccess => Get("EditingEnabledExclusiveAccess");

    /// <summary>Gets the localized ReadSnapshot text.</summary>
    public static string ReadSnapshot => Get("ReadSnapshot");

    /// <summary>Gets the localized FinishEditing text.</summary>
    public static string FinishEditing => Get("FinishEditing");

    /// <summary>Gets the localized NoRowsToDisplay text.</summary>
    public static string NoRowsToDisplay => Get("NoRowsToDisplay");

    /// <summary>Gets the localized TermsEmptyDescription text.</summary>
    public static string TermsEmptyDescription => Get("TermsEmptyDescription");

    /// <summary>Gets the localized TryAnotherQueryOrClearTheLoadedRowFilter text.</summary>
    public static string TryAnotherQueryOrClearTheLoadedRowFilter => Get("TryAnotherQueryOrClearTheLoadedRowFilter");

    /// <summary>Gets the localized SortLoadedRowsBy text.</summary>
    public static string SortLoadedRowsBy => Get("SortLoadedRowsBy");

    /// <summary>Gets the localized SelectRow text.</summary>
    public static string SelectRow => Get("SelectRow");

    /// <summary>Gets the localized First10000 text.</summary>
    public static string First10000 => Get("First10000");

    /// <summary>Gets the localized Selected text.</summary>
    public static string Selected => Get("Selected");

    /// <summary>Gets the localized ChooseAnIndexSchema text.</summary>
    public static string ChooseAnIndexSchema => Get("ChooseAnIndexSchema");

    /// <summary>Gets the localized EnterAProjectName text.</summary>
    public static string EnterAProjectName => Get("EnterAProjectName");

    /// <summary>Gets the localized ChooseAnExistingDiscoveryFolderOrLeaveItBlank text.</summary>
    public static string ChooseAnExistingDiscoveryFolderOrLeaveItBlank => Get("ChooseAnExistingDiscoveryFolderOrLeaveItBlank");

    /// <summary>Gets the localized ProjectSavedLinksAreAvailableInTheIndexExplorerTree text.</summary>
    public static string ProjectSavedLinksAreAvailableInTheIndexExplorerTree => Get("ProjectSavedLinksAreAvailableInTheIndexExplorerTree");

    /// <summary>Gets the localized RemoveProjectConfirmation text.</summary>
    public static string RemoveProjectConfirmation => Get("RemoveProjectConfirmation");

    /// <summary>Gets the localized RemoveShortcut text.</summary>
    public static string RemoveShortcut => Get("RemoveShortcut");

    /// <summary>Gets the localized ProjectShortcutRemoved text.</summary>
    public static string ProjectShortcutRemoved => Get("ProjectShortcutRemoved");

    /// <summary>Gets the localized Locate text.</summary>
    public static string Locate => Get("Locate");

    /// <summary>Gets the localized ThisIndexIsAlreadyLinked text.</summary>
    public static string ThisIndexIsAlreadyLinked => Get("ThisIndexIsAlreadyLinked");

    /// <summary>Gets the localized Unlink text.</summary>
    public static string Unlink => Get("Unlink");

    /// <summary>Gets the localized UnlinkIndexConfirmation text.</summary>
    public static string UnlinkIndexConfirmation => Get("UnlinkIndexConfirmation");

    /// <summary>Gets the localized NoIndexesLinkedAddASchemaFileToGetStarted text.</summary>
    public static string NoIndexesLinkedAddASchemaFileToGetStarted => Get("NoIndexesLinkedAddASchemaFileToGetStarted");

    /// <summary>Gets the localized LinkIndexSchema text.</summary>
    public static string LinkIndexSchema => Get("LinkIndexSchema");

    /// <summary>Gets the localized UnsavedProjectProperties text.</summary>
    public static string UnsavedProjectProperties => Get("UnsavedProjectProperties");

    /// <summary>Gets the localized DiscardTheUnsavedProjectNameOrFolderChanges text.</summary>
    public static string DiscardTheUnsavedProjectNameOrFolderChanges => Get("DiscardTheUnsavedProjectNameOrFolderChanges");

    /// <summary>Gets the localized TheSchemaTypeDoesNotMatchItsFileName text.</summary>
    public static string TheSchemaTypeDoesNotMatchItsFileName => Get("TheSchemaTypeDoesNotMatchItsFileName");

    /// <summary>Gets the localized TheDocumentStoreWdsIsMissing text.</summary>
    public static string TheDocumentStoreWdsIsMissing => Get("TheDocumentStoreWdsIsMissing");

    /// <summary>Gets the localized EnableEditingBeforeChangingDocuments text.</summary>
    public static string EnableEditingBeforeChangingDocuments => Get("EnableEditingBeforeChangingDocuments");

    /// <summary>Gets the localized SchemaMismatchError text.</summary>
    public static string SchemaMismatchError => Get("SchemaMismatchError");

    /// <summary>Gets the localized ADocumentObjectIsRequired text.</summary>
    public static string ADocumentObjectIsRequired => Get("ADocumentObjectIsRequired");

    /// <summary>Gets the localized IdMustNotBeEmpty text.</summary>
    public static string IdMustNotBeEmpty => Get("IdMustNotBeEmpty");

    /// <summary>Gets the localized AnExistingDocumentSIdCannotBeChanged text.</summary>
    public static string AnExistingDocumentSIdCannotBeChanged => Get("AnExistingDocumentSIdCannotBeChanged");

    /// <summary>Gets the localized TheDocumentNoLongerExistsRefreshTheIndex text.</summary>
    public static string TheDocumentNoLongerExistsRefreshTheIndex => Get("TheDocumentNoLongerExistsRefreshTheIndex");

    /// <summary>Gets the localized ADocumentWithThisIdAlreadyExists text.</summary>
    public static string ADocumentWithThisIdAlreadyExists => Get("ADocumentWithThisIdAlreadyExists");

    /// <summary>Gets the localized AJSONObjectIsRequired text.</summary>
    public static string AJSONObjectIsRequired => Get("AJSONObjectIsRequired");

    /// <summary>Gets the localized DuplicateJSONPropertiesAreNotAllowed text.</summary>
    public static string DuplicateJSONPropertiesAreNotAllowed => Get("DuplicateJSONPropertiesAreNotAllowed");

    /// <summary>Gets the localized TheDocumentNoLongerExists text.</summary>
    public static string TheDocumentNoLongerExists => Get("TheDocumentNoLongerExists");

    /// <summary>Gets the localized TheSchemaMustContainTypeAndFields text.</summary>
    public static string TheSchemaMustContainTypeAndFields => Get("TheSchemaMustContainTypeAndFields");

    /// <summary>Gets the localized TheSchemaMustContainAGuidFieldNamedId text.</summary>
    public static string TheSchemaMustContainAGuidFieldNamedId => Get("TheSchemaMustContainAGuidFieldNamedId");

    /// <summary>Gets the localized ADocumentIdentifierIsRequired text.</summary>
    public static string ADocumentIdentifierIsRequired => Get("ADocumentIdentifierIsRequired");

    /// <summary>Gets the localized InvalidField text.</summary>
    public static string InvalidField => Get("InvalidField");

    /// <summary>Gets the localized QueryError text.</summary>
    public static string QueryError => Get("QueryError");

    /// <summary>Gets the localized TermCount text.</summary>
    public static string TermCount => Get("TermCount");

    /// <summary>Gets the localized ExportComplete text.</summary>
    public static string ExportComplete => Get("ExportComplete");

    /// <summary>Gets the localized ConfirmDelete text.</summary>
    public static string ConfirmDelete => Get("ConfirmDelete");

    /// <summary>Gets the localized DocumentLinked text.</summary>
    public static string DocumentLinked => Get("DocumentLinked");

    /// <summary>Gets the localized LocateFile text.</summary>
    public static string LocateFile => Get("LocateFile");

    /// <summary>Gets the localized QueryResult text.</summary>
    public static string QueryResult => Get("QueryResult");

    /// <summary>Gets the localized IndexSummary text.</summary>
    public static string IndexSummary => Get("IndexSummary");

    /// <summary>Gets the localized DocumentLinks text.</summary>
    public static string DocumentLinks => Get("DocumentLinks");

    /// <summary>Gets the localized RequiredProperty text.</summary>
    public static string RequiredProperty => Get("RequiredProperty");

    /// <summary>Gets the localized InvalidSchemaField text.</summary>
    public static string InvalidSchemaField => Get("InvalidSchemaField");

    /// <summary>Gets the localized ConflictingPaths text.</summary>
    public static string ConflictingPaths => Get("ConflictingPaths");

    /// <summary>Gets the localized UnsupportedType text.</summary>
    public static string UnsupportedType => Get("UnsupportedType");

    /// <summary>Gets the localized WqlHelpBody text.</summary>
    public static string WqlHelpBody => Get("WqlHelpBody");
}
