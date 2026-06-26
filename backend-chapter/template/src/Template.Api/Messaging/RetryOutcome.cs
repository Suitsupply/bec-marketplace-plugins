namespace Template.Api.Messaging;

public enum RetryOutcome
{
    Rescheduled,
    DeadLettered
}