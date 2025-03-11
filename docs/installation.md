---
title: Installation
nav_order: 2
---

# Installation

This guide explains how to set up and launch a world to process GameObject instances. In the Init method, you need to specify:

- **maxThread**: the number of threads that will be used to process all GameObject instances.
- **frameInterval**: the time in milliseconds that will be spent processing a single tick.

### Example of world configuration:

```csharp
World world = new World();
world.Init(maxThread: 8, frameInterval: 100);
```

### Creating and adding components:

To add functionality to GameObject instances, components are created. Below is an example of creating a simple component:

```csharp
public class TestComponent : Component
{
    // Component logic
}
```

After that, the component can be added to a GameObject:

```csharp
GameObject obj = new GameObject("Test");
obj.AddComponent<TestComponent>();
```

Or using a constructor:

```csharp
obj.AddComponent(new TestComponent());
```

### Adding objects to the world:

Once a GameObject is created and configured, it must be added to the world for processing:

```csharp
world.AddGameObject(obj);
```

Now, all scripts attached to this GameObject will be processed within the game loop, bound to one of the threads.
