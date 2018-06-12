========================================
UDSO-Based PatternDetector Sample
======================================== 

Last change: 5/23/2012


This sample uses the StreamInsight UDSO framework to implement pattern detection.
The UDSO itself is generic, capable of performing pattern detection over any 
automaton specified by the query writer in the form of an augmented finite 
automaton (AFA). An AFA is basically a non-deterministic finite automaton (NFA) 
with additional information, called a register, that can be created and maintained 
as part of the automaton during runtime. The UDSO is located in the project 
UdsoAugmentedFiniteAutomaton.

The project UdsoPatternDetector is an example application that uses the above 
UDSO to perform pattern detection over randomly generated point event stream of 
stock quotes. The application defines an AFA that detects a sequence of consecutive 
downticks (price drops) followed by an equal number of consecutive upticks (price 
increases). We detect and report the pattern if, for any stock symbol, the above 
pattern occurs within a timespan of 10 seconds. The corresponding AFA is shown in 
the file AFAexample.pptx, and is implemented in AfaEqualDownticksUpticks.cs.

When the application is executed, the above AFA is detected over the generated
stream of stock quotes. An output event is produced for every detected occurrence
of the pattern. In the output event stream, the field named Counter indicates the 
difference between the number of downticks and the number of upticks at that 
instant - this should always be zero as we are detecting a pattern with an equal 
number of downticks and upticks.

In this example, we use:

- Attribute reflection to read/write data from text files. 
This sample shows how to develop a generic base class structure to import any types
of csv files. It provides enough flexibility even if the file structure change
drastically, and it is very easy to maintain and incorporate changes with minimal
effort. For example, users can add a new field, delete existing field, or switch
two fields in the input data file easily by update Payload class.
- IEnumerable, the pull-based model is used in the SimpleTextFileReader, 
while IObservable/IObserver, the push-based model is used in SimpleTextFileWriter 
to show the possible ways to use StreamInsight v2.1. 


For more information on AFA, see:

Badrish Chandramouli, Jonathan Goldstein, and David Maier. High-Performance 
Dynamic Pattern Matching over Disordered Streams. In VLDB 2010.


