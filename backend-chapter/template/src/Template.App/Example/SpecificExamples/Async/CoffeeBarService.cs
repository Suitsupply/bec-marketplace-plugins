using Microsoft.Extensions.Logging;
using Template.App.Example.SpecificExamples.Async.Interfaces;
using Template.App.Example.SpecificExamples.Async.Models;

namespace Template.App.Example.SpecificExamples.Async;

// Demonstrates the chapter async conventions:
//   * start independent I/O without awaiting, then await each task for its result (no Task.WhenAll needed for Task<T>);
//   * use Task.WhenAll for independent side effects that return Task (nothing to read back);
//   * fan out over a collection and await the whole batch in parallel.
public sealed class CoffeeBarService(IEspressoMachine espressoMachine, IMilkSteamer milkSteamer, IReceiptPrinter receiptPrinter, ISalesLogger salesLogger, ILogger<CoffeeBarService> logger)
{
    public async Task<PreparedDrink> PrepareDrinkAsync(DrinkOrder order, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        logger.LogInformation("Preparing {DrinkName} for {CustomerName}.", order.DrinkName, order.CustomerName);

        // Pulling the shot and steaming the milk are independent — start both, then await each result.
        var shotTask = espressoMachine.PullShotsAsync(order.ShotCount, cancellationToken);
        var milkTask = milkSteamer.SteamAsync(order.MilkMillilitres, cancellationToken);
        var shot = await shotTask;
        var milk = await milkTask;

        var drink = new PreparedDrink(order.CustomerName, order.DrinkName, shot.PullTime + milk.SteamTime);

        // Printing the receipt and recording the sale are independent side effects with no return value — WhenAll.
        var printTask = receiptPrinter.PrintAsync(drink, cancellationToken);
        var logTask = salesLogger.RecordAsync(drink, cancellationToken);
        await Task.WhenAll(printTask, logTask);

        return drink;
    }
}