using AuthDemo.DTOs;

namespace AuthDemo.Services
{
    // The SRS says Quick Actions depend on Team Leader / Team Member / Supervisor.
    public enum ProjectRole
    {
        TeamLeader,
        TeamMember,
        Supervisor
    }

    // One place that says WHICH role sees WHICH action.
    // Change the Roles arrays here if the team decides differently. Nothing else needs to change.
    public static class QuickActionCatalog
    {
        public record Definition(string Key, string Label, string Icon, ProjectRole[] Roles);

        public static readonly IReadOnlyList<Definition> All = new List<Definition>
        {
            // Only a Team Leader can create projects (Module 01 SRS: "Team Leader can create projects").
            new("create_project",    "Create Project",    "folder-plus",  new[] { ProjectRole.TeamLeader }),
            new("create_task",       "Create Task",       "check-square", new[] { ProjectRole.TeamLeader, ProjectRole.TeamMember }),
            new("upload_file",       "Upload File",       "upload",       new[] { ProjectRole.TeamLeader, ProjectRole.TeamMember }),
            new("schedule_meeting",  "Schedule Meeting",  "calendar",     new[] { ProjectRole.TeamLeader, ProjectRole.TeamMember, ProjectRole.Supervisor }),
            new("open_ai_assistant", "Open AI Assistant", "sparkles",     new[] { ProjectRole.TeamLeader, ProjectRole.TeamMember }),
        };

        // Other controllers (projects, tasks, files, meetings) can call this to enforce
        // "User permissions must be verified before executing actions" (SRS 5.5).
        public static bool IsAllowed(ProjectRole role, string actionKey)
            => All.Any(a => a.Key == actionKey && a.Roles.Contains(role));
    }

    public interface IProjectRoleResolver
    {
        Task<ProjectRole> ResolveAsync(string userId, IEnumerable<string> tokenRoles);
    }

    // TEMPORARY resolver. A real Team Leader / Team Member lookup needs the Projects tables
    // from Module 03, which this code does not depend on.
    // It reads the role text from the token (case-insensitive):
    //   contains "supervisor" -> Supervisor, "leader" -> TeamLeader, "member" -> TeamMember.
    // Anything else (e.g. "Student") -> value of QuickActions:DefaultStudentRole, else TeamMember.
    public class DefaultProjectRoleResolver : IProjectRoleResolver
    {
        private readonly IConfiguration _config;

        public DefaultProjectRoleResolver(IConfiguration config)
        {
            _config = config;
        }

        public Task<ProjectRole> ResolveAsync(string userId, IEnumerable<string> tokenRoles)
        {
            var text = string.Join(",", tokenRoles).ToLowerInvariant();

            if (text.Contains("supervisor")) return Task.FromResult(ProjectRole.Supervisor);
            if (text.Contains("leader")) return Task.FromResult(ProjectRole.TeamLeader);
            if (text.Contains("member")) return Task.FromResult(ProjectRole.TeamMember);

            var configured = _config["QuickActions:DefaultStudentRole"];
            var role = Enum.TryParse<ProjectRole>(configured, true, out var parsed)
                       && parsed != ProjectRole.Supervisor
                ? parsed
                : ProjectRole.TeamMember;

            return Task.FromResult(role);
        }
    }

    public interface IQuickActionsService
    {
        Task<QuickActionsResponseDto> GetForUserAsync(string userId, IEnumerable<string> tokenRoles);
    }

    public class QuickActionsService : IQuickActionsService
    {
        private readonly IProjectRoleResolver _roleResolver;

        public QuickActionsService(IProjectRoleResolver roleResolver)
        {
            _roleResolver = roleResolver;
        }

        public async Task<QuickActionsResponseDto> GetForUserAsync(string userId, IEnumerable<string> tokenRoles)
        {
            var role = await _roleResolver.ResolveAsync(userId, tokenRoles);

            return new QuickActionsResponseDto
            {
                Role = role.ToString(),
                // Unauthorized actions are simply not returned, so the frontend never shows them (SRS 5.8).
                Actions = QuickActionCatalog.All
                    .Where(a => a.Roles.Contains(role))
                    .Select(a => new QuickActionDto { Key = a.Key, Label = a.Label, Icon = a.Icon })
                    .ToList()
            };
        }
    }
}
