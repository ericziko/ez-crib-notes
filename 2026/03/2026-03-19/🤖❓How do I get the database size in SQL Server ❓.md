---
uid: dd5199bc-82be-4326-8728-a91594360b10
tags:
  - 3
title: 🤖❓How do I get the database size in SQL Server ❓
created: 2026-03-19T15:41:45
modified: 2026-03-19T15:46:41
---

# 🤖❓How do I get the database size in SQL Server ❓

There are a few solid ways to get **database size in SQL Server**, depending on how detailed you want to be.

---

# 🔹 1. Quick Overview (All Databases)

This is the fastest way to see sizes across your server:

```sql
EXEC sp_helpdb;
```

👉 Shows:

- Database size (combined data + log)
- File locations

---

# 🔹 2. Clean, Queryable Version (Recommended)

If you want something scriptable and precise:

```sql
SELECT  
    d.name AS DatabaseName,  
    SUM(mf.size) * 8 / 1024 AS SizeMB  
FROM sys.master_files mf  
JOIN sys.databases d ON d.database_id = mf.database_id  
GROUP BY d.name  
ORDER BY SizeMB DESC;
```

👉 Notes:

- `size` is in **8 KB pages**
- `* 8 / 1024` converts to **MB**

---

# 🔹 3. Separate Data vs Log Size

More useful in real-world scenarios:

```sql
SELECT  
    d.name AS DatabaseName,  
    SUM(CASE WHEN mf.type_desc = 'ROWS' THEN mf.size END) * 8 / 1024 AS DataSizeMB,  
    SUM(CASE WHEN mf.type_desc = 'LOG' THEN mf.size END) * 8 / 1024 AS LogSizeMB  
FROM sys.master_files mf  
JOIN sys.databases d ON d.database_id = mf.database_id  
GROUP BY d.name  
ORDER BY DataSizeMB DESC;
```

---

# 🔹 4. Inside a Single Database (Detailed Breakdown)

```sql
USE YourDatabaseName;  
  
SELECT  
    name AS FileName,  
    type_desc,  
    size * 8 / 1024 AS SizeMB  
FROM sys.database_files;
```

---

# 🔹 5. Actual Used vs Allocated Space (Important)

Allocated size ≠ used size. This shows **real usage**:

```sql
USE YourDatabaseName;  
EXEC sp_spaceused;
```

👉 Returns:

- Database size
- Unallocated space

---

# 🔹 6. Table-Level Breakdown (Where is the space going?)

```sql
USE YourDatabaseName;  
  
SELECT  
    t.name AS TableName,  
    SUM(p.rows) AS RowCounts,  
    SUM(a.total_pages) * 8 / 1024 AS TotalSpaceMB,  
    SUM(a.used_pages) * 8 / 1024 AS UsedSpaceMB,  
    SUM(a.data_pages) * 8 / 1024 AS DataSpaceMB  
FROM sys.tables t  
JOIN sys.indexes i ON t.object_id = i.object_id  
JOIN sys.partitions p ON i.object_id = p.object_id AND i.index_id = p.index_id  
JOIN sys.allocation_units a ON p.partition_id = a.container_id  
GROUP BY t.name  
ORDER BY TotalSpaceMB DESC;
```

---

# 🧠 Practical Guidance

- Use **#2 or #3** for dashboards / automation
- Use **#5 (`sp_spaceused`)** when someone says *"why is this DB so big?"*
- Use **#6** when you're hunting **top space consumers**

---

# ⚡ Pro Tip (Enterprise Reality)

If you're working in a real environment (like yours):

- Log files are often the hidden culprit
- Check for:
    - runaway transactions
    - missing log backups
    - FULL recovery model issues

---

If you want, I can wrap this into:

- a **PowerShell script**
- or a **stored procedure for your toolbox**
- or even a **dashboard-style query with percentages + growth trends**

Just tell me 👍
