FFIList
=================
This is a sample app to test the FFImageLoading's behavior with list of images. This version implements sample for WindowsPhone platform only.
The purpose is to have a clear and simple environmetn to test the FFImageLoading both through source code and binary from NuGet. 
The project uses mvvmCross framework and has he following structure:

 1 **FFImageLoading** [FOLDER] (*this folder contains the source code of https://github.com/luberda-molinet/FFImageLoading commit 5e23eb7 (v.2.2.8)*)
	 - FFImageLoading
	 - FFImageLoading.Windows
	 - FFImageLoading.Shared
 2 FFIList.Core (*base code, platform independet*)
 3 FFIList.WindowsPhone (*WP81 client linked to NuGet 2.2.8 release of FFImageLoading*)
 4 FFIList.WindowsPhone.FFSource (*WP81 client linked to FFImageLoading local folder*)
 5 FFIList.WindowsPhone.Shared (*Common code shared by FFIList.WindowsPhone and FFIList.WindowsPhone.FFSource*)
 
 To test this project you have just to choose the active project between  
