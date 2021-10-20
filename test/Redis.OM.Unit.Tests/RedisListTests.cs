//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Xunit;
//using Moq;

//namespace Redis.OM.Unit.Tests
//{
//    public class RedisListTests
//    {
//        Mock<IRedisConnection> _mock = new Mock<IRedisConnection>();
//        [Fact]
//        public void TestAdd()
//        {            
//            _mock.Setup(x => x.LPush("foo-list", "bar")).Returns(1);
//            _mock.Setup(x => x.LLen("foo-list")).Returns(1);
//            var list = new RedisList(_mock.Object, "foo-list");
//            var res = list.Add("bar");
//            Assert.Equal(1, res);
//        }

//        [Fact]
//        public void TestCount()
//        {
//            _mock.Setup(x => x.LLen("foo-list")).Returns(50);
//            var list = new RedisList(_mock.Object, "foo-list");
//            Assert.Equal(50, list.Count);   
//        }

//        [Fact]
//        public void TestEnumeration()
//        {
//            var items = new List<string>();
//            for(var i = 0; i < 300; i++)
//            {
//                items.Add(i.ToString());
//            }
//            _mock.Setup(x => x.LRange("foo-list", 0, 99)).Returns(items.Take(100).ToArray());
//            _mock.Setup(x => x.LRange("foo-list", 100, 199)).Returns(items.Skip(100).Take(100).ToArray());
//            _mock.Setup(x => x.LRange("foo-list", 200, 299)).Returns(items.Skip(200).Take(100).ToArray());
//            _mock.Setup(x => x.LRange("foo-list", 300, 399)).Returns(new string[0]);
//            var list = new RedisList(_mock.Object, "foo-list");
//            var j = 0;
//            foreach(var item in list)
//            {
//                Assert.Equal(j.ToString(), item);
//                j++;
//            }
//        }
//    }
//}
