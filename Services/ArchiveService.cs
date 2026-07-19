using BudgetMasterFinal.Data;
using BudgetMasterFinal.Interfaces;
using BudgetMasterFinal.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace BudgetMasterFinal.Services
{
    public interface IArchiveService
    {
        Task<bool> ArchiveAsync<T>(int id, string userId, string? reason = null) where T : class, IArchivable;
        Task<bool> ArchiveAsync<T>(string id, string userId, string? reason = null) where T : class, IArchivable;
        Task<bool> RestoreAsync<T>(int id, string userId) where T : class, IArchivable;
        Task<bool> RestoreAsync<T>(string id, string userId) where T : class, IArchivable;
        Task<List<ArchivedItem>> GetArchivedItemsAsync(int? tenantId = null, string? entityType = null);
        Task<bool> CanRestoreAsync<T>(int id) where T : class, IArchivable;
        Task<bool> PermanentDeleteAsync<T>(int id) where T : class, IArchivable;
    }

    public class ArchiveService : IArchiveService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ArchiveService> _logger;

        public ArchiveService(ApplicationDbContext context, ILogger<ArchiveService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Archive an entity by integer ID
        /// </summary>
        public async Task<bool> ArchiveAsync<T>(int id, string userId, string? reason = null) where T : class, IArchivable
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null || entity.IsArchived)
                {
                    return false;
                }

                // Mark as archived
                entity.IsArchived = true;
                entity.ArchivedAt = DateTime.UtcNow;
                entity.ArchivedBy = userId;
                entity.ArchiveReason = reason;

                // Create metadata record
                var archivedItem = new ArchivedItem
                {
                    EntityType = typeof(T).Name,
                    EntityId = id.ToString(),
                    EntityName = GetEntityName(entity),
                    ArchivedAt = DateTime.UtcNow,
                    ArchivedBy = userId,
                    ArchiveReason = reason,
                    TenantId = GetTenantId(entity),
                    OriginalData = JsonSerializer.Serialize(entity),
                    CanRestore = true
                };

                _context.ArchivedItems.Add(archivedItem);

                // Create audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    TenantId = GetTenantId(entity),
                    Action = "Archive",
                    EntityType = typeof(T).Name,
                    EntityId = id,
                    OldValues = "Active",
                    NewValues = reason != null ? $"Archived: {reason}" : "Archived",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Archived {typeof(T).Name} with ID {id} by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error archiving {typeof(T).Name} with ID {id}");
                return false;
            }
        }

        /// <summary>
        /// Archive an entity by string ID (for ApplicationUser)
        /// </summary>
        public async Task<bool> ArchiveAsync<T>(string id, string userId, string? reason = null) where T : class, IArchivable
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null || entity.IsArchived)
                {
                    return false;
                }

                // Mark as archived
                entity.IsArchived = true;
                entity.ArchivedAt = DateTime.UtcNow;
                entity.ArchivedBy = userId;
                entity.ArchiveReason = reason;

                // Create metadata record
                var archivedItem = new ArchivedItem
                {
                    EntityType = typeof(T).Name,
                    EntityId = id,
                    EntityName = GetEntityName(entity),
                    ArchivedAt = DateTime.UtcNow,
                    ArchivedBy = userId,
                    ArchiveReason = reason,
                    TenantId = GetTenantId(entity),
                    OriginalData = JsonSerializer.Serialize(entity),
                    CanRestore = true
                };

                _context.ArchivedItems.Add(archivedItem);

                // Create audit log (EntityId is int?, cannot store string ID)
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    TenantId = GetTenantId(entity),
                    Action = "Archive",
                    EntityType = typeof(T).Name,
                    EntityId = null, // String IDs cannot be stored in int? field
                    OldValues = "Active",
                    NewValues = reason != null ? $"Archived: {reason}" : "Archived",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Archived {typeof(T).Name} with ID {id} by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error archiving {typeof(T).Name} with ID {id}");
                return false;
            }
        }

        /// <summary>
        /// Restore an archived entity by integer ID
        /// </summary>
        public async Task<bool> RestoreAsync<T>(int id, string userId) where T : class, IArchivable
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null || !entity.IsArchived)
                {
                    return false;
                }

                // Restore the entity
                entity.IsArchived = false;
                entity.ArchivedAt = null;
                entity.ArchivedBy = null;
                entity.ArchiveReason = null;

                // REMOVE the ArchivedItem record (don't just mark as restored)
                var archivedItem = await _context.ArchivedItems
                    .FirstOrDefaultAsync(a => a.EntityType == typeof(T).Name && a.EntityId == id.ToString());

                if (archivedItem != null)
                {
                    _context.ArchivedItems.Remove(archivedItem);
                }

                // Create audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    TenantId = GetTenantId(entity),
                    Action = "Restore",
                    EntityType = typeof(T).Name,
                    EntityId = id,
                    OldValues = "Archived",
                    NewValues = "Active",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Restored {typeof(T).Name} with ID {id} by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring {typeof(T).Name} with ID {id}");
                return false;
            }
        }

        /// <summary>
        /// Restore an archived entity by string ID
        /// </summary>
        public async Task<bool> RestoreAsync<T>(string id, string userId) where T : class, IArchivable
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null || !entity.IsArchived)
                {
                    return false;
                }

                // Restore the entity
                entity.IsArchived = false;
                entity.ArchivedAt = null;
                entity.ArchivedBy = null;
                entity.ArchiveReason = null;

                // REMOVE the ArchivedItem record (don't just mark as restored)
                var archivedItem = await _context.ArchivedItems
                    .FirstOrDefaultAsync(a => a.EntityType == typeof(T).Name && a.EntityId == id);

                if (archivedItem != null)
                {
                    _context.ArchivedItems.Remove(archivedItem);
                }

                // Create audit log (EntityId is int?, cannot store string ID)
                _context.AuditLogs.Add(new AuditLog
                {
                    UserId = userId,
                    TenantId = GetTenantId(entity),
                    Action = "Restore",
                    EntityType = typeof(T).Name,
                    EntityId = null, // String IDs cannot be stored in int? field
                    OldValues = "Archived",
                    NewValues = "Active",
                    Timestamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Restored {typeof(T).Name} with ID {id} by user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring {typeof(T).Name} with ID {id}");
                return false;
            }
        }

        /// <summary>
        /// Get all archived items, optionally filtered by tenant and entity type
        /// </summary>
        public async Task<List<ArchivedItem>> GetArchivedItemsAsync(int? tenantId = null, string? entityType = null)
        {
            var query = _context.ArchivedItems
                .Where(a => a.RestoredAt == null); // Only show items that haven't been restored

            if (tenantId.HasValue)
            {
                query = query.Where(a => a.TenantId == tenantId.Value);
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            return await query
                .OrderByDescending(a => a.ArchivedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Check if an entity can be restored
        /// </summary>
        public async Task<bool> CanRestoreAsync<T>(int id) where T : class, IArchivable
        {
            var archivedItem = await _context.ArchivedItems
                .FirstOrDefaultAsync(a => a.EntityType == typeof(T).Name && a.EntityId == id.ToString());

            return archivedItem?.CanRestore ?? false;
        }

        /// <summary>
        /// Permanently delete an archived entity (SuperAdmin only)
        /// </summary>
        public async Task<bool> PermanentDeleteAsync<T>(int id) where T : class, IArchivable
        {
            try
            {
                var entity = await _context.Set<T>().FindAsync(id);
                if (entity == null || !entity.IsArchived)
                {
                    return false;
                }

                // Remove metadata record
                var archivedItem = await _context.ArchivedItems
                    .FirstOrDefaultAsync(a => a.EntityType == typeof(T).Name && a.EntityId == id.ToString());

                if (archivedItem != null)
                {
                    _context.ArchivedItems.Remove(archivedItem);
                }

                // Permanently delete the entity
                _context.Set<T>().Remove(entity);

                await _context.SaveChangesAsync();

                _logger.LogWarning($"Permanently deleted {typeof(T).Name} with ID {id}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error permanently deleting {typeof(T).Name} with ID {id}");
                return false;
            }
        }

        // Helper methods
        private string GetEntityName(object entity)
        {
            var type = entity.GetType();
            
            // Try common name properties in order of preference
            var nameProperty = type.GetProperty("Name") ?? 
                             type.GetProperty("CompanyName") ??  // For Tenant
                             type.GetProperty("Title") ?? 
                             type.GetProperty("FullName") ??     // For ApplicationUser
                             type.GetProperty("Email") ??        // Fallback for ApplicationUser
                             type.GetProperty("Description");
            
            if (nameProperty != null)
            {
                var value = nameProperty.GetValue(entity)?.ToString();
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            // If no suitable property found, return entity type + ID
            var idProperty = type.GetProperty("Id");
            if (idProperty != null)
            {
                var id = idProperty.GetValue(entity);
                return $"{type.Name} #{id}";
            }

            return "Unknown";
        }

        private int? GetTenantId(object entity)
        {
            var type = entity.GetType();
            var tenantIdProperty = type.GetProperty("TenantId");
            
            if (tenantIdProperty != null)
            {
                return tenantIdProperty.GetValue(entity) as int?;
            }

            return null;
        }
    }
}
