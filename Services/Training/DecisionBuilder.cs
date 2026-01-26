using System;
using Olympus.Config;

namespace Olympus.Services.Training;

/// <summary>
/// Fluent builder for recording training decisions.
/// Provides a clean API that unifies all role-specific training helpers.
/// </summary>
public sealed class DecisionBuilder
{
    private readonly ITrainingService? _service;
    private uint _actionId;
    private string _actionName = string.Empty;
    private string _category = string.Empty;
    private string? _targetName;
    private string _shortReason = string.Empty;
    private string _detailedReason = string.Empty;
    private string[] _factors = Array.Empty<string>();
    private string[] _alternatives = Array.Empty<string>();
    private string _tip = string.Empty;
    private string _conceptId = string.Empty;
    private ExplanationPriority _priority = ExplanationPriority.Normal;
    private DecisionContext? _context;

    /// <summary>
    /// Creates a new decision builder for the given training service.
    /// </summary>
    public DecisionBuilder(ITrainingService? service)
    {
        _service = service;
    }

    /// <summary>
    /// Sets the action being taken.
    /// </summary>
    public DecisionBuilder Action(uint id, string name)
    {
        _actionId = id;
        _actionName = name;
        return this;
    }

    /// <summary>
    /// Sets the decision category (use DecisionCategory constants).
    /// </summary>
    public DecisionBuilder Category(string category)
    {
        _category = category;
        return this;
    }

    /// <summary>
    /// Sets the target of the action (if applicable).
    /// </summary>
    public DecisionBuilder Target(string? name)
    {
        _targetName = name;
        return this;
    }

    /// <summary>
    /// Sets the short and detailed reasons for the decision.
    /// </summary>
    public DecisionBuilder Reason(string shortReason, string detailedReason)
    {
        _shortReason = shortReason;
        _detailedReason = detailedReason;
        return this;
    }

    /// <summary>
    /// Sets the factors that influenced this decision.
    /// </summary>
    public DecisionBuilder Factors(params string[] factors)
    {
        _factors = factors;
        return this;
    }

    /// <summary>
    /// Sets the alternative actions that were considered.
    /// </summary>
    public DecisionBuilder Alternatives(params string[] alternatives)
    {
        _alternatives = alternatives;
        return this;
    }

    /// <summary>
    /// Sets the learning tip for this decision.
    /// </summary>
    public DecisionBuilder Tip(string tip)
    {
        _tip = tip;
        return this;
    }

    /// <summary>
    /// Sets the concept ID for tracking learning progress.
    /// </summary>
    public DecisionBuilder Concept(string conceptId)
    {
        _conceptId = conceptId;
        return this;
    }

    /// <summary>
    /// Sets the priority level of the explanation.
    /// </summary>
    public DecisionBuilder Priority(ExplanationPriority priority)
    {
        _priority = priority;
        return this;
    }

    /// <summary>
    /// Sets the role-specific decision context.
    /// </summary>
    public DecisionBuilder Context(DecisionContext context)
    {
        _context = context;
        return this;
    }

    /// <summary>
    /// Merges additional context into the existing context.
    /// Creates a new context if none exists.
    /// </summary>
    internal DecisionBuilder MergeContext(Func<DecisionContext?, DecisionContext> merger)
    {
        _context = merger(_context);
        return this;
    }

    /// <summary>
    /// Records the decision to the training service.
    /// This is the terminal operation that finalizes the builder.
    /// </summary>
    public void Record()
    {
        if (_service?.IsTrainingEnabled != true)
            return;

        _service.RecordDecision(new ActionExplanation
        {
            Timestamp = DateTime.Now,
            ActionId = _actionId,
            ActionName = _actionName,
            Category = _category,
            TargetName = _targetName,
            ShortReason = _shortReason,
            DetailedReason = _detailedReason,
            Factors = _factors,
            Alternatives = _alternatives,
            Tip = _tip,
            ConceptId = _conceptId,
            Priority = _priority,
            Context = _context,
        });
    }
}
