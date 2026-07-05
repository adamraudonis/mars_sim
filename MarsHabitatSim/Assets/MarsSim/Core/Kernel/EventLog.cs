using System;
using System.Collections.Generic;

namespace MarsSim.Core
{
    public enum EventSeverity { Info, Milestone, Warning, Critical }

    public readonly struct SimEvent
    {
        public readonly double Sol;
        public readonly EventSeverity Severity;
        public readonly string Source;
        public readonly string Message;

        public SimEvent(double sol, EventSeverity severity, string source, string message)
        {
            Sol = sol; Severity = severity; Source = source; Message = message;
        }

        public override string ToString() => $"[Sol {Sol:F1}] {Severity} {Source}: {Message}";
    }

    /// <summary>Discrete event stream: landings, failures, storms, milestones, alarms.</summary>
    public sealed class EventLog
    {
        private readonly List<SimEvent> _events = new();
        public IReadOnlyList<SimEvent> Events => _events;

        /// <summary>Raised immediately when an event is logged (UI ticker subscribes).</summary>
        public event Action<SimEvent> OnEvent;

        public void Log(double sol, EventSeverity severity, string source, string message)
        {
            var e = new SimEvent(sol, severity, source, message);
            _events.Add(e);
            OnEvent?.Invoke(e);
        }

        public int Count => _events.Count;
    }
}
