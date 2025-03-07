---
title: Command
nav_order: 5
---

# Взаимодействие GameObject через команды

В архитектуре `GameObject` компоненты из одного объекта не могут напрямую взаимодействовать с компонентами другого объекта. Для вызова метода внутри другого `GameObject` необходимо отправить команду.

## Создание и отправка команды

Создаем команду, реализующую интерфейс `ICommand`:

```csharp
public struct SomeCommand : ICommand
{
    public int SomeValue { get; set; }
}
```

Отправка команды:

```csharp
gameObject.SendCommand(new SomeCommand()
{
    SomeValue = 1
});
```

## Обработка команды в компоненте

Компонент, прикрепленный к GameObject, может подписаться на обработку команды, реализуя интерфейс IReactCommand:

```csharp
public class SomeComponent : Component, IReactCommand<SomeCommand>
{
    public void ReactCommand(ref SomeCommand command)
    {
        // Обработка команды
    }
}
```

Если требуется получить результат выполнения команды, можно использовать метод SendC

## Отправка команды с получением результата

ommandAndReturnResult, указав команду и максимальное время ожидания ответа (в миллисекундах):

```csharp
SomeCommand command = new SomeCommand()
{
    SomeValue = 1
};

CommandResult cmdResult = await gameObject.SendCommandAndReturnResult<SomeCommand, bool>(command, 1_000);
bool result = !cmdResult.IsFailed && cmdResult.Result;
```

В данном примере:

Команда отправляется объекту.
SendCommandAndReturnResult ожидает результат выполнения или таймаут.
cmdResult.IsFailed указывает, была ли ошибка при обработке команды.
Если ошибок нет, результат возвращается в cmdResult.Result.

## Обработка команды с возвратом результата

Чтобы обработать команду и вернуть результат, компонент должен реализовать интерфейс IReactCommandWithResult:

```csharp
public class SomeComponent : Component, IReactCommandWithResult<SomeCommand, bool>
{
    public bool ReactCommand(ref SomeCommand command)
    {
        // Обработка команды
        return true;
    }
}
```