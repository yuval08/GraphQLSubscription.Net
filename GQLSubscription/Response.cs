using Newtonsoft.Json.Linq;

namespace GQLSubscription {
    public class Response {
        public Response(JToken data) => Data = data;

        protected JToken Data { get; }
        
        /// <summary>
        /// Convert the returned data object from GraphQL subscription to a deserialized class
        /// </summary>
        /// <param name="fieldName">The field name returned from the graphql subscription data object</param>
        /// <typeparam name="TOut">The class type to be deserialized</typeparam>
        /// <returns>Returned a deserialized object out of the GraphQL subscription data</returns>
        public TOut GetDataFieldAs<TOut>(string fieldName) where TOut:class => Data[fieldName].ToObject<TOut>();
    }
}