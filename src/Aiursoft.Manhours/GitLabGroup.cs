using System.Text.Json.Serialization;

namespace Aiursoft.ManHours;

public class GitLabGroup
{
    // [{
    //     "id": 5,
    //     "web_url": "https://gitlab.aiursoft.cn/groups/aiursoft",
    //     "name": "Aiursoft",
    //     "path": "aiursoft",
    //     "description": "Create a more open world. Aiursoft is an organization focusing on open platform and open communication.",
    //     "visibility": "public",
    //     "share_with_group_lock": false,
    //     "require_two_factor_authentication": false,
    //     "two_factor_grace_period": 48,
    //     "project_creation_level": "developer",
    //     "auto_devops_enabled": null,
    //     "subgroup_creation_level": "maintainer",
    //     "emails_disabled": null,
    //     "mentions_disabled": null,
    //     "lfs_enabled": true,
    //     "default_branch_protection": 2,
    //     "default_branch_protection_defaults": {
    //         "allowed_to_push": [{
    //             "access_level": 30
    //         }],
    //         "allow_force_push": true,
    //         "allowed_to_merge": [{
    //             "access_level": 30
    //         }]
    //     },
    //     "avatar_url": "https://gitlab.aiursoft.cn/uploads/-/system/group/avatar/5/logo.png",
    //     "request_access_enabled": true,
    //     "full_name": "Aiursoft",
    //     "full_path": "aiursoft",
    //     "created_at": "2023-05-06T07:09:59.788Z",
    //     "parent_id": null,
    //     "shared_runners_setting": "enabled",
    //     "ldap_cn": null,
    //     "ldap_access": null,
    //     "wiki_access_level": "enabled"
    // }]
    
    [JsonPropertyName("id")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public int Id { get; init;  }
    
    [JsonPropertyName("path")]
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string? Path { get; init; }
}