# Creating an Index

To unlock some of the nicest functionality of Redis OM, e.g., running searches, matches, aggregations, reductions, mappings, etc... You will need to tell Redis how you want data to be stored and how you want it indexed. One of the features the Redis OM library provides is creating indices that map directly to your objects by declaring the indices as attributes on your class.

Let's start with an example class.

```csharp
[Document]
public partial class Person
{
    [RedisIdField]
    public string Id { get; set; }    

    [Searchable(Sortable = true)]        
    public string Name { get; set; }

    [Indexed(Aggregatable = true)]
    public GeoLoc? Home { get; set; }

    [Indexed(Aggregatable = true)]
    public GeoLoc? Work { get; set; }

    [Indexed(Sortable = true)]
    public int? Age { get; set; }

    [Indexed(Sortable = true)]
    public int? DepartmentNumber { get; set; }

    [Indexed(Sortable = true)]
    public double? Sales { get; set; }

    [Indexed(Sortable = true)]
    public double? SalesAdjustment { get; set; }

    [Indexed(Sortable = true)]
    public long? LastTimeOnline { get; set; }
    
    [Indexed(Aggregatable = true)]
    public string Email { get; set; }
}
```

As shown above, you can declare a class as being indexed with the `Document` Attribute. In the `Document` attribute, you can set a few fields to help build the index:

|Property Name|Description|Default|Optional|
|-------------|-----------|-------|--------|
|StorageType|Defines the underlying data structure used to store the object in Redis, options are `HASH` and `JSON`, Note JSON is only useable with the [RedisJson module](https://oss.redis.com/redisjson/)|HASH|true|
|IndexName|The name of the index |`$"{SimpleClassName.ToLower()}-idx}`|true|
|Prefixes|The key prefixes for redis to build an index off of |`new string[]{$"{FullyQualifiedClassName}:"}`|true|
|Language| Language to use for full-text search indexing|`null`|true|
|LanguageField|The name of the field in which the document stores its Language|null|true|
|Filter|The filter to use to determine whether a particular item is indexed, e.g. `@Age>=18` |null|true|
|IdGenerationStrategy|The strategy used to generate Ids for documents, if left blank it will use a [ULID](https://github.com/ulid/spec) generation strategy|UlidGenerationStrategy|true|

## Field Level Declarations

### Id Fields

Every class indexed by Redis must contain an Id Field marked with the `RedisIdField`.

### Indexed Fields

In addition to declaring an Id Field, you can also declare indexed fields, which will let you search for values within those fields afterward. There are two types of Field level attributes.

1. Indexed - This type of index is valid for fields that are of the type `string`, a Numeric type (double/int/float etc. . .), or can be decorated for fields that are of the type `GeoLoc`, the exact way that the indexed field is interpreted depends on the indexed type
2. Searchable - This type is only valid for `string` fields, but this enables full-text search on the decorated fields.

#### IndexedAttribute Properties

There are properties inside the `IndexedAttribute` that let you further customize how things are stored & queried.

|PropertyName|type|Description|Default|Optional|
|------------|----|-----------|-------|--------|
|PropertyName|`string`|The name of the property to be indexed|The name of the property being indexed|true|
|Sortable|`bool`|Whether to index the item so it can be sorted on in queries, enables use of `OrderBy` & `OrderByDescending` -> `collection.OrderBy(x=>x.Email)`|`false`|true|
|Normalize|`bool`|Only applicable for `string` type fields Determines whether the text in a field is normalized (sent to lower case) for purposes of sorting|`true`|true|
|Separator|`char`|Only applicable for `string` type fields Character to use for separating tag field, allows the application of multiple tags fo the same item e.g. `article.Category = technology,parenting` is delineated by a `,` means that `collection.Where(x=>x.Category == "technology")` and `collection.Where(x=>x.Category == "parenting")` will both match the record|`|`|true|
|CaseSensitive|`bool`|Only applicable for `string` type fields - Determines whether case is considered when performing matches on tags|`false`|true|

#### SearchableAttribute Properties

There are properties for the `SearchableAttribute` that let you further customize how the full-text search determines matches

|PropertyName|type|Description|Default|Optional|
|------------|----|-----------|-------|--------|
|PropertyName|`string`|The name of the property to be indexed|The name of the indexed property |true|
|Sortable|`bool`|Whether to index the item so it can be sorted on in queries, enables use of `OrderBy` & `OrderByDescending` -> `collection.OrderBy(x=>x.Email)`|`false`|true|
|NoStem|`bool`|Determines whether to use [stemming](https://oss.redis.com/redisearch/Stemming/), in other words adding the stem of the word to the index, setting to true will stop the Redis from indexing the stems of words|`false`|true|
|PhoneticMatcher|`string`|The phonetic matcher to use if you'd like the index to use (PhoneticMatching)[https://oss.redis.com/redisearch/Phonetic_Matching/] with the index|null|true|
|Weight|`double`|determines the importance of the field for checking result accuracy|1.0|true|

## Creating The Index

After declaring the index, the creation of the index is pretty straightforward. All you have to do is call `CreateIndex` for the decorated type. The library will take care of serializing the provided type into a searchable index. The library does not try to be particularly clever, so if the index already exists it will the creation request will be rejected, and you will have to drop and re-add the index (migrations is a feature that may be added in the future)

```csharp
var connection = provider.Connection;
connection.CreateIndex(typeof(Person));
```