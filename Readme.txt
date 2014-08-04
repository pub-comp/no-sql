Prior to running automated tests, make sure you have MongoDB and Redis running.


Known limitations:

* MongoDB - Bitwise queries don't work in-DB,
run other filters first then bring results to memory to do this filtering.

* MongoDB - GroupBy doesn't work in-DB, bring results to memory then group them.

* Redis - inheritance doesn't work if base type isn't abstract.

* Redis - adaptor currently does not support indexes (could not find an API for them).

* Redis - transactions not implemented in adaptor due to fact that using ServiceStack.Redis,
you can create a non-typed transaction and cast it to a typed transaction and therefore,
if transactions are supported and enable retrieval of entities, the transactions have to be limited
to the scope of a single entity set and not to the entire context.
Plus, you can not create multiple transactions simultaneously on the same context as a workaround.

* Redis - no file system implementation.

* MongoDB - single file system per context.


Other points of comparison between DBs:

* MongoDB - update system seems more powerful (option to only update named properties)
* Redis - password only security
* MongoDB - supports users with various authentication
