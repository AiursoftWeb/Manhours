using System.Text.Json.Serialization;

namespace Aiursoft.ManHours;

public class GitLabProject
{
//     [{
// 	"id": 99,
// 	"description": null,
// 	"name": "Infra",
// 	"name_with_namespace": "Aiursoft / Infra",
// 	"path": "infra",
// 	"path_with_namespace": "aiursoft/infra",
// 	"created_at": "2023-10-30T15:00:41.136Z",
// 	"default_branch": "master",
// 	"tag_list": [],
// 	"topics": [],
// 	"ssh_url_to_repo": "ssh://git@gitlab.aiursoft.cn:2202/aiursoft/infra.git",
// 	"http_url_to_repo": "https://gitlab.aiursoft.cn/aiursoft/infra.git",
// 	"web_url": "https://gitlab.aiursoft.cn/aiursoft/infra",
// 	"readme_url": "https://gitlab.aiursoft.cn/aiursoft/infra/-/blob/master/README.md",
// 	"forks_count": 0,
// 	"avatar_url": null,
// 	"star_count": 1,
// 	"last_activity_at": "2023-11-21T16:57:39.260Z",
// 	"namespace": {
// 		"id": 5,
// 		"name": "Aiursoft",
// 		"path": "aiursoft",
// 		"kind": "group",
// 		"full_path": "aiursoft",
// 		"parent_id": null,
// 		"avatar_url": "/uploads/-/system/group/avatar/5/logo.png",
// 		"web_url": "https://gitlab.aiursoft.cn/groups/aiursoft"
// 	},
// 	"repository_storage": "default",
// 	"_links": {
// 		"self": "https://gitlab.aiursoft.cn/api/v4/projects/99",
// 		"issues": "https://gitlab.aiursoft.cn/api/v4/projects/99/issues",
// 		"merge_requests": "https://gitlab.aiursoft.cn/api/v4/projects/99/merge_requests",
// 		"repo_branches": "https://gitlab.aiursoft.cn/api/v4/projects/99/repository/branches",
// 		"labels": "https://gitlab.aiursoft.cn/api/v4/projects/99/labels",
// 		"events": "https://gitlab.aiursoft.cn/api/v4/projects/99/events",
// 		"members": "https://gitlab.aiursoft.cn/api/v4/projects/99/members",
// 		"cluster_agents": "https://gitlab.aiursoft.cn/api/v4/projects/99/cluster_agents"
// 	},
// 	"packages_enabled": true,
// 	"empty_repo": false,
// 	"archived": false,
// 	"visibility": "public",
// 	"resolve_outdated_diff_discussions": false,
// 	"container_expiration_policy": {
// 		"cadence": "1d",
// 		"enabled": false,
// 		"keep_n": 10,
// 		"older_than": "90d",
// 		"name_regex": ".*",
// 		"name_regex_keep": null,
// 		"next_run_at": "2023-10-31T15:00:41.192Z"
// 	},
// 	"issues_enabled": true,
// 	"merge_requests_enabled": true,
// 	"wiki_enabled": true,
// 	"jobs_enabled": true,
// 	"snippets_enabled": true,
// 	"container_registry_enabled": true,
// 	"service_desk_enabled": false,
// 	"service_desk_address": null,
// 	"can_create_merge_request_in": true,
// 	"issues_access_level": "enabled",
// 	"repository_access_level": "enabled",
// 	"merge_requests_access_level": "enabled",
// 	"forking_access_level": "enabled",
// 	"wiki_access_level": "enabled",
// 	"builds_access_level": "enabled",
// 	"snippets_access_level": "enabled",
// 	"pages_access_level": "private",
// 	"analytics_access_level": "enabled",
// 	"container_registry_access_level": "enabled",
// 	"security_and_compliance_access_level": "private",
// 	"releases_access_level": "enabled",
// 	"environments_access_level": "enabled",
// 	"feature_flags_access_level": "enabled",
// 	"infrastructure_access_level": "enabled",
// 	"monitor_access_level": "enabled",
// 	"model_experiments_access_level": "enabled",
// 	"emails_disabled": false,
// 	"emails_enabled": true,
// 	"shared_runners_enabled": true,
// 	"lfs_enabled": true,
// 	"creator_id": 1,
// 	"import_url": null,
// 	"import_type": null,
// 	"import_status": "none",
// 	"open_issues_count": 0,
// 	"description_html": "",
// 	"updated_at": "2023-11-21T16:57:39.260Z",
// 	"ci_default_git_depth": 20,
// 	"ci_forward_deployment_enabled": true,
// 	"ci_forward_deployment_rollback_allowed": true,
// 	"ci_job_token_scope_enabled": false,
// 	"ci_separated_caches": true,
// 	"ci_allow_fork_pipelines_to_run_in_parent_project": true,
// 	"build_git_strategy": "fetch",
// 	"keep_latest_artifact": true,
// 	"restrict_user_defined_variables": false,
// 	"runners_token": "GR1348941s-1i8bJKAMxMD5jqi6pP",
// 	"runner_token_expiration_interval": null,
// 	"group_runners_enabled": true,
// 	"auto_cancel_pending_pipelines": "enabled",
// 	"build_timeout": 3600,
// 	"auto_devops_enabled": true,
// 	"auto_devops_deploy_strategy": "continuous",
// 	"ci_config_path": null,
// 	"public_jobs": true,
// 	"shared_with_groups": [],
// 	"only_allow_merge_if_pipeline_succeeds": false,
// 	"allow_merge_on_skipped_pipeline": null,
// 	"request_access_enabled": true,
// 	"only_allow_merge_if_all_discussions_are_resolved": false,
// 	"remove_source_branch_after_merge": true,
// 	"printing_merge_request_link_enabled": true,
// 	"merge_method": "merge",
// 	"squash_option": "default_off",
// 	"enforce_auth_checks_on_uploads": true,
// 	"suggestion_commit_message": null,
// 	"merge_commit_template": null,
// 	"squash_commit_template": null,
// 	"issue_branch_template": null,
// 	"autoclose_referenced_issues": true,
// 	"requirements_enabled": false,
// 	"requirements_access_level": "enabled",
// 	"security_and_compliance_enabled": true,
// 	"compliance_frameworks": []
// }, {
// 	"id": 20,
// 	"description": null,
// 	"name": "Infrastructures",
// 	"name_with_namespace": "Aiursoft / Infrastructures",
// 	"path": "infrastructures",
// 	"path_with_namespace": "aiursoft/infrastructures",
// 	"created_at": "2023-05-10T06:52:34.148Z",
// 	"default_branch": "master",
// 	"tag_list": [],
// 	"topics": [],
// 	"ssh_url_to_repo": "ssh://git@gitlab.aiursoft.cn:2202/aiursoft/infrastructures.git",
// 	"http_url_to_repo": "https://gitlab.aiursoft.cn/aiursoft/infrastructures.git",
// 	"web_url": "https://gitlab.aiursoft.cn/aiursoft/infrastructures",
// 	"readme_url": "https://gitlab.aiursoft.cn/aiursoft/infrastructures/-/blob/master/Readme.md",
// 	"forks_count": 1,
// 	"avatar_url": null,
// 	"star_count": 1,
// 	"last_activity_at": "2023-11-22T07:42:00.384Z",
// 	"namespace": {
// 		"id": 5,
// 		"name": "Aiursoft",
// 		"path": "aiursoft",
// 		"kind": "group",
// 		"full_path": "aiursoft",
// 		"parent_id": null,
// 		"avatar_url": "/uploads/-/system/group/avatar/5/logo.png",
// 		"web_url": "https://gitlab.aiursoft.cn/groups/aiursoft"
// 	},
// 	"repository_storage": "default",
// 	"_links": {
// 		"self": "https://gitlab.aiursoft.cn/api/v4/projects/20",
// 		"issues": "https://gitlab.aiursoft.cn/api/v4/projects/20/issues",
// 		"merge_requests": "https://gitlab.aiursoft.cn/api/v4/projects/20/merge_requests",
// 		"repo_branches": "https://gitlab.aiursoft.cn/api/v4/projects/20/repository/branches",
// 		"labels": "https://gitlab.aiursoft.cn/api/v4/projects/20/labels",
// 		"events": "https://gitlab.aiursoft.cn/api/v4/projects/20/events",
// 		"members": "https://gitlab.aiursoft.cn/api/v4/projects/20/members",
// 		"cluster_agents": "https://gitlab.aiursoft.cn/api/v4/projects/20/cluster_agents"
// 	},
// 	"packages_enabled": true,
// 	"empty_repo": false,
// 	"archived": false,
// 	"visibility": "public",
// 	"resolve_outdated_diff_discussions": false,
// 	"container_expiration_policy": {
// 		"cadence": "1d",
// 		"enabled": false,
// 		"keep_n": 10,
// 		"older_than": "90d",
// 		"name_regex": ".*",
// 		"name_regex_keep": null,
// 		"next_run_at": "2023-05-11T06:52:34.181Z"
// 	},
// 	"issues_enabled": true,
// 	"merge_requests_enabled": true,
// 	"wiki_enabled": true,
// 	"jobs_enabled": true,
// 	"snippets_enabled": true,
// 	"container_registry_enabled": true,
// 	"service_desk_enabled": false,
// 	"service_desk_address": null,
// 	"can_create_merge_request_in": true,
// 	"issues_access_level": "enabled",
// 	"repository_access_level": "enabled",
// 	"merge_requests_access_level": "enabled",
// 	"forking_access_level": "enabled",
// 	"wiki_access_level": "enabled",
// 	"builds_access_level": "enabled",
// 	"snippets_access_level": "enabled",
// 	"pages_access_level": "enabled",
// 	"analytics_access_level": "enabled",
// 	"container_registry_access_level": "enabled",
// 	"security_and_compliance_access_level": "private",
// 	"releases_access_level": "enabled",
// 	"environments_access_level": "enabled",
// 	"feature_flags_access_level": "enabled",
// 	"infrastructure_access_level": "enabled",
// 	"monitor_access_level": "enabled",
// 	"model_experiments_access_level": "enabled",
// 	"emails_disabled": false,
// 	"emails_enabled": true,
// 	"shared_runners_enabled": true,
// 	"lfs_enabled": true,
// 	"creator_id": 1,
// 	"import_url": null,
// 	"import_type": null,
// 	"import_status": "none",
// 	"open_issues_count": 16,
// 	"description_html": "",
// 	"updated_at": "2023-11-22T07:42:00.414Z",
// 	"ci_default_git_depth": 20,
// 	"ci_forward_deployment_enabled": true,
// 	"ci_forward_deployment_rollback_allowed": true,
// 	"ci_job_token_scope_enabled": false,
// 	"ci_separated_caches": true,
// 	"ci_allow_fork_pipelines_to_run_in_parent_project": true,
// 	"build_git_strategy": "fetch",
// 	"keep_latest_artifact": true,
// 	"restrict_user_defined_variables": false,
// 	"runners_token": "GR1348941xGhVtL5WdvUg75yszwQw",
// 	"runner_token_expiration_interval": null,
// 	"group_runners_enabled": true,
// 	"auto_cancel_pending_pipelines": "enabled",
// 	"build_timeout": 3600,
// 	"auto_devops_enabled": true,
// 	"auto_devops_deploy_strategy": "continuous",
// 	"ci_config_path": null,
// 	"public_jobs": true,
// 	"shared_with_groups": [],
// 	"only_allow_merge_if_pipeline_succeeds": false,
// 	"allow_merge_on_skipped_pipeline": null,
// 	"request_access_enabled": true,
// 	"only_allow_merge_if_all_discussions_are_resolved": false,
// 	"remove_source_branch_after_merge": true,
// 	"printing_merge_request_link_enabled": true,
// 	"merge_method": "merge",
// 	"squash_option": "default_off",
// 	"enforce_auth_checks_on_uploads": true,
// 	"suggestion_commit_message": null,
// 	"merge_commit_template": null,
// 	"squash_commit_template": null,
// 	"issue_branch_template": null,
// 	"autoclose_referenced_issues": true,
// 	"requirements_enabled": false,
// 	"requirements_access_level": "enabled",
// 	"security_and_compliance_enabled": true,
// 	"compliance_frameworks": []
// }]

    [JsonPropertyName("id")]
    public int Id { get; init; }
    
    [JsonPropertyName("description")]
    public string? Description { get; init; }
    
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    
    [JsonPropertyName("name_with_namespace")]
    public string? NameWithNamespace { get; init; }
    
    [JsonPropertyName("path")]
    public string? Path { get; init; }
    
    [JsonPropertyName("path_with_namespace")]
    public string? PathWithNamespace { get; init; }
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
    
    [JsonPropertyName("default_branch")]
    public string? DefaultBranch { get; init; }
    
    [JsonPropertyName("tag_list")]
    public string[]? TagList { get; init; }
    
    [JsonPropertyName("topics")]
    public string[]? Topics { get; init; }
    
    [JsonPropertyName("ssh_url_to_repo")]
    public string? SshUrlToRepo { get; init; }
    
    [JsonPropertyName("http_url_to_repo")]
    public string? HttpUrlToRepo { get; init; }
    
    [JsonPropertyName("web_url")]
    public  string? WebUrl { get; init; }
    
    [JsonPropertyName("readme_url")]
    public string? ReadmeUrl { get; init; }
    
    [JsonPropertyName("forks_count")]
    public int ForksCount { get; init; }
}