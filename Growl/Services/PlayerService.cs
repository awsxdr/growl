namespace Growl.Services
{
    using System;

    public class PlayerService
    {
        private readonly GameRunner _gameRunner;
        private readonly Guid _sessionId;

        public PlayerService(GameRunner gameRunner, Guid sessionId)
        {
            _gameRunner = gameRunner;
            _sessionId = sessionId;
        }
    }
}