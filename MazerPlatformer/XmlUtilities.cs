using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LanguageExt;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace MazerPlatformer
{
    public static class ImmutableCopy
    {
        public static Either<IFailure, T> Copy<T>(this T original)
            => from xml in XmlUtilities<T>.ObjectToXml(original)
                from copy in XmlUtilities<T>.XmlToObject(xml)
                select copy;

        // This is probably faster than xml serializer but then i'll need to use [XmlSerialize] on my classes - not a trainsmash if xmlserializer is too slow
        public static Either<IFailure, T> DeepClone<T>(this T obj) => Statics.EnsureWithReturn(() =>
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;
                return (T) formatter.Deserialize(ms);
            }
        });

        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            TraceWriter = new MemoryTraceWriter(),
            Error = delegate (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
            {
                Console.WriteLine(SerializerSettings.TraceWriter);
                //args.ErrorContext.Handled = true;
            },
        };

        

        /// <summary>
        /// Perform a deep Copy of the object, using Json as a serialisation method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static Either<IFailure, T> CloneJson<T>(this T source) => Statics.EnsureWithReturn(() =>
        {
            // Don't serialize a null object, simply return the default for that object
            if (Object.ReferenceEquals(source, null))
            {
                return InvalidCastFailure.Create("Cannot copy null").ToEitherFailure<T>();
            }

            var serialized = JsonConvert.SerializeObject(source, SerializerSettings);
            var deserialized = JsonConvert.DeserializeObject<T>(serialized).ToEither();
            //return source.Equals(deserialized)
            //    ? deserialized
            //    : InvalidCastFailure.Create("deserialized is not the same as serialized").ToEitherFailure<T>();
            return deserialized;
        }).UnWrap();
    }

    public static class XmlUtilities<T>
    {
        public static Either<IFailure, string> ObjectToXml(T data) => Statics.EnsureWithReturn(() =>
        {
            using (var stringWriter = new System.IO.StringWriter())
            {
                var serializer = new XmlSerializer(data.GetType());
                serializer.Serialize(stringWriter, data);
                return stringWriter.ToString();
            }
        });



        public static Either<IFailure, T> XmlToObject(string xmlString) => Statics.EnsureWithReturn(() =>
        {
            using (var stringReader = new System.IO.StringReader(xmlString))
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T) serializer.Deserialize(stringReader);
            }
        });
    }
}
