namespace Bridge.Models;

public sealed record HardwareProfile(
    double TotalRamGb,
    int LogicalProcessorCount,
    double FreeDiskGb)
{
    public string Summary =>
        $"This PC: {TotalRamGb:F1} GB RAM · {LogicalProcessorCount} logical CPU cores · {FreeDiskGb:F1} GB free";

    public HardwareCompatibility Evaluate(TranslationEngineDefinition engine)
    {
        var missing = new List<string>();
        if (TotalRamGb + 0.05 < engine.MinimumRamGb)
        {
            missing.Add($"{engine.MinimumRamGb} GB RAM");
        }

        if (LogicalProcessorCount < engine.RecommendedCpuCores)
        {
            missing.Add($"{engine.RecommendedCpuCores} CPU cores");
        }

        if (FreeDiskGb + 0.05 < engine.RequiredDiskGb)
        {
            missing.Add($"{engine.RequiredDiskGb} GB free disk");
        }

        return missing.Count == 0
            ? new HardwareCompatibility(true, "This PC meets the RAM, CPU, and disk recommendation.")
            : new HardwareCompatibility(
                false,
                "Below the recommendation: " + string.Join(", ", missing) + ". You can still try it, but it may be slow or fail to load.");
    }
}

public sealed record HardwareCompatibility(bool IsRecommended, string Message);
