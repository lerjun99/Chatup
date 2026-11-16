namespace ChatUp.Services
{
    /// <summary>The link from a verified one-time code to the reset form (see LinkService.ProtectTimed).</summary>
    public static class PasswordResetLink
    {
        public const string Purpose = "ChatUp.PasswordReset.v1";
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(15);
    }
}
