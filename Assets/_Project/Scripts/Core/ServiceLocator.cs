using System;
using System.Collections.Generic;
using UnityEngine;

namespace KnightSurvivors.Core
{
    /// <summary>
    /// A tiny "phone book" for game-wide managers (EnemyManager, PoolService, RunManager...).
    ///
    /// WHY THIS INSTEAD OF SINGLETONS?
    /// A classic singleton ("EnemyManager.Instance") hard-codes "there is exactly one of me" into
    /// the class itself. That is fine for managers, but it is a trap for anything player-related
    /// because co-op means there can be several players. With a service locator:
    ///   * Managers register themselves when they wake up and unregister when destroyed.
    ///   * Other scripts ask for them by type: ServiceLocator.Get&lt;EnemyManager&gt;().
    ///   * Nothing about a manager's class says "I am global", so swapping or mocking one is easy.
    /// Players are NOT stored here. Players live in <c>PlayerRegistry</c> (a list), because there can be many.
    ///
    /// Rule of thumb: register in Awake(), look services up in Start() or later (never in Awake),
    /// so the order in which Unity wakes objects up does not matter.
    /// </summary>
    public static class ServiceLocator
    {
        // Type -> the object registered for that type.
        private static readonly Dictionary<Type, object> Services = new Dictionary<Type, object>();

        /// <summary>Register a service. Call this from the service's Awake().</summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null) return;

            var type = typeof(T);
            if (Services.TryGetValue(type, out var existing) && !ReferenceEquals(existing, service) && existing != null)
            {
                // Two managers of the same type is almost always a scene setup mistake.
                Debug.LogWarning($"[ServiceLocator] Replacing existing {type.Name}. Is there a duplicate in the scene?");
            }

            Services[type] = service;
        }

        /// <summary>Unregister a service. Call this from the service's OnDestroy().</summary>
        public static void Unregister<T>(T service) where T : class
        {
            var type = typeof(T);
            // Only remove it if it is still the one registered (a newer one may have replaced it).
            if (Services.TryGetValue(type, out var existing) && ReferenceEquals(existing, service))
            {
                Services.Remove(type);
            }
        }

        /// <summary>Get a service, or null (with an error in the Console) if none is registered.</summary>
        public static T Get<T>() where T : class
        {
            if (TryGet<T>(out var service)) return service;

            Debug.LogError($"[ServiceLocator] No {typeof(T).Name} registered. Did you run 'Knight Survivors > Build Graybox Scene'?");
            return null;
        }

        /// <summary>Get a service without logging an error if it is missing.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Services.TryGetValue(typeof(T), out var obj) && obj is T typed)
            {
                // Unity objects can be "destroyed but not null" in C#; this check catches that.
                if (typed is UnityEngine.Object unityObj && unityObj == null)
                {
                    service = null;
                    return false;
                }

                service = typed;
                return true;
            }

            service = null;
            return false;
        }

        // Unity can be set to skip "domain reload" when entering Play Mode (faster iteration).
        // Then static fields survive between plays, so we clear them by hand at startup.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Services.Clear();
        }
    }
}
