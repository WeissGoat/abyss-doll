public static class NarrativeRuntimeSelection {
    public const string CandidateName = "Yarn Spinner for Unity";
    public const string CandidateVersion = "v3.2.4";
    public const string CandidatePackageUrl = "https://github.com/YarnSpinnerTool/YarnSpinner-Unity.git#v3.2.4";
    public const string CandidateLicense = "MIT";
    public const string CandidateUnityCompatibility = "Unity 2022.3 and newer";

    public const string CurrentSpikeRuntimeName = "P3 Yarn-like equivalent runtime spike";
    public const string CurrentSpikeReason =
        "NARR-00 keeps the external runtime out of Packages/manifest.json until the Narrative config, scheduler, UGUI overlay and command bridge contracts are locked.";

    public static bool IsExternalRuntimeLocked {
        get { return false; }
    }
}
