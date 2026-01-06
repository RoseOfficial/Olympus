using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Models.Action;

namespace Olympus.Services.Action;

/// <summary>
/// Service for managing action definitions across all jobs.
/// Actions are registered at initialization and queried during runtime.
/// </summary>
public sealed class ActionLibraryService : IActionLibrary
{
    private readonly Dictionary<uint, ActionDefinition> _actionsByIdCache = new(256);
    private readonly Dictionary<uint, List<ActionDefinition>> _actionsByJob = new();

    /// <summary>
    /// Creates a new action library and registers all known job actions.
    /// </summary>
    public ActionLibraryService()
    {
        // Register all known job actions
        RegisterWhiteMageActions();
        // Future: RegisterScholarActions(), RegisterAstrologianActions(), etc.
    }

    /// <inheritdoc />
    public ActionDefinition? GetAction(uint actionId)
    {
        return _actionsByIdCache.TryGetValue(actionId, out var action) ? action : null;
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetJobActions(uint jobId)
    {
        return _actionsByJob.TryGetValue(jobId, out var actions) ? actions : Enumerable.Empty<ActionDefinition>();
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetHealingActions(uint jobId)
    {
        return GetJobActions(jobId).Where(a => a.IsHeal);
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetDamageActions(uint jobId)
    {
        return GetJobActions(jobId).Where(a => a.IsDamage);
    }

    /// <inheritdoc />
    public IEnumerable<ActionDefinition> GetActionsAtLevel(uint jobId, byte level)
    {
        return GetJobActions(jobId).Where(a => a.MinLevel <= level);
    }

    /// <inheritdoc />
    public bool HasAction(uint actionId)
    {
        return _actionsByIdCache.ContainsKey(actionId);
    }

    /// <summary>
    /// Registers an action for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="action">The action definition.</param>
    private void RegisterAction(uint jobId, ActionDefinition action)
    {
        // Add to ID lookup cache
        _actionsByIdCache[action.ActionId] = action;

        // Add to job-specific list
        if (!_actionsByJob.TryGetValue(jobId, out var jobActions))
        {
            jobActions = new List<ActionDefinition>(32);
            _actionsByJob[jobId] = jobActions;
        }
        jobActions.Add(action);
    }

    /// <summary>
    /// Registers multiple actions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="actions">The action definitions to register.</param>
    private void RegisterActions(uint jobId, params ActionDefinition[] actions)
    {
        foreach (var action in actions)
        {
            RegisterAction(jobId, action);
        }
    }

    /// <summary>
    /// Registers all White Mage (WHM) and Conjurer (CNJ) actions.
    /// </summary>
    private void RegisterWhiteMageActions()
    {
        const uint whmJobId = JobRegistry.WhiteMage;
        const uint cnjJobId = JobRegistry.Conjurer;

        // GCD Heals
        RegisterActions(whmJobId,
            WHMActions.Cure,
            WHMActions.CureII,
            WHMActions.CureIII,
            WHMActions.Regen,
            WHMActions.Medica,
            WHMActions.MedicaII,
            WHMActions.MedicaIII,
            WHMActions.AfflatusSolace,
            WHMActions.AfflatusRapture);

        // oGCD Heals
        RegisterActions(whmJobId,
            WHMActions.Tetragrammaton,
            WHMActions.Benediction,
            WHMActions.Assize,
            WHMActions.Asylum);

        // Damage
        RegisterActions(whmJobId,
            WHMActions.Stone,
            WHMActions.StoneII,
            WHMActions.StoneIII,
            WHMActions.StoneIV,
            WHMActions.Glare,
            WHMActions.GlareIII,
            WHMActions.GlareIV,
            WHMActions.Holy,
            WHMActions.HolyIII,
            WHMActions.AfflatusMisery);

        // DoTs
        RegisterActions(whmJobId,
            WHMActions.Aero,
            WHMActions.AeroII,
            WHMActions.Dia);

        // Defensive
        RegisterActions(whmJobId,
            WHMActions.DivineBenison,
            WHMActions.Aquaveil,
            WHMActions.Temperance,
            WHMActions.LiturgyOfTheBell,
            WHMActions.PlenaryIndulgence,
            WHMActions.DivineCaress);

        // Buffs
        RegisterActions(whmJobId,
            WHMActions.PresenceOfMind,
            WHMActions.ThinAir);

        // Role Actions
        RegisterActions(whmJobId,
            WHMActions.Esuna,
            WHMActions.Raise,
            WHMActions.Swiftcast,
            WHMActions.LucidDreaming,
            WHMActions.Surecast,
            WHMActions.Rescue);

        // Also register CNJ base class actions
        RegisterActions(cnjJobId,
            WHMActions.Cure,
            WHMActions.Stone,
            WHMActions.Aero,
            WHMActions.Medica,
            WHMActions.Raise,
            WHMActions.Esuna);
    }
}
