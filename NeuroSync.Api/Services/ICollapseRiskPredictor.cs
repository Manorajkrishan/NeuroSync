using NeuroSync.Core.Models;

namespace NeuroSync.Api.Services;

public interface ICollapseRiskPredictor
{
    Task<BurnoutRiskAnalysis> CalculateBurnoutRiskAsync(string userId);
}
