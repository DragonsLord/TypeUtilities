using TypeUtilities;

namespace DemoApp
{
    class Entity
    {
        public int Id { get; set; }
        public double Stats { get; set; }
    }


    [Pick(typeof(SourceType), "Id")]
    [Omit(typeof(Entity), "Id")]
    internal partial class MultiType
    {
    }
}
