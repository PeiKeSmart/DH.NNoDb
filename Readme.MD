NoDb数据库，基于MMF实现的NoSql，百万级读写性能。  

主要用途：  
+ **kv存储** key是String，value是Byte[]，kv均支持变长  
+ **list存储** 存储顺序数据，一般配合kv作为大型kv的时序key存储，例如每秒有哪些key  

项目进展：  
+ MemoryFile封装完成  
+ Heap堆分配完成  
+ ListDb列表数据库开发完成，测试中  
+ Database开发中  