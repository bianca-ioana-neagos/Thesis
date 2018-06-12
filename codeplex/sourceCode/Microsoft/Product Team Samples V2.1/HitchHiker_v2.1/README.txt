========================================
HitchHiker Sample
========================================

Last change: 6/1/2012

The samples in this VS solution are intended to be used along with the
HitchHiker's Guide white paper. The query examples progress in a lock step
fashion with the discussions in the paper.

This solution contains a console application HelloToll.
HelloToll contains HelloToll.cs - which demonstrates an exhaustive set of
StreamInsight query features. When you run the program, it offers you a menu
from which you can pick a number for the query you wish to run, and then runs the
query corresponding to your choice. Behind the covers, each of these maps to
a method that binds the relevant LINQ query logic to the input and output,
and runs the query. These queries are explained in the HitchHiker V2.1 white paper,
which can be downloaded at: http://go.microsoft.com/fwlink/?LinkId=256236