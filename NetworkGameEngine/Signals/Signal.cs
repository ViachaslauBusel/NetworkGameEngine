namespace NetworkGameEngine.Signals
{
    // Контейнер подписчиков для рассылки уведомлений о событиях. Замена event-ов. 
    public class Signal<TCommand> where TCommand : struct, ICommand
    {
        private readonly List<GameObject> _subscribers = new();
        private readonly object _syncRoot = new();

        // Новые имена
        public void Subscribe(GameObject gameObject)
        {
            if (gameObject is null) throw new ArgumentNullException(nameof(gameObject));
            lock (_syncRoot)
            {
                if (!_subscribers.Contains(gameObject))
                    _subscribers.Add(gameObject);
            }
        }

        public void Unsubscribe(GameObject gameObject)
        {
            if (gameObject is null) throw new ArgumentNullException(nameof(gameObject));
            lock (_syncRoot)
            {
                _subscribers.Remove(gameObject);
            }
        }

        // Публикация команды с значениями по умолчанию
        public void Publish()
        {
            lock (_syncRoot)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.SendCommand<TCommand>(default);
                }
            }
        }

        // Публикация конкретной команды
        public void Publish(in TCommand command)
        {
            lock (_syncRoot)
            {
                foreach (var subscriber in _subscribers)
                {
                    subscriber.SendCommand(command);
                }
            }
        }
    }
}
