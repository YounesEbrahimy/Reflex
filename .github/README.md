<p align="center">
  <img src="graphics/logo.png" width="250" alt="Reflex Logo">
</p>
<div align=center>

![Unity Tests](https://github.com/YounesEbrahimy/Reflex/actions/workflows/test.yml/badge.svg)
[![Releases](https://img.shields.io/github/release/younesebrahimy/reflex.svg)](https://github.com/YounesEbrahimy/Reflex/releases)
[![Unity](https://img.shields.io/badge/Unity-2021+-yellow.svg)](https://unity3d.com/pt/get-unity/download/archive)

</div>

---

This repository is a fork of the original [Reflex](https://github.com/gustavopsantos/reflex) dependency injection
library for Unity.

This fork is maintained in alignment with the original repository. All general credits go to the original
author, [Gustavo Santos](https://github.com/gustavopsantos).

This fork brings signature changes along with new features and quality-of-life improvements.

---

## 💾 Installation

### Via Unity Package Manager (Git URL)

1. Open **Window → Package Manager**
2. Click the **+** button → **Add package from git URL ...**
3. Enter:
   ```
   https://github.com/YounesEbrahimy/Reflex.git?path=/Assets/Reflex/#14.3.1
   ```

### Manual

Download the latest .unitypackage file form [Releases](https://github.com/YounesEbrahimy/Reflex/releases), And import it
in your project.

---

## 1. ⚙️ `IInitializable` Lifecycle

This fork introduces the `IInitializable` interface, providing a standardized way to execute initialization logic
automatically after an object is resolved and injected.

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

When the container resolves a type or factory (Singleton, Scoped, or Transient), it performs construction, runs
attribute injection, and then checks if the resolved instance implements `IInitializable`. If it does, `Initialize()` is
invoked.

> [!NOTE]
> **Exceptions (When `Initialize()` is NOT called):**
> * **Pre-existing Instances (`BindInstance` / `BindInstanceTo`)**: Since these objects are created outside the
    container, the container does not control their lifecycle or invoke initialization.
> * **Manual Injection (`AttributeInjector.Inject`, `GameObjectInjector.Inject*`)**: When you manually inject
    dependencies into an existing object (e.g. MonoBehaviours placed in a scene), it is a target of injection but not a
    container-resolved instance, meaning `Initialize()` must be handled manually or via Unity lifecycle methods.
> * **Instantiated Prefabs (from Mono Factories)**: Clone components instantiated by `MonoFactory` are injected
    recursively, not resolved from the container directly, meaning they rely on Unity's standard `Awake()` and `Start()`
    methods instead of `IInitializable`.

---

## 2. 🏭 Factories

We introduced a comprehensive system for instantiating plain C# objects (POCOs) and Unity `MonoBehaviour` prefabs with
dependency injection, supporting both parameterless creation and runtime data propagation.

### C# Plain Factories

To create and register a C# Plain Factory, follow the steps below:

#### A. Standard `Factory<T>`

Used to instantiate objects without passing any runtime parameters.

1. **Define the Class & Factory**:

```csharp
using Reflex.Factories.Plain;

public class Player
{
    [Inject] private readonly IInventoryService _inventoryService;
}

// Define the factory class inheriting from Factory<T>
public class PlayerFactory : Factory<Player> { }
```

2. **Register in Installer**:
   Use `BindFactory` in your installer to register the factory:

```csharp
using Reflex.Core;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        // Registers PlayerFactory in the container
        builder.BindFactory<Player, PlayerFactory>();
    }
}
```

3. **Usage**:
   Resolve and use the factory anywhere dependencies are injected:

```csharp
public class PlayerSpawner
{
    private readonly PlayerFactory _playerFactory;

    public PlayerSpawner(PlayerFactory playerFactory)
    {
        _playerFactory = playerFactory;
    }

    public void Spawn()
    {
        Player player = _playerFactory.Create();
    }
}
```

#### B. Parameterized `Factory<TData, T>`

Used to pass runtime parameters (`TData`) to the created object. The target class must implement `IData<TData>`.

1. **Define the Data, Class, & Factory**:

```csharp
using Reflex.Factories;
using Reflex.Factories.Plain;
using Reflex.DataTypes.Interfaces;

// Define the data container
public struct PlayerData
{
    public string Name;
    public int Level;
}

// Target class must implement IData<TData>
public class Player : IData<PlayerData>
{
    public PlayerData Data { get; set; } // Automatically populated by the factory
    
    [Inject] private readonly IInventoryService _inventoryService;
}

// Define the factory class inheriting from Factory<TData, T>
public class PlayerWithDataFactory : Factory<PlayerData, Player> { }
```

2. **Register in Installer**:
   Use `BindFactory` to register the parameterized factory:

```csharp
using Reflex.Core;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindFactory<Player, PlayerWithDataFactory>();
    }
}
```

3. **Usage**:
   Pass the runtime data during instantiation:

```csharp
public class PlayerSpawner
{
    private readonly PlayerWithDataFactory _playerFactory;

    public PlayerSpawner(PlayerWithDataFactory playerFactory)
    {
        _playerFactory = playerFactory;
    }

    public void Spawn()
    {
        var data = new PlayerData { Name = "Hero", Level = 1 };
        Player player = _playerFactory.Create(data);
    }
}
```

---

### Unity Mono Factories & Factory Scope

To instantiate prefabs via Mono Factories, follow similar steps:

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

#### B. Parameterized `MonoFactory<TData, T>`

Used to pass runtime parameters (`TData`) to the created prefab. The prefab component must implement `IData<TData>`.

1. **Define the Data, Component, & Factory**:

```csharp
using Reflex.Factories.Mono;
using Reflex.DataTypes.Interfaces;
using UnityEngine;

// Define the data container
public struct EnemyData
{
    public int Health;
    public float Speed;
}

// Component must implement IData<TData>
public class Enemy : MonoBehaviour, IData<EnemyData>
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

#### 🌐 Factory Scope (Isolated Scopes)

When binding a Mono factory, you can enable `hasFactoryScope: true`. This creates a completely isolated child container
scope unique to that cloned instance.

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
4. **Data Injection**: If using `MonoFactory<TData, T>`, the `TData` instance is automatically registered into the child
   scope's container, allowing child components of the prefab to inject `TData` directly.
5. **Automatic Disposal**: When the cloned GameObject is destroyed, `FactoryScope.OnDestroy()` automatically disposes of
   the child container, cleaning up scoped disposables and preventing memory leaks.

---

## 3. ♻️ Object Pooling

We introduced a comprehensive object pooling system to reuse objects dynamically, supporting both plain C# objects and
Unity `MonoBehaviour` prefabs, featuring automatic lifecycle management and data propagation.

### C# Plain Pools

#### A. Standard `Pool<T>`

Used to pool plain C# objects without any runtime parameters. It optimally reuses instances without creating garbage.

1. **Define the Class & IPoolable**:

```csharp
using Reflex.Pools.Interfaces;

// Inheriting IPoolable is optional but allows hooking into Take/Return events.
public class Bullet : IPoolable
{
    public void OnSpawn() { /* Called when taken */ }
    public void OnDespawn() { /* Called when returned */ }
}
```

2. **Register in Installer**:
   You can define initial pre-warm sizes, min sizes, and max sizes:

```csharp
using Reflex.Core;
using Reflex.Pools.Plain;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindPool<Bullet, Pool<Bullet>>(minSize: 0, maxSize: 50, preWarmSize: 10);
    }
}
```

3. **Usage**:
   Inject the pool and use `Take()` and `Return(instance)`:

```csharp
public class Weapon
{
    [Inject] private readonly Pool<Bullet> _bulletPool;
    
    public void Fire()
    {
        Bullet bullet = _bulletPool.Take();
        
        // Later when it dies...
        _bulletPool.Return(bullet);
    }
}
```

#### B. Parameterized `Pool<TData, T>`

Used to pool plain C# objects while passing runtime data (`TData`) when taking them from the pool. The target class must
implement `IData<TData>`.

1. **Define Data & Implement Interface**:

```csharp
using Reflex.DataTypes.Interfaces;
using Reflex.Pools.Interfaces;

public struct BulletData { public float Damage; }

public class Bullet : IData<BulletData>, IPoolable
{
    public BulletData Data { get; set; } // Populated automatically before OnSpawn
    
    public void OnSpawn() { }
    public void OnDespawn() { }
}
```

2. **Register in Installer**:

```csharp
using Reflex.Core;
using Reflex.Pools.Plain;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindPool<Bullet, Pool<BulletData, Bullet>>();
    }
}
```

3. **Usage**:

```csharp
public class Weapon
{
    [Inject] private readonly Pool<BulletData, Bullet> _bulletPool;
    
    public void Fire()
    {
        var data = new BulletData { Damage = 10f };
        Bullet bullet = _bulletPool.Take(data);
        
        // Later...
        _bulletPool.Return(bullet);
    }
}
```

---

### Unity Mono Pools

#### A. Standard `MonoPool<T>`

Used to pool Unity prefabs without any runtime parameters. It dynamically manages GameObject activation and child-parent
hierarchies in the scene.

1. **Define the Component**:

```csharp
using Reflex.Pools.Interfaces;
using UnityEngine;

public class Enemy : MonoBehaviour, IPoolable
{
    // The GameObject is activated automatically on Take, and deactivated on Return
    public void OnSpawn() { }
    public void OnDespawn() { }
}
```

2. **Register in Installer**:
   Bind the pool with the prefab reference:

```csharp
using Reflex.Core;
using Reflex.Pools.Mono;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindMonoPool<Enemy, MonoPool<Enemy>>(enemyPrefab, minSize: 0, maxSize: 20, preWarmSize: 5);
    }
}
```

3. **Usage**:

```csharp
public class Spawner : MonoBehaviour
{
    [Inject] private readonly MonoPool<Enemy> _enemyPool;

    public void Spawn()
    {
        Enemy enemy = _enemyPool.Take();
        
        // Later when it dies...
        _enemyPool.Return(enemy);
    }
}
```

#### B. Parameterized `MonoPool<TData, T>`

Used to pool Unity prefabs while passing runtime data when taking objects from the pool. The component must implement
`IData<TData>`.

1. **Define Data & Implement Interface**:

```csharp
using Reflex.DataTypes.Interfaces;
using Reflex.Pools.Interfaces;
using UnityEngine;

public struct EnemyData { public int Health; }

public class Enemy : MonoBehaviour, IData<EnemyData>, IPoolable
{
    public EnemyData Data { get; set; } // Populated automatically before OnSpawn
    
    public void OnSpawn() { }
    public void OnDespawn() { }
}
```

2. **Register in Installer**:

```csharp
using Reflex.Core;
using Reflex.Pools.Mono;
using UnityEngine;

public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        builder.BindMonoPool<Enemy, MonoPool<EnemyData, Enemy>>(enemyPrefab);
    }
}
```

3. **Usage**:

```csharp
public class Spawner : MonoBehaviour
{
    [Inject] private readonly MonoPool<EnemyData, Enemy> _enemyPool;

    public void Spawn()
    {
        var data = new EnemyData { Health = 100 };
        Enemy enemy = _enemyPool.Take(data);
        
        // Later when it dies...
        _enemyPool.Return(enemy);
    }
}
```

---

## 4. 🔩 Updated Binding API

To provide a cleaner, more fluent syntax, the original `Register*` methods in `ContainerBuilder` have been made
`internal`. A new public set of `Bind` methods replaces them.

### Binding API Methods

| Method Signature                        | Description                                                                                                                         |
|:----------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------|
| `Bind<TConcrete>(...)`                  | Binds a type to itself.                                                                                                             |
| `Bind<TContract, TConcrete>(...)`       | Binds a concrete type to a specific interface contract (supports overloads up to 7 contracts, e.g. `Bind<T1, T2, ..., TConcrete>`). |
| `BindInstance(object instance)`         | Registers an existing object instance as a singleton.                                                                               |
| `BindInstanceTo<T>(object instance)`    | Registers an existing instance to a specific interface contract.                                                                    |
| `BindInterFaces<TConcrete>(...)`        | Binds a type to all interfaces it implements.                                                                                       |
| `BindInterFacesAndSelf<TConcrete>(...)` | Binds a type to all interfaces it implements, plus itself.                                                                          |
| `BindFactory<T, TFactory>(...)`         | Registers a custom plain C# class factory.                                                                                          |
| `BindMonoFactory<T, TFactory>(...)`     | Registers a custom MonoBehaviour factory.                                                                                           |
| `BindPool<T, TPool>(...)`               | Registers a custom plain C# class pool.                                                                                             |
| `BindMonoPool<T, TPool>(...)`           | Registers a custom MonoBehaviour pool.                                                                                              |

### Registration Examples

```csharp
public class GameInstaller : MonoBehaviour, IInstaller
{
    [SerializeField] private Enemy enemyPrefab;

    public void InstallBindings(ContainerBuilder builder)
    {
        // 1. Self Binding
        builder.Bind<SaveSystem>();

        // 2. Contract Binding (Single contract)
        builder.Bind<IInputManager, UnityInputManager>();

        // 3. Contract Binding (Multiple contracts)
        builder.Bind<IShooter, IMover, PlayerController>();

        // 4. Interface Autodiscovery
        builder.BindInterFaces<NetworkManager>();
        builder.BindInterFacesAndSelf<GameLoop>();

        // 5. Existing Instance Bindings
        builder.BindInstance(new AppConfig { Version = "1.0.0" });
        // or use their interface
        builder.BindInstanceTo<IConfig>(new AppConfig());

        // 6. C# Plain Factory Bindings
        builder.BindFactory<Player, PlayerFactory>();

        // 7. Mono Factory Bindings
        builder.BindMonoFactory<Enemy, EnemyFactory>(enemyPrefab);

        // 8. C# Plain Pool Bindings
        builder.BindPool<Bullet, BulletPool>();

        // 9. Mono Pool Bindings
        builder.BindMonoPool<Enemy, EnemyMonoPool>(enemyPrefab);
    }
}
```
