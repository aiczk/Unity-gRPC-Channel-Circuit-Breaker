# Unity gRPC Channel CircuitBreaker
Circuit Breaker available in the Unity gRPC environment.
  
### What is Circuit Breaker?
See http://martinfowler.com/bliki/CircuitBreaker.html

### Installation

- UniTask(https://github.com/Cysharp/UniTask)

### Intoroduction

How to Use
```cs
var ccb = new ChannelCircuitBreaker<FooClient>(channel, failedThreshold: 5, invocationTimeout: 0.2f);
            
await ccb
    .Create(ch => fooClient = new FooClient(ch))
    .InvocationTimeout(1f)  					//Reconfiguring
    .Execute();
```

License
----

MIT
