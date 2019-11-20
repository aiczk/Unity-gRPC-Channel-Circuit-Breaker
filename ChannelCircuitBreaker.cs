using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Grpc.Core;
using UniRx.Async;

namespace _Script.Application.Utility
{
    public class ChannelCircuitBreaker<T> where T : ClientBase<T>
    {
        private enum CircuitState { Open, Closed }

        public bool IsConnected => channel.State == ChannelState.Idle;
        public DateTime LastFailureTime { get; private set; }

        private int failedThreshold;
        private int failureCount;
        private float invocationTimeout;
        
        private Action<Channel> connectAction;
        private Channel channel;
        
        private CircuitState State => failureCount >= failedThreshold ? CircuitState.Open : CircuitState.Closed;
        
        public ChannelCircuitBreaker(Channel channel,int failedThreshold = 5, float invocationTimeout = 0.2f)
        {
            this.channel = channel;
            this.failedThreshold = failedThreshold;
            this.invocationTimeout = invocationTimeout;
        }

        public ChannelCircuitBreaker<T> Create(in Action<Channel> func)
        {
            connectAction = Unsafe.AsRef(func);
            return this;
        }

        public async UniTask Execute()
        {
            if(channel.State == ChannelState.Ready)
                return;
            
            switch (State)
            {
                case CircuitState.Open:
                {
                    LastFailureTime = DateTime.Now;
                    throw new TimeoutException("接続できません。");
                }

                case CircuitState.Closed:
                    try
                    {
                        await CheckConnect();
                        Reset();
                    }
                    catch
                    {
                        RecordFailure();
                        await Execute();
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            async UniTask CheckConnect()
            {
                await channel
                    .ConnectAsync()
                    .Timeout(TimeSpan.FromSeconds(invocationTimeout));
                
                connectAction(channel);
            }
        }

        public ChannelCircuitBreaker<T> FailedThreshold(int threshold)
        {
            if (threshold > 0)
                failedThreshold = threshold;
            
            return this;
        }

        public ChannelCircuitBreaker<T> InvocationTimeout(float second)
        {
            if (second > 0f) 
                invocationTimeout = second;

            return this;
        }

        public async UniTask Retry()
        {
            if(State == CircuitState.Closed)
                return;
            
            Reset();
            await Execute();
        }

        private void RecordFailure() => ++failureCount;
        private void Reset() => failureCount = 0;
    }
    
    public static class TaskExtensions
    {
        public static async Task Timeout(this Task task, TimeSpan timeout)
        {
            var delay = Task.Delay(timeout);
            if (await Task.WhenAny(task, delay) == delay)
                throw new TimeoutException();
        }
 
        public static async Task<T> Timeout<T>(this Task<T> task, TimeSpan timeout)
        {
            await ((Task)task).Timeout(timeout);
            return await task;
        }
    }
}