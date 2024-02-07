namespace Newtera;

public interface INewteraClientFactory
{
    INewteraClient CreateClient();
    INewteraClient CreateClient(Action<INewteraClient> configureClient);
}
