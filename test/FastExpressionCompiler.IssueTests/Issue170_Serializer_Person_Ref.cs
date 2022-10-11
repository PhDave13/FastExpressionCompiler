using System.Reflection.Emit;
using NUnit.Framework;

#pragma warning disable 649
#pragma warning disable 219

#if LIGHT_EXPRESSION
using static FastExpressionCompiler.LightExpression.Expression;
namespace FastExpressionCompiler.LightExpression.IssueTests
#else
using static System.Linq.Expressions.Expression;
// ReSharper disable UnusedMember.Global
namespace FastExpressionCompiler.IssueTests
#endif
{
    [TestFixture]
    public class Issue170_Serializer_Person_Ref : ITest
    {
        public int Run()
        {
            InvokeActionConstantIsSupported();
            InvokeActionConstantIsSupportedSimple();
            InvokeActionConstantIsSupportedSimpleStruct();
            // InvokeActionConstantIsSupportedSimpleClass(); // todo: @fixme failing
            return 4;
        }

        delegate void DeserializeDelegate<T>(byte[] buffer, ref int offset, ref T value);

        class Person
        {
            public string Name;
            public int Health;
            public Person BestFriend;
        }

        [Test]
        public void InvokeActionConstantIsSupported()
        {
            var bufferArg = Parameter(typeof(byte[]), "buffer");
            var refOffsetArg = Parameter(typeof(int).MakeByRefType(), "offset");
            var refValueArg = Parameter(typeof(Person).MakeByRefType(), "value");

            var assignBlock = Block(
                Assign(PropertyOrField(refValueArg, nameof(Person.Health)), Constant(5)),
                Assign(PropertyOrField(refValueArg, nameof(Person.Name)), Constant("test result name"))
               );

            void AssigningRefs(byte[] buffer, ref int offset, ref Person value)
            {
                value.Health = 5;
                value.Name = "test result name";
            }

            var lambda = Lambda<DeserializeDelegate<Person>>(assignBlock, bufferArg, refOffsetArg, refValueArg);


            void LocalAssert(DeserializeDelegate<Person> invoke)
            {
                var person = new Person { Name = "a", Health = 1 };
                int offset = 0;

                invoke(null, ref offset, ref person);
                Assert.AreEqual(5, person.Health);
                Assert.AreEqual("test result name", person.Name);
            }

            LocalAssert(AssigningRefs);

            lambda.PrintCSharp();

            var func = lambda.CompileSys();
            func.PrintIL();
            LocalAssert(func);

            var funcFast = lambda.CompileFast(true);
            funcFast.PrintIL();
            funcFast.AssertOpCodes(
                OpCodes.Ldarg_3,
                OpCodes.Ldind_Ref,
                OpCodes.Ldc_I4_5,
                OpCodes.Stfld,
                OpCodes.Ldarg_3,
                OpCodes.Ldind_Ref,
                OpCodes.Ldstr,
                OpCodes.Stfld,
                OpCodes.Ret
            );
            LocalAssert(funcFast);
        }

        delegate void DeserializeDelegateSimple<T>(ref T value);


        class SimplePerson
        {
            public int Health;
        }

        [Test]
        public void InvokeActionConstantIsSupportedSimple()
        {
            var refValueArg = Parameter(typeof(SimplePerson).MakeByRefType(), "value");
            void AssigningRefs(ref SimplePerson value) => value.Health = 5;
            var lambda = Lambda<DeserializeDelegateSimple<SimplePerson>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePerson.Health)), Constant(5)),
                refValueArg);


            void LocalAssert(DeserializeDelegateSimple<SimplePerson> invoke)
            {
                var person = new SimplePerson { Health = 1 };
                invoke(ref person);
                Assert.AreEqual(5, person.Health);
            }

            LocalAssert(AssigningRefs);

            var func = lambda.CompileSys();
            LocalAssert(func);


            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }

        struct SimplePersonStruct
        {
            public int Health;
        }

        [Test]
        public void InvokeActionConstantIsSupportedSimpleStruct()
        {
            var refValueArg = Parameter(typeof(SimplePersonStruct).MakeByRefType(), "value");

            void AssigningRefs(ref SimplePersonStruct value) => value.Health = 5;

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonStruct>>(
                Assign(PropertyOrField(refValueArg, nameof(SimplePersonStruct.Health)), Constant(5)),
                refValueArg);

            void LocalAssert(DeserializeDelegateSimple<SimplePersonStruct> invoke)
            {
                var person = new SimplePersonStruct { Health = 1 };
                invoke(ref person);
                Assert.AreEqual(5, person.Health);
            }

            LocalAssert(AssigningRefs);

            Assert.DoesNotThrow(() => lambda.CompileSys());

            var funcFast = lambda.CompileFast(true);
            LocalAssert(funcFast);
        }

        class SimplePersonClass
        {
            public int Health;
        }

        [Test]
        public void InvokeActionConstantIsSupportedSimpleClass()
        {
            var refValueArg = Parameter(typeof(SimplePersonClass).MakeByRefType(), "value");

            void AssigningRefs(ref SimplePersonClass value) => value.Health += 5;

            var lambda = Lambda<DeserializeDelegateSimple<SimplePersonClass>>(
                AddAssign(PropertyOrField(refValueArg, nameof(SimplePersonClass.Health)), Constant(5)),
                refValueArg);
            lambda.PrintCSharp();

            void LocalAssert(DeserializeDelegateSimple<SimplePersonClass> invoke)
            {
                var person = new SimplePersonClass { Health = 1 };
                invoke(ref person);
                Assert.AreEqual(6, person.Health);
            }
            LocalAssert(AssigningRefs);

            var s = lambda.CompileSys();
            s.PrintIL();
            LocalAssert(s);

            var f = lambda.CompileFast(true);
            f.PrintIL();
            LocalAssert(f);
        }
    }
}
