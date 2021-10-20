## FAQ

* **Does Apply support String interpolation?** Not yet; rather than using string interpolation, you'll need to use `string.Format`
* **Do bitwise operations work for apply?** No - the Bitwise XOR operator `^` indicates an exponential relationship between the operands
* **When the Aggregation materializes, there's nothing in the `RecordShell` object. What gives?** The `RecordShell` item is used to preserve the original index through the aggregation pipeline and should only be used for operations within the pipeline. It will never materialize when the pipeline is enumerated
* **Why Do some Reductive aggregations condense down to a single number while others condense down to an IEnumerable?** When you build your pipeline, if you have a reductive aggregation not associated with a group, the aggregation is run immediately. The result of that reduction is furnished to you immediately for use.