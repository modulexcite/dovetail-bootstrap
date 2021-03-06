using System;
using System.Collections.Generic;
using Dovetail.SDK.Bootstrap.History.Parser;
using FChoice.Foundation.Clarify;
using FubuCore;
using FubuLocalization;

namespace Dovetail.SDK.Bootstrap.History.Configuration
{
	public class ActEntryTemplate
	{
		public ActEntryTemplate(IHistoryOutputParser encoder)
		{
			HTMLizer = item =>
			{
				item.Detail = encoder.Encode(item.Detail);
				item.Internal = encoder.Encode(item.Internal);
			};
			RelatedGenericFields = new string[0];
		}

		public ActEntryTemplate(ActEntryTemplate input)
		{
			Code = input.Code;
			DisplayName = input.DisplayName;
			ActivityDTOUpdater = input.ActivityDTOUpdater;
			ActivityDTOEditor = input.ActivityDTOEditor;

			RelatedGenericRelationName = input.RelatedGenericRelationName;
			RelatedGenericAction = input.RelatedGenericAction;
			RelatedGenericFields = input.RelatedGenericFields;
			HTMLizer = input.HTMLizer;
		}

		public int Code { get; set; }
		public bool IsVerbose { get; set; }
		public StringToken DisplayName { get; set; }
		public Action<ClarifyDataRow, HistoryItem, ActEntryTemplate> ActivityDTOUpdater;
		public Action<HistoryItem> ActivityDTOEditor { get; set; }

		public string RelatedGenericRelationName { get; set; }
		public Action<ClarifyGeneric> RelatedGenericAction { get; set; }
		public string[] RelatedGenericFields { get; set; }

		public Action<HistoryItem> HTMLizer { get; set; }

		public override string ToString()
		{
			var header = "{0} : {1} ".ToFormat(Code, DisplayName);
			var relatedGenericAction = "Related Generic Realation : {0}".ToFormat(RelatedGenericRelationName) + ((RelatedGenericAction == null) ? "" : " (has related generic action)");
			var therest = @"Related Generic:{0}
Has Updater: {1}
Has Editor: {2}
".ToFormat(ActivityDTOUpdater != null, ActivityDTOEditor != null);

			return header + relatedGenericAction + therest;
		}
	}

	public class ActEntry
	{
		public string Id { get; set; }
		public string Type { get; set; }
		public ClarifyDataRow ActEntryRecord { get; set; }
		public ActEntryTemplate Template { get; set; }
		public HistoryItemEmployee Who { get; set; }
		public DateTime When { get; set; }
		public string AdditionalInfo { get; set; }
	}

	public interface IAfterActEntryCode
	{
		/// <summary>
		/// Define the display name of the act entry 
		/// </summary>
		IAfterDisplayName DisplayName(StringToken token);

		void UseDefaultPolicy();

		/// <summary>
		/// Remove this act entry code from the current set of act entry templates
		/// </summary>
		void Remove();

		/// <summary>
		/// When a template has this set the history item will not be returned unless ShowAllActivities is true.
		/// </summary>
		IAfterActEntryCode IsVerbose();
	}

	public interface IAfterDisplayName
	{
		IHasRelatedRow GetRelatedRecord(string relationName);
		IAfterRelatedFields GetRelatedRecord(string relationName, Action<ClarifyGeneric> action);

		/// <summary>
		/// If you wish to customize how the resulting contents of the activity entry get converted to HTML add your own Action here.
		/// </summary>
		IAfterHtmlizer HtmlizeWith(Action<HistoryItem> htmlizer);

		/// <summary>
		/// Define an action which will modify the history item generated with the given data record. 
		/// The row given is the act_entry row if now related record is retrieved. When a related record is configured it will be the resulting row related to the act entry.  
		/// </summary>
		/// <param name="mapper">Action which will pull fields off the row related to the act entry and use them to modify the history item.</param>
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem> mapper);
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem, ActEntryTemplate> mapper);

		/// <summary>
		/// Define an action which will modify the resulting history item.
		/// </summary>
		void EditActivityDTO(Action<HistoryItem> action);
	}

	public interface IAfterHtmlizer
	{
		/// <summary>
		/// Have the history builder retrieve a record related to the act entry 
		/// </summary>
		/// <param name="relationName">This will be the Clarify schema name of the relation traversing from the act_entry table to the another of your choosing.</param>
		IHasRelatedRow GetRelatedRecord(string relationName);

		/// <summary>
		/// Define an action which will modify the history item generated with the given data record. 
		/// The row given is the act_entry row if now related record is retrieved. When a related record is configured it will be the resulting row related to the act entry.  
		/// </summary>
		/// <param name="mapper">Action which will pull fields off the row related to the act entry and use them to modify the history item.</param>
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem> mapper);
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem, ActEntryTemplate> mapper);

		/// <summary>
		/// Define an action which will modify the resulting history item.
		/// </summary>
		void EditActivityDTO(Action<HistoryItem> action);
	}

	public interface IHasRelatedRow
	{
		/// <summary>
		/// Define fields to retrieve for the record related to the act entry you are retrieving for this template
		/// </summary>
		IAfterRelatedFields WithFields(params string[] fieldNames);
	}

	public interface IAfterRelatedFields
	{
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem> mapper);
		void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem, ActEntryTemplate> mapper);
	}

	public abstract class ActEntryTemplatePolicyExpression : IAfterActEntryCode, IAfterDisplayName, IHasRelatedRow, IAfterRelatedFields, IAfterHtmlizer
	{
		private readonly IHistoryOutputParser _historyOutputParser;

		protected ActEntryTemplatePolicyExpression(IHistoryOutputParser historyOutputParser)
		{
			_historyOutputParser = historyOutputParser;
		}

		private ActEntryTemplate _currentActEntryTemplate;

		protected abstract void DefineTemplate(WorkflowObject workflowObject);

		public IDictionary<int, ActEntryTemplate> ActEntryTemplatesByCode { get; private set; }

		public void RenderTemplate(WorkflowObject workflowObject, IDictionary<int, ActEntryTemplate> actEntryTemplates)
		{
			ActEntryTemplatesByCode = actEntryTemplates;

			_currentActEntryTemplate = null;

			DefineTemplate(workflowObject);

			addCurrentActEntryTemplate();
		}

		/// <summary>
		/// Start the definition of act entry template for a given act_code. If a template for this act_code is already present it will replace it. 
		/// Each act_code cooresponds to a type of event in the clarify system.
		/// </summary>
		/// <param name="code">The act_code of the activity entry you wish to include. </param>
		/// <param name="isVerbose">When this is true the history item will not be returned unless ShowAllActivities is true.</param>
		public IAfterActEntryCode ActEntry(int code)
		{
			addCurrentActEntryTemplate();

			_currentActEntryTemplate = new ActEntryTemplate(_historyOutputParser) {Code = code};

			return this;
		}

		/// <summary>
		/// Edit the existing act entry template for the given act_code. If one does not already exist a new one is created.
		/// </summary>
		/// <param name="code">The act_code of the activity entry whose template you wish to edit.</param>
		public IAfterActEntryCode EditActEntry(int code)
		{
			addCurrentActEntryTemplate();

			//find existing template if it exists. 
			if (ActEntryTemplatesByCode.ContainsKey(code))
			{
				_currentActEntryTemplate = ActEntryTemplatesByCode[code];
			}
			else
			{
				_currentActEntryTemplate = new ActEntryTemplate(_historyOutputParser) { Code = code };
			}

			return this;
		}

		public IAfterActEntryCode IsVerbose()
		{
			_currentActEntryTemplate.IsVerbose = true;

			return this;
		}

		public IAfterDisplayName DisplayName(StringToken token)
		{
			_currentActEntryTemplate.DisplayName = token;

			return this;
		}

		/// <summary>
		/// This activity code should simply use the default policy. 
		/// The main reason this is here is to make explicty when policies are defining act codes that should always be part of the history yet have no fancy special handling.
		/// </summary>
		public void UseDefaultPolicy()
		{
		}

		public void Remove()
		{
			if (ActEntryTemplatesByCode.ContainsKey(_currentActEntryTemplate.Code))
			{
				ActEntryTemplatesByCode.Remove(_currentActEntryTemplate.Code);
			}
			_currentActEntryTemplate = null;
		}

		public IHasRelatedRow GetRelatedRecord(string relationName)
		{
			_currentActEntryTemplate.RelatedGenericRelationName = relationName;

			return this;
		}

		public IAfterRelatedFields GetRelatedRecord(string relationName, Action<ClarifyGeneric> action)
		{
			_currentActEntryTemplate.RelatedGenericAction = action;
			GetRelatedRecord(relationName);

			return this;
		}

		public IAfterHtmlizer HtmlizeWith(Action<HistoryItem> htmlizer)
		{
			_currentActEntryTemplate.HTMLizer = htmlizer;

			return this;
		}

		public IAfterRelatedFields WithFields(params string[] fieldNames)
		{
			validateThereIsARelatedRecord();

			_currentActEntryTemplate.RelatedGenericFields = fieldNames;

			return this;
		}

		public void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem> mapper)
		{
			var wrappedAction = new Action<ClarifyDataRow, HistoryItem, ActEntryTemplate>((row, item, template) => mapper(row, item));

			_currentActEntryTemplate.ActivityDTOUpdater = wrappedAction;
		}

		public void UpdateActivityDTOWith(Action<ClarifyDataRow, HistoryItem, ActEntryTemplate> mapper)
		{
			_currentActEntryTemplate.ActivityDTOUpdater = mapper;
		}

		public void EditActivityDTO(Action<HistoryItem> action)
		{
			_currentActEntryTemplate.ActivityDTOEditor = action;
		}

		private void validateThereIsARelatedRecord()
		{
			if (_currentActEntryTemplate.RelatedGenericRelationName.IsEmpty())
				throw new Exception("Cannot add fields unless a record is related. First call GetRelatedRecord()");
		}

		private void addCurrentActEntryTemplate()
		{
			if (_currentActEntryTemplate == null)
				return;

			//replace existing template
			if (ActEntryTemplatesByCode.ContainsKey(_currentActEntryTemplate.Code))
			{
				ActEntryTemplatesByCode.Remove(_currentActEntryTemplate.Code);
			}

			ActEntryTemplatesByCode.Add(_currentActEntryTemplate.Code, _currentActEntryTemplate);

			_currentActEntryTemplate = null;
		}
	}
}