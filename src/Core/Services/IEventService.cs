﻿using System;
using System.Threading.Tasks;
using Bit.Core.Enums;
using Bit.Core.Models.Table;

namespace Bit.Core.Services
{
    public interface IEventService
    {
        Task LogUserEventAsync(Guid userId, EventType type, DateTime? date = null);
        Task LogCipherEventAsync(Cipher cipher, EventType type, DateTime? date = null);
        Task LogCollectionEventAsync(Collection collection, EventType type, DateTime? date = null);
        Task LogGroupEventAsync(Group group, EventType type, DateTime? date = null);
        Task LogOrganizationUserEventAsync(OrganizationUser organizationUser, EventType type, DateTime? date = null);
        Task LogOrganizationEventAsync(Organization organization, EventType type, DateTime? date = null);
    }
}
