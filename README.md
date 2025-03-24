# ASWTools

A collection of tools for modding modern Arc System Works fighters. 

Currently, it contains a tool for working with BBScript (the battle mode scripts) and a tool for working with ADVScript (the story mode scripts). 

The reason I made a new tool for BBScript was partially to challenge myself, and partially to write a tool that more closely follows Arc System Works' own conventions. As a result, there is a larger deviation in syntax from existing tools (such as @super-continent 's wonderful BBScript repo, or @dantarion 's venerable bbtools repo). 

## BBScript

To decompile scripts, use the command `bbscript decompile -c <config_file> -i <input_file> -o <output_file>`. For example, if you were to decompile GGST's BBS_CMNEF, use `bbscript decompile -c ggst.json -i BBS_CMNEF.bbsbin -o BBS_CMNEF.bbs`.

To compile scripts, use the command `bbscript compile -c <config_file> -i <input_file> -o <output_file>`. For example, if you were to compile GGST's BBS_CMNEF, use `bbscript decompile -c ggst.json -i BBS_CMNEF.bbs -o BBS_CMNEF.bbsbin`.

Updated configurations may be found [here](https://github.com/WistfulHopes/ASWTools/tree/master/BBScript/Config).
