namespace NetworkGameEngine.JobsSystem
{
    public partial class Job
    {
        public static MillisDelayJob Delay(int millis)
        {
            return new MillisDelayJob(millis);
        }

        public static SecondsDelayJob Delay(float seconds)
        {
            return new SecondsDelayJob(seconds);
        }

        public static WaitUntilJob WaitUntil(Func<bool> predicate)
        {
            return new WaitUntilJob(predicate);
        }

        public static WaitWhileJob WaitWhile(Func<bool> predicate)
        {
            return new WaitWhileJob(predicate);
        }

        /// <summary>
        /// Waits for the task to complete
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Job Wait(Task task)
        {
            await task;
        }

        /// <summary>
        /// Waits for the task to complete and returns the result
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="task"></param>
        /// <returns></returns>
        public static async Job<T> Wait<T>(Task<T> task)
        {
            return await task;
        }
    }
}
