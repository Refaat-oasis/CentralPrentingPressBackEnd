using System.Collections.Generic;

namespace CentralPress.API.DTOs;

public class SaveOverridesDto
{
    public List<PermissionOverrideInputDto>? Overrides { get; init; }
}
