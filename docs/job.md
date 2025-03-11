---
title: Job
nav_order: 4
---

# Asynchronous Task Execution

**Job** provides the ability to continue executing code on the same thread after the await operator. It encapsulates a Task and waits for its completion or for specified conditions to be met.

### Waiting for 'Task' Completion

To wait for an asynchronous task to complete, use the Job.Wait method. Example:

```csharp
await Job.Wait(Task.Delay(5_000));
// Code execution will continue on the thread associated with the GameObject
```

### Waiting for a Condition to Be Met

You can wait for a specific condition using Job.WaitWhile or Job.WaitUntil. Example:

```csharp
await Job.WaitWhile(() => Time.CurrentMillis < TimeMark);
// Code execution will continue on the thread associated with the GameObject
```

### List of Available Job Methods
Job.Delay(int millis) — delays execution for the specified number of milliseconds.<br>
Job.Delay(float seconds) — delays execution for the specified number of seconds.<br>
Job.WaitUntil(Func<bool> predicate) — waits until the predicate function returns true.<br>
Job.WaitWhile(Func<bool> predicate) —  waits while the predicate function returns true.<br>
Job.Wait(Task task) — waits for the Task to complete.<br>
Job.Wait<T>(Task<T> task) — waits for the Task<T> to complete and returns the result.<br>
