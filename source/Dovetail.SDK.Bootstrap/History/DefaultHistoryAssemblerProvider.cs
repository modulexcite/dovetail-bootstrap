using System.Collections.Generic;
using FChoice.Foundation.Filters;

namespace Dovetail.SDK.Bootstrap.History
{
    public interface IHistoryAssemblerPolicy
    {
        bool Handles(WorkflowObject workflowObject);
        IEnumerable<HistoryItem> BuildHistory(WorkflowObject workflowObject, Filter actEntryFilter);
    }

    public class DefaultHistoryAssemblerPolicy : IHistoryAssemblerPolicy
    {   
        private readonly HistoryBuilder _historyBuilder;
        
        public DefaultHistoryAssemblerPolicy(HistoryBuilder historyBuilder)
        {
            _historyBuilder = historyBuilder;
        }

        public bool Handles(WorkflowObject workflowObject)
        {
            return workflowObject.Type != WorkflowObject.Case;
        }

        public IEnumerable<HistoryItem> BuildHistory(WorkflowObject workflowObject, Filter actEntryFilter)
        {
            return _historyBuilder.Build(workflowObject, actEntryFilter);
        }
    }
}