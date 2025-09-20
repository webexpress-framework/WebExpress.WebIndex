![WebExpress-Framework](https://raw.githubusercontent.com/webexpress-framework/.github/main/docs/assets/img/banner.png)

# WebExpress.WebIndex
The index model provides a reverse index to enable fast and efficient searching. A reverse 
index can significantly speed up access to the data. However, creating and storing a reverse 
index requires additional storage space and processing time. The storage requirement increases, 
especially with large amounts of data, which can be important. Therefore, it is important to 
weigh the pros and cons to achieve the best possible performance. The reverse index offers the 
added value of a fast and resource-saving full-text search and accepts the costs mentioned. The 
full-text search in `WebExpress` supports the following search options:

- Word search
- Wildcard search
- Phrase search (exact word sequence)
- Proximity search
- Fuzzy search

The indexing process begins with the analysis of documents, where the documents are broken 
down into smaller units, usually words or phrases. These broken-down units are then converted 
into normalized tokens. Normalization can take various forms, such as converting all letters 
to lowercase, removing punctuation, or reducing words to their stem. In addition, stop words 
are removed. Stop words are frequently occurring words like "and", "is", "in", which typically 
do not provide added value for the search. These words are filtered out to improve efficiency 
and reduce storage requirements. In many languages, words can appear in different forms that 
all refer to the same concept. Therefore, techniques such as stemming, or lemmatization are 
often applied to reduce different forms of a word to a common representation. These additional 
steps help improve the accuracy and relevance of search results and help keep the index compact 
and manageable. The normalized tokens are stored in a reverse index. This index is structured 
so that it lists the documents in which each token appears for each token. In addition to the 
tokens, more information is stored, such as the frequency of the tokens or the position of 
the tokens in the document. During the search process, the search words are tokenized and 
normalized in the same way, and then each token is looked up in the reverse index. The documents 
or positions found in the lists of all tokens are the search results. This method allows for a 
fast and efficient search, as the time-consuming part is carried out in advance during the 
indexing process and the actual search consists only of quick lookup operations in the reverse 
index.

```
┌──────────┐       indexing
│ document ├──────────────┐
└──────────┘              │
                          ▼
┌───────┐ searching ┌───────────┐       ┌─────────────────────────────────────────────────┐       ╔══════════╗
│ query ├──────────>│ tokenizer ├──────>│ stemming, lemmatization & stoppword filter pipe ├──────>║ WebIndex ║
└───────┘           └───────────┘       └─────────────────────────────────────────────────┘       ╚══════════╝
    ▲                                           results                                                 │
    └───────────────────────────────────────────────────────────────────────────────────────────────────┘
```

Stemming and lemmatization are text preprocessing techniques in natural language processing (NLP). They 
reduce the inflected forms of words in a text dataset to a common root form or dictionary form, also 
known as a "lemma" in computational linguistics.

Stemming usually refers to a crude heuristic process that trims the ends of words in the hope 
of achieving this goal mostly correctly, and often involves removing derivational affixes. Stemming 
algorithms attempt to find the common roots of different inflections by cutting off the endings or 
beginnings of the word.

Lemmatization usually refers to doing things correctly, using a vocabulary and morphological analysis 
of words, usually with the aim of removing only inflectional endings and returning the base or dictionary 
form of a word, known as a lemma. Unlike stemming, which operates on a single word without knowledge of 
the context, lemmatization can distinguish between words that have different meanings depending on the 
part of speech.

These techniques are particularly useful in information search systems such as search engines, where 
users can submit a query using one word (e.g., "meditate") but expect results that use any inflected 
form of the word (e.g. "meditated", "meditation", etc).

In this instance, indexing is performed on two documents by executing a series of operations: tokenization, 
normalization, and stop-word removal. The outcome of these operations is a multi-dimensional table, which 
serves as a representation of the reverse index.

```
┌document a────────────────────────────────────────┐      ┌document b────────────────────────────────────────┐
│ No, fine, no , good, fine, good. You know Marty, │      │ Thanks a lot, kid. Now, of course not, Biff, now,│
│ you look so familiar, do I know your mother? Hey │      │ I wouldn't want that to happen. I'm gonna ram    │
│ man, the dance is over. Unless you know someone  │      │ him. Well, Marty, I want to thank you for all    │
│ else who could play the guitar. Who's are these? │      │ your good advice, I'll never forget it. Doc,     │
│ Maybe you were adopted.                          │      │ look, all we need is a little plutonium.         │
└──────────────────────┬───────────────────────────┘      └──────────────────────┬───────────────────────────┘
                       │                                                         │
                       │                                                         │
                       ▼                                                         ▼
┌normalized document a─────────────────────────────┐      ┌normalized document b─────────────────────────────┐
│ no fine no good fine good you know marty         │      │ thank a lot kid now of course not biff now       │
│ you look so familiar do i know your mother hey   │      │ i would not want that to happen i am gonna ram   │
│ man the dance is over unless you know someone    │      │ him well marty i want to thank you for all       │
│ else who could play the guitar who is are these  │      │ your good advice i will never forget it doc      │
│ maybe you were adopted                           │      │ look all we need is a little plutonium           │
└──────────────────────┬───────────────────────────┘      └──────────────────────┬───────────────────────────┘
                       │                                                         │
                       │                                                         │
                       ▼                                                         ▼
┌stopword cleaned document a───────────────────────┐      ┌stopword cleaned document b───────────────────────┐
│ fine good fine good know marty                   │      │ thank lot kid course biff                        │
│ look familiar know mother                        │      │ would want happen gonna ram                      │
│ dance unless know someone                        │      │ well marty want thank                            │
│ else could play guitar these                     │      │ good advice never forget doc                     │
│ maybe adopted                                    │      │ look need little plutonium                       │
└─────────────────────┬────────────────────────────┘      └─────────────────────┬────────────────────────────┘
                      │                                                         │
                      │                                                         │
                      └───────────────┐                       ┌─────────────────┘
                                      ▼                       ▼
                          ╔WebIndex═══════════════════════════════════════╗
                          ║ Term      │ Fequency │ Documnet │ Position    ║
                          ║═══════════════════════════════════════════════║
                          ║ adopted   │ 1        │ a        │ 211         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ advice    │ 1        │ b        │ 153         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ biff      │ 1        │ b        │ 40          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ could     │ 1        │ a        │ 156         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ course    │ 1        │ b        │ 28          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ dance     │ 1        │ a        │ 108         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ doc       │ 1        │ b        │ 183         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ else      │ 1        │ a        │ 147         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ familiar  │ 1        │ a        │ 62          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ fine      │ 2        │ a        │ 5           ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ forget    │ 1        │ b        │ 22          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ gonna     │ 1        │ b        │ 87          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ good      │ 3        │ a        │ 16, 28      ║
                          ║           │          │ b        │ 148         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ guitar    │ 1        │ a        │ 171         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ happen    │ 1        │ b        │ 75          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ kid       │ 1        │ b        │ 15          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ know      │ 3        │ a        │ 38, 77, 134 ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ little    │ 1        │ b        │ 211         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ look      │ 2        │ a        │ 54          ║
                          ║           │          │ b        │ 188         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ lot       │ 1        │ b        │ 10          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ marty     │ 2        │ a        │ 43          ║
                          ║           │          │ b        │ 108         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ maybe     │ 1        │ a        │ 196         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ mother    │ 1        │ a        │ 87          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ need      │ 1        │ b        │ 201         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ never     │ 1        │ b        │ 166         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ play      │ 1        │ a        │ 162         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ plutonium │ 1        │ b        │ 218         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ ram       │ 1        │ b        │ 93          ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ someone   │ 1        │ a        │ 139         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ thank     │ 2        │ b        │ 0, 125      ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ these     │ 1        │ a        │ 189         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ unless    │ 1        │ a        │ 123         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ want      │ 2        │ b        │ 62, 117     ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ well      │ 1        │ b        │ 102         ║
                          ║───────────┼──────────┼──────────┼─────────────║
                          ║ would     │ 1        │ b        │ 53          ║
                          ╚═══════════════════════════════════════════════╝
                                                 ▲
                                                 │
                                                 │
                                    ┌stop word cleaned query──┐
                                    │ marty play guitar       │
                                    └─────────────────────────┘
                                                 ▲
                                                 │
                                                 │
                                    ┌normalized query─────────┐
                                    │ marty play the guitar   │
                                    └─────────────────────────┘
                                                 ▲
                                                 │
                                                 │
                                    ┌query───────┴─────────────┐
                                    │ 'Marty play the guitar.' │
                                    └──────────────────────────┘
```

`WebIndex` is an efficient system that combines document store and reverse indices to support 
a variety of search options. The `IndexDocumentStore` stores all instances of a document for 
quick access, regardless of other persistent storage forms such as databases. On the other hand, 
the reverse index is created for each field `IndexField` of a document unless it is marked with 
`IndexIgnore`. The field contents are tokenized, normalized, and filtered to create the terms of 
the reverse index. Each term in the reverse index is associated with a posting that contains the 
IDs of the document instances that contain the term. The position where the term was found within 
the attribute value is stored in the position. There can be multiple positions for each posting. 
When searching for one or more terms, the IDs of the instances and their positions within the 
attribute values can be determined.

```
╔IndexManager══════════════════════════════════════════╗
║   ┌──────────┐                                       ║
║   │ WebIndex │                                       ║
║   └────┬─────┘                                       ║
║      1 │                                             ║
║        │            ┌IndexDocumentStore---------┐    ║
║      * ▼            ¦                           ¦    ║
║ ┌───────────────┐ 1 ¦ * ┌──────┐                ¦    ║
║ │ IndexDocument ├──────►│ Item │                ¦    ║
║ └──────┬────────┘   ¦   └──────┘                ¦    ║
║      1 │            └---------------------------┘    ║
║        │                                             ║
║      * ▼                                             ║
║  ┌────────────┐                                      ║
║  │ IndexField │                                      ║
║  └─────┬──────┘                                      ║
║      1 │                                             ║
║ ┌------│--------IndexReverse┐                        ║
║ ¦    * ▼                    ¦                        ║
║ ¦  ┌──────┐                 ¦                        ║
║ ¦  │ Term │                 ¦                        ║
║ ¦  └───┬──┘                 ¦                        ║
║ ¦    1 │                    ¦                        ║
║ ¦      │                    ¦                        ║
║ ¦    * ▼                    ¦                        ║
║ ¦ ┌─────────┐               ¦                        ║
║ ¦ │ Posting │               ¦                        ║
║ ¦ └────┬────┘               ¦                        ║
║ ¦    1 │                    ¦                        ║
║ ¦      │                    ¦                        ║
║ ¦    * ▼                    ¦                        ║
║ ¦ ┌──────────┐              ¦                        ║
║ ¦ │ Position │              ¦                        ║
║ ¦ └──────────┘              ¦                        ║
║ └---------------------------┘                        ║
╚══════════════════════════════════════════════════════╝
```

# IndexManager
The index manager is a central component of the `WebIndex` system and serves as the primary interface 
for interacting with the indexing functions. It is responsible for managing the various `IndexDocuments` 
that are created in `WebIndex`. Each `IndexDocument` represents a collection of documents that need to 
be indexed, and the index manager ensures that these documents are indexed correctly and efficiently. In 
addition, the index manager provides functions for adding, updating, and deleting documents in the 
index. It also enables the execution of search queries on the index and returns the corresponding 
results. Finally, the index manager provides high control over the indexing process by allowing certain 
fields to be excluded from indexing or determining whether the index should be created in main memory or 
persistently in the file system. An `IndexDocument` created in main memory enables faster indexing and 
searching. However, the number of objects it can support is limited and depends on the size of the 
available main memory. Therefore, it is important to weigh the pros and cons and choose the best solution 
for the specific requirements. The following diagram serves as a guide to estimate the performance and 
resources required when using the file-based approach: 

```
                                                             TIME CHART                                        >01:50
  [h] ▲ [ms]                                                                                                        ▓
      │                                                                                                             ▓ 
 0:24 ┼ 12                                                                                                          ≈ 
 0:23 ┤                                                                                                             ▓ 
 0:22 ┼ 11                                                                                                          ▓ 
 0:21 ┤                                                                                                             ▓ 
 0:20 ┼ 10                                                                                                          ▓ 
 0:19 ┤                                                                                                             ▓ 
 0:18 ┼ 9                                                                                                           ▓ 
 0:17 ┤                                                                                                             ▓ 
 0:16 ┼ 8                                                                                                           ▓ 
 0:15 ┤                                                                                                             ▓ 
 0:14 ┼ 7                                                                                                           ▓ 
 0:13 ┤                                                                                                             ▓ 
 0:12 ┼ 6                                                                                              0:11         ▓ 
 0:11 ┤                                                                                      0:10         ▓         ▓>5
 0:10 ┼ 5                                                                                       ▓         ▓         ▓░
 0:09 ┤                                                                            0:08         ▓         ▓         ▓░
 0:08 ┼ 4                                                                0:07         ▓         ▓         ▓         ▓░
 0:06 ┤                                                        0:06         ▓         ▓         ▓         ▓         ▓░
 0:06 ┼ 3                                            0:05         ▓         ▓         ▓         ▓         ▓         ▓░
 0:05 ┤                                    0:04         ▓         ▓         ▓         ▓4        ▓         ▓         ▓░
 0:04 ┼ 2                        0:03         ▓         ▓         ▓         ▓         ▓░        ▓3        ▓         ▓░
 0:03 ┤          2     0:02         ▓         ▓         ▓2        ▓2        ▓2        ▓░        ▓░        ▓2        ▓░
 0:02 ┼ 1    0:01░        ▓1        ▓1        ▓1        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░
 0:01 ┤         ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░        ▓░
      └──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬───~~~───┬─────►
              10,000    20,000    30,000    40,000    50,000    60,000    70,000    80,000    90,000    100,000  1,000,000 [items]
              
        ▓ - time required to reindex in [h]
        ░ - time required to retrieve in [ms]
```

```
                                                         STORAGE SPACE CHART                                    >5,000
 [MB] ▲                                                                                                              ▒
      │                                                                                                    573       ≈
  550 ┤                                                                                                    ▒         ▒
      │                                                                                          516       ▒         ▒
  500 ┤                                                                                          ▒         ▒    1,525▒
      │                                                                                460       ▒         ▒        █▒
  450 ┤                                                                                ▒         ▒         ▒        ≈▒
      │                                                                                ▒         ▒         ▒        █▒
  400 ┤                                                                      403       ▒         ▒         ▒        █▒
      │                                                                      ▒         ▒         ▒         ▒        █▒
  350 ┤                                                            345       ▒         ▒         ▒         ▒        █▒
      │                                                            ▒         ▒         ▒         ▒         ▒        █▒
  300 ┤                                                  291       ▒         ▒         ▒         ▒         ▒        █▒
      │                                                  ▒         ▒         ▒         ▒         ▒         ▒        █▒
  250 ┤                                                  ▒         ▒         ▒         ▒         ▒         ▒        █▒
      │                                        234       ▒         ▒         ▒         ▒         ▒         ▒        █▒
  200 ┤                                        ▒         ▒         ▒         ▒         ▒         ▒         ▒        █▒
      │                              178       ▒         ▒         ▒         ▒         ▒         ▒         ▒        █▒
  150 ┤                              ▒         ▒         ▒         ▒         ▒         ▒         ▒      152▒        █▒
      │                    121       ▒         ▒         ▒         ▒         ▒      122▒      137▒        █▒        █▒
  100 ┤                    ▒         ▒         ▒         ▒         ▒      107▒        █▒        █▒        █▒        █▒
      │          65        ▒         ▒       61▒       76▒       91▒        █▒        █▒        █▒        █▒        █▒
   50 ┤        15▒       30▒       45▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒
      │         █▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒        █▒
      └──────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬─────────┬───~~~───┬─────►
              10,000    20,000    30,000    40,000    50,000    60,000    70,000    80,000    90,000    100,000  1,000,000 [items]
          
        █ - payload in [MB], calculation: payload = item count * (100 wrods * 15 character + 99 spaces) / 1024 / 1024
        ▒ - storage space required in [MB]
```

It should be noted that the actual execution times and memory consumption depend heavily on the platform 
on which WebExpress-WebIndex is operated. In addition, the vocabulary, the content of the documents and 
the number of IndexFields have a significant impact on the performance of the system. The measured values 
apply to the following conditions: The document contains a field with 100 words each consisting of 15 
characters. The words come from a vocabulary of 20,000 words. The series of measurements was created on 
the following system: 

- OS: Windows 11 64bit 
- System: Intel NUC-13 
- Processor: Intel i7-1360P 
- RAM: 64,0 GB (SODIMM 3200 MHz) RAM
- HDD: Samsung SSD 990 Pro 2TB

The `IndexManager` class offers a variety of functions for managing and optimizing indexed data. Here are some of the main 
methods and properties of this class:

- `IIndexContext Context`: The context of the index.
- `void Initialization(IIndexContext context)`: Initialization of the IndexManager.
- `void RegisterPipeState(IIndexPipeStage pipeStage)`: Registers a pipe state for processing the tokens.
- `void RegisterWqlFunction<TFunction>()`: Registers a wql function.
- `void ReIndex<TIndexItem>(IEnumerable<TIndexItem> items)`: Reindexing of the index.
- `Task ReIndexAsync<TIndexItem>(IEnumerable<TIndexItem> items, IProgress<int> progress, CancellationToken token)`: Performs an asynchronous reindexing of a collection of index items.
- `void Create<TIndexItem>(CultureInfo culture, IndexType type)`: Registers a data type in the index.
- `void Close<TIndexItem>()`: Closes the index file of type T.
- `Task CloseAsync<TIndexItem>()`: Asynchronously closes the index file of type T.
- `void Drop<TIndexItem>()`: Drops all index documents of type T.
- `Task DropAsync<TIndexItem>()`: Asynchronously drops all index documents of type T.
- `void Insert<TIndexItem>(TIndexItem item)`: Adds an item to the index.
- `Task InsertAsync<TIndexItem>(TIndexItem item)`: Performs an asynchronous addition of an item in the index.
- `void Update<TIndexItem>(TIndexItem item)`: Updates an item in the index.
- `Task UpdateAsync<TIndexItem>(TIndexItem item)`: Performs an asynchronous update of an item in the index.
- `void Delete<T>(TIndexItem item)`: Removes an item from the index.
- `uint Count<TIndexItem>()`: Counts the number of items of the index.
- `Task<int> CountAsync<TIndexItem>()`:  Performs an asynchronous determination of the number of elements.
- `Task DeleteAsync<TIndexItem>(TIndexItem item)`: Removes an item from the index asynchronously.
- `void Clear<TIndexItem>()`: Removed all data from the index.
- `Task ClearAsync<TIndexItem>()`: Removed all data from the index asynchronously.
- `IWqlStatement<TIndexItem> Retrieve<T>(string wql)`: Executes a WQL statement.
- `Task<IWqlStatement<TIndexItem>> RetrieveAsync<TIndexItem>(string wql)`: Executes a wql statement asynchronously.
- `IEnumerable<TIndexItem> All<TIndexItem>()`: Returns all documents from the index.
- `IIndexDocument<TIndexItem> GetIndexDocument<TIndexItem>()`: Returns an index type based on its type.
- `void Dispose()`: Disposes of the resources used by the current instance.

## IndexDocument
An `IndexDocument` representing a class that implements the `IIndexItem` interface. Each `IndexDocument` 
contains a collection of fields that hold the data to be indexed. These fields can contain various types 
of data, such as text, numbers, or dates. During the indexing process, the data in these fields are 
analyzed and tokenized, then stored in the reverse index. In addition to the reverse index, an `IndexDocument` 
also includes a document store for quick access to the existing instances. When a search query is made for 
one or more terms, the IDs of the instances that match the terms are identified in the reverse index and 
supplemented with the corresponding instances in the document store and returned to the searcher.

## IndexField
An `IndexField` is a property (C# property) in an index document that can accommodate various types 
of values, such as text, numbers, or other data. Each field is stored in a reverse index, which converts 
the field values into terms and associates them with document IDs and positional data in the reverse 
index. The name and type of the field are essential pieces of information used during indexing and searching. 
If a field is marked with the IndexIgnore attribute, it will be excluded from the indexing process.

```
                                                                                       ┌───────────────┐
                              ┌───────────────┐       ┌────────────────────┐           │ <<interface>> │
                              │ IndexDocument ├───────┤ IndexDocumentStore │           │ IIndexItem    │
                              └──────┬────────┘       └────────────────────┘           └───────────────┘
                                     │                                                         ▲
        ┌──────────────────┬─────────┴────────┬──────────────────┐                             ¦
        │ Property 1       │ Property 2       │ Property …       │ Property n           ┌─────────────┐
 ┌──────┴───────┐   ┌──────┴───────┐   ┌──────┴───────┐   ┌──────┴───────┐              │ MyIndexItem │
 │ IndexField 1 │   │ IndexField 2 │   │ IndexField … │   │ IndexField n │       <-->   ├─────────────┤
 └──────┬───────┘   └──────┬───────┘   └──────┬───────┘   └──────┬───────┘              │ Property 1  │
        │                  │                  │                  │                      │ Property 2  │
┌───────┴────────┐ ┌───────┴────────┐ ┌───────┴────────┐ ┌───────┴────────┐             │ Property …  │
│ IndexReverse 1 │ │ IndexReverse 2 │ │ IndexReverse … │ │ IndexReverse n │             │ Property n  │
└────────────────┘ └────────────────┘ └────────────────┘ └────────────────┘             ├─────────────┤
                                                                                        └─────────────┘
```
## IndexSchema
The index schema file contains important metadata, which provides detailed information about the 
structure and characteristics of the provide indexes. In addition, the file contains a precise object 
description of the document captured by the index and is managed. Furthermore, the JSON (JavaScript 
Object Notation) format is used for the index schema file and the have the extension `*.ws`.

## IndexStore
In a filesystem where the `WebIndex` is stored, a process is carried out where an inverted index 
is created for each field. These indexes are stored as files with the `<document name><field name>.wri` 
extension. In parallel, a special storage area known as the document store `<document name>.wds` is set 
up for each document. In this storage area, the document’s data is redundantly stored to enable quick 
access. The structure of these files follows a uniform format that is divided into various segments. Each 
of these segments is identifiable by a unique address and has a specific length. There is the option to 
specify whether a particular segment should be stored in the cache. If a segment is no longer needed, it 
can be removed from the main memory. These features contribute to efficient use of storage and improve 
the system performance.

```
         ╔Header═════════════╗
  3 Byte ║ "wds"|"wrt"|"wrn" ║ identification (magic number) of the file `wds` for IndexDocumentStore and `wrt` or 'wrn' for IndexReverse
  1 Byte ║ Version           ║ the file version
         ╠Allocator══════════╣ 
  8 Byte ║ NextFreeAddr      ║ the next free address
  8 Byte ║ FreeListAddr      ║ the address to the free list
         ╠Statistic══════════╣
  4 Byte ║ Count             ║ the number of elements in the file
         ╠Body═══════════════╣
         ║                   ║ a variable memory area in which the data is stored
         ╚~~~~~~~~~~~~~~~~~~~╝
```

Unused memory areas in the file are represented by the `Free` segment, which is located in the body area 
variable and forms a linked list. The `Allocator` points to the first element of this list.

```
         ╔Free═══════════════╗
  8 Byte ║ SuccessorAddr     ║ pointer to the address of the next element of a sorted list or 0 if there is no element
         ╚═══════════════════╝
```

When new documents are indexed, the new segments are accommodated in a free storage area in the file. Initially, 
this is the end of the file. In fragmented files, where segments have already been deleted, the freed storage 
areas are reused. The `Allocator` in the header always points to the next free storage space with `NextFreeAddr`.

### Example alloc
In this example, segments 2 and 3 are successively added. It is important to note that segment 1 already exists.

```
         initial                    add segment 2               add segment 3

 0 ╔Header═════════════╗        ╔Header═════════════╗        ╔Header═════════════╗
   ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║
 3 ╠Allocator══════════╣        ╠Allocator══════════╣        ╠Allocator══════════╣
   ║ 2                 ║        ║ 3                 ║        ║ 4                 ║
   ║ 0                 ║        ║ 0                 ║        ║ 0                 ║
19 ╠Statistic══════════╣        ╠Statistic══════════╣        ╠Statistic══════════╣
   ║ 1                 ║        ║ 2                 ║        ║ 3                 ║
23 ╠Body═══════════════╣        ╠Body═══════════════╣        ╠Body═══════════════╣
   ║     ┌Seg:  1┐     ║    =>  ║     ┌Seg:  1┐     ║    =>  ║     ┌Seg:  1┐     ║
   ║     │       │     ║        ║     │       │     ║        ║     │       │     ║
   ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║
   ╚═══════════════════╝        ║     ┌Seg:  2┐     ║        ║     ┌Seg:  2┐     ║
                                ║     │   +   │     ║        ║     │       │     ║
                                ║     └───────┘     ║        ║     └───────┘     ║
                                ╚═══════════════════╝        ║     ┌Seg:  3┐     ║
                                                             ║     │   +   │     ║
                                                             ║     └───────┘     ║
                                                             ╚═══════════════════╝
```

Free spaces are stored in a linked list that represents the free segments in the file. These can be reused 
to store new data. Unused Segments are replaced by the `free` segment.

### Example Free
In this example, segments 2, 1 and 4 are sequentially released and consolidated.

```
         initial                  remove segment 2             remove segment 1             remove segment 4

 0 ╔Header═════════════╗        ╔Header═════════════╗        ╔Header═════════════╗        ╔Header═════════════╗
   ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║
 3 ╠Allocator══════════╣        ╠Allocator══════════╣        ╠Allocator══════════╣        ╠Allocator══════════╣
   ║ 5                 ║        ║ 5                 ║        ║ 5                 ║        ║ 5                 ║
   ║ 0                 ║        ║ 2                 ║─┐      ║ 1                 ║─┐      ║ 4                 ║───┐
19 ╠Statistic══════════╣        ╠Statistic══════════╣ │      ╠Statistic══════════╣ │      ╠Statistic══════════╣   │
   ║ 4                 ║        ║ 3                 ║ │      ║ 2                 ║ │      ║ 1                 ║   │
23 ╠Body═══════════════╣        ╠Body═══════════════╣ │      ╠Body═══════════════╣ │      ╠Body═══════════════╣   │
   ║     ┌Seg:  1┐     ║    =>  ║     ┌Seg:  1┐     ║ │  =>  ║     ┌Free: 1┐     ║◄┘  =>  ║     ┌Free: 1┐     ║◄─┐│
   ║     │       │     ║        ║     │       │     ║ │      ║     │   X   │     ║─┐      ║     │       │     ║─┐││
   ║     └───────┘     ║        ║     └───────┘     ║ │      ║     └───────┘     ║ │      ║     └───────┘     ║ │││
   ║     ┌Seg:  2┐     ║        ║     ┌Free: 2┐     ║◄┘      ║     ┌Free: 2┐     ║◄┘      ║     ┌Free: 2┐     ║◄┘││
   ║     │       │     ║        ║     │   X   │     ║        ║     │       │     ║        ║     │       │     ║  ││
   ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║  ││
   ║     ┌Seg:  3┐     ║        ║     ┌Seg:  3┐     ║        ║     ┌Seg:  3┐     ║        ║     ┌Seg:  3┐     ║  ││
   ║     │       │     ║        ║     │       │     ║        ║     │       │     ║        ║     │       │     ║  ││
   ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║  ││
   ║     ┌Seg:  4┐     ║        ║     ┌Seg:  4┐     ║        ║     ┌Seg:  4┐     ║        ║     ┌Free: 4┐     ║◄─┼┘
   ║     │       │     ║        ║     │       │     ║        ║     │       │     ║        ║     │   X   │     ║──┘
   ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║        ║     └───────┘     ║
   ╚═══════════════════╝        ╚═══════════════════╝        ╚═══════════════════╝        ╚═══════════════════╝
```

The repurposing of unused segments reduces the space requirements of files, particularly for highly fluctuating 
index files. This practice not only optimizes storage but also enhances the overall performance and efficiency 
of data management systems. It is especially beneficial in environments where data is frequently updated or 
deleted, leading to a high turnover of index files. By reusing these segments, the system can maintain optimal 
performance while minimizing the need for additional storage space.

### Example realloc
This example reallocates a segment using the available free space. Since the size of the segments is fixed, 
it is irrelevant which free one Segment is reused. For efficiency reasons, the first element from the list 
of free segments is always used.

```
         initial                  realloc segment 4

 0 ╔Header═════════════╗        ╔Header═════════════╗
   ║ "wds"|"wrt"|"wrn" ║        ║ "wds"|"wrt"|"wrn" ║
 3 ╠Allocator══════════╣        ╠Allocator══════════╣
   ║ 5                 ║        ║ 5                 ║
   ║ 4                 ║───┐    ║ 1                 ║─┐
19 ╠Statistic══════════╣   │    ╠Statistic══════════╣ │
   ║ 1                 ║   │    ║ 2                 ║ │
23 ╠Body═══════════════╣   │    ╠Body═══════════════╣ │
   ║     ┌Free: 1┐     ║◄─┐│=>  ║     ┌Seg: 1─┐     ║◄┘
   ║     │       │     ║─┐││    ║     │       │     ║─┐
   ║     └───────┘     ║ │││    ║     └───────┘     ║ │
   ║     ┌Free: 2┐     ║◄┘││    ║     ┌Free: 2┐     ║◄┘
   ║     │       │     ║  ││    ║     │       │     ║
   ║     └───────┘     ║  ││    ║     └───────┘     ║
   ║     ┌Seg:  3┐     ║  ││    ║     ┌Seg:  3┐     ║
   ║     │       │     ║  ││    ║     │       │     ║
   ║     └───────┘     ║  ││    ║     └───────┘     ║
   ║     ┌Free: 4┐     ║◄─┼┘    ║     ┌Seg:  4┐     ║
   ║     │       │     ║──┘     ║     │   x   │     ║
   ║     └───────┘     ║        ║     └───────┘     ║
   ╚═══════════════════╝        ╚═══════════════════╝
```

### Caching
Caching is an efficient technique for optimizing data access by enabling fast access to frequently used 
data and simultaneously reducing the load on the file system. It stores frequently used data in memory, 
which speeds up access to this data as it does not have to be retrieved from the hard drive again. For 
write accesses, the data is first written to the read cache. The read cache uses a hash map to allow 
random access to the cached segments. Each cached segment has a defined lifetime. If this has expired, 
the segments are removed from the read cache, unless they have been marked as immortal via the `SegmentCached` 
attribute. The maximum size of the read cache can be determined using the `IndexStorageReadBuffer.MaxCachedSegments` 
parameter. The size influences all `IndexStore` files and can be changed during operation. However, it should 
be noted that if the size of memory already allocated will not be released.


## IndexDocumentStore
A `IndexDocumentStore` is a data structure in which each key is associated with a value. This allows 
efficient retrieval and retrieval of data based on the key. The document store plays a crucial role in 
improving the efficiency of queries by enabling direct access to the document instances that contain the 
desired terms. The internal structure of the document store:

```
         ╔Header═════════════╗
  3 Byte ║ "wds"             ║ identification (magic number) of the file
  1 Byte ║ Version           ║ the file version
         ╠Allocator══════════╣ 
  8 Byte ║ NextFreeAddr      ║ the next free address
  8 Byte ║ FreeItemAddr      ║ the address to the list with free item node segments
  8 Byte ║ FreeChunkAddr     ║ the address to the list with free chunk node segments
         ╠Statistic══════════╣
  4 Byte ║ Count             ║ the number of terms in the file
         ╠Body═══════════════╣
         ║ HashMap           ║ a hash map in which the data is stored
         ╚~~~~~~~~~~~~~~~~~~~╝
```

Access to the document instances is done via a HashMap, where the document id serves as the key. 

```
         ╔HashMap════════════╗
  4 Byte ║ BucketCount       ║ number of buckets (the next prime number of capacity)
         ╠Buckets════════════╣
         ║ Bucket 0          ║ slot in which items with the same hash value are stored
  n *    ║ Bucket 1          ║
  8 Byte ║~~~~~~~~~~~~~~~~~~~║
         ║~~~~~~~~~~~~~~~~~~~║
         ║ Bucket n-1        ║
         ╠Data═══════════════╣
         ║                   ║ a variable memory area in which the data is stored
         ╚~~~~~~~~~~~~~~~~~~~╝
```

Each bucket contains a pointer to blocks of a list, with the elements of the bucket.

```
         ╔Bucket═════════════╗
  8 Byte ║ ItemAddr          ║ pointer to the address of the first element of a sorted list that has the same hash values (collisions) or 0 if there is no element
         ╚═══════════════════╝
```

The document instances are stored in one or more segments. The size of the segment is fixed and the number of 
segments per record is determined by the size of the compressed document instance. The segment is stored in 
the variable storage area.

```
         ╔Item═══════════════╗
 16 Byte ║ Id                ║ guid of the document item
  4 Byte ║ Length            ║ size of the DataChunk in bytes
256 Byte ║ DataChunk         ║ a memory area in which a part of the element is stored (gzip compressed)
  8 Byte ║ NextChunkAddr     ║ pointer to the address of the next chunk element of a list or 0 if there is no element
  8 Byte ║ SuccessorAddr     ║ pointer to the address of the next bucket element of a sorted list or 0 if there is no element
         ╚═══════════════════╝
```

If the data is larger than what can fit in a chunk, additional chunks are created and the data is divided among 
them. The last chunk may not be completely filled. All chunks are linked in an ordered list.

```
         ╔Chunk══════════════╗
  4 Byte ║ Length            ║ size of the DataChunk in bytes
256 Byte ║ DataChunk         ║ a memory area in which a part of the element is stored (gzip compressed)
  8 Byte ║ NextChunkAddr     ║ pointer to the address of the next chunk element of a list or 0 if there is no element
         ╚═══════════════════╝
```

**Add**: The add function in the `IndexDocumentStore` is designed to permanently store the entire document. To 
efficiently utilize storage space and minimize storage requirements, the object is serialized into a JSON format 
and subsequently compressed. This process ensures efficient use of storage space without compromising the integrity 
or accessibility of the data. Furthermore, this method allows for faster data transmission and enhances the overall 
performance of the `IndexDocumentStore`. Access to the original data can be obtained at any time through 
decompression and deserialization.

```
┌──────────────────────────────┐
│ start                        │
│ ┌────────────────────────────┤
│ │ if !contains(id)           │ look up document id in hash map
│ │ ┌──────────────────────────┤
│ │ │ gzip(data)               │ gzip the data
│ │ │ add item                 │ adding an items segment
│ │ └──────────────────────────┤
│ │ else                       │
│ │ ┌──────────────────────────┤
│ │ │ throw ArgumentException  │
│ │ └──────────────────────────┤
│ │ end if                     │
│ └────────────────────────────┤
│ end                          │
└──────────────────────────────┘
```

**Update**: The update process consists of a combination of delete and add operations. If the data is of 
the same size, the existing item segment is reused. Otherwise, a new item segment is created and used. This 
approach optimizes storage usage and enhances system efficiency by avoiding unnecessary storage allocations.

```
┌──────────────────────────────┐
│ start                        │
│ ┌────────────────────────────┤
│ │ if contains(id)            │ look up document id in hash map
│ │ ┌──────────────────────────┤
│ │ │ delete                   │ delete item
│ │ │ gzip(data)               │ gzip the data
│ │ │ delete item              │ remove the existing item segment
│ │ │ add item                 │ adding the updated item segment
│ │ └──────────────────────────┤
│ │ else                       │
│ │ ┌──────────────────────────┤
│ │ │ add                      │ add item
│ │ └──────────────────────────┤
│ │ end if                     │
│ └────────────────────────────┤
│ end                          │
└──────────────────────────────┘
```

**Remove**: Documents that are no longer needed can be securely removed from the document storage by using 
the delete function. This ensures efficient use of storage and keeps the document storage tidy and 
well-organized.

```
┌──────────────────────────────┐
│ start                        │
│ ┌────────────────────────────┤
│ │ if !contains(id)           │ look up document id in hash map
│ │ ┌──────────────────────────┤
│ │ │ delete item              │ remove the existing item segment
│ │ └──────────────────────────┤
│ │ else                       │
│ │ ┌──────────────────────────┤
│ │ │ throw ArgumentException  │
│ │ └──────────────────────────┤
│ │ end if                     │
│ └────────────────────────────┤
│ end                          │
└──────────────────────────────┘
```

### IndexReverse term
A reverse index is a specialized type of index that allows access to data in reverse order. In the context of 
the `WebIndex`, the reverse index is used for efficient searching of terms. These terms are derived from the 
associated fields, and their values are broken down into tokens, normalized, filtered, and stored in a search 
tree for fast retrieval.

Unlike the general definition of the header, `IndexReverse` has a modified allocator. A separate list is kept 
for each segment type in which the free segments are saved. For faster storage and reuse, insertion and removal 
only ever occurs at the beginning of the list. When there is a new memory request, a free segment can be reused 
without any search effort. Merging segments is not necessary.

```
         ╔Header═════════════╗
  3 Byte ║ "wrt"             ║ identification (magic number) of the file
  1 Byte ║ Version           ║ the file version
         ╠Allocator══════════╣ 
  8 Byte ║ NextFreeAddr      ║ the next free address
  8 Byte ║ FreeTermAddr      ║ the address to the list with free term node segments
  8 Byte ║ FreePostingAddr   ║ the address to the list with free posting node segments
  8 Byte ║ FreePositionAddr  ║ the address to the list with free position segments
         ╠Statistic══════════╣
  4 Byte ║ Count             ║ the number of terms in the file
         ╠Body═══════════════╣
         ║ Term              ║ the root node
         ╚~~~~~~~~~~~~~~~~~~~╝
```

The term structure contains the root node, which in turn manages the term nodes.

```
         ╔Term═══════════════╗
 30 Byte ║ TermNode          ║ the root node
         ╠Data═══════════════╣
         ║                   ║ a variable memory area in which the data is stored
         ╚~~~~~~~~~~~~~~~~~~~╝
```

The tree structure enables efficient search and retrieval of terms. Each node in the tree represents a character 
of the term, and the sequence of characters along the path from the root node to a specific node forms the corresponding 
term. The `TermNode` segments in the data area of the reverse index is organized in such a way that they enable a fast 
and accurate search. A term segment contains important metadata. This includes the frequency of the term’s occurrence and 
a reference to a linked list. This list contains the documents in which the term appears.

```
         ╔TermNode═══════════╗
  2 Byte ║ Character         ║ a character from the term
  8 Byte ║ SiblingAddr       ║ address of the first sibling node or 0 if no sibling node exists
  8 Byte ║ ChildAddr         ║ address of the first child node or 0 if not present
  4 Byte ║ Fequency          ║ the number of times the term is used 
  8 Byte ║ PostingAddr       ║ adress of the first posting node of a binary tree or 0 if there is no element exists
         ╚═══════════════════╝
```

The posting node segment is designed as a binary tree and contains the ids of the documents that belong to a term. For 
each document, the posting node segment refers to the position information that indicates where the term is located in 
the document. The posting segment is stored in the variable memory area of the inverted index.

```
         ╔TermPostingNode════╗
 16 Byte ║ Id                ║ guid of the document item
  8 Byte ║ LeftAddr          ║ pointer to the address of the left child or 0 if there is no element exists
  8 Byte ║ RightAddr         ║ pointer to the address of the right child or 0 if there is no element exists
  8 Byte ║ PositionAddr      ║ adress of the first position element of a sorted list or 0 if there is no element exists
         ╚═══════════════════╝
```

The position segments form a linked list containing the position information of the associated terms. The position of a term 
refers to its original occurrence in the field value of a document. Each position segment has a fixed size and is created in 
the variable data area of the reverse index. This structure allows for efficient searching and retrieval of terms based on their 
position in the documents.

```
         ╔Position═══════════╗
  4 Byte ║ Position          ║ the position
  8 Byte ║ SuccessorAddr     ║ pointer to the address of the next element of a sorted list or 0 if there is no element exists
         ╚═══════════════════╝
```

**Add**: The procedure for adding terms from an 'IndexField' and saving references to the document with its position within 
the document is as follows:

```
┌──────────────────────────────┐
│ start                        │
│ ┌────────────────────────────┤
│ │ loop over terms            │ retrieve all terms from the new IndexField
│ │ ┌──────────────────────────┤
│ │ │ tn := gettermnode(term)  │
│ │ │ if tn != null            │
│ │ │ ┌────────────────────────┤
│ │ │ │ p := getposting(id)    │
│ │ │ │ if p != null           │
│ │ │ │ ┌──────────────────────┤
│ │ │ │ │ add position         │ add position associated with the existing posting
│ │ │ │ └──────────────────────┤
│ │ │ │ else                   │
│ │ │ │ ┌──────────────────────┤
│ │ │ │ │ add posting          │ add posting with the document id
│ │ │ │ │ ┌────────────────────┤
│ │ │ │ │ │ add position       │ add position associated with the new posting
│ │ │ │ └─┴────────────────────┤
│ │ │ │ end if                 │
│ │ │ └────────────────────────┤
│ │ │ else                     │
│ │ │ ┌────────────────────────┤
│ │ │ │ add term node          │ add new term node
│ │ │ │ ┌──────────────────────┤
│ │ │ │ │ add posting          │ add posting with the document id
│ │ │ │ │ ┌────────────────────┤
│ │ │ │ │ │ add position       │ add position associated with the new posting
│ │ │ └─┴─┴────────────────────┤
│ │ │ end if                   │
│ │ └──────────────────────────┤
│ │ end loop                   │
│ └────────────────────────────┤
│ end                          │
└──────────────────────────────┘
```

**Update**: Updating an `IndexField` in a document is done by determining the difference between the saved and changed 
terms. All postings (including positions) will be deleted if they no longer exist in the changed `IndexField`. At the 
same time, new postings (including positions) are created for terms that were not included in the original `IndexField`.

```
┌──────────────────────────────┐
│ current                      │
│                              │
│ to delete =        ┌─────────┼─────────────────┐
│ current\changed    │         │         changed │
└────────────────────┼─────────┘                 │
                     │                  to add = │
                     │           changed\current │
                     └───────────────────────────┘
```

This results in the following process:

```
┌──────────────────────────────────┐
│ start                            │
│ ┌────────────────────────────────┤
│ │ deleteTerms := current\changed │
│ │ addTerms := changed\current    │
│ │ ┌──────────────────────────────┤
│ │ │ loop over deleteTerms        │
│ │ │ ┌────────────────────────────┤
│ │ │ │ delete posting             │ remove all postings with the document id
│ │ │ │ ┌──────────────────────────┤
│ │ │ │ │ delete position          │ remove all positions associated with the deleted postings
│ │ │ └─┴──────────────────────────┤
│ │ │ end loop                     │
│ │ └──────────────────────────────┤
│ │ ┌──────────────────────────────┤
│ │ │ loop over addTerms           │
│ │ │ ┌────────────────────────────┤
│ │ │ │ add posting                │ add posting with the document id
│ │ │ │ ┌──────────────────────────┤
│ │ │ │ │ add position             │ add position associated with the posting
│ │ │ └─┴──────────────────────────┤
│ │ │ end loop                     │
│ └─┴──────────────────────────────┤
│ end                              │
└──────────────────────────────────┘
```

**Remove**: The removal of an IndexField from a reverse index is carried out according to the following procedure:

```
┌──────────────────────────────┐
│ start                        │
│ ┌────────────────────────────┤
│ │ loop over terms            │ retrieve all terms from the stored IndexField
│ │ ┌──────────────────────────┤
│ │ │ loop over termnode(term) │ retrieve all TermNodes that correspond to the term
│ │ │ ┌────────────────────────┤
│ │ │ │ delete posting         │ remove all postings with the document id
│ │ │ │ ┌──────────────────────┤
│ │ │ │ │ delete position      │ remove all positions associated with the deleted postings
│ │ │ └─┴──────────────────────┤
│ │ │ end loop                 │
│ │ └──────────────────────────┤
│ │ end loop                   │
│ └────────────────────────────┤
│ end                          │
└──────────────────────────────┘
```

### IndexReverse numeric
Unlike reverse indices based on terms, the reverse index based on numerical values offers significant advantages when 
indexing numerical data. The management structures for numerical reverse indices differ substantially: Instead of a term 
tree, a balanced binary tree is used, in which each node contains the complete numerical value. This structure enables 
more efficient management and faster search for numerical values. Balanced binary trees ensure that the tree height remains 
logarithmic, allowing for quick search, insert, and delete operations. This method offers clear performance advantages in 
processing and managing numerical data compared to term-based trees.

```
         ╔Header═════════════╗
  3 Byte ║ "wrn"             ║ identification (magic number) of the file
  1 Byte ║ Version           ║ the file version
         ╠Allocator══════════╣ 
  8 Byte ║ NextFreeAddr      ║ the next free address
  8 Byte ║ FreeTermAddr      ║ the address to the list with free term node segments
  8 Byte ║ FreePostingAddr   ║ the address to the list with free posting node segments
  8 Byte ║ FreePositionAddr  ║ the address to the list with free position segments
         ╠Statistic══════════╣
  4 Byte ║ Count             ║ the number of terms in the file
         ╠Body═══════════════╣
         ║ Numeric           ║ the root node
         ╚~~~~~~~~~~~~~~~~~~~╝
```

The numeric structure contains the root node, which in turn manages the numeric nodes.

```
         ╔Numeric════════════╗
 44 Byte ║ NumericNode       ║ the root node
         ╠Data═══════════════╣
         ║                   ║ a variable memory area in which the data is stored
         ╚~~~~~~~~~~~~~~~~~~~╝
```

The tree structure enables efficient search and retrieval of numeric values by using a balanced binary tree to store the 
values. Each node has a pointer to a posting tree where the document ids of the associated documents are stored.

```
         ╔NumericNode════════╗
 16 Byte ║ Value             ║ the numeric value of the node
  8 Byte ║ LeftAddr          ║ address of the left child node or 0 if no left child node exists
  8 Byte ║ RightdAddr        ║ address of the right child node or 0 if not right child node exists
  4 Byte ║ Fequency          ║ the number of times the numeric value is used
  8 Byte ║ NumPostingNode    ║ Address of the first posting node of a binary tree or 0 if no element exists
         ╚═══════════════════╝
```

The posting node segment is designed as a binary tree and contains the ids of the documents that belong to a term. For 
each document, the posting node segment refers to the position information that indicates where the term is located in 
the document. The posting segment is stored in the variable memory area of the inverted index.

```
         ╔NumericPostingNode═╗
 16 Byte ║ Id                ║ guid of the document item
  8 Byte ║ LeftAddr          ║ pointer to the address of the left child or 0 if there is no element
  8 Byte ║ RightAddr         ║ pointer to the address of the right child or 0 if there is no element
         ╚═══════════════════╝
```

## Indexing
Indexing is a crucial process that enables quick information retrieval. The index is created from the values of
the document fields. This index is stored on the file system and is updated whenever a document value is added or 
changed. Sometimes it is necessary to manually regenerate the index, for example, when a new document field is added 
or when the index is lost or damaged. The reindexing deletes all indexes and recreates them.

```csharp
public class Greetings : IIndexItem
  [IndexIgnore]
  public Guid Id { get; set; }
  
  public string Text { get; set; }
}
 
// somewhere in the code...
IndexManager.Create<Greetings>(CultureInfo.GetCultureInfo("en"), IndexType.Storage);

var greetings = new []
{
    new Greetings { Id = new Guid("b2e8a5c3-1f6d-4e7b-9e1f-8c1a9d0f2b4a"), Text = "Hello Helena!"},
    new Greetings { Id = new Guid("c7d8f9e0-3a2b-4c5d-8e6f-9a1b0c2d4e5f"), Text = "Hello Helena and Helge!"}
};

IndexManager.ReIndex(greetings);
```

From the data of the example, the following term tree results:

```
┌Term: 23┐
│ null   │ root
│ 0      │ 
│ 53     │────►┌Term: 53┐
│ 0      │     │ 'h'    │ first letter from helge and helena
│ 0      │     │ 0      │
└────────┘     │ 83     │────►┌Term: 83┐
               │ 0      │     │ 'e'    │ second letter from helge and helena
               │ 0      │     │ 0      │
               └────────┘     │ 113    │────►┌Term:113┐
                              │ 0      │     │ 'l'    │ third letter from helge and helena
                              │ 0      │     │ 0      │
                              └────────┘     │ 321    │────►┌Term:143┐
                                             │ 0      │     │ 'e'    │ fourth letter from helena
                                             │ 0      │  ┌──│ 321    │
                                             └────────┘  │  │ 173    │────►┌Term:173┐
                                                         │  │ 0      │     │ 'n'    │ fifth letter from helena
                                                         │  │ 0      │     │ 0      │
                                       ┌Term:321┐◄───────┘  └────────┘     │ 203    │────►┌Term:203┐
                                       │ 'g'    │ fourth letter from helge │ 0      │     │ 'a'    │ sixth letter from helena
                                       │ 0      │                          │ 0      │     │ 0      │
                                       │ 351    │────►┌Term:351┐           └────────┘     │ 0      │
                                       │ 0      │     │ 'e'    │ fifth letter from helge  │ 2      │
                                       │ 0      │     │ 0      │                        ┌─│ 233    │
                                       └────────┘     │ 0      │                        │ └────────┘
                                                      │ 1      │                        │
                                                    ┌─│ 381    │                        │
                                                    │ └────────┘                        ▼
                                                    ▼                                  ┌Post:233┐
                                                   ┌Post:381┐                          │ 'b2..' │
                                                   │ 'c7..' │                          │ 0      │
                                                   │ 0      │                          │ 277    │────►┌Post:277┐
                                                   │ 0      │           ┌Pos: 265┐◄────│ 265    │     │ 'c7..' │
                                    ┌Pos: 413┐◄────│ 413    │           │ 1      │     └────────┘     │ 0      │
                                    │ 3      │     └────────┘           │ 0      │                    │ 0      │
                                    │ 0      │                          └────────┘                    │ 309    │────►┌Pos: 309┐
                                    └────────┘                                                        └────────┘     │ 1      │
                                                                                                                     │ 0      │
                                                                                                                     └────────┘
```

# WQL
The WebExpress Query Language (WQL) is a query language that filters and sorts of a given 
amount of data from the reverse index. A statement of the query language is usually sent 
from the client to the server, which collects, filters and sorts the data in the reverse 
index and sends it back to the client. The following BNF is used to illustrate the grammar:

```
<WQL>                      ::= <Filter> <Order> <Partitioning> | ε
<Filter>                   ::= "(" <Filter> ")" | <Filter> <LogicalOperator> <Filter> |<Condition> | ε
<Condition>                ::= <Attribute> <BinaryOperator> <Parameter> <ParameterOptions> | <Attribute> <SetOperator> "(" <Parameter> <ParameterNext> ")"
<LogicalOperator>          ::= "and" | "or" | "&" | "||"
<Attribute>                ::= <Name> | <Name> "." <Name>
<Parameter>                ::= <Function> | <DoubleValue> | """ <StringValue> """ | "'" <StringValue> "'"  | <StringValue>
<ParameterOptions>         ::= <ParameterFuzzyOptions> | <ParameterDistanceOptions> | <ParameterFuzzyOptions> <ParameterDistanceOptions> | <ParameterDistanceOptions> <ParameterFuzzyOptions> | ε
<ParameterFuzzyOptions>    ::= "~" <Number>
<ParameterDistanceOptions> ::= ":" <Number>
<Function>                 ::= <Name> "(" <Parameter> <ParameterNext> ")" | Name "(" ")"
<ParameterNext>            ::= "," <Parameter> <ParameterNext> | ε
<BinaryOperator>           ::= "=" | ">" | "<" | ">=" | "<=" | "!=" | "~" | "is" | "is not"
<SetOperator>              ::= "in" | "not in"
<Order>                    ::= "order" "by" <Attribute> <DescendingOrder> <OrderNext> | ε
<OrderNext>                ::= "," <Attribute> <DescendingOrder> <OrderNext> | ε
<DescendingOrder>          ::= "asc" | "desc" | ε
<Partitioning>             ::= <Partitioning> <Partitioning> | <PartitioningOperator> <Number> | ε
<PartitioningOperator>     ::= "take" | "skip"
<Name>                     ::= [A-Za-z_][A-Za-z0-9_]+
<StringValue>              ::= [A-Za-z0-9_@<>=~$%/!+.,;:\-]+
<DoubleValue>              ::= [+-]?[0-9]*[.]?[0-9]+
<Number>                   ::= [0-9]+
```

## Term modifiers
Term modifiers in WQL are special characters or combinations of characters that serve to 
modify search terms, thus offering a wide range of search possibilities. The use of term 
modifiers can contribute to improving the efficiency and accuracy of the search. They can 
be used to find exact matches for phrases, to search for terms that match a certain pattern, 
to search for terms that are similar to a certain value, and to search for terms that are 
near another term. Term modifiers are an essential part of WQL and contribute to increasing 
the power and flexibility of the search. They allow users to create customized search queries 
tailored to their specific requirements. It is important to note that all queries are case-
insensitive. This means that the case is not considered in the search, which simplifies the 
search and improves user-friendliness.

**Phrase search (exact word sequence)**

Phrase search allows users to retrieve content from documents that contain a specific order 
and combination of words defined by the user. With phrase search, only records that contain 
the expression in exactly the searched order are returned. For this, the position information 
of the reverse index is used.

```wql
Description = 'lorem ipsum'
```

**Proximity search**

A proximity search looks for documents where two or more separately matching terms occur within a 
certain distance of each other. The distance is determined by the number of intervening words. Proximity 
search goes beyond simple word matching by adding the constraint of proximity. By limiting proximity, 
search results can be avoided where the words are scattered and do not cohere. The basic linguistic 
assumption of proximity search is that the proximity of words in a document implies a relationship 
between the words.

```wql
Description ~ 'lorem ipsum' :2
```

**Wildcard search**

A wildcard search is an advanced search technique used to maximize search results. Wildcards are used in search terms to represent 
one or more other characters.

- An asterisk `*` can be used to specify any number of characters.
- A question mark `?` can be used to represent a single character anywhere in the word. It is 
most useful when there is variable spellings for a word and you want to search all variants 
at once.

```wql
Description ~ '?orem'
Description ~ 'ips*'
```

**Fuzzy search**
Fuzzy search is used to find matches in texts that are not exact, but only approximate.

```wql
Description ~ 'house' ~80
```

**Word search**

Word search is the search for specific terms in a document, regardless of their capitalization 
or position. This concept is particularly useful when searching for specific terms in a document 
without having to pay attention to their exact spelling or occurrence in the document. It enables 
efficient searches for specific terms.

```wql
Description ~ 'lorem ipsum'
```

## WQL functions
The WebExpress Query Language (WQL) offers a set of functions that allow data to be processed and retrieved 
in versatile and specific ways. These functions can be integrated into queries to achieve more precise and 
targeted search results. Below are some common functions and their descriptions:

| Function | Params | Description
|----------|--------|------------------------------------------------
| day()    | n      | Returns n days before or after the current day.
| now()    | -      | Returns the current date and time.

Functions are only allowed on the right-hand side of conditions. This means that functions always appear as 
part of the parameters in conditions and not as standalone left-hand operands. Here are some examples to 
illustrate this:

```wql
dateField = day(-3)
```

This query filters all records where the dateField has the value that is 3 days before the current day.

Custom functions can be created by implementing the `IWqlExpressionNodeFilterFunction<TIndexItem>` interface and 
registering it in the `IndexManager`.

