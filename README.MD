# ms sql
Launched in 3 configurations (Run1, Run2, Run3).\
Configurations are located in:
- [MsSqlFactory/SqlServerDatabaseFixture.cs#L15-L21](./MsSqlFactory/SqlServerDatabaseFixture.cs#L15-L21) (ms sql docker container config).
- [MsSqlTest/Program.cs#L209-L210](./MsSqlTest/Program.cs#L209-L210) (reordering to ms sql friendly).
- [MsSqlTest/Program.cs#L235-L236](./MsSqlTest/Program.cs#L235-L236) (obsolete reordering for .net guid internal layout).

MsSqlTest.Program has configuration about NumberOfRuns, etc.

The MsSqlTest project automatically launches and uses Docker container.\
Run1 and Run2 produces bad results. Run3 is good. \
To test how GUIDs behave in the DBMS, run MsSqlTest and then MsSqlAnalyze. \
To unit test solutions about GUIDs, run MsSqlGuidV7SwapperTest with Run3 only configuration.\
MsSqlTest and MsSqlFactory have .txt files with results of running on my machine.