namespace Starter.UnitTests.Fakes;

internal interface IStagedStore
{
    void CommitStagedChanges();

    void DiscardStagedChanges();
}
