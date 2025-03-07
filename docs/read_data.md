---
title: Read Data
nav_order: 5
---

# Чтение данных из GameObject

В архитектуре `GameObject` компоненты из одного объекта не могут напрямую взаимодействовать с компонентами другого объекта. Для безопасного чтения данных из другого `GameObject` используется метод `ReadData`.

## Создание компонента для хранения и обновления данных

Сначала создадим структуру, которая будет представлять данные:

```csharp
public struct SomeData
{
    public int SomeValue { get; set; }
}

```

Регистрация скрипта для обновления данных
Метод UpdateData будет автоматически вызываться в специально отведенное время для записи данных в буфер.

```csharp
public class TransformComponent : Component, IReadData<SomeData>
{
    private int _value;

    public void UpdateData(ref SomeData data)
    {
        data.SomeValue = _value;
    }
}
```

## Чтение данных из GameObject

Чтобы прочитать актуальные данные, достаточно вызвать метод ReadData:

```csharp
gameObject.ReadData(out SomeData data);
```

## Чтение данных по интерфейсу

Если нужно прочитать данные всех компонентов, реализующих определенный интерфейс, можно использовать ReadAllData:

```csharp
public struct SomeData : ISomeData
{
    public int SomeValue { get; set; }
}
List<ISomeData> data = gameObject.ReadAllData<ISomeData>();
```

В этом случае возвращается список всех данных, соответствующих интерфейсу ISomeData.