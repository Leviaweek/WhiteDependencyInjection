## 📦 `ComposedServiceProvider` and `ComposedServiceProviderBuilder`

These classes implement a composite pattern for IServiceProvider from this project, allowing multiple service providers to be combined into one unified provider.

### 🔧ComposedServiceProviderBuilder
A builder class used to incrementally add `IServiceProvider` instances and construct a composed provider from them.

#### Methods
`AddProvider(IServiceProvider provider)`

Adds a new `IServiceProvider` to the builder.
```csharp
builder.AddProvider(provider);
```
* Parameter: `provider` - The `IServiceProvider` instance to add.
* Returns: `ComposedServiceProviderBuilder` - The builder instance for method chaining.

`Build()`
Creates a new `ComposedServiceProvider` instance from the added providers.
```csharp
var composedProvider = builder.Build();
```
* Returns: `ComposedServiceProvider` - The composed service provider instance.

### 🧩ComposedServiceProvider

This class implements `IServiceProvider` and aggregates multiple service providers into a single provider. It delegates service resolution to the first provider that can fulfill the request.

---

✅ This setup is useful for modular architectures, plugin systems, or multi-container DI scenarios, where dependencies might come from various sources but need to be queried as one.
