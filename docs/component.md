---
title: Component
nav_order: 3
---

# Event Function Execution Order

- **Init()**: A method in the script that executes once during object initialization. It is used to prepare the object before it starts working.

- **Start()**: A method in the script that executes once during object initialization. It’s typically used for initial setup and activating components after they have been initialized.

- **Update()**: A method in the script that runs every tick. This is where object logic is updated, such as movement, state checks, and other continuous operations.

- **Command**: Handles commands received by the game object. These commands can be sent to the object for execution within a single processing cycle.

- **Job**: Executes code after the await operator if the task inside the job is complete. This method is used for asynchronous operations that can run in parallel with other tasks.

- **LateUpdate()**: A method in the script that runs every tick, but after all Update() calls. It’s designed for logic that needs to depend on changes made during Update() processing.

- **OnDestroy()**: A method in the script that runs once when an object or component is deleted. This method is used for resource cleanup and object shutdown.

- **UpdateData**: Reads data into a thread-safe buffer. This method is necessary for working with multithreading, ensuring safe access to data.

- **Services**: Handles service logic. This stage is responsible for tasks like synchronizing data between objects, interacting with external services, and more.

![Screenshot of my project](images/component.png)
