namespace NetworkGameEngine.Database
{
    public interface IDatabaseRequestBuilder
    {
        DatabaseRequest<TResult> BuildRequest<TResult>();
    }
}