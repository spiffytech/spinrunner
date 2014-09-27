SpinRunner
==========

SpinRunner is a simple assistant for running SpinRite through VirtualBox. Designed to operate on a live CD, SpinRunner handles the messy parts of using SpinRite with VirtualBox by creating the raw virtual disks automatically. The user should be able to just boot the live CD, tell SpinRunner what drive has their copy of SpinRite, and see SpinRite come up inside VirtualBox.

The point of all of this is to run SpinRite on a broader range of hard drives and motherboards than SpinRite 6.0 supports, by capitalizing on Linux' broader driver ecosystem.


Building
--------

Install Mono and F#, then run `./fakebuild.sh`.

You'll also need to nuget install the FAKE build tool - eventually I may get around to making that easy. In the meantime, find a copy of nuget.exe and run

`mono --runtime=v4.0 $NW/nuget.exe install fake -OutputDirectory tools -ExcludeVersion`
