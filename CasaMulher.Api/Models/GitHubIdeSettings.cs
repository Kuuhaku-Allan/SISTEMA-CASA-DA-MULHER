namespace CasaMulher.Api.Models
{
    public class GitHubIdeSettings
    {
        public bool Enabled { get; set; } = false;
        public string Owner { get; set; } = string.Empty;
        public string Repo { get; set; } = string.Empty;
        public string BaseBranch { get; set; } = "main";
        public string AllowedRoot { get; set; } = "projetocasadamulher/telas/ide-rascunhos";
        public string Mode { get; set; } = "ManualToken";
        public string Token { get; set; } = string.Empty; // From user-secrets
        
        // OAuth / Personal Fork Settings
        public bool PersonalForkEnabled { get; set; } = true;
        public bool FallbackToCentralPr { get; set; } = true;
        public string OAuthCallbackPath { get; set; } = "/api/equipe-ide/github/callback";
        
        public string ClientId { get; set; } = string.Empty; // From user-secrets
        public string ClientSecret { get; set; } = string.Empty; // From user-secrets
        public string TokenEncryptionKey { get; set; } = string.Empty; // From user-secrets
    }
}
