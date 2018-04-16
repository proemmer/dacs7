using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dacs7
{
    public partial class Dacs7Client
    {

        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns>Returns the registered shortcuts</returns>
        public async Task<IEnumerable<string>> RegisterAsync(params string[] values) => await RegisterAsync(values as IEnumerable<string>);

        /// <summary>
        /// Register shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> RegisterAsync(IEnumerable<string> values)
        {
            var added = new List<KeyValuePair<string, ReadItemSpecification>>();
            var enumerator = values.GetEnumerator();
            var resList = CreateNodeIdCollection(values).Select(x =>
            {
                enumerator.MoveNext();
                added.Add(new KeyValuePair<string, ReadItemSpecification>(enumerator.Current, x));
                return x.ToString();
            }).ToList();

            UpdateRegistration(added, null);

            return await Task.FromResult(resList);
        }



        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> UnregisterAsync(params string[] values)
        {
            return await UnregisterAsync(values as IEnumerable<string>);
        }

        /// <summary>
        /// Remove shortcuts
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> UnregisterAsync(IEnumerable<string> values)
        {
            var removed = new List<KeyValuePair<string, ReadItemSpecification>>();
            foreach (var item in values)
            {
                if (_registeredTags.TryGetValue(item, out var obj))
                    removed.Add(new KeyValuePair<string, ReadItemSpecification>(item, obj));
            }
            UpdateRegistration(null, removed);

            return await Task.FromResult(removed.Select(x => x.Key).ToList());

        }

        /// <summary>
        /// Retruns true if the given tag is already registred
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public bool IsTagRegistered(string tag) => _registeredTags.ContainsKey(tag);



    }
}
