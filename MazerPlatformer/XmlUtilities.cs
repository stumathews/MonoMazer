using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using LanguageExt;

namespace MazerPlatformer
{
    public static class ImmutableCopy
    {
        public static Either<IFailure, T> Copy<T>(this T original)
            => from xml in XmlUtilities<T>.ObjectToXml(original)
                    .ToEither(InvalidCastFailure.Create("Could not serialize object"))
                from copy in XmlUtilities<T>.XmlToObject(xml)
                    .ToEither(InvalidCastFailure.Create("Could not deserialize object"))
                select copy;
    }

    public static class XmlUtilities<T>
    {
        public static Option<string> ObjectToXml(T data)
        {
            try
            {
                using (var stringWriter = new System.IO.StringWriter())
                {
                    var serializer = new XmlSerializer(data.GetType());
                    serializer.Serialize(stringWriter, data);
                    return stringWriter.ToString();
                }
            }
            catch (System.InvalidOperationException)
            {
                return Option<string>.None;
            }
        }

        

        public static Option<T> XmlToObject(string xmlString)
        {
            try
            {
                using (var stringReader = new System.IO.StringReader(xmlString))
                {
                    var serializer = new XmlSerializer(typeof(T));
                    return (T) serializer.Deserialize(stringReader);
                }
            }
            catch (Exception /*not used*/)
            {
                return Option<T>.None;
            }
        }
    }
}
