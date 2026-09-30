namespace Tickie.Manager.Entities;

public enum TicketStatus
{
    Todo,
    TestCases,
    WritingTests,
    Done,
    
    // Build / Debug
    Working,
    Testing,
    Fixing,
    NeedsDecision,
    AwaitingConfirmation,
    Canceled,

    // Design
    Demo,
    Adopted,
    Rejected,

    // QA
    WaitingForDev,
    Running,
    Analyzing,

}