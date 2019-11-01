using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class CollectionUtils {
    public static bool IsNullOrEmpty<T>(ICollection<T> collection) {
      if (collection != null)
        return collection.Count == 0;
      return true;
    }

    public static void AddRange<T>(this IList<T> initial, IEnumerable<T> collection) {
      if (initial == null)
        throw new ArgumentNullException(nameof(initial));
      if (collection == null)
        return;
      foreach (T obj in collection)
        initial.Add(obj);
    }

    public static void AddRange<T>(this IList<T> initial, IEnumerable collection) {
      ValidationUtils.ArgumentNotNull((object) initial, nameof(initial));
      CollectionUtils.AddRange<T>(initial, collection.Cast<T>());
    }

    public static bool IsDictionaryType(Type type) {
      ValidationUtils.ArgumentNotNull((object) type, nameof(type));
      return typeof(IDictionary).IsAssignableFrom(type) ||
             ReflectionUtils.ImplementsGenericDefinition(type, typeof(IDictionary<,>));
    }

    public static ConstructorInfo ResolveEnumerableCollectionConstructor(
      Type collectionType,
      Type collectionItemType) {
      Type constructorArgumentType = typeof(IList<>).MakeGenericType(collectionItemType);
      return CollectionUtils.ResolveEnumerableCollectionConstructor(collectionType, collectionItemType,
        constructorArgumentType);
    }

    public static ConstructorInfo ResolveEnumerableCollectionConstructor(
      Type collectionType,
      Type collectionItemType,
      Type constructorArgumentType) {
      Type type = typeof(IEnumerable<>).MakeGenericType(collectionItemType);
      ConstructorInfo constructorInfo = (ConstructorInfo) null;
      foreach (ConstructorInfo constructor in collectionType.GetConstructors(
        BindingFlags.Instance | BindingFlags.Public)) {
        IList<ParameterInfo> parameters = (IList<ParameterInfo>) constructor.GetParameters();
        if (parameters.Count == 1) {
          Type parameterType = parameters[0].ParameterType;
          if (type == parameterType) {
            constructorInfo = constructor;
            break;
          }

          if (constructorInfo == null && parameterType.IsAssignableFrom(constructorArgumentType))
            constructorInfo = constructor;
        }
      }

      return constructorInfo;
    }

    public static bool AddDistinct<T>(this IList<T> list, T value) {
      return list.AddDistinct<T>(value, (IEqualityComparer<T>) EqualityComparer<T>.Default);
    }

    public static bool AddDistinct<T>(this IList<T> list, T value, IEqualityComparer<T> comparer) {
      if (list.ContainsValue<T>(value, comparer))
        return false;
      list.Add(value);
      return true;
    }

    public static bool ContainsValue<TSource>(
      this IEnumerable<TSource> source,
      TSource value,
      IEqualityComparer<TSource> comparer) {
      if (comparer == null)
        comparer = (IEqualityComparer<TSource>) EqualityComparer<TSource>.Default;
      if (source == null)
        throw new ArgumentNullException(nameof(source));
      foreach (TSource x in source) {
        if (comparer.Equals(x, value))
          return true;
      }

      return false;
    }

    public static bool AddRangeDistinct<T>(
      this IList<T> list,
      IEnumerable<T> values,
      IEqualityComparer<T> comparer) {
      bool flag = true;
      foreach (T obj in values) {
        if (!list.AddDistinct<T>(obj, comparer))
          flag = false;
      }

      return flag;
    }

    public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> predicate) {
      int num = 0;
      foreach (T a in collection) {
        if (predicate(a))
          return num;
        ++num;
      }

      return -1;
    }

    public static bool Contains<T>(this List<T> list, T value, IEqualityComparer comparer) {
      for (int index = 0; index < list.Count; ++index) {
        if (comparer.Equals((object) value, (object) list[index]))
          return true;
      }

      return false;
    }

    public static int IndexOfReference<T>(this List<T> list, T item) {
      for (int index = 0; index < list.Count; ++index) {
        if ((object) item == (object) list[index])
          return index;
      }

      return -1;
    }

    private static IList<int> GetDimensions(IList values, int dimensionsCount) {
      IList<int> intList = (IList<int>) new List<int>();
      IList list1 = values;
      while (true) {
        intList.Add(list1.Count);
        if (intList.Count != dimensionsCount && list1.Count != 0) {
          IList list2 = list1[0] as IList;
          if (list2 != null)
            list1 = list2;
          else
            break;
        } else
          break;
      }

      return intList;
    }

    private static void CopyFromJaggedToMultidimensionalArray(
      IList values,
      Array multidimensionalArray,
      int[] indices) {
      int length1 = indices.Length;
      if (length1 == multidimensionalArray.Rank) {
        multidimensionalArray.SetValue(CollectionUtils.JaggedArrayGetValue(values, indices), indices);
      } else {
        int length2 = multidimensionalArray.GetLength(length1);
        if (((ICollection) CollectionUtils.JaggedArrayGetValue(values, indices)).Count != length2)
          throw new Exception("Cannot deserialize non-cubical array as multidimensional array.");
        int[] indices1 = new int[length1 + 1];
        for (int index = 0; index < length1; ++index)
          indices1[index] = indices[index];
        for (int index = 0; index < multidimensionalArray.GetLength(length1); ++index) {
          indices1[length1] = index;
          CollectionUtils.CopyFromJaggedToMultidimensionalArray(values, multidimensionalArray, indices1);
        }
      }
    }

    private static object JaggedArrayGetValue(IList values, int[] indices) {
      IList list = values;
      for (int index1 = 0; index1 < indices.Length; ++index1) {
        int index2 = indices[index1];
        if (index1 == indices.Length - 1)
          return list[index2];
        list = (IList) list[index2];
      }

      return (object) list;
    }

    public static Array ToMultidimensionalArray(IList values, Type type, int rank) {
      IList<int> dimensions = CollectionUtils.GetDimensions(values, rank);
      while (dimensions.Count < rank)
        dimensions.Add(0);
      Array instance = Array.CreateInstance(type, dimensions.ToArray<int>());
      CollectionUtils.CopyFromJaggedToMultidimensionalArray(values, instance, CollectionUtils.ArrayEmpty<int>());
      return instance;
    }

    public static T[] ArrayEmpty<T>() {
      return Enumerable.Empty<T>() as T[] ?? new T[0];
    }
  }
}