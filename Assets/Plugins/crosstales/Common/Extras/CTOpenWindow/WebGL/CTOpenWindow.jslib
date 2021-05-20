var CTOpenWindowPlugin = {
    ctOpenWindow: function(link)
    {
    	var url = Pointer_stringify(link);
        //document.onmouseup = function()
        //{
        	window.open(url);
        //	document.onmouseup = null;
        //}
    }
};

mergeInto(LibraryManager.library, CTOpenWindowPlugin);

// © 2021 crosstales LLC (https://www.crosstales.com)