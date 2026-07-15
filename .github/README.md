<p align="center">
  <img src="graphics/logo.png" width="250" alt="Reflex Logo">
</p>

---

This repository is a fork of the original [Reflex](https://github.com/gustavopsantos/reflex) dependency injection library for Unity.

This fork is maintained in alignment with the original repository. All general credits go to the original author, [Gustavo Santos](https://github.com/gustavopsantos).

This fork brings signature changes along with new features and quality-of-life (QoL) improvements.

---

## 1. ⚙️ `IInitializable` Lifecycle

This fork introduces the `IInitializable` interface, providing a standardized way to execute initialization logic automatically after an object is resolved and injected.

```csharp
using Reflex.Generics.Interfaces;

public class GameService : IInitializable
{
    public void Initialize()
    {
        // Executes automatically after constructor and attribute injection are completed
        UnityEngine.Debug.Log("GameService initialized successfully!");
    }
}
```

### How & When it Executes
When the container resolves a type or factory (Singleton, Scoped, or Transient), it performs construction, runs attribute injection, and then checks if the resolved instance implements `IInitializable`. If it does, `Initialize()` is invoked.

> [!NOTE]
> **Exceptions (When `Initialize()` is NOT called):**
> * **Pre-existing Instances (`BindInstance` / `BindInstanceTo`)**: Since these objects are created outside the container, the container does not control their lifecycle or invoke initialization.
> * **Manual Injection (`AttributeInjector.Inject`, `GameObjectInjector.Inject*`)**: When you manually inject dependencies into an existing object (e.g. MonoBehaviours placed in a scene), it is a target of injection but not a container-resolved instance, meaning `Initialize()` must be handled manually or via Unity lifecycle methods.
> * **Instantiated Prefabs (from Mono Factories)**: Clone components instantiated by `MonoFactory` are injected recursively, not resolved from the container directly, meaning they rely on Unity's standard `Awake()` and `Start()` methods instead of `IInitializable`.

---

## 2. 🏗️ Mono Factories & Factory Scope

We introduced a comprehensive system for instantiating Unity `MonoBehaviour` prefabs with dependency injection, supporting both parameterless creation and runtime data propagation, alongside isolated scoping.

### The Two Factory Types
To create a factory, inherit from one of the two abstract base classes:

#### A. Standard `MonoFactory<T>`
Used to instantiate prefabs without passing any runtime parameters.
```csharp
using Reflex.Factories.Mono;
using UnityEngine;

// Define your factory
public class EnemyFactory : MonoFactory<Enemy> { }

// Usage
public class SpawnManager : MonoBehaviour
{
    [Inject] private readonly EnemyFactory _enemyFactory;

    private void SpawnEnemy()
    {
        Enemy enemy = _enemyFactory.Create();
    }
}
```

#### B. Parameterized `MonoFactory<TData, T>`
Used to pass runtime parameters (`TData`) to the created prefab. The prefab component must implement `IFactoryData<TData>`.
```csharp
using Reflex.Factories.Mono;
using UnityEngine;

// 1. Define the data container
public struct EnemyData
{
    public int Health;
    public float Speed;
}

// 2. Implement the component on your prefab
public class Enemy : MonoBehaviour, IFactoryData<EnemyData>
{
    public EnemyData Data { get; set; } // Set automatically by the factory
    
    [Inject] private readonly IAudioService _audioService; // Resolved from DI
}

// 3. Define the factory
public class EnemyWithDataFactory : MonoFactory<EnemyData, Enemy> { }

// Usage
public class SpawnManager : MonoBehaviour
{
    [Inject] private readonly EnemyWithDataFactory _enemyFactory;

    private void SpawnEnemy()
    {
        var data = new EnemyData { Health = 100, Speed = 5.5f };
        Enemy enemy = _enemyFactory.Create(data);
    }
}
```

### 🌐 Factory Scope (Isolated Scopes)
When binding a factory, you can enable `hasFactoryScope: true`. This creates a completely isolated child container scope unique to that cloned instance.

1. **Setup**: Attach a custom class inheriting from `FactoryScope` to your prefab.
2. **Install Local Bindings**: Implement `InstallBindings` to register dependencies specific to this cloned instance:
   ```csharp
   using Reflex.Core;
   using Reflex.Factories.Mono;
   
   public class BossFactoryScope : FactoryScope
   {
       public override void InstallBindings(ContainerBuilder builder)
       {
           // Register local systems specific to this prefab instance
           builder.Bind<BossHealthSystem>(Lifetime.Scoped);
       }
   }
   ```
3. **Data Injection**: If using `MonoFactory<TData, T>`, the `TData` instance is automatically registered into the child scope's container, allowing child components of the prefab to inject `TData` directly.
4. **Automatic Disposal**: When the cloned GameObject is destroyed, `FactoryScope.OnDestroy()` automatically disposes of the child container, cleaning up scoped disposables and preventing memory leaks.

---

## 3. 🔩 Updated Binding API

To provide a cleaner, more fluent syntax, the original `Register*` methods in `ContainerBuilder` have been made `internal`. A new public set of `Bind` methods replaces them.

### Quick Comparison
| Old API | New Clean API | Description |
| :--- | :--- | :--- |
| `RegisterType(type, ...)` | `Bind<TConcrete>(...)` | Binds a type to itself. |
| `RegisterType(type, contracts, ...)` | `Bind<TContract, TConcrete>(...)` | Binds a concrete type to one or more interface contracts (up to 7). |
| `RegisterValue(value)` | `BindInstance(value)` | Registers an existing object instance as a singleton. |
| `RegisterValue(value, contracts)` | `BindInstanceTo<T>(value)` | Registers an existing instance to a specific interface contract. |
| N/A | `BindInterFaces<TConcrete>(...)` | Binds a type to all interfaces it implements. |
| N/A | `BindInterFacesAndSelf<TConcrete>(...)` | Binds a type to all interfaces it implements, plus itself. |
| N/A | `BindMonoFactory<T, TFactory>(...)` | Registers a custom MonoBehaviour factory. |

### Registration Examples
```csharp
public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        // 1. Self Binding
        builder.Bind<SaveSystem>(Lifetime.Singleton);

        // 2. Contract Binding (Single contract)
        builder.Bind<IInputManager, UnityInputManager>(Lifetime.Singleton);

        // 3. Contract Binding (Multiple contracts)
        builder.Bind<IShooter, IMover, PlayerController>(Lifetime.Singleton);

        // 4. Interface Autodiscovery
        builder.BindInterFaces<NetworkManager>(Lifetime.Singleton);
        builder.BindInterFacesAndSelf<GameLoop>(Lifetime.Singleton);

        // 5. Existing Instance Bindings
        builder.BindInstance(new AppConfig { Version = "1.0.0" });
        builder.BindInstanceTo<IConfig>(new AppConfig());

        // 6. Mono Factory Bindings
        builder.BindMonoFactory<Enemy, EnemyFactory>(enemyPrefab, hasFactoryScope: false);
    }
}
```
