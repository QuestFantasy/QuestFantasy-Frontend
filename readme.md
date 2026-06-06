# QuestFantasy Client (Frontend)

This is the game client for **QuestFantasy**, built using **Godot Engine 3.6.2 Mono Edition** and C# (.NET).

---

## 🛠️ Prerequisites

Before you compile and run the client, make sure your local system has:

1.  **Godot Engine 3.6.2 Mono Edition**:
    *   Download from the [Godot Engine Download Archive](https://godotengine.org/download/archive/3.6.2-stable/).
    *   *Note: You MUST download the **Mono** version. The standard version does not support C# scripts.*
2.  **.NET SDK 6.0**:
    *   Download from the [.NET Official Website](https://dotnet.microsoft.com/en-us/download/dotnet/6.0).
    *   Verify the installation by running:
        ```bash
        dotnet --version
        ```

---

## 🚀 Build Instructions

Restore the NuGet packages and compile the C# solution using the .NET CLI:

```bash
# Restore package dependencies
dotnet restore

# Build the QuestFantasy solution
dotnet build QuestFantasy.sln
```

---

## 🎨 Code Formatting

To maintain a consistent coding style across the codebase, you can run the built-in dotnet formatter:

```bash
dotnet format --verbosity diagnostic QuestFantasy.sln
```

---

## 🎮 Running the Game

### Method 1: Using the Godot Editor (Recommended)
1.  Open the **Godot 3.6.2 Mono** engine.
2.  Click **Import** and select the [project.godot](file:///home/darrin/QuestFantasy/frontend/project.godot) file inside this directory.
3.  Click the **Play** button (F5) in the top-right corner to compile and run the game.

### Method 2: Command Line (CLI)
Run the game client directly from the console by passing the path:
```bash
godot --path .
```

---

## 🌐 Connecting to the Backend

The frontend connects to the server API to handle authentication, data synchronization, and store transactions.
*   **Default URL**: `http://127.0.0.1:8000` (matches the local Docker environment).
*   **Custom Server Address**: If you host the backend on a different port or server, adjust the `BackendBaseUrl` parameter inside the Godot Editor inspector on these scripts:
    *   [AuthApiClient.cs](file:///home/darrin/QuestFantasy/frontend/Scripts/Auth/AuthApiClient.cs)
    *   [AuthFlowController.cs](file:///home/darrin/QuestFantasy/frontend/Scripts/Auth/AuthFlowController.cs)
    *   [Main.cs](file:///home/darrin/QuestFantasy/frontend/Scripts/Main.cs)