[![Official repository by buildingSMART International](https://img.shields.io/badge/buildingSMART-Official%20Repository-orange.svg)](https://www.buildingsmart.org)
[![This repo is managed by the TechnicalRoom](https://img.shields.io/badge/buildingSMART-TechnicalRoom-blue.svg)](https://www.buildingsmart.org/standards/rooms-and-groups/technical-room)

IFC Documentation and Toolkit
=============================

This repository contains the IFC schema, model view definitions, documentation, diagrams, examples, and tools for working with IFC data. 

The IFC schema is published in multiple programming languages. Historically, the "base" schema has been defined using the EXPRESS data definition language (along with EXPRESS-G graphical language), mappings to XML serialization have been defined using configuration files, extensions for custom properties have been defined using "psdXML" files, and extensions for model views have been defined using "mvdXML" files.

C# may now be used for capturing all of this information cohesively in a manner that can be easily viewed, compared, edited, and integrated with GitHub and common programming tools.

Along with capturing IFC definitions, libraries are also provided for reading and writing IFC data in various encoding formats.

Finally, tools are provided for converting and validating IFC data, publishing and consuming definitions with the buildingSmart Data Dictionary, and for editing IFC schemas and model view definitions.

The "IfcKit" folder contains the IFC schema and components. The root folder contains source code for a Windows utility ("IfcDoc") that may be used to edit this content and produce documentation in various formats. While IfcDoc can edit all content within, now with all content exposed as C# and HTML files, any tool can be used to make edits to these files as well, such as Eclipse or Visual Studio.
