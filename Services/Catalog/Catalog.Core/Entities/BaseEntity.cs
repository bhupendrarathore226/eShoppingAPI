using System.ComponentModel.DataAnnotations;

namespace Catalog.Core.Entities;

public class BaseEntity
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
}