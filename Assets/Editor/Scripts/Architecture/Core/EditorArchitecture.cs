using Aoiro;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gameplay.Asset;
using Gameplay.Tables;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor
{
    public class EditorArchitecture : Architecture<EditorArchitecture>, IArchitecture
    {
        private readonly HashSet<Type> _callStack = new HashSet<Type>();
        private TableFileWatchSystem _tableFileWatchSystem = null;
        
        protected override void OnInitialize()
        {
            //需要框架创建时就注册的就放在这里，否则都通过反射创建
            RegisterModule(new TablesModel());
            _tableFileWatchSystem = new TableFileWatchSystem();
            RegisterModule(_tableFileWatchSystem);
            RegisterModule<IAssetService>(new AddressableAssetService(initializeSynchronously: true));

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        protected override void OnShutdown()
        {
            EditorApplication.update -= OnEditorUpdate;
            _tableFileWatchSystem = null;
        }

        private void OnEditorUpdate()
        {
            _tableFileWatchSystem?.Tick();
        }

        public override T GetModule<T>()
        {
            var module = base.GetModule<T>();
            if (module != null)
            {
                return module;
            }

            if (!TryGetAssignableTypesIncludingSelf(typeof(T), out var type))
            {
                throw new InvalidOperationException($"找不到可创建的类型 [{typeof(T)}]");
            }

            module = Activator.CreateInstance(type) as T;
            RegisterModule(module);

            return module;
        }

        public override void ExecuteCommand<TCommand>(TCommand command)
        {
            try
            {
                Debug.Assert(_callStack.Add(command.GetType()), $"在同一次[Command]栈中重复执行了多次[{command.GetType()}]");
                base.ExecuteCommand(command);
            }
            finally
            {
                _callStack.Remove(command.GetType());
            }
        }

        public override TResult ExecuteCommand<TCommand, TResult>(TCommand command)
        {
            try
            {
                Debug.Assert(_callStack.Add(command.GetType()), $"在同一次[Command]栈中重复执行了多次[{command.GetType()}]");
                return base.ExecuteCommand<TCommand, TResult>(command);
            }
            finally
            {
                _callStack.Remove(command.GetType());
            }
        }

        public override TResult ExecuteQuery<TQuery, TResult>(TQuery query)
        {
            try
            {
                Debug.Assert(_callStack.Add(query.GetType()), $"在同一次[Query]栈中重复执行了多次[{query.GetType()}]");
                return base.ExecuteQuery<TQuery, TResult>(query);
            }
            finally
            {
                _callStack.Remove(query.GetType());
            }
        }

        public override void PublishEvent<TEvent>(in TEvent evt)
        {
            try
            {
                Debug.Assert(_callStack.Add(evt.GetType()), $"在同一次[Event]栈中重复执行了多次[{evt.GetType()}]");
                base.PublishEvent(in evt);
            }
            finally
            {
                _callStack.Remove(evt.GetType());
            }
        }

        private static bool TryGetAssignableTypesIncludingSelf(Type baseType, out Type target)
        {
            if (IsConcreteInstantiableType(baseType))
            {
                target = baseType;
                return true;
            }

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(x => x != null).ToArray()!;
                }

                foreach (var t in types)
                {
                    if (t == null || t == baseType || !IsConcreteInstantiableType(t))
                    {
                        continue;
                    }
                    if (baseType.IsAssignableFrom(t))
                    {
                        target = t;
                        return true;
                    }
                }
            }

            target = null;
            return false;
        }

        private static bool IsConcreteInstantiableType(Type t)
        {
            return !t.IsAbstract && !t.IsInterface && !t.ContainsGenericParameters;
        }
    }
}
