# Process Explorer
Process Explorer is Windows based PE editor made with C# that uses Windows Forms for the GUI. The primary purpose of Process Explorer is to view exe and dll's structure. 
However, it also displays the bytes of any file and allows users to edit the bytes directly along with giving support to replace strings with your own strings. After you finish
making your changes you can then save it as a new file. All in all, its minimalist easy to use GUI along with its darker color tones makes Process Explorer a great choice for 
simple file inspection or large edits.

![image](https://github.com/CloneLTaps/ProcessExplorer/assets/83735831/0b4347fc-a8d4-4881-b4cd-4e4f901e14fd)

# How it works
Firstly, click file then open. After you have opened your file, you will be presented with a tree view of your file’s structure on the left side of the screen. The resulting tree
view depends on the file you opened. Either way essentially all files should have the core page which will be labeled the same as the file you opened. If the file is an exe or dll
you will be able to expand the tree to view its structure. Some structures such as the PE Header contains clickable boxes such as the Machine, TimeDateStamp, and Characteristics
sections. Clicking these descriptions will open up a dialog box with more info pertaining to the data you are viewing.

Anyways when opened the center of the screen will display 3 columns the offsets will be the first, data will be in the second and the third will be the ASCII translations of the data
or the descriptions for headers. A tool bar will also be present at the top of the screen which allows you to toggle between displaying data in hex, decimal, and binary. The tool bar 
also allows you to switch between file offsets and relative offsets where relative offsets are just the offset from the start of the section you are viewing. The next thing you notice is 
the Byte and Double Byte buttons. By default, all data will be displayed in single byte little-endian form. If you instead select "BB" data will be displayed in double byte big-endian pairs.
Data inside headers will also be completely changed to big-endian when this is selected. The last thing you will notice
in the tool bar is the text replacing utilities. First type in the string you want to replace in the first box and the string you want to replace it with in the second box. Checking 
"Replace All" means it will then replace every instance of those strings in the entire file. If you instead only want to replace data in one section, simply navigate to that section via
the tree view and then uncheck "Replace All" before finally replacing the data by clicking the "Replace" button.
 
You can also directly modify data by clicking on a data row and changing the data manually. I highly suggest you keep data the same length when modifying it. This advice goes for replacing 
text as well. However, if you know what you are doing you should be to change the lengths of data by both the replace button and by directly typing it just keep in mind this may lead to unintended
consequences. The only other thing left to understand is the Settings button. This contains options such as "Remove extra zeros" which removes extra useless zeros in double byte mode. You will also
notice a "Display offsets in hex" option which when checked just means offsets will always be shown in hex no matter if you switch to decimal or binary. Next you will see the "Return to top" option
which just means you will be returned to the top of the section when switching between different sections. Lastly, you will see the "Treat null as '.'" option. This is used with the replace button 
and allows you to type a '.' in when you are trying to replace a null character. This is useful because you often find ASCII characters separated by 00.
