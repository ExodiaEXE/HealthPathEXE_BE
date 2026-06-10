using System;

namespace HealthPath.API.Models;

public class CompanionInventory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CatalogItemId { get; set; }
    public bool IsEquipped { get; set; }
    public DateTime AcquiredAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual CompanionCatalogItem CatalogItem { get; set; } = null!;
}
