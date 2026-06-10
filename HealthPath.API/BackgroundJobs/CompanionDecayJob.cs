using System;
using System.Linq;
using System.Threading.Tasks;
using HealthPath.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthPath.API.BackgroundJobs;

public interface ICompanionDecayJob
{
    Task ExecuteAsync();
}

public class CompanionDecayJob : ICompanionDecayJob
{
    private readonly HealthpathDbContext _context;
    private readonly ILogger<CompanionDecayJob> _logger;

    public CompanionDecayJob(HealthpathDbContext context, ILogger<CompanionDecayJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        var pets = await _context.UserCompanions.ToListAsync();
        var now = DateTime.UtcNow;
        foreach (var pet in pets)
        {
            var hours = (now - pet.LastDecayAt).TotalHours;
            if (hours < 1) continue;
            pet.Hunger = Math.Max(0, pet.Hunger - (int)(hours * 1.25));
            pet.Happiness = Math.Max(0, pet.Happiness - (int)(hours * 0.5));
            pet.Energy = Math.Max(0, pet.Energy - (int)(hours * 1.0));
            pet.LastDecayAt = now;
            pet.UpdatedAt = now;
        }
        if (pets.Any())
        {
            await _context.SaveChangesAsync();
        }
        _logger.LogInformation("CompanionDecayJob updated {Count} pets.", pets.Count);
    }
}
