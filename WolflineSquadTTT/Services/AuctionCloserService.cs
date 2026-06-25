namespace WolflineSquadTTT.Services
{
    // Periodically settles auctions whose end time has passed (award the winner, or return the item to the
    // seller if there were no bids). Settlement is skipped while no GMod socket is connected and retried later.
    public class AuctionCloserService : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromSeconds(30);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionCloserService> _logger;

        public AuctionCloserService(IServiceScopeFactory scopeFactory, ILogger<AuctionCloserService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new PeriodicTimer(Interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using IServiceScope scope = _scopeFactory.CreateScope();
                    IMarketService market = scope.ServiceProvider.GetRequiredService<IMarketService>();
                    await market.CloseExpiredAuctionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Auction close tick failed.");
                }
            }
        }
    }
}
