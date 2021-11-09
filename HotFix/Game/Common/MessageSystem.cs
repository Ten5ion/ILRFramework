using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace HotFix.Game.Common
{
    public delegate void OnReceiveMessage(MessageBase msg);

    public abstract class MessageBase
    {
        public string MsgType;

        public MessageBase() {
            MsgType = GetType().Name;
        }
    }
    
    public class MessageSystem : GenericSingleton<MessageSystem>, IUpdatable
    {
        private struct MessageListener
        {
            public OnReceiveMessage Handler;
            public int Priority;
        }
        
        private readonly Dictionary<string, List<MessageListener>> _listeners =
            new Dictionary<string, List<MessageListener>>();

        private readonly Queue<MessageBase> _messageQueue = new Queue<MessageBase>();

        private readonly Dictionary<string, MessageBase> _residentMessages = new Dictionary<string, MessageBase>();
        
        private const int MsgQueueProcessTimeThreshold = 22; // 45 FPS
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool AddListener<T>(OnReceiveMessage handler, int priority = 0) where T: MessageBase {
            var msgType = typeof(T).Name;

            if (!_listeners.ContainsKey(msgType)) {
                _listeners.Add(msgType, new List<MessageListener>());
            }

            var listeners = _listeners[msgType];
            var newListener = new MessageListener {Handler = handler, Priority = priority};

            var insertIndex = 0;
            for (var i = listeners.Count - 1; i >= 0; i--) {
                var l = listeners[i];
                if (l.Handler == handler) return false;

                if (priority > l.Priority) {
                    insertIndex = i + 1;
                }
            }

            listeners.Insert(insertIndex, newListener);

            var exist = _residentMessages.TryGetValue(msgType, out var msg);
            if (exist) {
                handler(msg);
            }
            
            return true;
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <param name="handler"></param>
        public void RemoveListener<T>(OnReceiveMessage handler) where T: MessageBase {
            var msgType = typeof(T).Name;
            var exist = _listeners.TryGetValue(msgType, out var listeners);
            if (exist) {
                for (var i = 0; i < listeners.Count; i++) {
                    var l = listeners[i];
                    if (l.Handler == handler) {
                        listeners.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 发送消息（广播）
        /// </summary>
        /// <param name="msg">消息</param>
        /// <param name="resident">消息是否驻留（如果消息驻留，那么队列会缓存同类型消息的最后一条，后面新加入的消息监听者在注册的时候会收到这条驻留消息）</param>
        public bool AppendMessage(MessageBase msg, bool resident = false) {
            if (!resident && !_listeners.ContainsKey(msg.MsgType)) {
                return false;
            }

            _messageQueue.Enqueue(msg);

            if (resident) {
                _residentMessages[msg.MsgType] = msg;
            }
            
            return true;
        }

        /// <summary>
        /// 获取某种类型的驻留消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetResidentMessage<T>() where T : MessageBase {
            var msgType = typeof(T).Name;
            var exist = _residentMessages.TryGetValue(msgType, out var msg);
            if (exist) {
                return (T) msg;
            }

            return null;
        }

        private void BroadcastMessage(MessageBase msg) {
            var msgType = msg.MsgType;
            
            var exist = _listeners.TryGetValue(msgType, out var listeners);
            if (exist) {
                var len = listeners.Count;
                // Debug.Log($"MsgType: {msgType}, Broadcast: {len}");
                for (var i = len - 1; i >= 0; i--) {
                    var listener = listeners[i];
                    listener.Handler(msg);
                }
            }
        }

        public void OnUpdate(float dt) {
            _stopwatch.Restart();
            
            // 在一帧内将消息队列中的消息广播出去，如果处理超时了，则留到下一帧再广播
            while (_messageQueue.Count > 0) {
                if (_stopwatch.ElapsedMilliseconds > MsgQueueProcessTimeThreshold) {
                    _stopwatch.Stop();
                    break;
                }

                var msg = _messageQueue.Dequeue();
                BroadcastMessage(msg);
            }
        }
    }
}
