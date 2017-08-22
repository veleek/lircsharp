# LIRC#

A simple client library to allow a .NET application to interact with an LIRC server to control or be controlled by IR devices.

It has been tested to work with [WinLIRC](http://winlirc.sourceforge.net/) but there shouldn't be anything that will prevent it from working with [LIRC](http://lirc.org/).

The library is designed to be extensible so that a client can easily implement a different network protocol for sending and recieving messages without having to deal with the details of the message format.

Supported Platforms:
* WP7
* .NET 4.0

## LIRC Remote
LIRC Remote is a simple Windows Phone application that utilizes LIRC# to provide a simple universal remote interface from your WP device.

In addition to being able to query all the available remotes and commands, there is also the option of creating a custom remote panel using basically anything that's available in a regular XAML layout and the attach one or more remote commands to it.  The app loads this XAML file from any url on the internet.  Take a look at the `Remote.xaml` file included in the WPLirc project in the source section for some more details.
