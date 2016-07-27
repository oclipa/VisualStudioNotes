This project creates a Visual Studio 2012 add-in, primarily as a excuse to mess around with some WPF.  The add-in adds a new "Notes" window that simply displays (and allows editing of) a local text file.

To build and run:
1) Fetch the solution locally
2) Build in VS2012

After building, run the following command to install the add-in (the vsix file can be found in the bin folder):

"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\VSIXInstaller.exe" NotesWindow.vsix

