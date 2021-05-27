namespace Growl.Services
{
    using System;
    using Func;
    using static Func.Option;

    public class SessionService
    {
        public Option<Guid> SessionId { get; private set; } = None<Guid>();

        public Guid GetOrInitSessionId() =>
            SessionId is Some<Guid> v ? v.Value : InitSessionId();

        public void SetSessionId(Guid sessionId) =>
            SessionId = Some(sessionId);

        private Guid InitSessionId()
        {
            var sessionId = Guid.NewGuid();
            SessionId = Some(sessionId);
            return sessionId;
        }
    }
}