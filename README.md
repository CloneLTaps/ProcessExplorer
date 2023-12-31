# Process Explorer
Process Explorer is Windows based PE editor made with C# that uses Windows Forms for the GUI. The primary purpose of Process Explorer is to view exe and dll's structure. 
However, it also displays the bytes of any file and allows users to edit the bytes directly along with giving support to replace strings with your own strings. After you finish
making your changes you can then save it as a new file. All in all, its minimalist easy to use GUI along with its darker color tones makes Process Explorer a great choice for 
simple file inspection or large edits.

![image](https://github.com/CloneLTaps/ProcessExplorer/assets/83735831/0b4347fc-a8d4-4881-b4cd-4e4f901e14fd)

# How it Works
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

# PE's Explained
PE stands for 'Portable Executable' and is a file format designed for Windows that works with both exe's 'executables' and dll's 'dynamic link libraries'. In fact, when analyzing the headers of these
two, their structures will appear very similar. The main way of telling the difference is by going to the PE's general pe header and then navigating to the characteristics section. Clicking on this you can scroll 
down and look to see if the 'IMAGE_FILE_DLL' option is selected.

When viewing the headers of a PE the first two things you will notice are the dos header and the dos stub. These are both legacy headers designed for the MS dos days with the only real useful info being at the bottom
of the dos header. Particularly the 'e_lfanew' field which represents the file offset in hex to the start of the PE Header. Also please keep in mind PE's will be stored in little-endian meaning the least significant
bytes will be stored first. However, we need to read this data in big-endian to do this first look at the size of the field which in our case is 4 bytes. Next flip the order of each byte. For example, 08 10 00 00
would turn into 00 00 10 08. Once cleaned up we can see it is actually 0x108 which defines the file offset to the start of our PE Header. The rest of the bytes to that will be the dos header which is not used in todays
world. Sometimes you can also find Rich Headers between the Dos Header and the PE Header, but this is often just treated as an extension to the Dos Header since its nonstandard.

## PE Headers
The start of the PE Header always start with PE followed up by 2 null termianting bytes. The hex takes the appearnce of 50 45 00 00. Please keep in mind ASCII characters are read in little-endian form which is unique in the 
sense that most fields are read in big-endian. Next you will notice the Machine field which just contains the target machines arcitexture. After that you will notice the NumberOfSections field which contains the total amount 
of sections located inside this PE. You will then notice a few more fields most notablly being the SizeOfOptionalHeader field and the Characteristics field which was mentioned earlier. When SizeOfOptionalHeader has a size 
greater than 0 we know that optional headers are present along with their size.

## Optional PE Headers
Even though the word "option" is used here you will find in the vast majority of all PE cases these will be present. Additionally Optional PE Headers are often broken down into 3 types. Firstly, the main Optional PE Header 
which contains 8 fields. Starting off we have the Magic field where 20B means we have a 64-bit header while 10B means we have a 32 bit header (this will be relevant in the next header). Going down a bit the next key field 
is the SizeOfCode field which determines the size of the '.text' section. In general, the '.text' section is what holds all of the PE's executable code. Next up is the SizeOfInitializedData field which represents
the size of initilized data inside the '.data' section. In General, the '.data' section us used to contain nonvolatile mutable data such as global and static variables. After that the SizeOfUninitializedData field 
contains the uninitlized data inside the '.data' section. Next up is the AddressOfEntryPoint field which represents the RVA (relative virtual address). This often points to the virtual memory address that contains
our main function is something like C/C++. Lastly, we have the BaseOfCode field which is the RVA to the start of the '.text' section. Also, in case you are curious these are often relative to the Image Base.

## Optional PE Headers 64 / 32
Based on the Magic field from the previous header this will either be formatted in 64 or 32 bit. The primary difference between these is that 32-bit has one extra field at the start BaseOfData which is just the RVA
to the start of the '.data' section and secondly most of the 8-byte fields located in the 64-bit version are instead in 4 bytes in the 32 bit version. The next field in the 32-bit header / first field in the 64-bit header 
is ImageBase which was briefly mentioned above and is used when determining the base address. Next field is SectionAlignment which dictates at what multiple of addresses sections should be loaded into memory at. 
Padding will be added to ensure this size is reached. FileAlignment is very similar with the only difference being it sets the file alignment size instead of memory alignment size. The next important field is SizeOfImage
which is always a multiple of SectionAlignment and defines the files complete size in memory. SizeOfHeaders on the other hand defines the size of all PE Headers and Section Headers and is always a multiple of FileAlignment.
The next field is CheckSum which was created using an algorithm that reads through your entire PE. Then when the PE is executed Windows can recalculate that CheckSum and verify if it matches the one orignally computed. This failing
can be caused by data corruption or by malicious actors but it’s important to keep in mind that there’s more security features used in combination such as code signing. The following fields like Subsystem and DllCharacteristics 
provide more details but one thing I would like to point out in the DllCharacteristics is 0x40 Image DLL Characteristics Dynamic Base. This means the program would use ASLR if its turned on inside your Windows Security manager.
This means that the virtual base address will be randomly selected each time the program has been started making it harder for bad actors to predict where certain data will be stored in memory. After that there will be some 
fields that determine stack and heap size.

## Optional PE Header Data Directories and Section Headers
This header contains RVA to many different sections along with the size of that section in memory. However, most of these fields at least the section specific fields can be found in the individual section headers. Section
headers begin after Optional PE Header Data Directories are usally 40 - 60 bytes long. Section headers contain several fields firstly being Name which just contains the name of section. Next is the Virtual Size field which
denotes the size in memory. Then comes the Virtual Address field which is just the RVA to the st art of the section in memory. After that comes the Size of Raw Data field which denotes the size of the section on disk. Next
is the Pointer to Raw Data field which is the file offset to the start of the section. Then comes Pointer to Relocations field which is used when that sections requires hard coded address paths. Then when something like ASLR
is used relocations will be pivotal in recreating those correct addreses. Then the section header contains a few more fields for debugging info and for relocations along with a Characterisitc field. 


# Overview of PE's and Virtual Memory






