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

> [!NOTE]
> **Exceptions (When `Initialize()` is NOT called):**
> * **Pre-existing Instances (`BindInstance` / `BindInstanceTo`)**: Since these objects are created outside the container, the container does not control their lifecycle or invoke initialization.
> * **Manual Injection (`AttributeInjector.Inject`, `GameObjectInjector.Inject*`)**: When you manually inject dependencies into an existing object (e.g. MonoBehaviours placed in a scene), it is a target of injection but not a container-resolved instance, meaning `Initialize()` must be handled manually or via Unity lifecycle methods.
> * **Instantiated Prefabs (from Mono Factories)**: Clone components instantiated by `MonoFactory` are injected recursively, not resolved from the container directly, meaning they rely on Unity's standard `Awake()` and `Start()` methods instead of `IInitializable`.

---

## 2. 🏗️ Mono Factories & Factory Scope

We introduced a comprehensive system for instantiating Unity `MonoBehaviour` prefabs with dependency injection, supporting both parameterless creation and runtime data propagation, alongside isolated scoping.

### The Two Factory Types & Installer Bindings
To create and register a Mono Factory, follow the steps below:

#### A. Standard `MonoFactory<T>`
Used to instantiate prefabs without passing any runtime parameters.

1. **Define the Component & Factory**:
```csharp
using Reflex.Factories.Mono;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Inject] private readonly IAudioService _audioService;
}

// Define the factory class inheriting from MonoFactory<T>
public class EnemyFactory : MonoFactory<Enemy> { }
```

2. **Register in Installer**:
Use `BindMonoFactory` in your installer to register the factory, providing the reference to your prefab:
```csharp
using Reflex.Core;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        // Registers EnemyFactory in the container, linked to enemyPrefab
        builder.BindMonoFactory<Enemy, EnemyFactory>(enemyPrefab);
    }
}
```

3. **Usage**:
Resolve and use the factory anywhere dependencies are injected:
```csharp
public class SpawnManager : MonoBehaviour
{
    [Inject] private readonly EnemyFactory _enemyFactory;

    private void SpawnEnemy()
    {
        Enemy enemy = _enemyFactory.Create();
    }
}
```

---

#### B. Parameterized `MonoFactory<TData, T>`
Used to pass runtime parameters (`TData`) to the created prefab. The prefab component must implement `IFactoryData<TData>`.

1. **Define the Data, Component, & Factory**:
```csharp
using Reflex.Factories.Mono;
using UnityEngine;

// Define the data container
public struct EnemyData
{
    public int Health;
    public float Speed;
}

// Component must implement IFactoryData<TData>
public class Enemy : MonoBehaviour, IFactoryData<EnemyData>
{
    public EnemyData Data { get; set; } // Automatically populated by the factory
    
    [Inject] private readonly IAudioService _audioService;
}

// Define the factory class inheriting from MonoFactory<TData, T>
public class EnemyWithDataFactory : MonoFactory<EnemyData, Enemy> { }
```

2. **Register in Installer**:
Use `BindMonoFactory` to register the parameterized factory:
```csharp
using Reflex.Core;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindMonoFactory<Enemy, EnemyWithDataFactory>(enemyPrefab);
    }
}
```

3. **Usage**:
Pass the runtime data during instantiation:
```csharp
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

---

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
           builder.Bind<BossHealthSystem>();
       }
   }
   ```
3. **Register in Installer**:
   Set `hasFactoryScope` to `true` when registering in your installer:
   ```csharp
   using Reflex.Core;
   using UnityEngine;

   public class BossInstaller : MonoBehaviour, IInstaller
   {
       [SerializeField] private Boss bossPrefab; // Attached BossFactoryScope

       public void InstallBindings(ContainerBuilder builder)
       {
           builder.BindMonoFactory<Boss, BossFactory>(bossPrefab, hasFactoryScope: true);
       }
   }
   ```
4. **Data Injection**: If using `MonoFactory<TData, T>`, the `TData` instance is automatically registered into the child scope's container, allowing child components of the prefab to inject `TData` directly.
5. **Automatic Disposal**: When the cloned GameObject is destroyed, `FactoryScope.OnDestroy()` automatically disposes of the child container, cleaning up scoped disposables and preventing memory leaks.

---

## 3. 🔩 Updated Binding API

To provide a cleaner, more fluent syntax, the original `Register*` methods in `ContainerBuilder` have been made `internal`. A new public set of `Bind` methods replaces them.

### Binding API Methods
| Method Signature | Description |
| :--- | :--- |
| `Bind<TConcrete>(...)` | Binds a concrete type to itself. |
| `Bind<TContract, TConcrete>(...)` | Binds a concrete type to a specific interface contract (supports overloads up to 7 contracts, e.g. `Bind<T1, T2, ..., TConcrete>`). |
| `BindInstance(object instance)` | Registers an existing object instance as a singleton. |
| `BindInstanceTo<T>(object instance)` | Registers an existing instance to a specific interface contract. |
| `BindInterFaces<TConcrete>(...)` | Binds a type to all interfaces it implements. |
| `BindInterFacesAndSelf<TConcrete>(...)` | Binds a type to all interfaces it implements, plus itself. |
| `BindMonoFactory<T, TFactory>(...)` | Registers a custom MonoBehaviour factory. |

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
