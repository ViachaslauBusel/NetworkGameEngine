---
title: Read Data
nav_order: 5
---

# Reading Data from GameObject

In the GameObject architecture, components from one object cannot directly interact with components of another object. To safely read data from another GameObject, use the ReadData method.

## Creating a Component for Storing and Updating Data

First, let’s create a structure to represent the data:

```csharp
public struct SomeData
{
    public int SomeValue { get; set; }
}

```

Registering a Script to Update Data
The UpdateData method will be automatically called at a designated time to write data to the buffer.

```csharp
public class SomeComponent : Component, IReadData<SomeData>
{
    private int _value;

    public void UpdateData(ref SomeData data)
    {
        data.SomeValue = _value;
    }
}
```

## Reading Data from GameObject

To read the current data, simply call the ReadData method:

```csharp
gameObject.ReadData(out SomeData data);
```

## Reading Data via Interface

If you need to read data from all components implementing a specific interface, you can use ReadAllData:

```csharp
public struct SomeData : ISomeData
{
    public int SomeValue { get; set; }
}
List<ISomeData> data = gameObject.ReadAllData<ISomeData>();
```

In this case, a list of all data matching the ISomeData interface will be returned.
