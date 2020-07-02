# dacs7

Data access S7 is a library to connect to S7 plcs for reading and writing data.

# NuGet
    PM>  Install-Package Dacs7

# Description


Dacs7 is used to connect to a SIEMENS Plc by using the RFC1006 protocol to perform operations.


# Compatibility

|             | 300 | 400 | WinAC | 1200 | 1500 |
|:------------|:---:|:---:|:-----:|:----:|:----:|
|DB Read/Write|  X  |  X  |   X   |   X  |   X  |
|EB Read/Write|  X  |  X  |   X   |   X  |   X  |
|AB Read/Write|  X  |  X  |   X   |   X  |   X  |
|MB Read/Write|  X  |  X  |   X   |   X  |   X  |
|TM Read/Write|  X  |  X  |   X   |      |      |
|CT Read/Write|  X  |  X  |   X   |      |      |

## Additional TIA Settings (1200 and 1500 CPUs)

### DB Properties

Select the DB in the left pane under 'Program blocks' and click 'Properties' in the context menu.

<image src="/images/BlockSettings.PNG" class="inline"/>

### FullAccess

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security'.

<image src="/images/FullAccess.PNG" class="inline"/>

### Connection mechanisms

Select the CPU in the left pane and click 'Properties' in the context menu and go to 'Protection & Security/Connection mechanisms'.

<image src="/images/Connectionmechanism.PNG" class="inline"/>

