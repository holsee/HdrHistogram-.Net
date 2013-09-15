.NET Port of https://github.com/giltene/HdrHistogram by Steven Holdsworth

I notice that the Java version of the disruptor (https://github.com/LMAX-Exchange/disruptor) is using the HdrHistogram for performance metrics. 
I hope to provide the same for the .NET port of the Disruptor (https://github.com/disruptor-net/Disruptor-net).

TODO
----

In the process of performing a rapid manual port of the code to C#.

Once I have completed this, there will be a few areas to address, namely:
- Where stubs have been created for Java libraries which have been used, find .NET equivalent.
- Idiomatic C# *& .NET
- Examine the micro optimizations for JVM and try to achieve the same for the CLR.

Acknowlegements
---------------

Original Java Version (CC Public Domain): Gil Tene https://github.com/giltene/HdrHistogram
C# AtomicLong Implementation (Apache v2): Matt Bolt https://github.com/mbolt35/CSharp.Atomic/blob/master/CSharp/Atomic/AtomicLong.cs