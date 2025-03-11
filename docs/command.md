---
title: Command
nav_order: 5
---

# GameObject Interaction via Commands

In the GameObject architecture, components of one object cannot directly interact with components of another object. To call a method inside another GameObject, you need to send a command.

## Creating and Sending a Command

Create a command by implementing the ICommand interface:

```csharp
public struct SomeCommand : ICommand
{
    public int SomeValue { get; set; }
}
```

Send the command:

```csharp
gameObject.SendCommand(new SomeCommand()
{
    SomeValue = 1
});
```

## Handling the Command in a Component

A component attached to a GameObject can subscribe to handle the command by implementing the IReactCommand interface:

```csharp
public class SomeComponent : Component, IReactCommand<SomeCommand>
{
    public void ReactCommand(ref SomeCommand command)
    {
       // Command handling
    }
}
```

## Sending a Command and Receiving a Result

If you need to get the result of the command execution, you can use the SendCommandAndReturnResult method, sends a command and waits for a response, specifying the maximum wait time in milliseconds:

```csharp
SomeCommand command = new SomeCommand()
{
    SomeValue = 1
};

CommandResult cmdResult = await gameObject.SendCommandAndReturnResult<SomeCommand, bool>(command, 1_000);
bool result = !cmdResult.IsFailed && cmdResult.Result;
```

In this example:
- The command is sent to the object.
- SendCommandAndReturnResult waits for the command to complete or times out.
- cmdResult.IsFailed indicates whether there was an error during command processing.
- If there are no errors, the result is returned in cmdResult.Result.

## Handling a Command and Returning a Result

To handle a command and return a result, the component must implement the IReactCommandWithResult interface:

```csharp
public class SomeComponent : Component, IReactCommandWithResult<SomeCommand, bool>
{
    public bool ReactCommand(ref SomeCommand command)
    {
        // Command handling
        return true;
    }
}
```
