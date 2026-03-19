using System;
using System.Collections.Generic;
using Olympus.Models;

namespace Olympus.Services.Analytics;

public interface IFightSummaryService
{
    event Action<FightSummaryRecord>? OnSummaryReady;
    IReadOnlyList<FightSummaryRecord> RecentSummaries { get; }
}
