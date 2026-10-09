using Tickie.Manager.Entities;

namespace Tickie.Manager.Rules;

public static class TicketFlow
{
    public static readonly TicketStatus[] FinishedStatuses =
    {
        TicketStatus.Done,
        TicketStatus.Canceled,
        TicketStatus.Adopted,
        TicketStatus.Rejected
    };
    
    private static readonly HashSet<(TicketStatus From, TicketStatus To)> BuildMoves = new()
    {
        (TicketStatus.Todo, TicketStatus.TestCases),
        (TicketStatus.TestCases, TicketStatus.WritingTests),
        (TicketStatus.WritingTests, TicketStatus.Working),
        (TicketStatus.Working, TicketStatus.Testing),
        (TicketStatus.Testing, TicketStatus.Fixing),
        (TicketStatus.Fixing, TicketStatus.Testing),
        (TicketStatus.Fixing, TicketStatus.NeedsDecision),
        (TicketStatus.NeedsDecision, TicketStatus.TestCases),
        (TicketStatus.NeedsDecision, TicketStatus.WritingTests),
        (TicketStatus.NeedsDecision, TicketStatus.Working),
        (TicketStatus.NeedsDecision, TicketStatus.Testing),
        (TicketStatus.Testing, TicketStatus.AwaitingConfirmation),
        (TicketStatus.AwaitingConfirmation, TicketStatus.Done),
    };

    private static readonly HashSet<(TicketStatus From, TicketStatus To)> DesignMoves = new()
    {
        (TicketStatus.Todo, TicketStatus.Demo),
        (TicketStatus.Demo, TicketStatus.Adopted),
        (TicketStatus.Demo, TicketStatus.Rejected),
        (TicketStatus.Demo, TicketStatus.Demo)
    };

    private static readonly HashSet<(TicketStatus From, TicketStatus To)> QaMoves = new()
    {
        (TicketStatus.Todo, TicketStatus.TestCases),
        (TicketStatus.TestCases, TicketStatus.WritingTests),
        (TicketStatus.WritingTests, TicketStatus.WaitingForDev),
        (TicketStatus.WaitingForDev, TicketStatus.Running),
        (TicketStatus.Running, TicketStatus.Done),
        (TicketStatus.Running, TicketStatus.Analyzing),
        (TicketStatus.Running, TicketStatus.TestCases),
        (TicketStatus.Analyzing, TicketStatus.WaitingForDev),
        (TicketStatus.Done, TicketStatus.WaitingForDev)
    };

    public static bool IsAllowed(TicketType type, TicketStatus from, TicketStatus to)
    {
        if (type == TicketType.Build || type == TicketType.Debug)
        {
            if (to == TicketStatus.Canceled)
            {
                return from != TicketStatus.Done && from != TicketStatus.Canceled;
            }
            return BuildMoves.Contains((from, to));
        }

        if (type == TicketType.Design)
        {
            return DesignMoves.Contains((from, to));
        }

        if (type == TicketType.QA)
        {
            return QaMoves.Contains((from, to));
        }

        throw new InvalidOperationException($"No flow defined for ticket type {type}.");
    }
}