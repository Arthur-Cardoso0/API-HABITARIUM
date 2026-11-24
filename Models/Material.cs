using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Habitarium.Api.Models;

public class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string Company { get; set; } = "";
    public string Brand { get; set; } = "";
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int Stock { get; set; }
    public bool IsSponsored { get; set; }
    public bool HasOwnInstallation { get; set; }
    public Dictionary<string, string> Specs { get; set; } = new();
}