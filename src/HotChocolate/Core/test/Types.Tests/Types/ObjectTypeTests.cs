using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Moq;
#if NETCOREAPP2_1
using Snapshooter;
#endif
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types
{
    public class ObjectTypeTests : TypeTestBase
    {
        [Fact]
        public void ObjectType_DynamicName()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>()
                    .Field("bar")
                    .Type<StringType>()
                    .Resolve("foo"))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void ObjectType_DynamicName_NonGeneric()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType))
                    .Field("bar")
                    .Type<StringType>()
                    .Resolve("foo"))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericObjectType_DynamicName()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn<StringType>())
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void GenericObjectType_DynamicName_NonGeneric()
        {
            // act
            ISchema schema = SchemaBuilder.New()
                .AddObjectType(d => d
                    .Name(dep => dep.Name + "Foo")
                    .DependsOn(typeof(StringType)))
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("StringFoo");
            Assert.NotNull(type);
        }

        [Fact]
        public void InitializeExplicitFieldWithImplicitResolver()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d => d
                .Field(f => f.Description)
                .Name("a")));

            // assert
            Assert.NotNull(fooType.Fields["a"].Resolver);
        }

        [Fact]
        public void IntArgumentIsInferredAsNonNullType()
        {
            // arrange
            // act
            ObjectType<QueryWithIntArg> fooType =
                CreateType(new ObjectType<QueryWithIntArg>());

            // assert
            IType argumentType = fooType.Fields["bar"]
                .Arguments.First()
                .Type;

            Assert.NotNull(argumentType);
            Assert.True(argumentType.IsNonNullType());
            Assert.Equal("Int", argumentType.NamedType().Name.Value);
        }

        [Fact]
        public async Task FieldMiddlewareIsIntegrated()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ObjectType fooType = CreateType(new ObjectType(c => c
                    .Name("Foo")
                    .Field("bar")
                    .Resolve(() => "baz")),
                b => b.Use(next => async context =>
                {
                    await next(context);

                    if (context.Result is string s)
                    {
                        context.Result = s.ToUpperInvariant();
                    }
                }));

            // assert
            await fooType.Fields["bar"].Middleware(resolverContext.Object);
            Assert.Equal("BAZ", resolverContext.Object.Result);
        }

        [Obsolete("DeprecationReason is obsolete.")]
        [Fact]
        public void DeprecationReason_Obsolete()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ObjectType fooType = CreateType(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .DeprecationReason("fooBar")
                .Resolve(() => "baz")));

            // assert
            Assert.Equal("fooBar", fooType.Fields["bar"].DeprecationReason);
            Assert.True(fooType.Fields["bar"].IsDeprecated);
        }

        [Fact]
        public void Deprecated_Field_With_Reason()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ObjectType fooType = CreateType(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .Deprecated("fooBar")
                .Resolve(() => "baz")));

            // assert
            Assert.Equal("fooBar", fooType.Fields["bar"].DeprecationReason);
            Assert.True(fooType.Fields["bar"].IsDeprecated);
        }

        [Fact]
        public void Deprecated_Field_With_Reason_Is_Serialized()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ISchema schema = CreateSchema(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .Deprecated("fooBar")
                .Resolve(() => "baz")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Deprecated_Field_Without_Reason()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ObjectType fooType = CreateType(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .Deprecated()
                .Resolve(() => "baz")));

            // assert
            Assert.Equal(
                WellKnownDirectives.DeprecationDefaultReason,
                fooType.Fields["bar"].DeprecationReason);
            Assert.True(fooType.Fields["bar"].IsDeprecated);
        }

        [Fact]
        public void Deprecated_Field_Without_Reason_Is_Serialized()
        {
            // arrange
            var resolverContext = new Mock<IMiddlewareContext>();
            resolverContext.SetupAllProperties();

            // act
            ISchema schema = CreateSchema(new ObjectType(c => c
                .Name("Foo")
                .Field("bar")
                .Deprecated()
                .Resolve(() => "baz")));

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void InitializesImplicitFieldWithImplicitResolver()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>());

            // assert
            Assert.NotNull(fooType.Fields.First().Resolver);
        }

        [Fact]
        public void EnsureObjectTypeKindIsCorrect()
        {
            // arrange
            // act
            ObjectType<Foo> someObject = CreateType(new ObjectType<Foo>());

            // assert
            Assert.Equal(TypeKind.Object, someObject.Kind);
        }

        /// <summary>
        /// For the type detection the order of the resolver or type descriptor function should not matter.
        ///
        /// descriptor.Field("test")
        ///   .Resolver{List{string}}(() => new List{string}())
        ///   .Type{ListType{StringType}}();
        ///
        /// descriptor.Field("test")
        ///   .Type{ListType{StringType}}();
        ///   .Resolver{List{string}}(() => new List{string}())
        /// </summary>
        [Fact]
        public void ObjectTypeWithDynamicField_TypeDeclareOrderShouldNotMatter()
        {
            // act
            FooType fooType = CreateType(new FooType());

            // assert
            Assert.True(fooType.Fields.TryGetField("test", out ObjectField field));
            Assert.IsType<ListType>(field.Type);
            Assert.IsType<StringType>(((ListType)field.Type).ElementType);
        }

        [Fact]
        public void GenericObjectTypes()
        {
            // arrange
            // act
            ObjectType<GenericFoo<string>> genericType =
                CreateType(new ObjectType<GenericFoo<string>>());

            // assert
            Assert.Equal("GenericFooOfString", genericType.Name);
        }

        [Fact]
        public void NestedGenericObjectTypes()
        {
            // arrange
            // act
            ObjectType<GenericFoo<GenericFoo<string>>> genericType =
                CreateType(new ObjectType<GenericFoo<GenericFoo<string>>>());

            // assert
            Assert.Equal("GenericFooOfGenericFooOfString", genericType.Name);
        }

        [Fact]
        public void BindFieldToResolverTypeField()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d => d
                .Field<FooResolver>(t => t.GetBar(default))));

            // assert
            Assert.Equal("foo", fooType.Fields["bar"].Arguments.First().Name);
            Assert.NotNull(fooType.Fields["bar"].Resolver);
            Assert.IsType<StringType>(fooType.Fields["bar"].Type);
        }


        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentOutputType()
        {
            // arrange
            var source = @"
                interface A {
                    a: String
                }

                interface B {
                    a: Int
                }

                type C implements A & B {
                    a: String
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments1()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments2()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: Int): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void TwoInterfacesProvideFieldAWithDifferentArguments3()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String, b: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void SpecifyQueryTypeNameInSchemaFirst()
        {
            // arrange
            var source = @"
                type A { field: String }
                type B { field: String }
                type C { field: String }

                schema {
                  query: A
                  mutation: B
                  subscription: C
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(source)
                .Use(_ => _)
                .Create();

            Assert.Equal("A", schema.QueryType.Name.Value);
            Assert.Equal("B", schema.MutationType?.Name.Value);
            Assert.Equal("C", schema.SubscriptionType?.Name.Value);
        }

        [Fact]
        public void SpecifyQueryTypeNameInSchemaFirstWithOptions()
        {
            // arrange
            var source = @"
                type A { field: String }
                type B { field: String }
                type C { field: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(source)
                .Use(_ => _)
                .ModifyOptions(o =>
                {
                    o.QueryTypeName = "A";
                    o.MutationTypeName = "B";
                    o.SubscriptionTypeName = "C";
                })
                .Create();

            Assert.Equal("A", schema.QueryType.Name.Value);
            Assert.Equal("B", schema.MutationType?.Name.Value);
            Assert.Equal("C", schema.SubscriptionType?.Name.Value);
        }

        [Fact]
        public void NoQueryType()
        {
            // arrange
            var source = @"type A { field: String }";

            // act
            void Action()
                => SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .Use(_ => _)
                    .Create();

            Assert.Throws<SchemaException>(Action).Errors.MatchSnapshot();
        }

        [Fact]
        public void ObjectFieldDoesNotMatchInterfaceDefinitionArgTypeInvalid()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: [String]): String
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void ObjectFieldDoesNotMatchInterfaceDefinitionReturnTypeInvalid()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): Int
                }";

            // act
            try
            {
                SchemaBuilder.New()
                    .AddDocumentFromString(source)
                    .AddResolver("C.a", _ => new("foo"))
                    .Create();
            }
            catch (SchemaException ex)
            {
                ex.Message.MatchSnapshot();
                return;
            }

            Assert.True(false, "Schema exception was not thrown.");
        }

        [Fact]
        public void ObjectTypeImplementsAllFields()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String): String
                }

                interface B {
                    a(a: String): String
                }

                type C implements A & B {
                    a(a: String): String
                }

                schema {
                  query: C
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("C");
            Assert.Equal(2, type.Implements.Count);
        }

        [Fact]
        public void ObjectTypeImplementsAllFieldsWithWrappedTypes()
        {
            // arrange
            var source = @"
                interface A {
                    a(a: String!): String!
                }

                interface B {
                    a(a: String!): String!
                }

                type C implements A & B {
                    a(a: String!): String!
                }

                schema {
                  query: C
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(source)
                .AddResolver("C.a", _ => new("foo"))
                .Create();

            // assert
            ObjectType type = schema.GetType<ObjectType>("C");
            Assert.Equal(2, type.Implements.Count);
        }

        [Fact]
        public void NonNullAttribute_StringIsRewritten_NonNullStringType()
        {
            // arrange
            // act
            ObjectType<Bar> fooType = CreateType(new ObjectType<Bar>());

            // assert
            Assert.True(fooType.Fields["baz"].Type.IsNonNullType());
            Assert.Equal("String", fooType.Fields["baz"].Type.NamedType().Name);
        }

        [Fact]
        public void ObjectType_FieldDefaultValue_SerializesCorrectly()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => "")
                .Argument("_456",
                    a => a.Type<InputObjectType<Foo>>()
                        .DefaultValue(new Foo())));

            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_ResolverOverrides_FieldMember()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Resolve("World"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncString_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => "fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncString_ResolverInferType()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Resolve(() => "fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_ConstantString_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve("fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_ConstantString_ResolverInferType()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Resolve("fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxString_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(ctx => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxString_ResolverInferType()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Resolve(ctx => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxCtString_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((ctx, _) => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxCtString_ResolverInferType()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Resolve((ctx, _) => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(() => (object)"fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_ConstantObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((object)"fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve(ctx => (object)ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_FuncCtxCtObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType(t => t
                .Name("Bar")
                .Field("_123")
                .Type<StringType>()
                .Resolve((ctx, _) => (object)ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ _123 }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncString_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(() => "fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_ConstantString_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve("fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncCtxString_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(ctx => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncCtxCtString_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((ctx, _) => ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(() => (object)"fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_ConstantObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((object)"fooBar"));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncCtxObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve(ctx => (object)ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectTypeOfFoo_FuncCtxCtObject_Resolver()
        {
            // arrange
            var objectType = new ObjectType<Foo>(t => t
                .Field(f => f.Description)
                .Type<StringType>()
                .Resolve((ctx, _) => (object)ctx.Selection.Field.Name.Value));

            // act
            IRequestExecutor executor =
                SchemaBuilder.New()
                    .AddQueryType(objectType)
                    .Create()
                    .MakeExecutable();

            // assert
            executor.Execute("{ description }").ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task ObjectType_SourceTypeObject_BindsResolverCorrectly()
        {
            // arrange
            var objectType = new ObjectType(t => t.Name("Bar")
                .Field<FooResolver>(f => f.GetDescription(default))
                .Name("desc")
                .Type<StringType>());

            ISchema schema = SchemaBuilder.New()
                .AddQueryType(objectType)
                .Create();

            IRequestExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ desc }")
                    .SetInitialValue(new Foo())
                    .Create());

            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void InferInterfaceImplementation()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(),
                b => b.AddType(new InterfaceType<IFoo>()));

            // assert
            Assert.IsType<InterfaceType<IFoo>>(
                fooType.Implements[0]);
        }

        [Fact]
        public void IgnoreFieldWithShortcut()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d =>
            {
                d.Ignore(t => t.Description);
                d.Field("foo").Type<StringType>().Resolve("abc");
            }));

            // assert
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("foo", t.Name));
        }

        [Fact]
        public void UnIgnoreFieldWithShortcut()
        {
            // arrange
            // act
            ObjectType<Foo> fooType = CreateType(new ObjectType<Foo>(d =>
            {
                d.Ignore(t => t.Description);
                d.Field("foo").Type<StringType>().Resolve("abc");
                d.Field(t => t.Description).Ignore(false);
            }));

            // assert
            Assert.Collection(
                fooType.Fields.Where(t => !t.IsIntrospectionField),
                t => Assert.Equal("description", t.Name),
                t => Assert.Equal("foo", t.Name));
        }

        [Fact]
        public void IgnoreField_DescriptorIsNull_ArgumentNullException()
        {
            // arrange
            // act
            void Action() => ObjectTypeDescriptorExtensions.Ignore<Foo>(null, t => t.Description);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public void IgnoreField_ExpressionIsNull_ArgumentNullException()
        {
            // arrange
            var descriptor = new Mock<IObjectTypeDescriptor<Foo>>();

            // act
            Action a = () => descriptor.Object.Ignore(null);

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void DoNotAllow_InputTypes_OnFields()
        {
            // arrange
            // act
            void Action() =>
                SchemaBuilder.New()
                    .AddType(new ObjectType(t => t.Name("Foo")
                        .Field("bar")
                        .Type<NonNullType<InputObjectType<Foo>>>()))
                    .Create();

            // assert
            Assert.Throws<SchemaException>(Action)
                .Errors[0]
                .Message.MatchSnapshot();
        }

        [Fact]
        public void DoNotAllow_DynamicInputTypes_OnFields()
        {
            // arrange
            // act
            void Action() =>
                SchemaBuilder.New()
                    .AddType(new ObjectType(t => t.Name("Foo")
                        .Field("bar")
                        .Type(new NonNullType(new InputObjectType<Foo>()))))
                    .Create();

            // assert
            Assert.Throws<SchemaException>(Action)
                .Errors[0]
                .Message.MatchSnapshot();
        }

        [Fact]
        public void Support_Argument_Attributes()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Baz>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Argument_Type_IsInferred_From_Parameter()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithIntArg>(t => t
                    .Field(f => f.GetBar(1))
                    .Argument("foo", a => a.DefaultValue(default)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Argument_Type_Cannot_Be_Inferred()
        {
            // arrange
            // act
            void Action() =>
                SchemaBuilder.New()
                    .AddQueryType<QueryWithIntArg>(t => t.Field(f => f.GetBar(1))
                        .Argument("bar", a => a.DefaultValue(default)))
                    .Create();

            // assert
            Assert.Throws<SchemaException>(Action)
                .Errors[0]
                .Message.MatchSnapshot();
        }

        [Fact]
        public void CreateObjectTypeWithXmlDocumentation()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithDocumentation>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateObjectTypeWithXmlDocumentation_IgnoreXmlDocs()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithDocumentation>()
                .ModifyOptions(options => options.UseXmlDocumentation = false)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void CreateObjectTypeWithXmlDocumentation_IgnoreXmlDocs_SchemaCreate()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithDocumentation>()
                .ModifyOptions(o => o.UseXmlDocumentation = false)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Field_Is_Missing_Type_Throws_SchemaException()
        {
            // arrange
            // act
            Action action = () => SchemaBuilder.New()
                .AddObjectType(t => t
                    .Name("abc")
                    .Field("def")
                    .Resolve((object)"ghi"))
                .Create();

            // assert
            Assert.Throws<SchemaException>(action)
                .Errors.Select(t => new { t.Message, t.Code })
                .MatchSnapshot();
        }

        [Fact]
        public void Deprecate_Obsolete_Fields()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
                .AddType(new ObjectType<FooObsolete>())
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Deprecate_Fields_With_Deprecated_Attribute()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(c => c
                    .Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"))
                .AddType(new ObjectType<FooDeprecated>())
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_From_Struct()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(new ObjectType<FooStruct>())
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task Execute_With_Query_As_Struct()
        {
            // arrange
            IRequestExecutor executor = SchemaBuilder.New()
                .AddQueryType(new ObjectType<FooStruct>())
                .Create()
                .MakeExecutable();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                QueryRequestBuilder.New()
                    .SetQuery("{ bar baz }")
                    .SetInitialValue(new FooStruct { Qux = "Qux_Value", Baz = "Baz_Value" })
                    .Create());
            // assert
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void ObjectType_From_Dictionary()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooWithDict>()
                .Create();

            // assert
#if NETCOREAPP2_1
            schema.ToString().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.ToString().MatchSnapshot();
#endif
        }

        [Fact]
        public void Infer_List_From_Queryable()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<MyListQuery>()
                .Create();

            // assert
#if NETCOREAPP2_1
            schema.ToString().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.ToString().MatchSnapshot();
#endif
        }

        [Fact]
        public void NonNull_Attribute_With_Explicit_Nullability_Definition()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<AnnotatedNestedList>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Infer_Non_Null_Filed()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<Bar>()
                .Create();

            // assert
#if NETCOREAPP2_1
            schema.ToString().MatchSnapshot(new SnapshotNameExtension("NETCOREAPP2_1"));
#else
            schema.ToString().MatchSnapshot();
#endif
        }

        [Fact]
        public void Ignore_Fields_With_GraphQLIgnoreAttribute()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<FooIgnore>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_String()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(string)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_NativeTypeListOfInt()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(NativeType<List<int>>)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_ListTypeOfIntType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(ListType<IntType>)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_Override_ListTypeOfIntType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(
                        _ => new ValueTask<object>("abc"),
                        typeof(ListType<IntType>)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_Weak_Override_ListTypeOfIntType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object>("abc"), typeof(int)))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Declare_Resolver_With_Result_Type_Is_Null()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType(t => t
                    .Name("Query")
                    .Field("test")
                    .Type<StringType>()
                    .Resolve(_ => new ValueTask<object>("abc"), null))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Infer_Argument_Default_Values()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddQueryType<QueryWithArgumentDefaults>()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void Inferred_Interfaces_From_Type_Extensions_Are_Merged()
        {
            SchemaBuilder.New()
                .AddDocumentFromString(
                    @"type Query {
                        some: Some
                    }

                    type Some {
                        foo: String
                    }")
                .AddType<SomeTypeExtensionWithInterface>()
                .Use(_ => _ => default)
                .EnableRelaySupport()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Interfaces_From_Type_Extensions_Are_Merged()
        {
            SchemaBuilder.New()
                .AddDocumentFromString("type Query { some: Some } type Some { foo: String }")
                .AddDocumentFromString("extend type Some implements Node { id: ID! }")
                .Use(next => context => default(ValueTask))
                .EnableRelaySupport()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Nested_Lists_With_Sdl_First()
        {
            SchemaBuilder.New()
                .AddDocumentFromString("type Query { some: [[Some]] } type Some { foo: String }")
                .Use(next => context => default(ValueTask))
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Nested_Lists_With_Code_First()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithNestedList>()
                .Create()
                .ToString()
                .MatchSnapshot();
        }

        [Fact]
        public void Execute_Nested_Lists_With_Code_First()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithNestedList>()
                .Create()
                .MakeExecutable()
                .Execute("{ fooMatrix { baz } }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void ResolveWith()
        {
            SchemaBuilder.New()
                .AddQueryType<ResolveWithQueryType>()
                .Create()
                .MakeExecutable()
                .Execute("{ foo baz }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void ResolveWithAsync()
        {
            SchemaBuilder.New()
                .AddQueryType<ResolveWithQueryTypeAsync>()
                .Create()
                .MakeExecutable()
                .Execute("{ foo baz qux quux quuz }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void ResolveWith_NonGeneric()
        {
            SchemaBuilder.New()
                .AddQueryType<ResolveWithNonGenericObjectType>()
                .Create()
                .MakeExecutable()
                .Execute("{ foo }")
                .ToJson()
                .MatchSnapshot();
        }

        [Fact]
        public void IgnoreIndexers()
        {
            SchemaBuilder.New()
                .AddQueryType<QueryWithIndexer>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void ObjectType_InObjectType_ThrowsSchemaException()
        {
            // arrange
            // act
            Exception ex = Record.Exception(
                () => SchemaBuilder
                    .New()
                    .AddQueryType(x => x.Name("Query").Field("Foo").Resolve("bar"))
                    .AddType<ObjectType<ObjectType<Foo>>>()
                    .ModifyOptions(o => o.StrictRuntimeTypeValidation = true)
                    .Create());

            // assert
            Assert.IsType<SchemaException>(ex);
            ex.Message.MatchSnapshot();
        }

        [Fact]
        public void Specify_Field_Type_With_SDL_Syntax()
        {
            SchemaBuilder.New()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("Foo").Type("String").Resolve(_ => null);
                })
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Specify_Argument_Type_With_SDL_Syntax()
        {
            SchemaBuilder.New()
                .AddQueryType(d =>
                {
                    d.Name("Query");
                    d.Field("Foo")
                        .Argument("a", t => t.Type("Int"))
                        .Type("String")
                        .Resolve(_ => null);
                })
                .Create()
                .Print()
                .MatchSnapshot();
        }

        [Fact]
        public void Infer_Types_Correctly_When_Using_ResolveWith()
        {
            SchemaBuilder.New()
                .AddQueryType<InferNonNullTypesWithResolveWith>()
                .Create()
                .Print()
                .MatchSnapshot();
        }

        public class GenericFoo<T>
        {
            public T Value { get; }
        }

        public class Foo
            : IFoo
        {
            public Foo() { }

            public Foo(string description)
            {
                Description = description;
            }

            public string Description { get; } = "hello";
        }

        public interface IFoo
        {
            string Description { get; }
        }

        public class FooResolver
        {
            public string GetBar(string foo) => "hello foo";

            public string GetDescription([Parent] Foo foo) => foo.Description;
        }

        public class QueryWithIntArg
        {
            public string GetBar(int foo) => "hello foo";
        }

#nullable enable
        public class Bar
        {
            [GraphQLNonNullType]
            public string Baz { get; set; }
        }
#nullable disable

        public class Baz
        {
            public string Qux(
                [GraphQLName("arg2")] [GraphQLDescription("argdesc")] [GraphQLNonNullType]
                string arg) => arg;

            public string Quux([GraphQLType(typeof(ListType<StringType>))] string arg) => arg;
        }

        public class FooType
            : ObjectType<Foo>
        {
            protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Description);
                descriptor.Field("test")
                    .Resolve(() => new List<string>())
                    .Type<ListType<StringType>>();
            }
        }

        public class FooObsolete
        {
            [Obsolete("Baz")]
            public string Bar() => "foo";
        }

        public class FooIgnore
        {
            [GraphQLIgnore]
            public string Bar() => "foo";

            public string Baz() => "foo";
        }

        public class FooDeprecated
        {
            [GraphQLDeprecated("Use Bar2.")]
            public string Bar() => "foo";

            public string Bar2() => "Foo 2: Electric foo-galoo";
        }

        public struct FooStruct
        {
            // should be ignored by the automatic field
            // inference.
            public string Qux;

            // should be included by the automatic field
            // inference.
            public string Baz { get; set; }

            // should be ignored by the automatic field
            // inference since we cannot determine what object means
            // in the graphql context.
            // This field has to be included explicitly.
            public object Quux { get; set; }

            // should be included by the automatic field
            // inference.
            public string GetBar() => Qux + "_Bar_Value";
        }

        public class FooWithDict
        {
            public Dictionary<string, Bar> Map { get; set; }
        }

        public class MyList
            : MyListBase
        {
        }

        public class MyListBase
            : IQueryable<Bar>
        {
            public Type ElementType => throw new NotImplementedException();

            public Expression Expression => throw new NotImplementedException();

            public IQueryProvider Provider => throw new NotImplementedException();

            public IEnumerator<Bar> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }

        public class MyListQuery
        {
            public MyList List { get; set; }
        }

        public class FooWithNullable
        {
            public bool? Bar { get; set; }

            public List<bool?> Bars { get; set; }
        }

        public class QueryWithArgumentDefaults
        {
            public string Field1(
                string a = null,
                string b = "abc") => null;

            public string Field2(
                [DefaultValue(null)] string a,
                [DefaultValue("abc")] string b) => null;
        }

        [ExtendObjectType("Some")]
        public class SomeTypeExtensionWithInterface : INode
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string Id { get; }
        }

        public class QueryWithNestedList
        {
            public List<List<FooIgnore>> FooMatrix =>
                new() { new() { new() } };
        }

        public class ResolveWithQuery
        {
            public int Foo { get; set; } = 123;
        }

        public class ResolveWithQueryResolver
        {
            public string Bar { get; set; } = "Bar";

            public Task<string> FooAsync() => Task.FromResult("Foo");

            public Task<bool> BarAsync(IResolverContext context)
                => Task.FromResult(context is not null);
        }

        public class ResolveWithQueryType : ObjectType<ResolveWithQuery>
        {
            protected override void Configure(IObjectTypeDescriptor<ResolveWithQuery> descriptor)
            {
                descriptor.Field(t => t.Foo).ResolveWith<ResolveWithQueryResolver>(t => t.Bar);
                descriptor.Field("baz").ResolveWith<ResolveWithQueryResolver>(t => t.Bar);
            }
        }

        public class ResolveWithQueryTypeAsync : ObjectType<ResolveWithQuery>
        {
            protected override void Configure(IObjectTypeDescriptor<ResolveWithQuery> descriptor)
            {
                descriptor.Field(t => t.Foo).ResolveWith<ResolveWithQueryResolver>(t => t.FooAsync());
                descriptor.Field("baz").ResolveWith<ResolveWithQueryResolver>(t => t.FooAsync());

                descriptor.Field("qux").ResolveWith<ResolveWithQueryResolver, string>(t => t.Bar);
                descriptor.Field("quux").ResolveWith<ResolveWithQueryResolver, string>(t => t.FooAsync());

                descriptor.Field("quuz").ResolveWith<ResolveWithQueryResolver, bool>(t => t.BarAsync(default));
            }
        }

        public class ResolveWithNonGenericObjectType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                Type type = typeof(ResolveWithQuery);

                descriptor.Name("ResolveWithQuery");

                descriptor.Field("foo")
                    .Type<IntType>()
                    .ResolveWith(type.GetProperty("Foo"));
            }
        }

        public class AnnotatedNestedList
        {
            [GraphQLNonNullType(true, false, false)]
            public List<List<string>> NestedList { get; set; }
        }

        public class QueryWithIndexer
        {
            public string this[int i]
            {
                get => throw new NotImplementedException();
            }

            public string GetFoo() => throw new NotImplementedException();
        }

        #nullable enable

        public class InferNonNullTypesWithResolveWith : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Name("Query");

                descriptor
                    .Field("foo")
                    .ResolveWith<InferNonNullTypesWithResolveWithResolvers>(t => t.Foo);

                descriptor
                    .Field("bar")
                    .ResolveWith<InferNonNullTypesWithResolveWithResolvers>(t => t.Bar);
            }
        }

        public class InferNonNullTypesWithResolveWithResolvers
        {
            public string? Foo => "Foo";

            public string Bar => "Bar";
        }
    }
}
