========================================
Checkpointing Sample
========================================

Last change: 5/23/2012

This sample showcases checkpointing using the new programming model.
It processes data from a resilient csv file reader and streams its
results to a resilient csv file writer. It takes checkpoints periodically;
when the application is interrupted (e.g., by closing the console window),
it skips the data it already processed when restarted.

For more information about checkpointing, please refer to:
http://msdn.microsoft.com/en-us/library/hh290501.aspx
http://blogs.msdn.com/b/meek/archive/2012/04/09/streaminsight-checkpoints-what-how-and-why.aspx

This sample demonstrates the following aspects:
  * Creating a checkpointable process using the new programming model.
  * Using resilient artifacts using the new programming model.
  * Recovering from a failure and ignoring already processed events using the checkpointing data.
  
