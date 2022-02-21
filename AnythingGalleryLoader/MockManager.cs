//MIT License

//Copyright (c) 2021 JotunnLib Team

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnythingGalleryLoader.Managers
{
    /// <summary>
    ///     Handles all logic to do with managing mocked prefabs added into the game.
    /// </summary>
    internal class MockManager// : IManager
    {
        /// <summary>
        ///     Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string AGLMockPrefix = "AGLmock_";

        /*private static MockManager _instance;
        /// <summary>
        ///     The singleton instance of this manager.
        /// </summary>
        public static MockManager Instance => _instance ??= new MockManager();

        /// <summary>
        ///     Hide .ctor
        /// </summary>
        private MockManager() { }

        /// <summary>
        ///     Legacy ValheimLib prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        [Obsolete("Legacy ValheimLib mock prefix. Use JVLMockPrefix \"JVLmock_\" instead.")]
        public const string MockPrefix = "VLmock_";

        /// <summary>
        ///     Prefix used by the Mock System to recognize Mock gameObject that must be replaced at some point.
        /// </summary>
        public const string JVLMockPrefix = "JVLmock_";

        /// <summary>
        ///     Internal container for mocked prefabs
        /// </summary>
        internal GameObject MockPrefabContainer;

        /// <summary>
        ///     Creates the container and registers all hooks
        /// </summary>
        public void Init()
        {
            MockPrefabContainer = new GameObject("MockPrefabs");
            MockPrefabContainer.transform.parent = Main.RootObject.transform;
            MockPrefabContainer.SetActive(false);
        }

        /// <summary>
        ///     Create an empty GameObject with the mock string prepended
        /// </summary>
        /// <param name="prefabName">Name of the mocked vanilla prefab</param>
        /// <returns>Mocked GameObject reference</returns>
        public GameObject CreateMockedGameObject(string prefabName)
        {
            string name = JVLMockPrefix + prefabName;

            Transform transform = MockPrefabContainer.transform.Find(name);
            if (transform != null)
            {
                return transform.gameObject;
            }

            GameObject g = new GameObject(name);
            g.transform.parent = MockPrefabContainer.transform;
            g.SetActive(false);

            return g;
        }

        /// <summary>
        ///     Create a mocked component on an empty GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="prefabName"></param>
        /// <returns></returns>
        public T CreateMockedPrefab<T>(string prefabName) where T : Component
        {
            GameObject g = CreateMockedGameObject(prefabName);
            string name = g.name;

            T mock = g.GetOrAddComponent<T>();
            if (mock == null)
            {
                Logger.LogWarning($"Could not create mock for prefab {prefabName} of type {typeof(T)}");
                return null;
            }
            mock.name = name;

            Logger.LogDebug($"Mock {name} created");

            return mock;
        }

        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="unityObject"></param>
        /// <returns>the real prefab</returns>
        public static T GetRealPrefabFromMock<T>(Object unityObject) where T : Object
        {
            return (T)GetRealPrefabFromMock(unityObject, typeof(T));
        }*/

#pragma warning disable CS0618
        /// <summary>
        ///     Will try to find the real vanilla prefab from the given mock
        /// </summary>
        /// <param name="unityObject"></param>
        /// <param name="mockObjectType"></param>
        /// <returns>the real prefab</returns>
        public static Object GetRealPrefabFromMock(Object unityObject, Type mockObjectType)
        {
            if (unityObject)
            {
                var unityObjectName = unityObject.name;
                var isAGLMock = unityObjectName.StartsWith(AGLMockPrefix);
                if (isAGLMock)
                {
                    unityObjectName = unityObjectName.Substring(AGLMockPrefix.Length);

                    // Cut off the suffix in the name to correctly query the original material
                    if (unityObject is Material)
                    {
                        const string materialInstance = " (Instance)";
                        if (unityObjectName.EndsWith(materialInstance))
                        {
                            unityObjectName =
                                unityObjectName.Substring(0, unityObjectName.Length - materialInstance.Length);
                        }
                    }

                    Object ret = PrefabManager.Cache.GetPrefab(mockObjectType, unityObjectName);

                    if (!ret)
                    {
                        throw new Exception($"Mock prefab {unityObjectName} could not be resolved");
                    }

                    return ret;
                }
                else if (mockObjectType == typeof(Material))
                {
                    Material mat = (Material)unityObject;
                    if (mat != null)
                    {
                        if (mat.shader.name.StartsWith(AGLMockPrefix))
                        {
                            string ShaderName = mat.shader.name.Substring(AGLMockPrefix.Length);
                            mat.shader = Shader.Find(ShaderName);
                            return unityObject;
                        }
                    }
                }
            }

            return null;
        }
#pragma warning restore CS0618
    }

    static class PrefabManager
    {
        /// <summary>
        ///     The global cache of prefabs per scene.
        /// </summary>
        public static class Cache
        {
            private static readonly Dictionary<Type, Dictionary<string, Object>> dictionaryCache =
                new Dictionary<Type, Dictionary<string, Object>>();

            /// <summary>
            ///     Get an instance of an Unity Object from the current scene with the given name.
            /// </summary>
            /// <param name="type"><see cref="Type"/> to search for.</param>
            /// <param name="name">Name of the actual object to search for.</param>
            /// <returns></returns>
            public static Object GetPrefab(Type type, string name)
            {
                if (dictionaryCache.TryGetValue(type, out var map))
                {
                    if (map.TryGetValue(name, out var unityObject))
                    {
                        return unityObject;
                    }
                }
                else
                {
                    InitCache(type);
                    return GetPrefab(type, name);
                }

                return null;
            }

            /// <summary>
            ///     Get an instance of an Unity Object from the current scene by name.
            /// </summary>
            /// <typeparam name="T"></typeparam>
            /// <param name="name"></param>
            /// <returns></returns>
            public static T GetPrefab<T>(string name) where T : Object
            {
                return (T)GetPrefab(typeof(T), name);
            }

            /// <summary>
            ///     Get all instances of an Unity Object from the current scene by type.
            /// </summary>
            /// <param name="type"><see cref="Type"/> to search for.</param>
            /// <returns></returns>
            public static Dictionary<string, Object> GetPrefabs(Type type)
            {
                if (dictionaryCache.TryGetValue(type, out var map))
                {
                    return map;
                }
                InitCache(type);
                return GetPrefabs(type);
            }

            private static void InitCache(Type type, Dictionary<string, Object> map = null)
            {
                if(map == null) map = new Dictionary<string, Object>();
                foreach (var unityObject in Resources.FindObjectsOfTypeAll(type))
                {
                    switch (unityObject.name)
                    {
                        case "Plane.002":
                            switch (unityObject.GetInstanceID())
                            {
                                case 2258: map["Plane.002"] = unityObject; break;
                                case 2260: map["PlaneFern"] = unityObject; break;
                                default:
                                    Debug.Log($"Collision {type.Name} \"{unityObject.name}\" {unityObject.GetInstanceID()} over {map[unityObject.name].GetInstanceID()}");
                                    break;
                            }
                            break;
                        case "Cube":
                            switch (unityObject.GetInstanceID())
                            {
                                case 2230: map["CubeSign"] = unityObject; break;
                                case 2232: map["Cube"] = unityObject; break;
                                default:
                                    Debug.Log($"Collision {type.Name} \"{unityObject.name}\" {unityObject.GetInstanceID()} over {map[unityObject.name].GetInstanceID()}");
                                    break;
                            }
                            break;
                        case "Plane.003":
                            switch (unityObject.GetInstanceID())
                            {
                                case 2222: map["PlaneGrass"] = unityObject; break;
                                case 2224: map["Plane.003"] = unityObject; break;
                                default:
                                    Debug.Log($"Collision {type.Name} \"{unityObject.name}\" {unityObject.GetInstanceID()} over {map[unityObject.name].GetInstanceID()}");
                                    break;
                            }
                            break;
                        default:
                            if (map.ContainsKey(unityObject.name))
                            {
                                Debug.Log($"Collision {type.Name} \"{unityObject.name}\" {unityObject.GetInstanceID()} over {map[unityObject.name].GetInstanceID()}");
                                map[unityObject.name] = unityObject;
                            }
                            else
                            {
                                map[unityObject.name] = unityObject;
                            }
                            break;
                    }
                }

                dictionaryCache[type] = map;
            }

            internal static void ClearCache()
            {
                dictionaryCache.Clear();
            }
        }
    }
}