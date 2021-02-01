# ZipLog

zip some log files

-------------------------------------------------
Must pass at least 1 argument.

Usage: SaqZipLog {options} "<pathToArchive>", <daysToKeep>, <extension>, <zipName>

Options:
	-d                      zip filename daily. ex: D201904
	-w                      zip filename weekly. ex: W201945
	-m                      zip filename monthly. ex: M201911

other parameters, if not passed.
	<pathToArchive>         CurrentPath
	<daysToKeep>            30 daysToKeep
	<extension>             .log extension
	<zipName>               Name of zip file to produce

Exemple of a batch file for the scheduler: 
	@Echo off
	ZipLog -m "D:\webtrends\weblog", 30, log, someZip


-------------------------------------------------
After build merge ICSharpCode.SharpZipLib.dll with Ziplog.exe, so you`ll need only one file.

ILMerge.exe ICSharpCode.SharpZipLib.dll Ziplog.exe /out:zipLogger.exe
