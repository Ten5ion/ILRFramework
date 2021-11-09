namespace HotFix.Game.Common
{
    public class GenericSingleton<T> where T: class, new()
    {
        private static T _instance = null;
        private static readonly object _locker = new object();
        
        public static T Instance {
            get {
                if (_instance == null) {
                    lock (_locker) {
                        if (_instance == null) {
                            _instance = new T();
                        }
                    }
                }
                return _instance;
            }
        }
    }
}