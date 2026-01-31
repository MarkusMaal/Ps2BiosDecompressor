# Ps2BiosDecompressor

This little application allows you to extract all files from the PlayStation 2 BIOS ROM. This requires you to first dump the BIOS from a physical console using either homebrew or some other way of dumping the ROM.

The decompression algorithm was provided by balika011: https://gist.github.com/balika011/7a2443011c3a79ea53e0b98edb905a86

I adapted the decompressor to C#, generalized the code a bit so that you can also use it on the DVD ROM files and made it a bit more user friendly.
