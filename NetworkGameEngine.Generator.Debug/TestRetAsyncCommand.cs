using NetworkGameEngine;

namespace NetworkGameEngine.Generator.Debug
{
    public struct TestRetAsyncCommand : ICommand
    {
        public int SomeValue { get; }

        public TestRetAsyncCommand()
        {
        }

        public TestRetAsyncCommand(int someValue)
        {
            SomeValue = someValue;
        }

        public TestRetAsyncCommand(int arg_0, string arg_1)
        {
            SomeValue = arg_0;
        }

        public TestRetAsyncCommand(int arg_0, string seconArg, float justForTest)
        {
            SomeValue = arg_0;
        }
    }
}