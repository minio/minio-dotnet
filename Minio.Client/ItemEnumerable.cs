using Minio.Client.xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minio.Client
{
    public class ItemEnumerable : IEnumerable<Item>
    {
        internal string Bucket { get; set; }
        private bool IsRunning = true;
        LinkedList<Item> items = new LinkedList<Item>();

        public ItemEnumerable()
        {

        }

        public IEnumerator<Item> GetEnumerator()
        {
            while (IsRunning)
            {
                if (items.Count == 0)
                {
                    populate();
                }
                if (items.Count > 0)
                {
                    Item item = items.First();
                    items.RemoveFirst();
                    yield return item;
                }
                else
                {
                    IsRunning = false;
                }
            }

        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal void populate()
        {

        }
    }
}
